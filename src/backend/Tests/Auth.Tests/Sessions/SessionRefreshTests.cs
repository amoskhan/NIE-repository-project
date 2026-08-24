using Auth.Tests.TestDoubles;
using Microsoft.AspNetCore.Mvc;

namespace Auth.Tests.Sessions;

/// <summary>
/// NIE-AUTHN-002 / NIE-AUTHN-003: refreshing must rotate the opaque token and the superseded token
/// must stop working immediately.
/// </summary>
public sealed class SessionRefreshTests
{
    private const string OldToken = "old-session-token";
    private const string RotatedToken = "rotated-session-token";

    [Fact]
    public async Task Refreshing_rotates_the_session_token_and_revokes_the_previous_one()
    {
        var harness = Seeded();

        var ok = Assert.IsType<OkObjectResult>(await harness.Controller.Refresh(OldToken));

        Assert.Equal(RotatedToken, ok.Value);
        Assert.False(harness.HasSession(OldToken));
        Assert.True(harness.HasSession(RotatedToken));
    }

    [Fact]
    public async Task The_superseded_token_is_rejected_by_verification_after_a_refresh()
    {
        var harness = Seeded();

        await harness.Controller.Refresh(OldToken);

        harness.UseSessionHeader(OldToken);
        Assert.IsType<UnauthorizedObjectResult>(await harness.Controller.Verify());
    }

    [Fact]
    public async Task The_rotated_token_carries_the_same_identity_forward()
    {
        var harness = Seeded();

        await harness.Controller.Refresh(OldToken);

        var rotated = harness.ReadSession(RotatedToken);
        Assert.NotNull(rotated);
        Assert.Equal("devia", rotated.UserId);
        Assert.Equal("Dev IA", rotated.Name);
        Assert.Equal("dev.ia@nie.edu.sg", rotated.Email);
    }

    [Fact]
    public async Task Refreshing_stamps_a_new_last_active_time_on_the_rotated_session()
    {
        var harness = Seeded();
        var before = DateTime.Now.AddSeconds(-5);

        await harness.Controller.Refresh(OldToken);

        var rotated = harness.ReadSession(RotatedToken)!;
        Assert.InRange(rotated.LastActive, before, DateTime.Now.AddSeconds(5));
    }

    [Fact]
    public async Task Refreshing_applies_the_configured_lifetime_to_the_rotated_session()
    {
        var harness = Seeded(sessionMinutes: 20);

        await harness.Controller.Refresh(OldToken);

        var options = harness.Cache.OptionsFor(AuthControllerHarness.SessionKeyPrefix + RotatedToken);
        Assert.NotNull(options);
        Assert.Equal(TimeSpan.FromMinutes(20), options.AbsoluteExpirationRelativeToNow);
    }

    [Fact]
    public async Task Refreshing_a_session_the_identity_provider_rejects_leaves_the_session_untouched()
    {
        var harness = Seeded(authenticated: false);

        var result = await harness.Controller.Refresh(OldToken);

        Assert.IsType<UnauthorizedResult>(result);
        Assert.True(harness.HasSession(OldToken));
        Assert.Empty(harness.Cache.RemovedKeys);
    }

    [Fact]
    public async Task Refreshing_with_an_unreadable_identity_provider_response_is_refused()
    {
        var harness = new AuthControllerHarness(identityProvider: IdentityProviderResponses.Raw("{}"));
        harness.SeedSession(OldToken, AuthControllerHarness.Session());

        Assert.IsType<UnauthorizedResult>(await harness.Controller.Refresh(OldToken));
        Assert.True(harness.HasSession(OldToken));
    }

    [Fact]
    public async Task Refreshing_an_expired_session_is_refused_after_the_stale_key_is_dropped()
    {
        var harness = new AuthControllerHarness(identityProvider: IdentityProviderResponses.Refresh(true));

        var denied = Assert.IsType<UnauthorizedObjectResult>(await harness.Controller.Refresh(OldToken));

        Assert.Equal("Session not found or expired.", denied.Value);
        Assert.Equal([AuthControllerHarness.SessionKeyPrefix + OldToken], harness.Cache.RemovedKeys);
        Assert.False(harness.HasSession(RotatedToken));
    }

    [Fact]
    public async Task Refreshing_a_session_whose_payload_is_json_null_is_refused()
    {
        var harness = new AuthControllerHarness(identityProvider: IdentityProviderResponses.Refresh(true));
        harness.SeedRawSession(OldToken, "null");

        var denied = Assert.IsType<UnauthorizedObjectResult>(await harness.Controller.Refresh(OldToken));

        Assert.Equal("Invalid session data.", denied.Value);
        Assert.Empty(harness.Cache.Keys);
    }

    [Fact]
    public async Task Refreshing_presents_the_subscription_key_and_the_current_token_upstream()
    {
        var harness = Seeded();

        await harness.Controller.Refresh(OldToken);

        var request = Assert.Single(harness.IdentityProvider.Requests);
        Assert.Equal(AuthControllerHarness.IdentityProviderBaseUrl + "/RefreshSession", request.Url);
        Assert.Equal(AuthControllerHarness.SubscriptionKey, request.Headers["x-nie-aws-api-gw-key"]);
        Assert.Equal(OldToken, request.Headers["sessiontoken"]);
    }

    [Fact]
    public async Task Refreshing_never_writes_a_session_token_to_the_log()
    {
        var harness = Seeded();

        await harness.Controller.Refresh(OldToken);

        Assert.DoesNotContain(OldToken, harness.Logger.AllLoggedText, StringComparison.Ordinal);
        Assert.DoesNotContain(RotatedToken, harness.Logger.AllLoggedText, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Refreshing_currently_emits_no_audit_record_at_all()
    {
        var harness = Seeded();

        await harness.Controller.Refresh(OldToken);

        // NIE-AUTHN-005 requires refresh and expiry to be audited; nothing is recorded today.
        Assert.Empty(harness.Logger.Entries);
    }

    [Fact]
    public async Task An_expired_refresh_is_not_audited_either()
    {
        var harness = new AuthControllerHarness(identityProvider: IdentityProviderResponses.Refresh(true));

        await harness.Controller.Refresh(OldToken);

        Assert.Empty(harness.Logger.Entries);
    }

    private static AuthControllerHarness Seeded(bool authenticated = true, int sessionMinutes = 30)
    {
        var harness = new AuthControllerHarness(
            identityProvider: IdentityProviderResponses.Refresh(authenticated, RotatedToken),
            sessionMinutes: sessionMinutes);
        harness.SeedSession(OldToken, AuthControllerHarness.Session());
        return harness;
    }
}

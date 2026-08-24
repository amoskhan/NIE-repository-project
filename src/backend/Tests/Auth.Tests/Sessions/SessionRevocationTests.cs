using Auth.Tests.TestDoubles;
using Microsoft.AspNetCore.Mvc;

namespace Auth.Tests.Sessions;

/// <summary>
/// NIE-AUTHN-003: every logout path must revoke the server-side session, and the revoked token must
/// stop being accepted afterwards.
/// </summary>
public sealed class SessionRevocationTests
{
    private const string LiveToken = "live-session-token";

    [Fact]
    public async Task Logout_revokes_the_session_named_in_the_request_body()
    {
        var harness = Seeded();

        var result = await harness.Controller.Logout(LiveToken);

        AssertLogoutSucceeded(result);
        Assert.False(harness.HasSession(LiveToken));
        Assert.Equal([AuthControllerHarness.SessionKeyPrefix + LiveToken], harness.Cache.RemovedKeys);
    }

    [Fact]
    public async Task Logout_revokes_the_session_supplied_through_the_session_header()
    {
        var harness = Seeded();
        harness.UseSessionHeader(LiveToken);

        await harness.Controller.Logout(sessionToken: null);

        Assert.False(harness.HasSession(LiveToken));
    }

    [Fact]
    public async Task Logout_revokes_the_session_supplied_through_the_query_string()
    {
        var harness = Seeded();
        harness.UseSessionQuery(LiveToken);

        await harness.Controller.Logout(sessionToken: null);

        Assert.False(harness.HasSession(LiveToken));
    }

    [Theory]
    [InlineData("Application-SessionToken")]
    [InlineData("SessionToken")]
    [InlineData("SessionId")]
    public async Task Logout_revokes_the_session_carried_by_any_recognised_cookie(string cookieName)
    {
        var harness = Seeded();
        harness.UseCookies((cookieName, LiveToken));

        await harness.Controller.Logout(sessionToken: null);

        Assert.False(harness.HasSession(LiveToken));
    }

    [Fact]
    public async Task Logout_ignores_cookies_it_does_not_recognise()
    {
        var harness = Seeded();
        harness.UseCookies(("X-Legacy-Session", LiveToken));

        await harness.Controller.Logout(sessionToken: null);

        Assert.True(harness.HasSession(LiveToken));
        Assert.Empty(harness.Cache.RemovedKeys);
    }

    [Fact]
    public async Task Logout_prefers_the_request_body_over_every_other_transport()
    {
        var harness = Seeded();
        harness.SeedSession("header-token", AuthControllerHarness.Session());
        harness.SeedSession("query-token", AuthControllerHarness.Session());
        harness.SeedSession("cookie-token", AuthControllerHarness.Session());
        harness.UseSessionHeader("header-token");
        harness.UseSessionQuery("query-token");
        harness.UseCookies(("Application-SessionToken", "cookie-token"));

        await harness.Controller.Logout(LiveToken);

        Assert.False(harness.HasSession(LiveToken));
        Assert.True(harness.HasSession("header-token"));
        Assert.True(harness.HasSession("query-token"));
        Assert.True(harness.HasSession("cookie-token"));
    }

    [Fact]
    public async Task Logout_prefers_the_session_header_over_the_query_string_and_cookies()
    {
        var harness = Seeded();
        harness.SeedSession("query-token", AuthControllerHarness.Session());
        harness.UseSessionHeader(LiveToken);
        harness.UseSessionQuery("query-token");
        harness.UseCookies(("SessionToken", "cookie-token"));

        await harness.Controller.Logout(sessionToken: null);

        Assert.False(harness.HasSession(LiveToken));
        Assert.True(harness.HasSession("query-token"));
    }

    [Fact]
    public async Task Logout_prefers_the_query_string_over_cookies()
    {
        var harness = Seeded();
        harness.SeedSession("cookie-token", AuthControllerHarness.Session());
        harness.UseSessionQuery(LiveToken);
        harness.UseCookies(("SessionToken", "cookie-token"));

        await harness.Controller.Logout(sessionToken: null);

        Assert.False(harness.HasSession(LiveToken));
        Assert.True(harness.HasSession("cookie-token"));
    }

    [Fact]
    public async Task Logout_prefers_the_application_cookie_over_the_legacy_cookie_names()
    {
        var harness = Seeded();
        harness.SeedSession("legacy-token", AuthControllerHarness.Session());
        harness.UseCookies(
            ("Application-SessionToken", LiveToken),
            ("SessionToken", "legacy-token"),
            ("SessionId", "legacy-token"));

        await harness.Controller.Logout(sessionToken: null);

        Assert.False(harness.HasSession(LiveToken));
        Assert.True(harness.HasSession("legacy-token"));
    }

    [Fact]
    public async Task Logout_skips_blank_cookies_and_falls_through_to_the_next_recognised_name()
    {
        var harness = Seeded();
        harness.UseCookies(
            ("Application-SessionToken", string.Empty),
            ("SessionToken", LiveToken));

        await harness.Controller.Logout(sessionToken: null);

        Assert.False(harness.HasSession(LiveToken));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task A_blank_body_token_falls_back_to_the_transport_supplied_token(string? bodyToken)
    {
        var harness = Seeded();
        harness.UseSessionHeader(LiveToken);

        await harness.Controller.Logout(bodyToken);

        Assert.False(harness.HasSession(LiveToken));
    }

    [Fact]
    public async Task Logout_without_any_session_identifier_succeeds_without_touching_the_cache()
    {
        var harness = Seeded();

        var result = await harness.Controller.Logout(sessionToken: null);

        AssertLogoutSucceeded(result);
        Assert.Empty(harness.Cache.RemovedKeys);
        Assert.True(harness.HasSession(LiveToken));
    }

    [Fact]
    public async Task A_revoked_token_is_rejected_by_subsequent_verification()
    {
        var harness = Seeded();
        harness.UseSessionHeader(LiveToken);

        Assert.IsType<OkObjectResult>(await harness.Controller.Verify());

        await harness.Controller.Logout(LiveToken);

        Assert.IsType<UnauthorizedObjectResult>(await harness.Controller.Verify());
    }

    [Fact]
    public async Task A_revoked_token_is_rejected_by_subsequent_profile_lookups()
    {
        var harness = Seeded();

        Assert.IsType<OkObjectResult>(await harness.Controller.GetProfile(LiveToken));

        await harness.Controller.Logout(LiveToken);

        var denied = Assert.IsType<UnauthorizedObjectResult>(await harness.Controller.GetProfile(LiveToken));
        Assert.Equal("Session not found or expired.", denied.Value);
    }

    [Fact]
    public async Task Logging_out_twice_is_idempotent()
    {
        var harness = Seeded();

        AssertLogoutSucceeded(await harness.Controller.Logout(LiveToken));
        AssertLogoutSucceeded(await harness.Controller.Logout(LiveToken));

        Assert.False(harness.HasSession(LiveToken));
    }

    [Fact]
    public async Task Logout_revokes_only_the_targeted_session()
    {
        var harness = Seeded();
        harness.SeedSession("other-user-token", AuthControllerHarness.Session("other"));

        await harness.Controller.Logout(LiveToken);

        Assert.False(harness.HasSession(LiveToken));
        Assert.True(harness.HasSession("other-user-token"));
    }

    [Fact]
    public async Task Logout_never_writes_the_session_token_to_the_log()
    {
        var harness = Seeded();

        await harness.Controller.Logout(LiveToken);

        Assert.DoesNotContain(LiveToken, harness.Logger.AllLoggedText, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Logout_currently_emits_no_audit_record_at_all()
    {
        var harness = Seeded();

        await harness.Controller.Logout(LiveToken);

        // NIE-AUTHN-005 requires logout and session revocation to be audited. Nothing is logged
        // today; this test fails the moment an audit entry is added, which is the intended fix.
        Assert.Empty(harness.Logger.Entries);
    }

    private static AuthControllerHarness Seeded()
    {
        var harness = new AuthControllerHarness();
        harness.SeedSession(LiveToken, AuthControllerHarness.Session());
        return harness;
    }

    private static void AssertLogoutSucceeded(IActionResult result)
    {
        var ok = Assert.IsType<OkObjectResult>(result);
        var values = ResponseValues.Of(ok.Value);
        Assert.True(Assert.IsType<bool>(values["success"]));
    }
}

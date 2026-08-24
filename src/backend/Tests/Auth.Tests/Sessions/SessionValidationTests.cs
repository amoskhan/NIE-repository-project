using Auth.Tests.TestDoubles;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using NSubstitute;

namespace Auth.Tests.Sessions;

/// <summary>
/// NIE-AUTHN-002: server-side session validation must be the only thing that makes a request
/// trusted, and an unknown or unreadable session must never be treated as valid.
/// </summary>
public sealed class SessionValidationTests
{
    private const string LiveToken = "live-session-token";

    [Fact]
    public async Task Verification_without_any_session_identifier_is_refused()
    {
        var harness = new AuthControllerHarness();

        var result = await harness.Controller.Verify();

        AssertInvalid(result);
    }

    [Fact]
    public async Task Verification_of_an_unknown_session_is_refused()
    {
        var harness = new AuthControllerHarness();
        harness.UseSessionHeader("never-issued");

        AssertInvalid(await harness.Controller.Verify());
    }

    [Fact]
    public async Task Verification_of_a_session_whose_payload_is_json_null_is_refused()
    {
        var harness = new AuthControllerHarness();
        harness.SeedRawSession(LiveToken, "null");
        harness.UseSessionHeader(LiveToken);

        AssertInvalid(await harness.Controller.Verify());
    }

    [Fact]
    public async Task Verification_of_a_live_session_returns_the_identity_without_the_token()
    {
        var harness = new AuthControllerHarness();
        harness.SeedSession(LiveToken, AuthControllerHarness.Session());
        harness.UseSessionHeader(LiveToken);

        var ok = Assert.IsType<OkObjectResult>(await harness.Controller.Verify());
        var values = ResponseValues.Of(ok.Value);

        Assert.True(Assert.IsType<bool>(values["isValid"]));
        Assert.Equal("devia", values["userId"]);
        Assert.Equal("Dev IA", values["userName"]);
        Assert.Equal(["isValid", "userId", "userName"], values.Keys.Order(StringComparer.Ordinal));
    }

    [Fact]
    public async Task Verification_reads_the_session_from_the_query_string()
    {
        var harness = new AuthControllerHarness();
        harness.SeedSession(LiveToken, AuthControllerHarness.Session());
        harness.UseSessionQuery(LiveToken);

        Assert.IsType<OkObjectResult>(await harness.Controller.Verify());
    }

    [Fact]
    public async Task Verification_reads_the_session_from_the_session_token_cookie()
    {
        var harness = new AuthControllerHarness();
        harness.SeedSession(LiveToken, AuthControllerHarness.Session());
        harness.UseCookies(("SessionToken", LiveToken));

        Assert.IsType<OkObjectResult>(await harness.Controller.Verify());
    }

    [Theory]
    [InlineData("Application-SessionToken")]
    [InlineData("SessionId")]
    public async Task Verification_ignores_the_cookie_names_that_logout_still_accepts(string cookieName)
    {
        var harness = new AuthControllerHarness();
        harness.SeedSession(LiveToken, AuthControllerHarness.Session());
        harness.UseCookies((cookieName, LiveToken));

        // Verify() only inspects the "SessionToken" cookie while Logout() also accepts
        // "Application-SessionToken" and "SessionId": the two paths disagree on the transport.
        AssertInvalid(await harness.Controller.Verify());
    }

    [Fact]
    public async Task Verification_prefers_the_session_header_over_the_query_string_and_cookie()
    {
        var harness = new AuthControllerHarness();
        harness.SeedSession(LiveToken, AuthControllerHarness.Session("header-user"));
        harness.SeedSession("query-token", AuthControllerHarness.Session("query-user"));
        harness.UseSessionHeader(LiveToken);
        harness.UseSessionQuery("query-token");
        harness.UseCookies(("SessionToken", "query-token"));

        var ok = Assert.IsType<OkObjectResult>(await harness.Controller.Verify());

        Assert.Equal("header-user", ResponseValues.Of(ok.Value)["userId"]);
    }

    [Fact]
    public async Task An_empty_session_header_short_circuits_verification_instead_of_falling_back()
    {
        var harness = new AuthControllerHarness();
        harness.SeedSession(LiveToken, AuthControllerHarness.Session());
        harness.UseSessionHeader(string.Empty);
        harness.UseCookies(("SessionToken", LiveToken));

        // The null-coalescing chain in Verify() treats a present-but-empty header as a supplied
        // value, so the valid cookie is never consulted.
        AssertInvalid(await harness.Controller.Verify());
    }

    [Fact]
    public async Task Profile_lookup_of_an_unknown_session_is_refused()
    {
        var harness = new AuthControllerHarness();

        var denied = Assert.IsType<UnauthorizedObjectResult>(await harness.Controller.GetProfile("never-issued"));

        Assert.Equal("Session not found or expired.", denied.Value);
    }

    [Fact]
    public async Task Profile_lookup_of_a_session_whose_payload_is_json_null_is_refused()
    {
        var harness = new AuthControllerHarness();
        harness.SeedRawSession(LiveToken, "null");

        var denied = Assert.IsType<UnauthorizedObjectResult>(await harness.Controller.GetProfile(LiveToken));

        Assert.Equal("Invalid session data.", denied.Value);
    }

    [Fact]
    public async Task Profile_lookup_returns_only_the_display_identity()
    {
        var harness = new AuthControllerHarness();
        harness.SeedSession(LiveToken, AuthControllerHarness.Session());

        var ok = Assert.IsType<OkObjectResult>(await harness.Controller.GetProfile(LiveToken));
        var values = ResponseValues.Of(ok.Value);

        Assert.Equal(["Department", "Email", "Name"], values.Keys.Order(StringComparer.Ordinal));
        Assert.Equal("Dev IA", values["Name"]);
        Assert.Equal("dev.ia@nie.edu.sg", values["Email"]);
    }

    [Fact]
    public async Task Session_reads_never_extend_the_stored_expiry()
    {
        var harness = new AuthControllerHarness();
        harness.SeedSession(LiveToken, AuthControllerHarness.Session());
        harness.UseSessionHeader(LiveToken);

        await harness.Controller.Verify();
        await harness.Controller.GetProfile(LiveToken);

        await harness.Cache.Cache.DidNotReceive().SetAsync(
            Arg.Any<string>(),
            Arg.Any<byte[]>(),
            Arg.Any<DistributedCacheEntryOptions>(),
            Arg.Any<CancellationToken>());
        Assert.Empty(harness.Cache.RemovedKeys);
    }

    [Fact]
    public async Task Post_verification_against_the_identity_provider_accepts_a_successful_result()
    {
        var harness = new AuthControllerHarness(identityProvider: IdentityProviderResponses.VerifyUser(true));

        Assert.IsType<OkResult>(await harness.Controller.Verify("devia", LiveToken));
    }

    [Fact]
    public async Task Post_verification_against_the_identity_provider_forbids_a_failed_result()
    {
        var harness = new AuthControllerHarness(identityProvider: IdentityProviderResponses.VerifyUser(false));

        Assert.IsType<ForbidResult>(await harness.Controller.Verify("devia", LiveToken));
    }

    [Fact]
    public async Task Post_verification_forbids_when_the_identity_provider_returns_no_body()
    {
        var harness = new AuthControllerHarness(identityProvider: IdentityProviderResponses.Raw("null"));

        Assert.IsType<ForbidResult>(await harness.Controller.Verify("devia", LiveToken));
    }

    [Fact]
    public async Task Post_verification_forwards_the_subscription_key_and_session_token_upstream()
    {
        var harness = new AuthControllerHarness(identityProvider: IdentityProviderResponses.VerifyUser(true));

        await harness.Controller.Verify("devia", LiveToken);

        var request = Assert.Single(harness.IdentityProvider.Requests);
        Assert.Equal(AuthControllerHarness.SubscriptionKey, request.Headers["x-nie-aws-api-gw-key"]);
        Assert.Equal("devia", request.Headers["UserId"]);
        Assert.Equal(LiveToken, request.Headers["sessionToken"]);
    }

    private static void AssertInvalid(IActionResult result)
    {
        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.False(Assert.IsType<bool>(ResponseValues.Of(unauthorized.Value)["isValid"]));
    }
}

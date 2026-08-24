using Auth.Models;
using Auth.Tests.TestDoubles;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Auth.Tests.Audit;

/// <summary>
/// NIE-AUTHN-005: login success and failure must be audited, and no audit record may contain the
/// submitted password or the issued session token.
/// </summary>
public sealed class LoginAuditTests
{
    private const string Password = "correct-horse-battery-staple";
    private const string IssuedToken = "issued-session-token";

    [Fact]
    public async Task A_failed_login_is_refused_and_audited_as_a_warning()
    {
        var harness = new AuthControllerHarness(
            identityProvider: IdentityProviderResponses.Login(authenticated: false));

        var result = await harness.Controller.Login(Credentials());

        Assert.IsType<UnauthorizedObjectResult>(result);
        var entry = Assert.Single(harness.Logger.Entries);
        Assert.Equal(LogLevel.Warning, entry.Level);
        Assert.Contains("devia", entry.AllText, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_failed_login_never_issues_a_server_side_session()
    {
        var harness = new AuthControllerHarness(
            identityProvider: IdentityProviderResponses.Login(authenticated: false));

        await harness.Controller.Login(Credentials());

        await harness.SessionService.DidNotReceive()
            .IssueSessionAsync(Arg.Any<LoginResponse>(), Arg.Any<CancellationToken>());
        Assert.Empty(harness.Cache.Keys);
    }

    [Fact]
    public async Task A_failed_login_never_writes_the_submitted_password_to_the_log()
    {
        var harness = new AuthControllerHarness(
            identityProvider: IdentityProviderResponses.Login(authenticated: false));

        await harness.Controller.Login(Credentials());

        Assert.DoesNotContain(Password, harness.Logger.AllLoggedText, StringComparison.Ordinal);
    }

    [Fact]
    public async Task An_identity_provider_response_that_cannot_be_read_is_treated_as_a_failed_login()
    {
        var harness = new AuthControllerHarness(identityProvider: IdentityProviderResponses.Raw("null"));

        var result = await harness.Controller.Login(Credentials());

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Null(unauthorized.Value);
        await harness.SessionService.DidNotReceive()
            .IssueSessionAsync(Arg.Any<LoginResponse>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task A_successful_login_is_audited_at_information_level()
    {
        var harness = SuccessfulLoginHarness();

        var result = await harness.Controller.Login(Credentials());

        Assert.IsType<OkObjectResult>(result);
        var entry = Assert.Single(harness.Logger.Entries);
        Assert.Equal(LogLevel.Information, entry.Level);
        Assert.Contains("devia", entry.AllText, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_successful_login_never_writes_the_password_or_the_issued_token_to_the_log()
    {
        var harness = SuccessfulLoginHarness();

        await harness.Controller.Login(Credentials());

        Assert.DoesNotContain(Password, harness.Logger.AllLoggedText, StringComparison.Ordinal);
        Assert.DoesNotContain(IssuedToken, harness.Logger.AllLoggedText, StringComparison.Ordinal);
        Assert.DoesNotContain("idp-session-token", harness.Logger.AllLoggedText, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_successful_login_delegates_session_creation_and_returns_the_issued_response()
    {
        var harness = SuccessfulLoginHarness();

        var ok = Assert.IsType<OkObjectResult>(await harness.Controller.Login(Credentials()));

        var issued = Assert.IsType<IssuedLoginResponse>(ok.Value);
        Assert.Equal(IssuedToken, issued.sessionToken);
        await harness.SessionService.Received(1)
            .IssueSessionAsync(
                Arg.Is<LoginResponse>(login => login != null && login.userId == "devia"),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task A_successful_login_forwards_the_request_abort_token_to_session_creation()
    {
        var harness = SuccessfulLoginHarness();

        await harness.Controller.Login(Credentials());

        await harness.SessionService.Received(1)
            .IssueSessionAsync(Arg.Any<LoginResponse>(), harness.HttpContext.RequestAborted);
    }

    [Fact]
    public async Task Login_presents_the_subscription_key_and_the_credentials_to_the_identity_provider()
    {
        var harness = SuccessfulLoginHarness();

        await harness.Controller.Login(Credentials());

        var request = Assert.Single(harness.IdentityProvider.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal(AuthControllerHarness.IdentityProviderBaseUrl + "/LogInUser", request.Url);
        Assert.Equal(AuthControllerHarness.SubscriptionKey, request.Headers["x-nie-aws-api-gw-key"]);
        Assert.Contains("\"userid\":\"devia\"", request.Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task The_audit_trail_for_a_login_attempt_is_a_single_record()
    {
        var harness = SuccessfulLoginHarness();

        await harness.Controller.Login(Credentials());

        Assert.Single(harness.Logger.Entries);
    }

    private static AuthControllerHarness SuccessfulLoginHarness()
    {
        var harness = new AuthControllerHarness(
            identityProvider: IdentityProviderResponses.Login(authenticated: true));

        harness.SessionService
            .IssueSessionAsync(Arg.Any<LoginResponse>(), Arg.Any<CancellationToken>())
            .Returns(new IssuedLoginResponse
            {
                isAuthenticated = true,
                userId = "devia",
                userName = "dev.ia",
                sessionToken = IssuedToken
            });

        return harness;
    }

    private static LoginRequest Credentials() => new() { userid = "devia", pd = Password };
}

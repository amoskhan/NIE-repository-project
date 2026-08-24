using System.Net;
using Auth.Models;
using Auth.Tests.TestDoubles;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using NSubstitute;

namespace Auth.Tests.Sso;

/// <summary>
/// The controller is the only place where SSO failures become HTTP status codes, and the mapping
/// decides what an anonymous caller learns about the failure.
/// </summary>
public sealed class AuthControllerSsoTests
{
    [Fact]
    public async Task Starting_sso_passes_the_return_url_and_the_generated_callback_url_to_the_service()
    {
        var harness = new AuthControllerHarness();
        StubStart(harness, new SsoStartResponse { state = "s", nonce = "n", launchUrl = "https://portal", pollIntervalMs = 1500 });

        await harness.Controller.SsoStart("https://app.nie.edu.sg/inbox");

        await harness.SsoService.Received(1).StartAsync(
            "https://app.nie.edu.sg/inbox",
            AuthControllerHarness.CallbackUrl,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Starting_sso_returns_the_handshake_the_service_produced()
    {
        var harness = new AuthControllerHarness();
        var response = new SsoStartResponse { state = "s", nonce = "n", launchUrl = "https://portal/launch", pollIntervalMs = 1500 };
        StubStart(harness, response);

        var ok = Assert.IsType<OkObjectResult>(await harness.Controller.SsoStart(returnUrl: null));

        Assert.Same(response, ok.Value);
    }

    [Fact]
    public async Task Starting_sso_reports_service_unavailable_when_the_feature_is_not_configured()
    {
        var harness = new AuthControllerHarness();
        harness.SsoService
            .StartAsync(Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<SsoStartResponse>(
                new InvalidOperationException("Portal SSO is not enabled.")));

        var result = Assert.IsType<ObjectResult>(await harness.Controller.SsoStart(returnUrl: null));

        Assert.Equal(StatusCodes.Status503ServiceUnavailable, result.StatusCode);
        Assert.Equal("Portal SSO is not enabled.", ResponseValues.Of(result.Value)["message"]);
        Assert.Equal(LogLevel.Warning, Assert.Single(harness.Logger.Entries).Level);
    }

    [Fact]
    public async Task The_sso_callback_hands_the_caller_address_to_the_service()
    {
        var harness = new AuthControllerHarness();
        harness.UseRemoteIp("10.1.2.7");
        StubCallback(harness, new SsoFinalizeResult { status = SsoStateStatus.Completed });

        await harness.Controller.SsoCallback(Callback());

        await harness.SsoService.Received(1).HandleCallbackAsync(
            Arg.Any<SsoCallbackRequest>(),
            IPAddress.Parse("10.1.2.7"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task The_sso_callback_returns_the_finalize_result_on_success()
    {
        var harness = new AuthControllerHarness();
        var finalizeResult = new SsoFinalizeResult
        {
            status = SsoStateStatus.Completed,
            login = new IssuedLoginResponse { isAuthenticated = true, sessionToken = "tok" }
        };
        StubCallback(harness, finalizeResult);

        var ok = Assert.IsType<OkObjectResult>(await harness.Controller.SsoCallback(Callback()));

        Assert.Same(finalizeResult, ok.Value);
    }

    [Fact]
    public async Task A_rejected_sso_payload_becomes_an_unauthorized_response()
    {
        var harness = new AuthControllerHarness();
        StubCallbackFailure(harness, new SecurityTokenException("The SSO nonce claim does not match the pending login."));

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(await harness.Controller.SsoCallback(Callback()));

        Assert.Equal(
            "The SSO nonce claim does not match the pending login.",
            ResponseValues.Of(unauthorized.Value)["message"]);
    }

    [Fact]
    public async Task A_rejected_sso_payload_is_not_audited_by_the_controller()
    {
        var harness = new AuthControllerHarness();
        StubCallbackFailure(harness, new SecurityTokenException("The SSO payload has already been used."));

        await harness.Controller.SsoCallback(Callback());

        // Only the service logs token rejections; the controller adds no record of its own.
        Assert.Empty(harness.Logger.Entries);
    }

    [Fact]
    public async Task An_unconfigured_sso_callback_reports_service_unavailable()
    {
        var harness = new AuthControllerHarness();
        StubCallbackFailure(harness, new InvalidOperationException("PortalSso:ExchangeApi:BaseUrl configuration is required."));

        var result = Assert.IsType<ObjectResult>(await harness.Controller.SsoCallback(Callback()));

        Assert.Equal(StatusCodes.Status503ServiceUnavailable, result.StatusCode);
        Assert.Equal(LogLevel.Warning, Assert.Single(harness.Logger.Entries).Level);
    }

    [Fact]
    public async Task An_unreachable_exchange_api_reports_bad_gateway()
    {
        var harness = new AuthControllerHarness();
        StubCallbackFailure(harness, new HttpRequestException("Response status code does not indicate success: 500."));

        var result = Assert.IsType<ObjectResult>(await harness.Controller.SsoCallback(Callback()));

        Assert.Equal(StatusCodes.Status502BadGateway, result.StatusCode);
        Assert.Equal(LogLevel.Warning, Assert.Single(harness.Logger.Entries).Level);
    }

    [Fact]
    public async Task Finalizing_a_completed_handshake_returns_only_the_issued_login()
    {
        var harness = new AuthControllerHarness();
        var login = new IssuedLoginResponse { isAuthenticated = true, userId = "devia", sessionToken = "tok" };
        StubFinalize(harness, new SsoFinalizeResult { status = SsoStateStatus.Completed, login = login });

        var ok = Assert.IsType<OkObjectResult>(await harness.Controller.SsoFinalize("state"));

        Assert.Same(login, ok.Value);
    }

    [Fact]
    public async Task A_completed_handshake_without_a_login_is_reported_as_still_running()
    {
        var harness = new AuthControllerHarness();
        StubFinalize(harness, new SsoFinalizeResult { status = SsoStateStatus.Completed, login = null });

        Assert.IsType<AcceptedResult>(await harness.Controller.SsoFinalize("state"));
    }

    [Fact]
    public async Task Finalizing_a_failed_handshake_returns_unauthorized_with_the_reason()
    {
        var harness = new AuthControllerHarness();
        StubFinalize(harness, new SsoFinalizeResult { status = SsoStateStatus.Failed, message = "Account is locked." });

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(await harness.Controller.SsoFinalize("state"));
        var values = ResponseValues.Of(unauthorized.Value);

        Assert.Equal("Account is locked.", values["message"]);
        Assert.Equal(SsoStateStatus.Failed, values["status"]);
    }

    [Fact]
    public async Task Finalizing_a_pending_handshake_asks_the_caller_to_poll_again()
    {
        var harness = new AuthControllerHarness();
        var pending = new SsoFinalizeResult { status = SsoStateStatus.Pending, pollIntervalMs = 1500 };
        StubFinalize(harness, pending);

        var accepted = Assert.IsType<AcceptedResult>(await harness.Controller.SsoFinalize("state"));

        Assert.Same(pending, accepted.Value);
    }

    [Fact]
    public async Task An_unconfigured_finalize_reports_service_unavailable()
    {
        var harness = new AuthControllerHarness();
        harness.SsoService
            .FinalizeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<SsoFinalizeResult>(
                new InvalidOperationException("Portal SSO is not enabled.")));

        var result = Assert.IsType<ObjectResult>(await harness.Controller.SsoFinalize("state"));

        Assert.Equal(StatusCodes.Status503ServiceUnavailable, result.StatusCode);
    }

    private static SsoCallbackRequest Callback() =>
        new() { state = "state", encryptedPayload = "encrypted.payload.value.goes.here" };

    private static void StubStart(AuthControllerHarness harness, SsoStartResponse response) =>
        harness.SsoService
            .StartAsync(Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(response);

    private static void StubCallback(AuthControllerHarness harness, SsoFinalizeResult result) =>
        harness.SsoService
            .HandleCallbackAsync(Arg.Any<SsoCallbackRequest>(), Arg.Any<IPAddress?>(), Arg.Any<CancellationToken>())
            .Returns(result);

    private static void StubCallbackFailure(AuthControllerHarness harness, Exception exception) =>
        harness.SsoService
            .HandleCallbackAsync(Arg.Any<SsoCallbackRequest>(), Arg.Any<IPAddress?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<SsoFinalizeResult>(exception));

    private static void StubFinalize(AuthControllerHarness harness, SsoFinalizeResult result) =>
        harness.SsoService
            .FinalizeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(result);
}

using Auth.Models;
using Auth.Tests.TestDoubles;
using Microsoft.AspNetCore.Mvc;

namespace Auth.Tests.Security;

/// <summary>
/// NIE-AUTHN-004: the credential-free test-session endpoint must be reachable only in Development.
/// </summary>
public sealed class TestSessionEndpointTests
{
    [Theory]
    [InlineData("Production")]
    [InlineData("Staging")]
    [InlineData("Local")]
    [InlineData("Test")]
    [InlineData("Preproduction")]
    [InlineData("")]
    [InlineData("Development ")]
    [InlineData("Dev")]
    public async Task Test_session_endpoint_is_absent_outside_development(string environmentName)
    {
        var harness = new AuthControllerHarness(environmentName);

        var result = await harness.Controller.CreateTestSession(new CreateTestSessionRequest { UserId = "attacker" });

        Assert.IsType<NotFoundResult>(result);
        Assert.Empty(harness.Cache.Keys);
    }

    [Theory]
    [InlineData("Development")]
    [InlineData("development")]
    [InlineData("DEVELOPMENT")]
    public async Task Test_session_endpoint_is_available_in_development_regardless_of_name_casing(
        string environmentName)
    {
        var harness = new AuthControllerHarness(environmentName);

        var result = await harness.Controller.CreateTestSession(req: null);

        var response = ReadResponse(result);
        Assert.True(response.Success);
        Assert.NotNull(response.SessionToken);
    }

    [Fact]
    public async Task A_development_test_session_is_stored_server_side_under_an_opaque_token()
    {
        var harness = new AuthControllerHarness("Development");

        var result = await harness.Controller.CreateTestSession(req: null);

        var response = ReadResponse(result);
        var token = Assert.IsType<string>(response.SessionToken);
        Assert.Equal(32, token.Length);
        Assert.True(token.All(Uri.IsHexDigit));

        var stored = harness.ReadSession(token);
        Assert.NotNull(stored);
        Assert.Equal("devia", stored.UserId);
    }

    [Fact]
    public async Task A_development_test_session_uses_documented_defaults_when_nothing_is_supplied()
    {
        var harness = new AuthControllerHarness("Development");

        var result = await harness.Controller.CreateTestSession(req: null);

        var response = ReadResponse(result);
        var stored = harness.ReadSession(response.SessionToken!)!;
        Assert.Equal("devia", response.UserId);
        Assert.Equal("devia", response.UserName);
        Assert.Equal("devia@nie.edu.sg", response.Email);
        Assert.Equal("Digital Solutions", stored.Department);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task A_blank_requested_user_identifier_falls_back_to_the_default_developer(string? userId)
    {
        var harness = new AuthControllerHarness("Development");

        var result = await harness.Controller.CreateTestSession(new CreateTestSessionRequest { UserId = userId });

        Assert.Equal("devia", ReadResponse(result).UserId);
    }

    [Fact]
    public async Task Supplied_test_session_values_are_trimmed_before_they_are_stored()
    {
        var harness = new AuthControllerHarness("Development");

        var result = await harness.Controller.CreateTestSession(new CreateTestSessionRequest
        {
            UserId = "  tester  ",
            Name = "  Tess Ter  ",
            Email = "  tess@nie.edu.sg  ",
            Department = "  Registry  "
        });

        var response = ReadResponse(result);
        var stored = harness.ReadSession(response.SessionToken!)!;
        Assert.Equal("tester", stored.UserId);
        Assert.Equal("Tess Ter", stored.Name);
        Assert.Equal("tess@nie.edu.sg", stored.Email);
        Assert.Equal("Registry", stored.Department);
    }

    [Fact]
    public async Task A_test_session_email_defaults_to_the_institutional_address_of_the_trimmed_user()
    {
        var harness = new AuthControllerHarness("Development");

        var result = await harness.Controller.CreateTestSession(new CreateTestSessionRequest { UserId = "  tester  " });

        Assert.Equal("tester@nie.edu.sg", ReadResponse(result).Email);
    }

    [Fact]
    public async Task A_test_session_display_name_defaults_to_the_user_identifier()
    {
        var harness = new AuthControllerHarness("Development");

        var result = await harness.Controller.CreateTestSession(new CreateTestSessionRequest
        {
            UserId = "tester",
            Name = "   "
        });

        Assert.Equal("tester", ReadResponse(result).UserName);
    }

    [Fact]
    public async Task Each_test_session_receives_a_distinct_token()
    {
        var harness = new AuthControllerHarness("Development");

        var first = ReadResponse(await harness.Controller.CreateTestSession(req: null));
        var second = ReadResponse(await harness.Controller.CreateTestSession(req: null));

        Assert.NotEqual(first.SessionToken, second.SessionToken);
        Assert.Equal(2, harness.Cache.Keys.Count);
    }

    [Fact]
    public async Task A_test_session_expires_after_the_configured_session_lifetime()
    {
        var harness = new AuthControllerHarness("Development", sessionMinutes: 45);

        var result = await harness.Controller.CreateTestSession(req: null);

        var response = ReadResponse(result);
        var options = harness.Cache.OptionsFor(AuthControllerHarness.SessionKeyPrefix + response.SessionToken);
        Assert.NotNull(options);
        Assert.Equal(TimeSpan.FromMinutes(45), options.AbsoluteExpirationRelativeToNow);
    }

    [Fact]
    public async Task A_test_session_response_never_carries_an_error_message()
    {
        var harness = new AuthControllerHarness("Development");

        var result = await harness.Controller.CreateTestSession(req: null);

        Assert.Null(ReadResponse(result).ErrorMessage);
    }

    private static CreateTestSessionResponse ReadResponse(IActionResult result)
    {
        var ok = Assert.IsType<OkObjectResult>(result);
        return Assert.IsType<CreateTestSessionResponse>(ok.Value);
    }
}

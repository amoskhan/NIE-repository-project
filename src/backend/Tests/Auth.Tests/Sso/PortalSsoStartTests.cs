using Auth.Models;
using Auth.Tests.TestDoubles;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.IdentityModel.Tokens;
using NSubstitute;

namespace Auth.Tests.Sso;

/// <summary>
/// Portal SSO handshake start: state issuance, launch URL construction and — for NIE-AUTHN-004 —
/// the treatment of caller-supplied redirect targets.
/// </summary>
public sealed class PortalSsoStartTests
{
    [Fact]
    public async Task Starting_sso_while_the_feature_is_disabled_is_refused()
    {
        var harness = new PortalSsoHarness(options => options.Enabled = false);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => harness.Service.StartAsync(null, null, TestContext.Current.CancellationToken));

        Assert.Contains("not enabled", exception.Message, StringComparison.Ordinal);
        Assert.Empty(harness.Cache.Keys);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Starting_sso_without_a_launch_url_template_is_refused(string template)
    {
        var harness = new PortalSsoHarness(options => options.LaunchUrlTemplate = template);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => harness.Service.StartAsync(null, null, TestContext.Current.CancellationToken));

        Assert.Contains("LaunchUrlTemplate", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Starting_sso_without_any_return_url_is_refused()
    {
        var harness = new PortalSsoHarness(options => options.DefaultReturnUrl = string.Empty);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => harness.Service.StartAsync(null, null, TestContext.Current.CancellationToken));

        Assert.Contains("DefaultReturnUrl", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Starting_sso_without_any_callback_url_is_refused()
    {
        var harness = new PortalSsoHarness(options => options.CallbackUrl = string.Empty);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => harness.Service.StartAsync(null, null, TestContext.Current.CancellationToken));

        Assert.Contains("callback URL", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Starting_sso_prefers_the_request_scoped_callback_url()
    {
        var harness = new PortalSsoHarness();

        var response = await harness.Service.StartAsync(
            null,
            "https://auth.nie.edu.sg/api/Auth/SsoCallback?tenant=a",
            TestContext.Current.CancellationToken);

        Assert.Contains(
            Uri.EscapeDataString("https://auth.nie.edu.sg/api/Auth/SsoCallback?tenant=a"),
            response.launchUrl,
            StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Starting_sso_falls_back_to_the_configured_callback_url(string? callbackUrl)
    {
        var harness = new PortalSsoHarness();

        var response = await harness.Service.StartAsync(
            null,
            callbackUrl,
            TestContext.Current.CancellationToken);

        Assert.Contains(
            Uri.EscapeDataString("https://auth.nie.edu.sg/api/Auth/SsoCallback"),
            response.launchUrl,
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task Each_handshake_receives_a_distinct_high_entropy_state_and_nonce()
    {
        var harness = new PortalSsoHarness();

        var first = await harness.Service.StartAsync(null, null, TestContext.Current.CancellationToken);
        var second = await harness.Service.StartAsync(null, null, TestContext.Current.CancellationToken);

        Assert.NotEqual(first.state, second.state);
        Assert.NotEqual(first.nonce, second.nonce);
        Assert.NotEqual(first.state, first.nonce);
        Assert.Equal(32, Base64UrlEncoder.DecodeBytes(first.state).Length);
        Assert.Equal(32, Base64UrlEncoder.DecodeBytes(first.nonce).Length);
    }

    [Fact]
    public async Task Starting_sso_persists_a_pending_state_record_with_the_configured_expiry()
    {
        var harness = new PortalSsoHarness(options => options.StateTtlMinutes = 7);

        var response = await harness.Service.StartAsync(null, null, TestContext.Current.CancellationToken);

        var record = harness.ReadState(response.state);
        Assert.NotNull(record);
        Assert.Equal(SsoStateStatus.Pending, record.Status);
        Assert.Equal(response.nonce, record.Nonce);
        Assert.Null(record.Login);
        Assert.Null(record.ErrorMessage);

        var options = harness.Cache.OptionsFor(PortalSsoHarness.StateKeyPrefix + response.state);
        Assert.NotNull(options);
        Assert.Equal(TimeSpan.FromMinutes(7), options.AbsoluteExpirationRelativeToNow);
    }

    [Fact]
    public async Task The_nonce_is_never_written_into_the_browser_visible_return_url()
    {
        var harness = new PortalSsoHarness();

        var response = await harness.Service.StartAsync(null, null, TestContext.Current.CancellationToken);

        var record = harness.ReadState(response.state)!;
        Assert.DoesNotContain(response.nonce, record.ReturnUrl!, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Starting_sso_marks_the_return_url_and_carries_the_state_back_to_the_browser()
    {
        var harness = new PortalSsoHarness();

        var response = await harness.Service.StartAsync(null, null, TestContext.Current.CancellationToken);

        var record = harness.ReadState(response.state)!;
        Assert.Equal(
            "https://app.nie.edu.sg/sso?sso=1&state=" + Uri.EscapeDataString(response.state),
            record.ReturnUrl);
    }

    [Fact]
    public async Task Starting_sso_appends_to_an_existing_return_url_query_string()
    {
        var harness = new PortalSsoHarness(
            options => options.DefaultReturnUrl = "https://app.nie.edu.sg/sso?tab=inbox");

        var response = await harness.Service.StartAsync(null, null, TestContext.Current.CancellationToken);

        var record = harness.ReadState(response.state)!;
        Assert.StartsWith("https://app.nie.edu.sg/sso?tab=inbox&sso=1&state=", record.ReturnUrl, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task A_blank_requested_return_url_falls_back_to_the_configured_default(string? requestedReturnUrl)
    {
        var harness = new PortalSsoHarness();

        var response = await harness.Service.StartAsync(
            requestedReturnUrl,
            null,
            TestContext.Current.CancellationToken);

        var record = harness.ReadState(response.state)!;
        Assert.StartsWith("https://app.nie.edu.sg/sso?", record.ReturnUrl, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Starting_sso_escapes_every_launch_url_placeholder()
    {
        var harness = new PortalSsoHarness();

        var response = await harness.Service.StartAsync(null, null, TestContext.Current.CancellationToken);

        var record = harness.ReadState(response.state)!;
        Assert.Equal(
            "https://portal.nie.edu.sg/launch"
            + "?state=" + Uri.EscapeDataString(response.state)
            + "&nonce=" + Uri.EscapeDataString(response.nonce)
            + "&return=" + Uri.EscapeDataString(record.ReturnUrl!)
            + "&callback=" + Uri.EscapeDataString("https://auth.nie.edu.sg/api/Auth/SsoCallback"),
            response.launchUrl);
    }

    [Fact]
    public async Task Starting_sso_returns_the_configured_poll_interval()
    {
        var harness = new PortalSsoHarness(options => options.FinalizePollIntervalMs = 2500);

        var response = await harness.Service.StartAsync(null, null, TestContext.Current.CancellationToken);

        Assert.Equal(2500, response.pollIntervalMs);
    }

    [Theory]
    [InlineData("https://evil.example.com/harvest")]
    [InlineData("http://evil.example.com/harvest")]
    [InlineData("//evil.example.com/harvest")]
    [InlineData("/\\evil.example.com/harvest")]
    [InlineData("https:/\\evil.example.com")]
    [InlineData("https://app.nie.edu.sg@evil.example.com/harvest")]
    [InlineData("%2F%2Fevil.example.com")]
    [InlineData("javascript:alert(1)")]
    [InlineData("https://app.nie.edu.sg.evil.example.com/harvest")]
    public async Task A_caller_supplied_return_url_is_accepted_without_any_allowlist_check(string hostileReturnUrl)
    {
        var harness = new PortalSsoHarness(options =>
        {
            options.AllowedSourceUrls = ["https://portal.nie.edu.sg/launch"];
            options.DefaultReturnUrl = "https://app.nie.edu.sg/sso";
        });

        var response = await harness.Service.StartAsync(
            hostileReturnUrl,
            null,
            TestContext.Current.CancellationToken);

        // NIE-AUTHN-004 requires redirect targets to be validated. StartAsync currently echoes any
        // caller-supplied returnUrl straight into the persisted state and the portal launch URL.
        var record = harness.ReadState(response.state)!;
        Assert.StartsWith(hostileReturnUrl, record.ReturnUrl, StringComparison.Ordinal);
        Assert.Contains(Uri.EscapeDataString(hostileReturnUrl), response.launchUrl, StringComparison.Ordinal);
        Assert.DoesNotContain("app.nie.edu.sg/sso?sso=1", record.ReturnUrl!, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_hostile_return_url_still_produces_a_pending_state_the_portal_can_complete()
    {
        var harness = new PortalSsoHarness();

        var response = await harness.Service.StartAsync(
            "https://evil.example.com/harvest",
            null,
            TestContext.Current.CancellationToken);

        var record = harness.ReadState(response.state)!;
        Assert.Equal(SsoStateStatus.Pending, record.Status);
    }

    [Fact]
    public async Task Starting_sso_forwards_the_caller_cancellation_token_to_the_state_store()
    {
        var harness = new PortalSsoHarness();
        var cancellationToken = TestContext.Current.CancellationToken;

        var response = await harness.Service.StartAsync(null, null, cancellationToken);

        await harness.Cache.Cache.Received(1).SetAsync(
            PortalSsoHarness.StateKeyPrefix + response.state,
            Arg.Any<byte[]>(),
            Arg.Any<DistributedCacheEntryOptions>(),
            cancellationToken);
    }
}

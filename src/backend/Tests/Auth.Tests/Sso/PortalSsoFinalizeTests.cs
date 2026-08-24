using Auth.Models;
using Auth.Tests.TestDoubles;

namespace Auth.Tests.Sso;

/// <summary>
/// Portal SSO polling: the browser exchanges an opaque state for the issued session, so every state
/// transition has to map onto exactly one outcome.
/// </summary>
public sealed class PortalSsoFinalizeTests
{
    [Fact]
    public async Task Finalizing_while_the_feature_is_disabled_is_refused()
    {
        var harness = new PortalSsoHarness(options => options.Enabled = false);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => harness.Service.FinalizeAsync("any-state", TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Finalizing_an_unknown_state_reports_failure_rather_than_a_pending_login()
    {
        var harness = new PortalSsoHarness();

        var result = await harness.Service.FinalizeAsync("never-issued", TestContext.Current.CancellationToken);

        Assert.Equal(SsoStateStatus.Failed, result.status);
        Assert.Null(result.login);
        Assert.Contains("expired", result.message!, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Finalizing_a_pending_state_reports_pending_without_a_login()
    {
        var harness = new PortalSsoHarness();
        harness.SeedPendingState();

        var result = await harness.Service.FinalizeAsync("pending-state", TestContext.Current.CancellationToken);

        Assert.Equal(SsoStateStatus.Pending, result.status);
        Assert.Null(result.login);
        Assert.Null(result.message);
        Assert.Equal(1500, result.pollIntervalMs);
    }

    [Theory]
    [InlineData("in-progress")]
    [InlineData("PENDING")]
    [InlineData("")]
    public async Task An_unrecognised_state_status_is_treated_as_still_pending(string status)
    {
        var harness = new PortalSsoHarness();
        harness.SeedState(new SsoStateRecord { State = "s", Nonce = "n", Status = status });

        var result = await harness.Service.FinalizeAsync("s", TestContext.Current.CancellationToken);

        Assert.Equal(SsoStateStatus.Pending, result.status);
        Assert.Null(result.login);
    }

    [Fact]
    public async Task Finalizing_a_completed_state_returns_the_issued_login()
    {
        var harness = new PortalSsoHarness();
        harness.SeedState(new SsoStateRecord
        {
            State = "s",
            Nonce = "n",
            Status = SsoStateStatus.Completed,
            Login = new IssuedLoginResponse { isAuthenticated = true, userId = "devia", sessionToken = "tok" }
        });

        var result = await harness.Service.FinalizeAsync("s", TestContext.Current.CancellationToken);

        Assert.Equal(SsoStateStatus.Completed, result.status);
        Assert.NotNull(result.login);
        Assert.Equal("tok", result.login.sessionToken);
        Assert.Null(result.message);
    }

    [Fact]
    public async Task Finalizing_a_failed_state_surfaces_the_recorded_reason()
    {
        var harness = new PortalSsoHarness();
        harness.SeedState(new SsoStateRecord
        {
            State = "s",
            Nonce = "n",
            Status = SsoStateStatus.Failed,
            ErrorMessage = "The SSO payload source system is not trusted."
        });

        var result = await harness.Service.FinalizeAsync("s", TestContext.Current.CancellationToken);

        Assert.Equal(SsoStateStatus.Failed, result.status);
        Assert.Equal("The SSO payload source system is not trusted.", result.message);
        Assert.Null(result.login);
    }

    [Fact]
    public async Task A_failed_state_without_a_reason_still_reports_a_generic_failure()
    {
        var harness = new PortalSsoHarness();
        harness.SeedState(new SsoStateRecord { State = "s", Nonce = "n", Status = SsoStateStatus.Failed });

        var result = await harness.Service.FinalizeAsync("s", TestContext.Current.CancellationToken);

        Assert.Equal("The SSO request failed.", result.message);
    }

    [Fact]
    public async Task A_failed_state_never_leaks_a_login_even_if_one_was_recorded()
    {
        var harness = new PortalSsoHarness();
        harness.SeedState(new SsoStateRecord
        {
            State = "s",
            Nonce = "n",
            Status = SsoStateStatus.Failed,
            ErrorMessage = "denied",
            Login = new IssuedLoginResponse { isAuthenticated = true, sessionToken = "leaked" }
        });

        var result = await harness.Service.FinalizeAsync("s", TestContext.Current.CancellationToken);

        Assert.Null(result.login);
    }

    [Theory]
    [InlineData(SsoStateStatus.Pending)]
    [InlineData(SsoStateStatus.Failed)]
    [InlineData(SsoStateStatus.Completed)]
    public async Task Finalizing_never_mutates_the_stored_state(string status)
    {
        var harness = new PortalSsoHarness();
        harness.SeedState(new SsoStateRecord
        {
            State = "s",
            Nonce = "n",
            Status = status,
            Login = new IssuedLoginResponse { isAuthenticated = true, sessionToken = "tok" }
        });

        await harness.Service.FinalizeAsync("s", TestContext.Current.CancellationToken);

        var record = harness.ReadState("s");
        Assert.NotNull(record);
        Assert.Equal(status, record.Status);
        Assert.Empty(harness.Cache.RemovedKeys);
    }

    [Fact]
    public async Task A_completed_state_keeps_handing_out_the_session_token_on_every_poll()
    {
        var harness = new PortalSsoHarness();
        harness.SeedState(new SsoStateRecord
        {
            State = "s",
            Nonce = "n",
            Status = SsoStateStatus.Completed,
            Login = new IssuedLoginResponse { isAuthenticated = true, sessionToken = "tok" }
        });

        var first = await harness.Service.FinalizeAsync("s", TestContext.Current.CancellationToken);
        var second = await harness.Service.FinalizeAsync("s", TestContext.Current.CancellationToken);

        // The state is not single-use: anyone who replays the state value within the state TTL is
        // handed the same session token again.
        Assert.Equal("tok", first.login!.sessionToken);
        Assert.Equal("tok", second.login!.sessionToken);
    }

    [Theory]
    [InlineData(500)]
    [InlineData(3000)]
    public async Task Every_finalize_outcome_carries_the_configured_poll_interval(int pollIntervalMs)
    {
        var harness = new PortalSsoHarness(options => options.FinalizePollIntervalMs = pollIntervalMs);
        harness.SeedPendingState();

        var pending = await harness.Service.FinalizeAsync("pending-state", TestContext.Current.CancellationToken);
        var unknown = await harness.Service.FinalizeAsync("never-issued", TestContext.Current.CancellationToken);

        Assert.Equal(pollIntervalMs, pending.pollIntervalMs);
        Assert.Equal(pollIntervalMs, unknown.pollIntervalMs);
    }
}

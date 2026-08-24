using Api.Observability;
using Sentry;

namespace Api.Tests.Observability;

/// <summary>
/// Scheduled jobs are wrapped by this helper. It must never swallow a failure, and it
/// must always forward the caller's cancellation token to the wrapped operation.
/// </summary>
public sealed class SentryCronMonitorTests
{
    [Fact]
    public async Task The_wrapped_operation_runs_exactly_once()
    {
        var runs = 0;

        await SentryCronMonitor.TrackAsync(
            "job-slug",
            _ =>
            {
                runs++;
                return Task.CompletedTask;
            },
            ConfigureNothing,
            TestContext.Current.CancellationToken);

        Assert.Equal(1, runs);
    }

    [Fact]
    public async Task The_caller_cancellation_token_reaches_the_operation()
    {
        using var cancellation = new CancellationTokenSource();
        CancellationToken observed = default;

        await SentryCronMonitor.TrackAsync(
            "job-slug",
            token =>
            {
                observed = token;
                return Task.CompletedTask;
            },
            ConfigureNothing,
            cancellation.Token);

        Assert.Equal(cancellation.Token, observed);
    }

    [Fact]
    public async Task A_failing_job_is_rethrown_so_the_scheduler_can_retry()
    {
        var failure = new InvalidOperationException("purge failed");

        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(
            () => SentryCronMonitor.TrackAsync(
                "job-slug",
                _ => Task.FromException(failure),
                ConfigureNothing,
                TestContext.Current.CancellationToken));

        Assert.Same(failure, thrown);
    }

    [Fact]
    public async Task A_cancelled_job_still_propagates_the_cancellation()
    {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.ThrowsAsync<TaskCanceledException>(
            () => SentryCronMonitor.TrackAsync(
                "job-slug",
                token => Task.FromCanceled(token),
                ConfigureNothing,
                cancellation.Token));
    }

    private static void ConfigureNothing(SentryMonitorOptions options) => options.Interval("0 2 * * *");
}

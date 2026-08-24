using System.Diagnostics;
using Sentry;

namespace Api.Observability;

/// <summary>
/// Sends Sentry check-ins around scheduled jobs when Sentry is configured.
/// </summary>
public static class SentryCronMonitor
{
    public static async Task TrackAsync(
        string monitorSlug,
        Func<CancellationToken, Task> operation,
        Action<SentryMonitorOptions> configureMonitor,
        CancellationToken cancellationToken)
    {
        var startedAt = Stopwatch.GetTimestamp();
        var checkInId = SentrySdk.CaptureCheckIn(
            monitorSlug,
            CheckInStatus.InProgress,
            configureMonitorOptions: configureMonitor);

        try
        {
            await operation(cancellationToken);

            SentrySdk.CaptureCheckIn(
                monitorSlug,
                CheckInStatus.Ok,
                checkInId,
                Stopwatch.GetElapsedTime(startedAt),
                configureMonitorOptions: configureMonitor);
        }
        catch
        {
            SentrySdk.CaptureCheckIn(
                monitorSlug,
                CheckInStatus.Error,
                checkInId,
                Stopwatch.GetElapsedTime(startedAt),
                configureMonitorOptions: configureMonitor);
            throw;
        }
    }
}

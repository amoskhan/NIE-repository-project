using System.Diagnostics;
using System.Diagnostics.Metrics;
using Application.Integration;

namespace Infrastructure.Integration.Observability;

public static class ServiceIntegrationTelemetry
{
    private static long _outboxBacklog;
    private static double _oldestOutboxAgeSeconds;

    public const string ActivitySourceName = "Application.ServiceIntegration";
    public const string MeterName = "Application.ServiceIntegration";

    public static ActivitySource ActivitySource { get; } = new(ActivitySourceName);

    public static Meter Meter { get; } = new(MeterName);

    public static Counter<long> PublishedEvents { get; } =
        Meter.CreateCounter<long>("integration.events.published");

    public static Counter<long> ConsumedEvents { get; } =
        Meter.CreateCounter<long>("integration.events.consumed");

    public static Counter<long> RetriedEvents { get; } =
        Meter.CreateCounter<long>("integration.events.retried");

    public static Counter<long> DeadLetteredEvents { get; } =
        Meter.CreateCounter<long>("integration.events.dead_lettered");

    public static Histogram<double> PublishDurationMilliseconds { get; } =
        Meter.CreateHistogram<double>("integration.publish.duration", "ms");

    public static Histogram<double> ConsumeDurationMilliseconds { get; } =
        Meter.CreateHistogram<double>("integration.consume.duration", "ms");

    public static ObservableGauge<long> OutboxBacklog { get; } =
        Meter.CreateObservableGauge(
            "integration.outbox.backlog",
            () => Interlocked.Read(ref _outboxBacklog),
            "{message}");

    public static ObservableGauge<double> OldestOutboxAgeSeconds { get; } =
        Meter.CreateObservableGauge(
            "integration.outbox.oldest_age",
            () => Volatile.Read(ref _oldestOutboxAgeSeconds),
            "s");

    public static void SetOutboxStatistics(IntegrationOutboxStatistics statistics)
    {
        ArgumentNullException.ThrowIfNull(statistics);
        Interlocked.Exchange(ref _outboxBacklog, statistics.PendingCount);
        var age = statistics.OldestOccurredAtUtc is null
            ? 0
            : Math.Max(0, (DateTimeOffset.UtcNow - statistics.OldestOccurredAtUtc.Value).TotalSeconds);
        Volatile.Write(ref _oldestOutboxAgeSeconds, age);
    }
}

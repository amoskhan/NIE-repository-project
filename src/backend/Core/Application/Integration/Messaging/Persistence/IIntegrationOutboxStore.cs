using Contracts.Integration;

namespace Application.Integration;

public sealed record ClaimedIntegrationEvent(
    Guid OutboxId,
    Guid LockToken,
    IntegrationEventEnvelope Envelope,
    int AttemptCount);

public sealed record IntegrationOutboxStatistics(
    long PendingCount,
    DateTimeOffset? OldestOccurredAtUtc);

public interface IIntegrationOutboxStore
{
    Task<IntegrationOutboxStatistics> GetPendingStatisticsAsync(
        CancellationToken cancellationToken);

    Task<IReadOnlyList<ClaimedIntegrationEvent>> ClaimAsync(
        int batchSize,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken);

    Task MarkPublishedAsync(
        Guid outboxId,
        Guid lockToken,
        CancellationToken cancellationToken);

    Task MarkFailedAsync(
        Guid outboxId,
        Guid lockToken,
        string failureCode,
        int maximumAttempts,
        TimeSpan retryDelay,
        CancellationToken cancellationToken);
}

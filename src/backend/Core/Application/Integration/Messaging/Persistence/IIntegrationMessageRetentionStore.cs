namespace Application.Integration;

public sealed record IntegrationRetentionResult(
    int PublishedOutboxMessagesDeleted,
    int InboxReceiptsDeleted);

public interface IIntegrationMessageRetentionStore
{
    Task<IntegrationRetentionResult> PruneAsync(
        DateTimeOffset publishedBeforeUtc,
        DateTimeOffset processedBeforeUtc,
        int batchSize,
        CancellationToken cancellationToken);
}

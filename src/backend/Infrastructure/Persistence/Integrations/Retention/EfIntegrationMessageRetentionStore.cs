using Application.Integration;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Integrations;

public sealed class EfIntegrationMessageRetentionStore(
    MainDbContext dbContext) : IIntegrationMessageRetentionStore
{
    private readonly MainDbContext _dbContext = dbContext;

    public async Task<IntegrationRetentionResult> PruneAsync(
        DateTimeOffset publishedBeforeUtc,
        DateTimeOffset processedBeforeUtc,
        int batchSize,
        CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(batchSize, 1);

        var outboxIds = await _dbContext.IntegrationOutboxMessages
            .AsNoTracking()
            .Where(message =>
                message.PublishedAtUtc != null
                && message.PublishedAtUtc < publishedBeforeUtc)
            .OrderBy(message => message.PublishedAtUtc)
            .Select(message => message.Id)
            .Take(batchSize)
            .ToArrayAsync(cancellationToken);
        var outboxDeleted = outboxIds.Length == 0
            ? 0
            : await _dbContext.IntegrationOutboxMessages
                .Where(message => outboxIds.Contains(message.Id))
                .ExecuteDeleteAsync(cancellationToken);

        var inboxIds = await _dbContext.IntegrationInboxMessages
            .AsNoTracking()
            .Where(message => message.ProcessedAtUtc < processedBeforeUtc)
            .OrderBy(message => message.ProcessedAtUtc)
            .Select(message => message.Id)
            .Take(batchSize)
            .ToArrayAsync(cancellationToken);
        var inboxDeleted = inboxIds.Length == 0
            ? 0
            : await _dbContext.IntegrationInboxMessages
                .Where(message => inboxIds.Contains(message.Id))
                .ExecuteDeleteAsync(cancellationToken);

        return new IntegrationRetentionResult(outboxDeleted, inboxDeleted);
    }
}

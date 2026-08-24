using System.Text.Json;
using Application.Integration;
using Contracts.Integration;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Infrastructure.Integrations;

public sealed class EfIntegrationOutboxStore(
    MainDbContext dbContext,
    ILogger<EfIntegrationOutboxStore>? logger = null) : IIntegrationOutboxStore
{
    private readonly MainDbContext _dbContext = dbContext;
    private readonly ILogger<EfIntegrationOutboxStore> _logger = logger
        ?? NullLogger<EfIntegrationOutboxStore>.Instance;

    public async Task<IntegrationOutboxStatistics> GetPendingStatisticsAsync(
        CancellationToken cancellationToken)
    {
        var query = _dbContext.IntegrationOutboxMessages
            .AsNoTracking()
            .Where(message =>
                message.PublishedAtUtc == null
                && message.DeadLetteredAtUtc == null);
        var pendingCount = await query.LongCountAsync(cancellationToken);
        DateTimeOffset? oldestOccurredAtUtc = pendingCount == 0
            ? null
            : await query.MinAsync(message => message.OccurredAtUtc, cancellationToken);
        return new IntegrationOutboxStatistics(pendingCount, oldestOccurredAtUtc);
    }

    public Task<IReadOnlyList<ClaimedIntegrationEvent>> ClaimAsync(
        int batchSize,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken) =>
        ClaimCoreAsync(batchSize, leaseDuration, cancellationToken);

    private async Task<IReadOnlyList<ClaimedIntegrationEvent>> ClaimCoreAsync(
        int batchSize,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(batchSize, 1);
        if (leaseDuration <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(leaseDuration));
        }

        var claimedAtUtc = DateTimeOffset.UtcNow;
        var lockToken = Guid.CreateVersion7();
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        var messages = await _dbContext.IntegrationOutboxMessages
            .FromSqlInterpolated($"""
                SELECT *
                FROM "IntegrationOutboxMessages"
                WHERE "PublishedAtUtc" IS NULL
                  AND "DeadLetteredAtUtc" IS NULL
                  AND "AvailableAtUtc" <= {claimedAtUtc}
                  AND ("LockExpiresAtUtc" IS NULL OR "LockExpiresAtUtc" <= {claimedAtUtc})
                ORDER BY "OccurredAtUtc", "Id"
                FOR UPDATE SKIP LOCKED
                LIMIT {batchSize}
                """)
            .ToListAsync(cancellationToken);

        var claimedMessages = new List<ClaimedIntegrationEvent>(messages.Count);
        foreach (var message in messages)
        {
            message.AttemptCount++;
            IntegrationEventEnvelope envelope;
            try
            {
                envelope = DeserializeEnvelope(message.Payload);
            }
            catch (InvalidOperationException exception)
            {
                message.LastFailureCode = "InvalidOutboxEnvelope";
                message.DeadLetteredAtUtc = claimedAtUtc;
                message.LockToken = null;
                message.LockExpiresAtUtc = null;
                _logger.LogError(
                    exception,
                    "Malformed integration outbox row {OutboxId} was quarantined before publication",
                    message.Id);
                continue;
            }

            message.LockToken = lockToken;
            message.LockExpiresAtUtc = claimedAtUtc.Add(leaseDuration);
            claimedMessages.Add(new ClaimedIntegrationEvent(
                message.Id,
                lockToken,
                envelope,
                message.AttemptCount));
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return claimedMessages;
    }

    public Task MarkPublishedAsync(
        Guid outboxId,
        Guid lockToken,
        CancellationToken cancellationToken) =>
        MarkPublishedCoreAsync(outboxId, lockToken, cancellationToken);

    public Task MarkFailedAsync(
        Guid outboxId,
        Guid lockToken,
        string failureCode,
        int maximumAttempts,
        TimeSpan retryDelay,
        CancellationToken cancellationToken) =>
        MarkFailedCoreAsync(
            outboxId,
            lockToken,
            failureCode,
            maximumAttempts,
            retryDelay,
            cancellationToken);

    private async Task MarkPublishedCoreAsync(
        Guid outboxId,
        Guid lockToken,
        CancellationToken cancellationToken)
    {
        ValidateLockIdentifiers(outboxId, lockToken);
        var message = await FindClaimedMessageAsync(outboxId, lockToken, cancellationToken);
        message.PublishedAtUtc = DateTimeOffset.UtcNow;
        message.LockToken = null;
        message.LockExpiresAtUtc = null;
        message.LastFailureCode = null;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task MarkFailedCoreAsync(
        Guid outboxId,
        Guid lockToken,
        string failureCode,
        int maximumAttempts,
        TimeSpan retryDelay,
        CancellationToken cancellationToken)
    {
        ValidateLockIdentifiers(outboxId, lockToken);
        ArgumentOutOfRangeException.ThrowIfLessThan(maximumAttempts, 1);
        if (retryDelay < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(retryDelay));
        }

        var message = await FindClaimedMessageAsync(outboxId, lockToken, cancellationToken);
        message.LastFailureCode = NormalizeFailureCode(failureCode);
        message.LockToken = null;
        message.LockExpiresAtUtc = null;
        if (message.AttemptCount >= maximumAttempts)
        {
            message.DeadLetteredAtUtc = DateTimeOffset.UtcNow;
        }
        else
        {
            message.AvailableAtUtc = DateTimeOffset.UtcNow.Add(retryDelay);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private Task<Domain.Models.IntegrationOutboxMessage> FindClaimedMessageAsync(
        Guid outboxId,
        Guid lockToken,
        CancellationToken cancellationToken) =>
        _dbContext.IntegrationOutboxMessages.SingleAsync(
            message => message.Id == outboxId && message.LockToken == lockToken,
            cancellationToken);

    private static IntegrationEventEnvelope DeserializeEnvelope(string payload)
    {
        try
        {
            return JsonSerializer.Deserialize<IntegrationEventEnvelope>(
                payload,
                IntegrationJsonOptions.Default)
                ?? throw new InvalidOperationException("The outbox envelope is empty.");
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException("The outbox envelope is invalid.", exception);
        }
    }

    private static void ValidateLockIdentifiers(Guid outboxId, Guid lockToken)
    {
        if (outboxId == Guid.Empty || lockToken == Guid.Empty)
        {
            throw new ArgumentException("Outbox and lock identifiers must be non-empty.");
        }
    }

    private static string NormalizeFailureCode(string failureCode)
    {
        var normalized = string.IsNullOrWhiteSpace(failureCode)
            ? "UnclassifiedFailure"
            : failureCode.Trim();
        return normalized.Length <= 200 ? normalized : normalized[..200];
    }
}

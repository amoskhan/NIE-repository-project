using Application.Integration;
using Contracts.Integration;
using Domain.Models;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Integrations;

public sealed class EfIntegrationEventProcessor(
    MainDbContext dbContext,
    IIntegrationEventDispatcher dispatcher) : IIntegrationEventProcessor
{
    private readonly MainDbContext _dbContext = dbContext;
    private readonly IIntegrationEventDispatcher _dispatcher = dispatcher;

    public Task<IntegrationEventProcessingResult> ProcessAsync(
        IntegrationEventEnvelope envelope,
        string consumer,
        CancellationToken cancellationToken) =>
        ProcessCoreAsync(envelope, consumer, cancellationToken);

    private async Task<IntegrationEventProcessingResult> ProcessCoreAsync(
        IntegrationEventEnvelope envelope,
        string consumer,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        if (envelope.MessageId == Guid.Empty || envelope.MessageId.Version != 7)
        {
            throw new PermanentIntegrationEventException("The integration message ID must be UUIDv7.");
        }

        if (string.IsNullOrWhiteSpace(consumer))
        {
            throw new ArgumentException("The consumer name is required.", nameof(consumer));
        }

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var alreadyProcessed = await _dbContext.IntegrationInboxMessages
                .AsNoTracking()
                .AnyAsync(
                    message => message.MessageId == envelope.MessageId && message.Consumer == consumer,
                    cancellationToken);
            if (alreadyProcessed)
            {
                await transaction.RollbackAsync(cancellationToken);
                return IntegrationEventProcessingResult.Duplicate;
            }

            await _dispatcher.DispatchAsync(envelope, consumer, cancellationToken);
            _dbContext.IntegrationInboxMessages.Add(new IntegrationInboxMessage
            {
                MessageId = envelope.MessageId,
                Consumer = consumer,
                EventName = envelope.EventName,
                ProcessedAtUtc = DateTimeOffset.UtcNow,
            });
            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return IntegrationEventProcessingResult.Processed;
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            _dbContext.ChangeTracker.Clear();
            throw;
        }
    }
}

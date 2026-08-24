using System.Text.Json;
using Application.Integration;
using Contracts.Integration;
using Domain.Models;
using Infrastructure.Persistence;

namespace Infrastructure.Integrations;

public sealed class EfIntegrationEventPublisher(
    MainDbContext dbContext,
    string applicationKey) : IIntegrationEventPublisher
{
    private readonly MainDbContext _dbContext = dbContext;
    private readonly string _applicationKey = applicationKey;

    public ValueTask EnqueueAsync<TEvent>(
        IntegrationContractDescriptor contract,
        TEvent payload,
        string? correlationId = null,
        string? causationId = null,
        CancellationToken cancellationToken = default)
        where TEvent : class
    {
        cancellationToken.ThrowIfCancellationRequested();
        var envelope = IntegrationEventEnvelopeFactory.Create(
            contract,
            _applicationKey,
            payload,
            correlationId,
            causationId);
        var serializedEnvelope = JsonSerializer.Serialize(envelope, IntegrationJsonOptions.Default);

        _dbContext.IntegrationOutboxMessages.Add(new IntegrationOutboxMessage
        {
            MessageId = envelope.MessageId,
            EventName = envelope.EventName,
            EventVersion = envelope.EventVersion,
            Producer = envelope.Producer,
            CorrelationId = envelope.CorrelationId,
            CausationId = envelope.CausationId,
            Payload = serializedEnvelope,
            OccurredAtUtc = envelope.OccurredAtUtc,
            AvailableAtUtc = envelope.OccurredAtUtc,
        });

        return ValueTask.CompletedTask;
    }
}

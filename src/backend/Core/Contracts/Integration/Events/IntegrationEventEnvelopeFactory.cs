using System.Diagnostics;
using System.Text.Json;

namespace Contracts.Integration;

/// <summary>
/// Creates validated event envelopes without depending on a message broker.
/// </summary>
public static class IntegrationEventEnvelopeFactory
{
    /// <summary>
    /// Creates an envelope for a catalogued payload.
    /// </summary>
    public static IntegrationEventEnvelope Create<TEvent>(
        IntegrationContractDescriptor contract,
        string producer,
        TEvent payload,
        string? correlationId = null,
        string? causationId = null,
        DateTimeOffset? occurredAtUtc = null,
        Guid? messageId = null)
        where TEvent : class
    {
        ArgumentNullException.ThrowIfNull(contract);
        ArgumentNullException.ThrowIfNull(payload);

        if (contract.ContractType != typeof(TEvent))
        {
            throw new ArgumentException(
                $"The payload type {typeof(TEvent).FullName} does not match {contract.ContractType.FullName}.",
                nameof(payload));
        }

        if (string.IsNullOrWhiteSpace(contract.Name) || contract.Version < 1)
        {
            throw new ArgumentException("The event contract name and version must be valid.", nameof(contract));
        }

        if (string.IsNullOrWhiteSpace(producer))
        {
            throw new ArgumentException("The producer application key is required.", nameof(producer));
        }

        var resolvedMessageId = messageId ?? Guid.CreateVersion7();
        if (resolvedMessageId == Guid.Empty || resolvedMessageId.Version != 7)
        {
            throw new ArgumentException("The message ID must be a non-empty UUIDv7.", nameof(messageId));
        }

        return new IntegrationEventEnvelope
        {
            MessageId = resolvedMessageId,
            EventName = contract.Name,
            EventVersion = contract.Version,
            Producer = producer.Trim(),
            OccurredAtUtc = (occurredAtUtc ?? DateTimeOffset.UtcNow).ToUniversalTime(),
            CorrelationId = string.IsNullOrWhiteSpace(correlationId)
                ? resolvedMessageId.ToString("D")
                : correlationId.Trim(),
            CausationId = string.IsNullOrWhiteSpace(causationId) ? null : causationId.Trim(),
            TraceParent = Activity.Current?.Id,
            TraceState = Activity.Current?.TraceStateString,
            Data = JsonSerializer.SerializeToElement(payload, IntegrationJsonOptions.Default),
        };
    }
}

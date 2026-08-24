using System.Text.Json;
using Application.Integration;
using Contracts.Integration;

namespace Infrastructure.Integration.RabbitMq;

public static class RabbitMqDeliveryEnvelopeParser
{
    public static IntegrationEventEnvelope Parse(
        ReadOnlySpan<byte> body,
        IntegrationContractDescriptor contract,
        int maximumMessageBytes)
    {
        ArgumentNullException.ThrowIfNull(contract);
        if (body.Length == 0 || body.Length > maximumMessageBytes)
        {
            throw new PermanentIntegrationEventException(
                "The integration event body is empty or exceeds the configured message-size limit.");
        }

        IntegrationEventEnvelope envelope;
        try
        {
            envelope = JsonSerializer.Deserialize<IntegrationEventEnvelope>(
                body,
                IntegrationJsonOptions.Default)
                ?? throw new JsonException("The integration event envelope is empty.");
        }
        catch (JsonException exception)
        {
            throw new PermanentIntegrationEventException(
                "The integration event envelope is not valid JSON.",
                exception);
        }

        if (envelope.MessageId == Guid.Empty || envelope.MessageId.Version != 7)
        {
            throw new PermanentIntegrationEventException("The integration message ID must be UUIDv7.");
        }

        if (!string.Equals(envelope.EventName, contract.Name, StringComparison.Ordinal)
            || envelope.EventVersion != contract.Version)
        {
            throw new PermanentIntegrationEventException(
                "The integration event does not match the subscribed contract name and version.");
        }

        if (string.IsNullOrWhiteSpace(envelope.Producer)
            || string.IsNullOrWhiteSpace(envelope.CorrelationId)
            || envelope.OccurredAtUtc == default
            || envelope.Data.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
        {
            throw new PermanentIntegrationEventException(
                "The integration event is missing mandatory envelope metadata.");
        }

        if ((envelope.TraceParent?.Length ?? 0) > 128
            || (envelope.TraceState?.Length ?? 0) > 512)
        {
            throw new PermanentIntegrationEventException(
                "The integration event tracing metadata exceeds supported limits.");
        }

        return envelope;
    }
}

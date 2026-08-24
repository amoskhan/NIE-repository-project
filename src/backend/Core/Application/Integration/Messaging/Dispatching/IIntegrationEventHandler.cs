using System.Text.Json;
using Contracts.Integration;

namespace Application.Integration;

/// <summary>
/// Type-erased handler contract used by the integration dispatcher.
/// </summary>
public interface IIntegrationEventHandler
{
    IntegrationContractDescriptor Contract { get; }

    Task HandleAsync(
        JsonElement payload,
        IntegrationEventContext context,
        CancellationToken cancellationToken);
}

/// <summary>
/// Strongly typed JSON event-handler base class.
/// </summary>
public abstract class IntegrationEventHandler<TEvent> : IIntegrationEventHandler
    where TEvent : class
{
    public abstract IntegrationContractDescriptor Contract { get; }

    public async Task HandleAsync(
        JsonElement payload,
        IntegrationEventContext context,
        CancellationToken cancellationToken)
    {
        try
        {
            var typedPayload = payload.Deserialize<TEvent>(IntegrationJsonSerializer.Options)
                ?? throw new PermanentIntegrationEventException("The integration event payload was empty.");
            await HandleAsync(typedPayload, context, cancellationToken);
        }
        catch (JsonException exception)
        {
            throw new PermanentIntegrationEventException(
                $"The payload does not conform to {typeof(TEvent).FullName}.",
                exception);
        }
    }

    protected abstract Task HandleAsync(
        TEvent payload,
        IntegrationEventContext context,
        CancellationToken cancellationToken);
}

using Contracts.Integration;

namespace Application.Integration;

public sealed class IntegrationEventDispatcher(
    IEnumerable<IIntegrationEventHandler> handlers) : IIntegrationEventDispatcher
{
    private readonly IReadOnlyList<IIntegrationEventHandler> _handlers = handlers.ToArray();

    public Task DispatchAsync(
        IntegrationEventEnvelope envelope,
        string consumer,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        if (string.IsNullOrWhiteSpace(consumer))
        {
            throw new ArgumentException("The consumer name is required.", nameof(consumer));
        }

        var matches = _handlers
            .Where(handler =>
                handler.Contract.Name.Equals(envelope.EventName, StringComparison.Ordinal)
                && handler.Contract.Version == envelope.EventVersion)
            .ToArray();

        if (matches.Length == 0)
        {
            throw new PermanentIntegrationEventException(
                $"No handler is registered for {envelope.EventName} v{envelope.EventVersion}.");
        }

        if (matches.Length > 1)
        {
            throw new InvalidOperationException(
                $"Multiple handlers are registered for {envelope.EventName} v{envelope.EventVersion}.");
        }

        var context = new IntegrationEventContext(
            envelope.MessageId,
            envelope.EventName,
            envelope.EventVersion,
            envelope.Producer,
            envelope.OccurredAtUtc,
            envelope.CorrelationId,
            envelope.CausationId,
            consumer);

        return matches[0].HandleAsync(envelope.Data, context, cancellationToken);
    }
}

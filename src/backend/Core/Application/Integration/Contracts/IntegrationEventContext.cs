namespace Application.Integration;

/// <summary>
/// Broker-independent metadata supplied to integration event handlers.
/// </summary>
public sealed record IntegrationEventContext(
    Guid MessageId,
    string EventName,
    int EventVersion,
    string Producer,
    DateTimeOffset OccurredAtUtc,
    string CorrelationId,
    string? CausationId,
    string Consumer);

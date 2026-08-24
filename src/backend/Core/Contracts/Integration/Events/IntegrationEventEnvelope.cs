using System.Text.Json;

namespace Contracts.Integration;

/// <summary>
/// Transport-neutral metadata envelope for an asynchronous integration event.
/// </summary>
public sealed record IntegrationEventEnvelope
{
    public required Guid MessageId { get; init; }

    public required string EventName { get; init; }

    public required int EventVersion { get; init; }

    public required string Producer { get; init; }

    public required DateTimeOffset OccurredAtUtc { get; init; }

    public required string CorrelationId { get; init; }

    public string? CausationId { get; init; }

    public string? TraceParent { get; init; }

    public string? TraceState { get; init; }

    public required JsonElement Data { get; init; }
}

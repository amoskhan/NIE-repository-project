using Contracts.Integration;

namespace Application.Integration;

/// <summary>
/// Enqueues events transactionally without exposing a broker SDK to application code.
/// </summary>
public interface IIntegrationEventPublisher
{
    ValueTask EnqueueAsync<TEvent>(
        IntegrationContractDescriptor contract,
        TEvent payload,
        string? correlationId = null,
        string? causationId = null,
        CancellationToken cancellationToken = default)
        where TEvent : class;
}

using Contracts.Integration;

namespace Application.Integration;

public sealed class DisabledIntegrationEventPublisher : IIntegrationEventPublisher
{
    public ValueTask EnqueueAsync<TEvent>(
        IntegrationContractDescriptor contract,
        TEvent payload,
        string? correlationId = null,
        string? causationId = null,
        CancellationToken cancellationToken = default)
        where TEvent : class => ValueTask.CompletedTask;
}

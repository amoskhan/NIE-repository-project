using Contracts.Integration;

namespace Application.Integration;

public interface IIntegrationEventDispatcher
{
    Task DispatchAsync(
        IntegrationEventEnvelope envelope,
        string consumer,
        CancellationToken cancellationToken);
}

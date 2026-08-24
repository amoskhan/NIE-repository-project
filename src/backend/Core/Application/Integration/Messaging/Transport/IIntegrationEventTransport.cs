using Contracts.Integration;

namespace Application.Integration;

public interface IIntegrationEventTransport
{
    Task PublishAsync(IntegrationEventEnvelope envelope, CancellationToken cancellationToken);
}

using Contracts.Integration;

namespace Application.Integration;

public enum IntegrationEventProcessingResult
{
    Processed,
    Duplicate,
}

public interface IIntegrationEventProcessor
{
    Task<IntegrationEventProcessingResult> ProcessAsync(
        IntegrationEventEnvelope envelope,
        string consumer,
        CancellationToken cancellationToken);
}

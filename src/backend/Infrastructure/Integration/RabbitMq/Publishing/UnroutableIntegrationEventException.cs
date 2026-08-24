namespace Infrastructure.Integration.RabbitMq;

public sealed class UnroutableIntegrationEventException : Exception
{
    public UnroutableIntegrationEventException(string eventName, Exception innerException)
        : base($"No RabbitMQ queue accepted integration event '{eventName}'.", innerException)
    {
    }
}

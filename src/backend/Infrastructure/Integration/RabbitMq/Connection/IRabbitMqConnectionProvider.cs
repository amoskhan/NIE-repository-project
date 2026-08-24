using RabbitMQ.Client;

namespace Infrastructure.Integration.RabbitMq;

public interface IRabbitMqConnectionProvider
{
    Task<IConnection> GetConnectionAsync(CancellationToken cancellationToken);
}

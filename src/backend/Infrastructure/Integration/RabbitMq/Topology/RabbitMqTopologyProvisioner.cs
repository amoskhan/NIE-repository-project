using RabbitMQ.Client;

namespace Infrastructure.Integration.RabbitMq;

public static class RabbitMqTopologyProvisioner
{
    public static async Task ApplyAsync(
        IChannel channel,
        RabbitMqSubscriptionTopology topology,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(channel);
        ArgumentNullException.ThrowIfNull(topology);

        foreach (var exchange in new[]
                 {
                     topology.EventExchange,
                     topology.RetryExchange,
                     topology.DeadLetterExchange,
                 })
        {
            await channel.ExchangeDeclareAsync(
                exchange.Name,
                exchange.Type,
                exchange.Durable,
                autoDelete: false,
                arguments: null,
                cancellationToken: cancellationToken);
        }

        foreach (var queue in new[]
                 {
                     topology.MainQueue,
                     topology.RetryQueue,
                     topology.DeadLetterQueue,
                 })
        {
            await channel.QueueDeclareAsync(
                queue.Name,
                queue.Durable,
                exclusive: false,
                autoDelete: false,
                queue.Arguments.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal),
                cancellationToken: cancellationToken);
        }

        foreach (var binding in topology.Bindings)
        {
            await channel.QueueBindAsync(
                binding.Queue,
                binding.Exchange,
                binding.RoutingKey,
                arguments: null,
                cancellationToken: cancellationToken);
        }
    }
}

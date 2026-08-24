using Contracts.Integration;
using Infrastructure.Integration.Options;

namespace Infrastructure.Integration.RabbitMq;

public sealed record RabbitMqExchangeDefinition(string Name, string Type, bool Durable);

public sealed record RabbitMqQueueDefinition(
    string Name,
    bool Durable,
    IReadOnlyDictionary<string, object?> Arguments);

public sealed record RabbitMqBindingDefinition(string Queue, string Exchange, string RoutingKey);

public sealed record RabbitMqSubscriptionTopology(
    RabbitMqExchangeDefinition EventExchange,
    RabbitMqExchangeDefinition RetryExchange,
    RabbitMqExchangeDefinition DeadLetterExchange,
    RabbitMqQueueDefinition MainQueue,
    RabbitMqQueueDefinition RetryQueue,
    RabbitMqQueueDefinition DeadLetterQueue,
    IReadOnlyList<RabbitMqBindingDefinition> Bindings);

public static class RabbitMqTopologyPlan
{
    public static RabbitMqSubscriptionTopology Create(
        RabbitMqOptions options,
        IntegrationContractDescriptor contract)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(contract);

        var eventExchange = new RabbitMqExchangeDefinition(options.Exchange, "topic", true);
        var retryExchange = new RabbitMqExchangeDefinition($"{options.Exchange}.retry", "direct", true);
        var deadLetterExchange = new RabbitMqExchangeDefinition($"{options.Exchange}.dead-letter", "direct", true);
        var queueName = $"{options.QueuePrefix}.{contract.Name}.v{contract.Version}";

        var mainQueue = new RabbitMqQueueDefinition(
            queueName,
            true,
            new Dictionary<string, object?>
            {
                ["x-queue-type"] = "quorum",
                ["x-dead-letter-strategy"] = "at-least-once",
                ["x-overflow"] = "reject-publish",
                ["x-dead-letter-exchange"] = deadLetterExchange.Name,
                ["x-dead-letter-routing-key"] = queueName,
                ["x-message-ttl"] = checked(options.MaximumReplayWindowDays * 86_400_000L),
            });
        var retryQueue = new RabbitMqQueueDefinition(
            $"{queueName}.retry",
            true,
            new Dictionary<string, object?>
            {
                ["x-queue-type"] = "quorum",
                ["x-dead-letter-strategy"] = "at-least-once",
                ["x-overflow"] = "reject-publish",
                ["x-message-ttl"] = checked(options.RetryDelaySeconds * 1000),
                ["x-dead-letter-exchange"] = eventExchange.Name,
                ["x-dead-letter-routing-key"] = IntegrationContractRoutingKey.Create(
                    contract.Name,
                    contract.Version),
            });
        var deadLetterQueue = new RabbitMqQueueDefinition(
            $"{queueName}.dead-letter",
            true,
            new Dictionary<string, object?>
            {
                ["x-queue-type"] = "quorum",
            });

        return new RabbitMqSubscriptionTopology(
            eventExchange,
            retryExchange,
            deadLetterExchange,
            mainQueue,
            retryQueue,
            deadLetterQueue,
            [
                new RabbitMqBindingDefinition(
                    mainQueue.Name,
                    eventExchange.Name,
                    IntegrationContractRoutingKey.Create(contract.Name, contract.Version)),
                new RabbitMqBindingDefinition(retryQueue.Name, retryExchange.Name, mainQueue.Name),
                new RabbitMqBindingDefinition(deadLetterQueue.Name, deadLetterExchange.Name, mainQueue.Name),
            ]);
    }
}

using Application.Integration;
using Contracts.Integration;
using Infrastructure.Integration.Options;
using Infrastructure.Integration.RabbitMq;

namespace Integration.Tests;

public class RabbitMqTopologyPlanTests
{
    [Fact]
    public void Subscription_topology_is_durable_quorum_and_dead_lettered()
    {
        var options = new RabbitMqOptions
        {
            Exchange = "nie.events",
            QueuePrefix = "procurement",
            RetryDelaySeconds = 45,
            MaximumReplayWindowDays = 7,
        };

        var topology = RabbitMqTopologyPlan.Create(
            options,
            IntegrationContractCatalog.VendorProfileChanged);

        Assert.Equal("topic", topology.EventExchange.Type);
        Assert.True(topology.EventExchange.Durable);
        Assert.Equal("quorum", topology.MainQueue.Arguments["x-queue-type"]);
        Assert.Equal("at-least-once", topology.MainQueue.Arguments["x-dead-letter-strategy"]);
        Assert.Equal("reject-publish", topology.MainQueue.Arguments["x-overflow"]);
        Assert.Equal(topology.DeadLetterExchange.Name, topology.MainQueue.Arguments["x-dead-letter-exchange"]);
        Assert.Equal(604_800_000L, topology.MainQueue.Arguments["x-message-ttl"]);
        Assert.Equal("at-least-once", topology.RetryQueue.Arguments["x-dead-letter-strategy"]);
        Assert.Equal("reject-publish", topology.RetryQueue.Arguments["x-overflow"]);
        Assert.Equal(45_000, topology.RetryQueue.Arguments["x-message-ttl"]);
        Assert.Equal(topology.EventExchange.Name, topology.RetryQueue.Arguments["x-dead-letter-exchange"]);
        Assert.Contains(topology.Bindings, binding =>
            binding.Queue == topology.MainQueue.Name
            && binding.RoutingKey == "nie.vendor-master.vendor-profile.changed.v1");
    }

    [Theory]
    [InlineData(0, 5, RabbitMqDeliveryDecision.Retry)]
    [InlineData(3, 5, RabbitMqDeliveryDecision.Retry)]
    [InlineData(4, 5, RabbitMqDeliveryDecision.DeadLetter)]
    public void Transient_failures_retry_only_within_the_delivery_budget(
        int completedRetries,
        int maximumAttempts,
        RabbitMqDeliveryDecision expected)
    {
        var decision = RabbitMqDeliveryPolicy.Decide(
            new TimeoutException("transient"),
            completedRetries,
            maximumAttempts);

        Assert.Equal(expected, decision);
    }

    [Fact]
    public void Permanent_failures_are_dead_lettered_without_retry()
    {
        var decision = RabbitMqDeliveryPolicy.Decide(
            new PermanentIntegrationEventException("invalid contract"),
            completedRetries: 0,
            maximumDeliveryAttempts: 5);

        Assert.Equal(RabbitMqDeliveryDecision.DeadLetter, decision);
    }
}

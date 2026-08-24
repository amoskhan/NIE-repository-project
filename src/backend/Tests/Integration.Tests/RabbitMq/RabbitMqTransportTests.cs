using System.Text.Json;
using Contracts.Events.Procurement;
using Contracts.Integration;
using Infrastructure.Integration.Options;
using Infrastructure.Integration.RabbitMq;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Integration.Tests;

public class RabbitMqTransportTests
{
    [Fact]
    public async Task Publisher_confirms_a_persistent_routable_event()
    {
        var options = CreateOptions();
        await using var provider = new RabbitMqConnectionProvider(Options.Create(options));
        await using var transport = new RabbitMqEventTransport(provider, Options.Create(options));
        var connection = await provider.GetConnectionAsync(TestContext.Current.CancellationToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: TestContext.Current.CancellationToken);
        var topology = RabbitMqTopologyPlan.Create(
            options.RabbitMq,
            IntegrationContractCatalog.PurchaseOrderStatusChanged);
        await RabbitMqTopologyProvisioner.ApplyAsync(
            channel,
            topology,
            TestContext.Current.CancellationToken);
        var envelope = CreateEnvelope();

        await transport.PublishAsync(envelope, TestContext.Current.CancellationToken);

        var delivery = await channel.BasicGetAsync(
            topology.MainQueue.Name,
            autoAck: true,
            TestContext.Current.CancellationToken);
        Assert.NotNull(delivery);
        Assert.Equal(DeliveryModes.Persistent, delivery.BasicProperties.DeliveryMode);
        Assert.Equal(envelope.MessageId.ToString("D"), delivery.BasicProperties.MessageId);
        var received = JsonSerializer.Deserialize<IntegrationEventEnvelope>(
            delivery.Body.Span,
            IntegrationJsonOptions.Default);
        Assert.NotNull(received);
        Assert.Equal(envelope.MessageId, received.MessageId);
        Assert.Equal(envelope.EventName, received.EventName);
        Assert.Equal(envelope.EventVersion, received.EventVersion);
        Assert.Equal(envelope.Producer, received.Producer);
        Assert.Equal(
            envelope.Data.GetProperty("purchaseOrderId").GetGuid(),
            received.Data.GetProperty("purchaseOrderId").GetGuid());
    }

    [Fact]
    public async Task Publisher_rejects_an_unroutable_mandatory_event()
    {
        var options = CreateOptions();
        options.RabbitMq.Exchange = $"nie.events.{Guid.CreateVersion7():N}";
        await using var provider = new RabbitMqConnectionProvider(Options.Create(options));
        await using var transport = new RabbitMqEventTransport(provider, Options.Create(options));

        await Assert.ThrowsAsync<UnroutableIntegrationEventException>(() =>
            transport.PublishAsync(CreateEnvelope(), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Retry_and_dead_letter_topology_routes_through_durable_queues()
    {
        var options = CreateOptions();
        options.RabbitMq.RetryDelaySeconds = 1;
        await using var provider = new RabbitMqConnectionProvider(Options.Create(options));
        var connection = await provider.GetConnectionAsync(TestContext.Current.CancellationToken);
        await using var channel = await connection.CreateChannelAsync(
            cancellationToken: TestContext.Current.CancellationToken);
        var topology = RabbitMqTopologyPlan.Create(
            options.RabbitMq,
            IntegrationContractCatalog.VendorProfileChanged);
        await RabbitMqTopologyProvisioner.ApplyAsync(
            channel,
            topology,
            TestContext.Current.CancellationToken);
        var properties = new BasicProperties { DeliveryMode = DeliveryModes.Persistent };

        await channel.BasicPublishAsync(
            topology.RetryExchange.Name,
            topology.MainQueue.Name,
            mandatory: true,
            properties,
            "retry-me"u8.ToArray(),
            TestContext.Current.CancellationToken);

        var retried = await WaitForMessageAsync(
            channel,
            topology.MainQueue.Name,
            autoAck: false,
            TestContext.Current.CancellationToken);
        Assert.NotNull(retried);
        await channel.BasicRejectAsync(
            retried.DeliveryTag,
            requeue: false,
            TestContext.Current.CancellationToken);

        var deadLetter = await WaitForMessageAsync(
            channel,
            topology.DeadLetterQueue.Name,
            autoAck: true,
            TestContext.Current.CancellationToken);
        Assert.NotNull(deadLetter);
        Assert.Equal("retry-me", System.Text.Encoding.UTF8.GetString(deadLetter.Body.Span));
    }

    private static async Task<BasicGetResult?> WaitForMessageAsync(
        IChannel channel,
        string queue,
        bool autoAck,
        CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 30; attempt++)
        {
            var delivery = await channel.BasicGetAsync(queue, autoAck, cancellationToken);
            if (delivery is not null)
            {
                return delivery;
            }

            await Task.Delay(100, cancellationToken);
        }

        return null;
    }

    private static ServiceIntegrationOptions CreateOptions()
    {
        var connectionString = RabbitMqTestBroker.RequireConnectionString();
        return new ServiceIntegrationOptions
        {
            Enabled = true,
            ApplicationKey = "procurement",
            RabbitMq = new RabbitMqOptions
            {
                Enabled = true,
                ConnectionString = connectionString,
                Exchange = $"nie.events.{Guid.CreateVersion7():N}",
                QueuePrefix = $"procurement-tests-{Guid.CreateVersion7():N}",
            },
        };
    }

    private static IntegrationEventEnvelope CreateEnvelope()
    {
        var payload = new PurchaseOrderStatusChangedV1(
            Guid.CreateVersion7(),
            "PO-2026-00001",
            "Draft",
            "PendingManagerApproval",
            1250m,
            "SGD",
            DateTimeOffset.UtcNow);
        return IntegrationEventEnvelopeFactory.Create(
            IntegrationContractCatalog.PurchaseOrderStatusChanged,
            "procurement",
            payload);
    }
}

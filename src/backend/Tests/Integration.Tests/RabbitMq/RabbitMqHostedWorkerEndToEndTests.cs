using System.Text.Json;
using Application.Abstractions;
using Application.Integration;
using Application.Integration.Validation;
using Contracts.Events.Procurement;
using Contracts.Events.VendorMaster;
using Contracts.Integration;
using Domain.Models;
using FluentValidation;
using Infrastructure.Integration.Options;
using Infrastructure.Integration.RabbitMq;
using Infrastructure.Integrations;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Integration.Tests;

public sealed class RabbitMqHostedWorkerEndToEndTests
{
    [Fact]
    public async Task Outbox_worker_publishes_and_marks_the_row_only_after_broker_confirmation()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var options = CreateOptions();
        await using var database = await PostgresTestDatabase.CreateAsync(cancellationToken);
        Guid outboxId;
        await using (var setupContext = database.CreateContext())
        {
            await setupContext.Database.EnsureCreatedAsync(cancellationToken);
            var publisher = new EfIntegrationEventPublisher(setupContext, options.ApplicationKey);
            await publisher.EnqueueAsync(
                IntegrationContractCatalog.PurchaseOrderStatusChanged,
                CreatePurchaseOrderPayload(),
                cancellationToken: cancellationToken);
            await setupContext.SaveChangesAsync(cancellationToken);
            outboxId = await setupContext.IntegrationOutboxMessages
                .Select(message => message.Id)
                .SingleAsync(cancellationToken);
        }

        await using var connectionProvider = new RabbitMqConnectionProvider(Options.Create(options));
        await using var transport = new RabbitMqEventTransport(
            connectionProvider,
            Options.Create(options));
        var connection = await connectionProvider.GetConnectionAsync(cancellationToken);
        await using var inspectionChannel = await connection.CreateChannelAsync(
            cancellationToken: cancellationToken);
        var topology = RabbitMqTopologyPlan.Create(
            options.RabbitMq,
            IntegrationContractCatalog.PurchaseOrderStatusChanged);
        await RabbitMqTopologyProvisioner.ApplyAsync(
            inspectionChannel,
            topology,
            cancellationToken);

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScoped<MainDbContext>(_ => database.CreateContext());
        services.AddScoped<IIntegrationOutboxStore, EfIntegrationOutboxStore>();
        using var serviceProvider = services.BuildServiceProvider();
        using var worker = new IntegrationOutboxPublisherWorker(
            serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            transport,
            Options.Create(options),
            NullLogger<IntegrationOutboxPublisherWorker>.Instance);

        await worker.StartAsync(cancellationToken);
        try
        {
            await WaitUntilAsync(async () =>
            {
                await using var context = database.CreateContext();
                return await context.IntegrationOutboxMessages
                    .AnyAsync(
                        message => message.Id == outboxId && message.PublishedAtUtc != null,
                        cancellationToken);
            }, cancellationToken);

            var delivery = await WaitForMessageAsync(
                inspectionChannel,
                topology.MainQueue.Name,
                cancellationToken);
            Assert.NotNull(delivery);
            Assert.Equal(DeliveryModes.Persistent, delivery.BasicProperties.DeliveryMode);
        }
        finally
        {
            await worker.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task Consumer_worker_acks_valid_events_and_dead_letters_permanent_contract_failures()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var options = CreateOptions();
        await using var database = await PostgresTestDatabase.CreateAsync(cancellationToken);
        await using (var setupContext = database.CreateContext())
        {
            await setupContext.Database.EnsureCreatedAsync(cancellationToken);
        }

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScoped<MainDbContext>(_ => database.CreateContext());
        services.AddScoped<IApplicationDbContext>(provider =>
            provider.GetRequiredService<MainDbContext>());
        services.AddScoped<IValidator<VendorProfileChangedV1>, VendorProfileChangedV1Validator>();
        services.AddScoped<IIntegrationEventHandler, VendorProfileChangedIntegrationEventHandler>();
        services.AddScoped<IIntegrationEventDispatcher, IntegrationEventDispatcher>();
        services.AddScoped<IIntegrationEventProcessor, EfIntegrationEventProcessor>();
        using var serviceProvider = services.BuildServiceProvider();

        await using var connectionProvider = new RabbitMqConnectionProvider(Options.Create(options));
        await using var transport = new RabbitMqEventTransport(
            connectionProvider,
            Options.Create(options));
        var subscriptionState = new RabbitMqSubscriptionState();
        using var worker = new RabbitMqConsumerWorker(
            connectionProvider,
            serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(options),
            subscriptionState,
            NullLogger<RabbitMqConsumerWorker>.Instance);
        var topology = RabbitMqTopologyPlan.Create(
            options.RabbitMq,
            IntegrationContractCatalog.VendorProfileChanged);

        await worker.StartAsync(cancellationToken);
        try
        {
            await WaitUntilAsync(
                () => Task.FromResult(subscriptionState.Snapshot().IsReady),
                cancellationToken);

            var accepted = CreateVendorEnvelope("VENDOR-E2E", "End-to-end Supplier");
            await transport.PublishAsync(accepted, cancellationToken);
            await WaitUntilAsync(async () =>
            {
                await using var context = database.CreateContext();
                return await context.Vendors.AnyAsync(
                    vendor => vendor.Code == "VENDOR-E2E",
                    cancellationToken)
                    && await context.IntegrationInboxMessages.AnyAsync(
                        receipt => receipt.MessageId == accepted.MessageId,
                        cancellationToken);
            }, cancellationToken);

            var rejected = CreateVendorEnvelope(
                "VENDOR-INVALID",
                new string('x', 201));
            await transport.PublishAsync(rejected, cancellationToken);
            var connection = await connectionProvider.GetConnectionAsync(cancellationToken);
            await using var inspectionChannel = await connection.CreateChannelAsync(
                cancellationToken: cancellationToken);
            var deadLetter = await WaitForMessageAsync(
                inspectionChannel,
                topology.DeadLetterQueue.Name,
                cancellationToken);

            Assert.NotNull(deadLetter);
            var deadLetterEnvelope = JsonSerializer.Deserialize<IntegrationEventEnvelope>(
                deadLetter.Body.Span,
                IntegrationJsonOptions.Default);
            Assert.NotNull(deadLetterEnvelope);
            Assert.Equal(rejected.MessageId, deadLetterEnvelope.MessageId);
            await using var verifyContext = database.CreateContext();
            Assert.False(await verifyContext.IntegrationInboxMessages.AnyAsync(
                receipt => receipt.MessageId == rejected.MessageId,
                cancellationToken));
            Assert.False(await verifyContext.Vendors.AnyAsync(
                vendor => vendor.Code == "VENDOR-INVALID",
                cancellationToken));
        }
        finally
        {
            await worker.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task Retention_worker_runs_an_immediate_bounded_prune_sweep()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var options = CreateOptions();
        await using var database = await PostgresTestDatabase.CreateAsync(cancellationToken);
        await using (var setupContext = database.CreateContext())
        {
            await setupContext.Database.EnsureCreatedAsync(cancellationToken);
            var old = DateTimeOffset.UtcNow.AddDays(-31);
            setupContext.IntegrationOutboxMessages.Add(new IntegrationOutboxMessage
            {
                MessageId = Guid.CreateVersion7(),
                EventName = IntegrationContractCatalog.PurchaseOrderStatusChanged.Name,
                EventVersion = IntegrationContractCatalog.PurchaseOrderStatusChanged.Version,
                Producer = options.ApplicationKey,
                CorrelationId = Guid.CreateVersion7().ToString("D"),
                Payload = "{}",
                OccurredAtUtc = old,
                AvailableAtUtc = old,
                PublishedAtUtc = old,
            });
            setupContext.IntegrationInboxMessages.Add(new IntegrationInboxMessage
            {
                MessageId = Guid.CreateVersion7(),
                Consumer = "retention-worker-e2e",
                EventName = IntegrationContractCatalog.VendorProfileChanged.Name,
                ProcessedAtUtc = old,
            });
            await setupContext.SaveChangesAsync(cancellationToken);
        }

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScoped<MainDbContext>(_ => database.CreateContext());
        services.AddScoped<IIntegrationMessageRetentionStore, EfIntegrationMessageRetentionStore>();
        using var serviceProvider = services.BuildServiceProvider();
        using var worker = new IntegrationMessageRetentionWorker(
            serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(options),
            NullLogger<IntegrationMessageRetentionWorker>.Instance);

        await worker.StartAsync(cancellationToken);
        try
        {
            await WaitUntilAsync(async () =>
            {
                await using var context = database.CreateContext();
                return !await context.IntegrationOutboxMessages.AnyAsync(cancellationToken)
                    && !await context.IntegrationInboxMessages.AnyAsync(cancellationToken);
            }, cancellationToken);
        }
        finally
        {
            await worker.StopAsync(CancellationToken.None);
        }
    }

    private static ServiceIntegrationOptions CreateOptions()
    {
        var connectionString = RabbitMqTestBroker.RequireConnectionString();
        return new ServiceIntegrationOptions
        {
            Enabled = true,
            ApplicationKey = "procurement-worker-tests",
            RabbitMq = new RabbitMqOptions
            {
                Enabled = true,
                ConnectionString = connectionString,
                Exchange = $"nie.events.{Guid.CreateVersion7():N}",
                QueuePrefix = $"procurement-worker-tests-{Guid.CreateVersion7():N}",
                RetryDelaySeconds = 1,
            },
            Outbox = new IntegrationOutboxOptions
            {
                PollIntervalMilliseconds = 50,
                BatchSize = 10,
                LeaseSeconds = 5,
                MaximumAttempts = 3,
                PublishedRetentionDays = 30,
                InboxRetentionDays = 30,
                RetentionSweepMinutes = 360,
                RetentionBatchSize = 100,
                MetricsSampleSeconds = 60,
            },
        };
    }

    private static PurchaseOrderStatusChangedV1 CreatePurchaseOrderPayload() => new(
        Guid.CreateVersion7(),
        "PO-WORKER-E2E",
        "Draft",
        "PendingManagerApproval",
        500m,
        "SGD",
        DateTimeOffset.UtcNow);

    private static IntegrationEventEnvelope CreateVendorEnvelope(string code, string name) =>
        IntegrationEventEnvelopeFactory.Create(
            IntegrationContractCatalog.VendorProfileChanged,
            "vendor-master-worker-tests",
            new VendorProfileChangedV1(
                code,
                name,
                null,
                null,
                null,
                null,
                "Technology",
                true,
                DateTimeOffset.UtcNow));

    private static async Task WaitUntilAsync(
        Func<Task<bool>> condition,
        CancellationToken cancellationToken)
    {
        var timeoutAt = DateTimeOffset.UtcNow.AddSeconds(10);
        while (DateTimeOffset.UtcNow < timeoutAt)
        {
            if (await condition())
            {
                return;
            }

            await Task.Delay(100, cancellationToken);
        }

        throw new TimeoutException("The hosted-worker integration condition was not observed.");
    }

    private static async Task<BasicGetResult?> WaitForMessageAsync(
        IChannel channel,
        string queue,
        CancellationToken cancellationToken)
    {
        var timeoutAt = DateTimeOffset.UtcNow.AddSeconds(10);
        while (DateTimeOffset.UtcNow < timeoutAt)
        {
            var delivery = await channel.BasicGetAsync(queue, autoAck: true, cancellationToken);
            if (delivery is not null)
            {
                return delivery;
            }

            await Task.Delay(100, cancellationToken);
        }

        return null;
    }
}

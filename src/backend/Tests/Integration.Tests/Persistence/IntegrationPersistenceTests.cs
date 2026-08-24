using System.Text.Json;
using Contracts.Events.Procurement;
using Contracts.Integration;
using Domain.Models;
using Infrastructure.Integrations;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Integration.Tests;

public class IntegrationPersistenceTests
{
    [Fact]
    public async Task Enqueue_adds_a_uuid_v7_outbox_envelope_to_the_ambient_unit_of_work()
    {
        await using var context = CreateContext();
        var publisher = new EfIntegrationEventPublisher(context, "procurement");
        var payload = new PurchaseOrderStatusChangedV1(
            Guid.CreateVersion7(),
            "PO-2026-00001",
            "Draft",
            "PendingManagerApproval",
            1250m,
            "SGD",
            DateTimeOffset.UtcNow);

        await publisher.EnqueueAsync(
            IntegrationContractCatalog.PurchaseOrderStatusChanged,
            payload,
            correlationId: "trace-001",
            cancellationToken: TestContext.Current.CancellationToken);

        var entry = Assert.Single(context.ChangeTracker.Entries<IntegrationOutboxMessage>());
        Assert.Equal(EntityState.Added, entry.State);
        Assert.Equal(7, entry.Entity.Id.Version);
        Assert.Equal(7, entry.Entity.MessageId.Version);
        Assert.Equal("trace-001", entry.Entity.CorrelationId);
        Assert.Equal(0, entry.Entity.AttemptCount);
        var envelope = JsonSerializer.Deserialize<IntegrationEventEnvelope>(
            entry.Entity.Payload,
            IntegrationJsonOptions.Default);
        Assert.NotNull(envelope);
        Assert.Equal(payload.PurchaseOrderId, envelope.Data.GetProperty("purchaseOrderId").GetGuid());
    }

    [Fact]
    public async Task Persistence_model_enforces_message_and_consumer_idempotency()
    {
        await using var context = CreateContext();
        var entityType = context.Model.FindEntityType(typeof(IntegrationInboxMessage));

        Assert.NotNull(entityType);
        var uniqueIndex = Assert.Single(entityType.GetIndexes(), index => index.IsUnique);
        Assert.Equal(
            [nameof(IntegrationInboxMessage.MessageId), nameof(IntegrationInboxMessage.Consumer)],
            uniqueIndex.Properties.Select(property => property.Name));
        Assert.Equal(typeof(Guid), entityType.FindPrimaryKey()!.Properties.Single().ClrType);
    }

    private static MainDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<MainDbContext>()
            .UseNpgsql("Host=localhost;Database=integration_contract;Username=contract;Password=not-used")
            .Options;
        return new MainDbContext(options);
    }
}

using Application.Integration;
using Contracts.Events.Procurement;
using Contracts.Integration;
using Domain.Models;
using Infrastructure.Integrations;
using Microsoft.EntityFrameworkCore;

namespace Integration.Tests;

public class IntegrationOutboxStoreTests
{
    [Fact]
    public async Task Claims_are_leased_across_instances_and_can_be_completed_or_dead_lettered()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var database = await PostgresTestDatabase.CreateAsync(cancellationToken);
        await using (var setupContext = database.CreateContext())
        {
            await setupContext.Database.EnsureCreatedAsync(cancellationToken);
            var publisher = new EfIntegrationEventPublisher(setupContext, "procurement");
            await publisher.EnqueueAsync(
                IntegrationContractCatalog.PurchaseOrderStatusChanged,
                CreatePayload("PO-2026-00001"),
                cancellationToken: cancellationToken);
            await publisher.EnqueueAsync(
                IntegrationContractCatalog.PurchaseOrderStatusChanged,
                CreatePayload("PO-2026-00002"),
                cancellationToken: cancellationToken);
            await setupContext.SaveChangesAsync(cancellationToken);
        }

        await using var firstContext = database.CreateContext();
        await using var secondContext = database.CreateContext();
        var firstStore = new EfIntegrationOutboxStore(firstContext);
        var secondStore = new EfIntegrationOutboxStore(secondContext);

        var firstClaim = Assert.Single(await firstStore.ClaimAsync(1, TimeSpan.FromMinutes(1), cancellationToken));
        var secondClaim = Assert.Single(await secondStore.ClaimAsync(1, TimeSpan.FromMinutes(1), cancellationToken));

        Assert.NotEqual(firstClaim.OutboxId, secondClaim.OutboxId);
        Assert.Equal(7, firstClaim.LockToken.Version);
        Assert.Equal(1, firstClaim.AttemptCount);

        await firstStore.MarkPublishedAsync(firstClaim.OutboxId, firstClaim.LockToken, cancellationToken);
        await secondStore.MarkFailedAsync(
            secondClaim.OutboxId,
            secondClaim.LockToken,
            "TimeoutException",
            maximumAttempts: 1,
            retryDelay: TimeSpan.FromSeconds(1),
            cancellationToken);

        await using var verifyContext = database.CreateContext();
        var published = await verifyContext.IntegrationOutboxMessages
            .SingleAsync(message => message.Id == firstClaim.OutboxId, cancellationToken);
        var deadLettered = await verifyContext.IntegrationOutboxMessages
            .SingleAsync(message => message.Id == secondClaim.OutboxId, cancellationToken);
        Assert.NotNull(published.PublishedAtUtc);
        Assert.Null(published.LockToken);
        Assert.NotNull(deadLettered.DeadLetteredAtUtc);
        Assert.Equal("TimeoutException", deadLettered.LastFailureCode);
    }

    [Fact]
    public async Task Malformed_outbox_envelope_is_quarantined_without_blocking_a_valid_message()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var database = await PostgresTestDatabase.CreateAsync(cancellationToken);
        await using var context = database.CreateContext();
        await context.Database.EnsureCreatedAsync(cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var malformed = new IntegrationOutboxMessage
        {
            MessageId = Guid.CreateVersion7(),
            EventName = IntegrationContractCatalog.PurchaseOrderStatusChanged.Name,
            EventVersion = IntegrationContractCatalog.PurchaseOrderStatusChanged.Version,
            Producer = "procurement",
            CorrelationId = Guid.CreateVersion7().ToString(),
            Payload = "{}",
            OccurredAtUtc = now.AddSeconds(-1),
            AvailableAtUtc = now.AddSeconds(-1),
        };
        context.IntegrationOutboxMessages.Add(malformed);

        var publisher = new EfIntegrationEventPublisher(context, "procurement");
        await publisher.EnqueueAsync(
            IntegrationContractCatalog.PurchaseOrderStatusChanged,
            CreatePayload("PO-2026-POISON-TEST"),
            cancellationToken: cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        var store = new EfIntegrationOutboxStore(context);
        var claimed = Assert.Single(
            await store.ClaimAsync(10, TimeSpan.FromMinutes(1), cancellationToken));

        Assert.NotEqual(malformed.Id, claimed.OutboxId);
        await context.Entry(malformed).ReloadAsync(cancellationToken);
        Assert.NotNull(malformed.DeadLetteredAtUtc);
        Assert.Equal("InvalidOutboxEnvelope", malformed.LastFailureCode);
        Assert.Null(malformed.LockToken);
    }

    [Fact]
    public async Task Retention_prunes_only_old_published_messages_and_inbox_receipts()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var database = await PostgresTestDatabase.CreateAsync(cancellationToken);
        await using var context = database.CreateContext();
        await context.Database.EnsureCreatedAsync(cancellationToken);
        var publisher = new EfIntegrationEventPublisher(context, "procurement");
        await publisher.EnqueueAsync(
            IntegrationContractCatalog.PurchaseOrderStatusChanged,
            CreatePayload("PO-2026-00003"),
            cancellationToken: cancellationToken);
        await publisher.EnqueueAsync(
            IntegrationContractCatalog.PurchaseOrderStatusChanged,
            CreatePayload("PO-2026-00004"),
            cancellationToken: cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        var messages = await context.IntegrationOutboxMessages
            .OrderBy(message => message.OccurredAtUtc)
            .ToArrayAsync(cancellationToken);
        messages[0].PublishedAtUtc = DateTimeOffset.UtcNow.AddDays(-31);
        messages[1].PublishedAtUtc = DateTimeOffset.UtcNow;
        context.IntegrationInboxMessages.AddRange(
            new IntegrationInboxMessage
            {
                MessageId = Guid.CreateVersion7(),
                Consumer = "procurement.vendor-profile.v1",
                EventName = IntegrationContractCatalog.VendorProfileChanged.Name,
                ProcessedAtUtc = DateTimeOffset.UtcNow.AddDays(-31),
            },
            new IntegrationInboxMessage
            {
                MessageId = Guid.CreateVersion7(),
                Consumer = "procurement.vendor-profile.v1",
                EventName = IntegrationContractCatalog.VendorProfileChanged.Name,
                ProcessedAtUtc = DateTimeOffset.UtcNow,
            });
        await context.SaveChangesAsync(cancellationToken);

        var store = new EfIntegrationMessageRetentionStore(context);
        var result = await store.PruneAsync(
            DateTimeOffset.UtcNow.AddDays(-30),
            DateTimeOffset.UtcNow.AddDays(-30),
            100,
            cancellationToken);

        Assert.Equal(1, result.PublishedOutboxMessagesDeleted);
        Assert.Equal(1, result.InboxReceiptsDeleted);
        Assert.Equal(1, await context.IntegrationOutboxMessages.CountAsync(cancellationToken));
        Assert.Equal(1, await context.IntegrationInboxMessages.CountAsync(cancellationToken));
    }

    private static PurchaseOrderStatusChangedV1 CreatePayload(string purchaseOrderNumber) => new(
        Guid.CreateVersion7(),
        purchaseOrderNumber,
        "Draft",
        "PendingManagerApproval",
        1250m,
        "SGD",
        DateTimeOffset.UtcNow);
}

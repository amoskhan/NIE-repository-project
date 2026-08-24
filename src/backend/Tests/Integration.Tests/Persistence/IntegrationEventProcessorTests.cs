using System.Text.Json;
using Application.Integration;
using Application.Integration.Validation;
using Contracts.Events.VendorMaster;
using Contracts.Integration;
using Infrastructure.Integrations;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Integration.Tests;

public class IntegrationEventProcessorTests
{
    [Fact]
    public async Task Processor_commits_handler_effect_and_inbox_receipt_once()
    {
        await using var database = await PostgresTestDatabase.CreateAsync(TestContext.Current.CancellationToken);
        await using var context = database.CreateContext();
        await context.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
        var handler = new CountingHandler();
        var processor = new EfIntegrationEventProcessor(
            context,
            new IntegrationEventDispatcher([handler]));
        var envelope = CreateEnvelope();

        var first = await processor.ProcessAsync(
            envelope,
            "procurement.vendor-profile",
            TestContext.Current.CancellationToken);
        var duplicate = await processor.ProcessAsync(
            envelope,
            "procurement.vendor-profile",
            TestContext.Current.CancellationToken);

        Assert.Equal(IntegrationEventProcessingResult.Processed, first);
        Assert.Equal(IntegrationEventProcessingResult.Duplicate, duplicate);
        Assert.Equal(1, handler.CallCount);
        Assert.Equal(1, await context.IntegrationInboxMessages.CountAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Processor_rolls_back_the_inbox_receipt_when_the_handler_fails()
    {
        await using var database = await PostgresTestDatabase.CreateAsync(TestContext.Current.CancellationToken);
        await using var context = database.CreateContext();
        await context.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
        var processor = new EfIntegrationEventProcessor(
            context,
            new IntegrationEventDispatcher([new FailingHandler()]));

        await Assert.ThrowsAsync<TimeoutException>(() =>
            processor.ProcessAsync(
                CreateEnvelope(),
                "procurement.vendor-profile",
                TestContext.Current.CancellationToken));

        Assert.Equal(0, await context.IntegrationInboxMessages.CountAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Vendor_projection_ignores_an_out_of_order_older_event()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var database = await PostgresTestDatabase.CreateAsync(cancellationToken);
        await using var context = database.CreateContext();
        await context.Database.EnsureCreatedAsync(cancellationToken);
        var processor = new EfIntegrationEventProcessor(
            context,
            new IntegrationEventDispatcher([
                new VendorProfileChangedIntegrationEventHandler(
                    context,
                    new VendorProfileChangedV1Validator()),
            ]));
        var newerAt = DateTimeOffset.UtcNow;

        await processor.ProcessAsync(
            CreateEnvelope("New supplier name", newerAt),
            "procurement.vendor-profile",
            cancellationToken);
        await processor.ProcessAsync(
            CreateEnvelope("Stale supplier name", newerAt.AddMinutes(-5)),
            "procurement.vendor-profile",
            cancellationToken);

        var vendor = await context.Set<Domain.Models.Vendor>()
            .AsNoTracking()
            .SingleAsync(cancellationToken);
        Assert.Equal("New supplier name", vendor.Name);
        Assert.NotNull(vendor.SourceChangedAtUtc);
        Assert.True((newerAt - vendor.SourceChangedAtUtc.Value).Duration() < TimeSpan.FromMilliseconds(1));
        Assert.Equal(2, await context.IntegrationInboxMessages.CountAsync(cancellationToken));
    }

    [Fact]
    public async Task Vendor_projection_rejects_an_invalid_contract_without_recording_a_receipt()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var database = await PostgresTestDatabase.CreateAsync(cancellationToken);
        await using var context = database.CreateContext();
        await context.Database.EnsureCreatedAsync(cancellationToken);
        var processor = new EfIntegrationEventProcessor(
            context,
            new IntegrationEventDispatcher([
                new VendorProfileChangedIntegrationEventHandler(
                    context,
                    new VendorProfileChangedV1Validator()),
            ]));
        var envelope = CreateEnvelope(new string('x', 201), DateTimeOffset.UtcNow);

        var exception = await Assert.ThrowsAsync<PermanentIntegrationEventException>(() =>
            processor.ProcessAsync(
                envelope,
                "procurement.vendor-profile",
                cancellationToken));

        Assert.Equal(
            "The vendor profile event payload failed contract validation.",
            exception.Message);
        Assert.Empty(await context.Set<Domain.Models.Vendor>()
            .AsNoTracking()
            .ToListAsync(cancellationToken));
        Assert.Empty(await context.IntegrationInboxMessages
            .AsNoTracking()
            .ToListAsync(cancellationToken));
    }

    private static IntegrationEventEnvelope CreateEnvelope()
        => CreateEnvelope("Sample Vendor", DateTimeOffset.UtcNow);

    private static IntegrationEventEnvelope CreateEnvelope(
        string vendorName,
        DateTimeOffset changedAtUtc)
    {
        var payload = new VendorProfileChangedV1(
            "VENDOR-001",
            vendorName,
            null,
            null,
            null,
            null,
            null,
            true,
            changedAtUtc);
        return new IntegrationEventEnvelope
        {
            MessageId = Guid.CreateVersion7(),
            EventName = IntegrationContractCatalog.VendorProfileChanged.Name,
            EventVersion = IntegrationContractCatalog.VendorProfileChanged.Version,
            Producer = "vendor-master",
            OccurredAtUtc = DateTimeOffset.UtcNow,
            CorrelationId = Guid.CreateVersion7().ToString("D"),
            Data = JsonSerializer.SerializeToElement(payload, IntegrationJsonOptions.Default),
        };
    }

    private sealed class CountingHandler : IntegrationEventHandler<VendorProfileChangedV1>
    {
        public override IntegrationContractDescriptor Contract =>
            IntegrationContractCatalog.VendorProfileChanged;

        public int CallCount { get; private set; }

        protected override Task HandleAsync(
            VendorProfileChangedV1 payload,
            IntegrationEventContext context,
            CancellationToken cancellationToken)
        {
            CallCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class FailingHandler : IntegrationEventHandler<VendorProfileChangedV1>
    {
        public override IntegrationContractDescriptor Contract =>
            IntegrationContractCatalog.VendorProfileChanged;

        protected override Task HandleAsync(
            VendorProfileChangedV1 payload,
            IntegrationEventContext context,
            CancellationToken cancellationToken) =>
            throw new TimeoutException("Dependency unavailable.");
    }

}

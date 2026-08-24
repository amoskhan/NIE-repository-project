using System.Text.Json;
using Application.Integration;
using Contracts.Events.VendorMaster;
using Contracts.Integration;

namespace Integration.Tests;

public class IntegrationEventDispatcherTests
{
    [Fact]
    public async Task Dispatch_invokes_the_single_matching_versioned_handler()
    {
        var handler = new RecordingVendorHandler();
        var dispatcher = new IntegrationEventDispatcher([handler]);
        var payload = CreatePayload();
        var envelope = CreateEnvelope(payload);

        await dispatcher.DispatchAsync(envelope, "procurement.vendor-profile", CancellationToken.None);

        Assert.Equal(payload, handler.Payload);
        Assert.NotNull(handler.Context);
        Assert.Equal(envelope.MessageId, handler.Context.MessageId);
        Assert.Equal("procurement.vendor-profile", handler.Context.Consumer);
    }

    [Fact]
    public async Task Dispatch_rejects_an_unregistered_contract_without_retrying()
    {
        var dispatcher = new IntegrationEventDispatcher([]);
        var envelope = CreateEnvelope(CreatePayload());

        var exception = await Assert.ThrowsAsync<PermanentIntegrationEventException>(() =>
            dispatcher.DispatchAsync(envelope, "procurement.vendor-profile", CancellationToken.None));

        Assert.Contains(envelope.EventName, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Dispatch_rejects_duplicate_handlers_as_configuration_error()
    {
        var dispatcher = new IntegrationEventDispatcher(
            [new RecordingVendorHandler(), new RecordingVendorHandler()]);
        var envelope = CreateEnvelope(CreatePayload());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            dispatcher.DispatchAsync(envelope, "procurement.vendor-profile", CancellationToken.None));
    }

    private static VendorProfileChangedV1 CreatePayload() => new(
        "VENDOR-001",
        "Sample Vendor",
        null,
        null,
        null,
        null,
        "Technology",
        true,
        DateTimeOffset.UtcNow);

    private static IntegrationEventEnvelope CreateEnvelope(VendorProfileChangedV1 payload) => new()
    {
        MessageId = Guid.CreateVersion7(),
        EventName = IntegrationContractCatalog.VendorProfileChanged.Name,
        EventVersion = IntegrationContractCatalog.VendorProfileChanged.Version,
        Producer = "vendor-master",
        OccurredAtUtc = DateTimeOffset.UtcNow,
        CorrelationId = Guid.CreateVersion7().ToString("D"),
        Data = JsonSerializer.SerializeToElement(payload, IntegrationJsonSerializer.Options),
    };

    private sealed class RecordingVendorHandler : IntegrationEventHandler<VendorProfileChangedV1>
    {
        public override IntegrationContractDescriptor Contract =>
            IntegrationContractCatalog.VendorProfileChanged;

        public VendorProfileChangedV1? Payload { get; private set; }

        public IntegrationEventContext? Context { get; private set; }

        protected override Task HandleAsync(
            VendorProfileChangedV1 payload,
            IntegrationEventContext context,
            CancellationToken cancellationToken)
        {
            Payload = payload;
            Context = context;
            return Task.CompletedTask;
        }
    }
}

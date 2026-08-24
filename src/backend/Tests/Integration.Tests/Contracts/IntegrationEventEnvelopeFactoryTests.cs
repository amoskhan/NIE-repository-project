using Contracts.Events.Procurement;
using Contracts.Integration;

namespace Integration.Tests;

public class IntegrationEventEnvelopeFactoryTests
{
    [Fact]
    public void Create_generates_uuid_v7_and_normalizes_time_to_utc()
    {
        var payload = CreatePayload();
        var occurredAt = new DateTimeOffset(2026, 8, 7, 12, 30, 0, TimeSpan.FromHours(8));

        var envelope = IntegrationEventEnvelopeFactory.Create(
            IntegrationContractCatalog.PurchaseOrderStatusChanged,
            "procurement",
            payload,
            occurredAtUtc: occurredAt);

        Assert.Equal(7, envelope.MessageId.Version);
        Assert.Equal(TimeSpan.Zero, envelope.OccurredAtUtc.Offset);
        Assert.Equal("nie.procurement.purchase-order.status-changed", envelope.EventName);
        Assert.Equal(1, envelope.EventVersion);
        Assert.Equal("procurement", envelope.Producer);
        Assert.Equal(envelope.MessageId.ToString("D"), envelope.CorrelationId);
        Assert.Equal(payload.PurchaseOrderId, envelope.Data.GetProperty("purchaseOrderId").GetGuid());
    }

    [Fact]
    public void Create_rejects_contract_type_mismatch()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            IntegrationEventEnvelopeFactory.Create(
                IntegrationContractCatalog.VendorProfileChanged,
                "procurement",
                CreatePayload()));

        Assert.Contains("payload type", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Create_rejects_non_uuid_v7_message_ids()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            IntegrationEventEnvelopeFactory.Create(
                IntegrationContractCatalog.PurchaseOrderStatusChanged,
                "procurement",
                CreatePayload(),
                messageId: Guid.Parse("2f1b7dd2-241d-4c79-867c-44eecf70ea8f")));

        Assert.Contains("UUIDv7", exception.Message, StringComparison.Ordinal);
    }

    private static PurchaseOrderStatusChangedV1 CreatePayload() => new(
        Guid.CreateVersion7(),
        "PO-2026-00001",
        "Draft",
        "PendingManagerApproval",
        1250m,
        "SGD",
        DateTimeOffset.UtcNow);
}

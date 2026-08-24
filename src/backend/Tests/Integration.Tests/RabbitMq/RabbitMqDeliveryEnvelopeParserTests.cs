using System.Text.Json;
using Application.Integration;
using Contracts.Integration;
using Infrastructure.Integration.RabbitMq;

namespace Integration.Tests.RabbitMq;

public sealed class RabbitMqDeliveryEnvelopeParserTests
{
    [Fact]
    public void Parse_accepts_a_matching_versioned_envelope()
    {
        var envelope = IntegrationEventEnvelopeFactory.Create(
            IntegrationContractCatalog.VendorProfileChanged,
            "vendor-master",
            new Contracts.Events.VendorMaster.VendorProfileChangedV1(
                "V-100",
                "NIE Supplier",
                "Devi",
                "devi@example.test",
                null,
                null,
                "Education",
                true,
                DateTimeOffset.UtcNow),
            "correlation-1");

        var body = JsonSerializer.SerializeToUtf8Bytes(envelope, IntegrationJsonOptions.Default);

        var parsed = RabbitMqDeliveryEnvelopeParser.Parse(
            body,
            IntegrationContractCatalog.VendorProfileChanged,
            1_048_576);

        Assert.Equal(envelope.MessageId, parsed.MessageId);
        Assert.Equal(envelope.EventName, parsed.EventName);
        Assert.Equal(envelope.EventVersion, parsed.EventVersion);
    }

    [Fact]
    public void Parse_rejects_a_contract_mismatch_as_permanent()
    {
        var envelope = IntegrationEventEnvelopeFactory.Create(
            IntegrationContractCatalog.PurchaseOrderStatusChanged,
            "procurement",
            new Contracts.Events.Procurement.PurchaseOrderStatusChangedV1(
                Guid.CreateVersion7(),
                "PO-100",
                "Draft",
                "Submitted",
                12.50m,
                "SGD",
                DateTimeOffset.UtcNow),
            "correlation-1");
        var body = JsonSerializer.SerializeToUtf8Bytes(envelope, IntegrationJsonOptions.Default);

        Assert.Throws<PermanentIntegrationEventException>(() =>
            RabbitMqDeliveryEnvelopeParser.Parse(
                body,
                IntegrationContractCatalog.VendorProfileChanged,
                1_048_576));
    }

    [Fact]
    public void Parse_rejects_oversized_or_malformed_content_as_permanent()
    {
        Assert.Throws<PermanentIntegrationEventException>(() =>
            RabbitMqDeliveryEnvelopeParser.Parse(
                new byte[17],
                IntegrationContractCatalog.VendorProfileChanged,
                16));

        Assert.Throws<PermanentIntegrationEventException>(() =>
            RabbitMqDeliveryEnvelopeParser.Parse(
                "not-json"u8.ToArray(),
                IntegrationContractCatalog.VendorProfileChanged,
                1_048_576));
    }
}

using System.Text.Json;
using Contracts.Events.Procurement;
using Contracts.Events.VendorMaster;
using Contracts.Integration;

namespace Application.Tests;

public sealed class IntegrationEventEnvelopeFactoryTests
{
    private static readonly IntegrationContractDescriptor VendorContract =
        IntegrationContractCatalog.VendorProfileChanged;

    private static VendorProfileChangedV1 CreatePayload() =>
        new(
            "V-001",
            "Acme Supplies",
            "Ada",
            "ops@acme.test",
            "+65 6000 0000",
            "1 Nanyang Walk",
            "Stationery",
            true,
            new DateTimeOffset(2026, 3, 4, 5, 6, 7, TimeSpan.Zero));

    [Fact]
    public void Stamps_the_contract_name_and_version_on_the_envelope()
    {
        var envelope = IntegrationEventEnvelopeFactory.Create(VendorContract, "vendor-master", CreatePayload());

        Assert.Equal(VendorContract.Name, envelope.EventName);
        Assert.Equal(VendorContract.Version, envelope.EventVersion);
        Assert.Equal("vendor-master", envelope.Producer);
        Assert.Equal(7, envelope.MessageId.Version);
    }

    [Fact]
    public void Rejects_a_payload_that_does_not_match_the_contract_type()
    {
        var payload = new PurchaseOrderStatusChangedV1(
            Guid.CreateVersion7(),
            "PO-1",
            "Draft",
            "Submitted",
            10m,
            "SGD",
            DateTimeOffset.UtcNow);

        var exception = Assert.Throws<ArgumentException>(() =>
        {
            _ = IntegrationEventEnvelopeFactory.Create(VendorContract, "vendor-master", payload);
        });

        Assert.Equal("payload", exception.ParamName);
    }

    [Fact]
    public void Rejects_a_null_contract()
    {
        Assert.Throws<ArgumentNullException>(() =>
        {
            _ = IntegrationEventEnvelopeFactory.Create(null!, "vendor-master", CreatePayload());
        });
    }

    [Fact]
    public void Rejects_a_null_payload()
    {
        Assert.Throws<ArgumentNullException>(() =>
        {
            _ = IntegrationEventEnvelopeFactory.Create<VendorProfileChangedV1>(
                VendorContract,
                "vendor-master",
                null!);
        });
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Rejects_a_missing_producer(string? producer)
    {
        var exception = Assert.Throws<ArgumentException>(() =>
        {
            _ = IntegrationEventEnvelopeFactory.Create(VendorContract, producer!, CreatePayload());
        });

        Assert.Equal("producer", exception.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Rejects_a_contract_without_a_name(string name)
    {
        var contract = new IntegrationContractDescriptor(name, 1, typeof(VendorProfileChangedV1));

        var exception = Assert.Throws<ArgumentException>(() =>
        {
            _ = IntegrationEventEnvelopeFactory.Create(contract, "vendor-master", CreatePayload());
        });

        Assert.Equal("contract", exception.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Rejects_a_contract_without_a_positive_version(int version)
    {
        var contract = new IntegrationContractDescriptor("nie.test.event", version, typeof(VendorProfileChangedV1));

        var exception = Assert.Throws<ArgumentException>(() =>
        {
            _ = IntegrationEventEnvelopeFactory.Create(contract, "vendor-master", CreatePayload());
        });

        Assert.Equal("contract", exception.ParamName);
    }

    [Fact]
    public void Rejects_a_message_id_that_is_not_a_version_seven_uuid()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
        {
            _ = IntegrationEventEnvelopeFactory.Create(
                VendorContract,
                "vendor-master",
                CreatePayload(),
                messageId: Guid.NewGuid());
        });

        Assert.Equal("messageId", exception.ParamName);
    }

    [Fact]
    public void Rejects_an_empty_message_id()
    {
        Assert.Throws<ArgumentException>(() =>
        {
            _ = IntegrationEventEnvelopeFactory.Create(
                VendorContract,
                "vendor-master",
                CreatePayload(),
                messageId: Guid.Empty);
        });
    }

    [Fact]
    public void Falls_back_to_the_message_id_when_no_correlation_id_is_supplied()
    {
        var messageId = Guid.CreateVersion7();

        var envelope = IntegrationEventEnvelopeFactory.Create(
            VendorContract,
            "vendor-master",
            CreatePayload(),
            messageId: messageId);

        Assert.Equal(messageId.ToString("D"), envelope.CorrelationId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("    ")]
    public void Falls_back_to_the_message_id_for_a_blank_correlation_id(string correlationId)
    {
        var messageId = Guid.CreateVersion7();

        var envelope = IntegrationEventEnvelopeFactory.Create(
            VendorContract,
            "vendor-master",
            CreatePayload(),
            correlationId,
            messageId: messageId);

        Assert.Equal(messageId.ToString("D"), envelope.CorrelationId);
    }

    [Fact]
    public void Trims_the_producer_correlation_and_causation_identifiers()
    {
        var envelope = IntegrationEventEnvelopeFactory.Create(
            VendorContract,
            "  vendor-master  ",
            CreatePayload(),
            "  correlation-1  ",
            "  causation-1  ");

        Assert.Equal("vendor-master", envelope.Producer);
        Assert.Equal("correlation-1", envelope.CorrelationId);
        Assert.Equal("causation-1", envelope.CausationId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Drops_a_blank_causation_id(string? causationId)
    {
        var envelope = IntegrationEventEnvelopeFactory.Create(
            VendorContract,
            "vendor-master",
            CreatePayload(),
            "correlation-1",
            causationId);

        Assert.Null(envelope.CausationId);
    }

    [Fact]
    public void Normalises_the_occurrence_time_to_utc()
    {
        var occurredAt = new DateTimeOffset(2026, 3, 4, 13, 0, 0, TimeSpan.FromHours(8));

        var envelope = IntegrationEventEnvelopeFactory.Create(
            VendorContract,
            "vendor-master",
            CreatePayload(),
            occurredAtUtc: occurredAt);

        Assert.Equal(TimeSpan.Zero, envelope.OccurredAtUtc.Offset);
        Assert.Equal(new DateTime(2026, 3, 4, 5, 0, 0, DateTimeKind.Utc), envelope.OccurredAtUtc.UtcDateTime);
    }

    [Fact]
    public void Serialises_the_payload_with_the_shared_integration_settings()
    {
        var payload = CreatePayload();

        var envelope = IntegrationEventEnvelopeFactory.Create(VendorContract, "vendor-master", payload);
        var roundTripped = envelope.Data.Deserialize<VendorProfileChangedV1>(IntegrationJsonOptions.Default);

        Assert.NotNull(roundTripped);
        Assert.Equal(payload, roundTripped);
        Assert.Equal("V-001", envelope.Data.GetProperty("vendorCode").GetString());
    }

    [Fact]
    public void Omits_null_payload_members_from_the_serialised_data()
    {
        var payload = new VendorProfileChangedV1(
            "V-002",
            "Beta",
            null,
            null,
            null,
            null,
            null,
            false,
            new DateTimeOffset(2026, 3, 4, 5, 6, 7, TimeSpan.Zero));

        var envelope = IntegrationEventEnvelopeFactory.Create(VendorContract, "vendor-master", payload);

        Assert.False(envelope.Data.TryGetProperty("contactPerson", out _));
        Assert.False(envelope.Data.TryGetProperty("category", out _));
        Assert.True(envelope.Data.TryGetProperty("vendorCode", out _));
    }

    [Fact]
    public void Generates_a_sortable_message_id_when_none_is_supplied()
    {
        var first = IntegrationEventEnvelopeFactory.Create(VendorContract, "vendor-master", CreatePayload());
        var second = IntegrationEventEnvelopeFactory.Create(VendorContract, "vendor-master", CreatePayload());

        Assert.NotEqual(first.MessageId, second.MessageId);
        Assert.Equal(7, first.MessageId.Version);
        Assert.Equal(7, second.MessageId.Version);
    }
}

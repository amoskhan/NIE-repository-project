using System.Text.Json;
using Application.Integration;
using Contracts.Events.VendorMaster;
using Contracts.Integration;

namespace Application.Tests;

public sealed class IntegrationContractSerializationTests
{
    private static VendorProfileChangedV1 CreatePayload(string? category = "Stationery") =>
        new(
            "V-001",
            "Acme Supplies",
            "Ada",
            "ops@acme.test",
            "+65 6000 0000",
            "1 Nanyang Walk",
            category,
            true,
            new DateTimeOffset(2026, 3, 4, 5, 6, 7, TimeSpan.Zero));

    [Fact]
    public void Exposes_one_shared_serializer_configuration()
    {
        Assert.Same(IntegrationJsonOptions.Default, IntegrationJsonSerializer.Options);
    }

    [Fact]
    public void Writes_contract_members_in_camel_case()
    {
        var json = JsonSerializer.Serialize(CreatePayload(), IntegrationJsonOptions.Default);

        Assert.Contains("\"vendorCode\":", json, StringComparison.Ordinal);
        Assert.DoesNotContain("\"VendorCode\":", json, StringComparison.Ordinal);
    }

    [Fact]
    public void Omits_members_that_are_null()
    {
        var json = JsonSerializer.Serialize(CreatePayload(category: null), IntegrationJsonOptions.Default);

        Assert.DoesNotContain("category", json, StringComparison.Ordinal);
    }

    [Fact]
    public void Round_trips_a_contract_without_loss()
    {
        var payload = CreatePayload();

        var json = JsonSerializer.Serialize(payload, IntegrationJsonOptions.Default);
        var restored = JsonSerializer.Deserialize<VendorProfileChangedV1>(json, IntegrationJsonOptions.Default);

        Assert.NotNull(restored);
        Assert.Equal(payload, restored);
    }

    [Fact]
    public void Rejects_a_document_that_carries_a_member_outside_the_contract()
    {
        const string json = """{"vendorCode":"V-1","name":"Acme","isActive":true,"extra":1}""";

        Assert.Throws<JsonException>(() =>
        {
            _ = JsonSerializer.Deserialize<VendorProfileChangedV1>(json, IntegrationJsonOptions.Default);
        });
    }

    [Fact]
    public void Rejects_a_document_that_uses_the_wrong_member_casing()
    {
        const string json = """{"VendorCode":"V-1","name":"Acme","isActive":true}""";

        Assert.Throws<JsonException>(() =>
        {
            _ = JsonSerializer.Deserialize<VendorProfileChangedV1>(json, IntegrationJsonOptions.Default);
        });
    }
}

public sealed class IntegrationContractRoutingKeyTests
{
    [Theory]
    [InlineData("nie.vendor-master.vendor-profile.changed", 1, "nie.vendor-master.vendor-profile.changed.v1")]
    [InlineData("nie.procurement.purchase-order.status-changed", 12, "nie.procurement.purchase-order.status-changed.v12")]
    public void Appends_the_version_suffix_to_the_event_name(string name, int version, string expected)
    {
        Assert.Equal(expected, IntegrationContractRoutingKey.Create(name, version));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Rejects_a_missing_event_name(string? name)
    {
        var exception = Assert.Throws<ArgumentException>(() =>
        {
            _ = IntegrationContractRoutingKey.Create(name!, 1);
        });

        Assert.Equal("eventName", exception.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Rejects_a_version_below_one(int version)
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            _ = IntegrationContractRoutingKey.Create("nie.test.event", version);
        });

        Assert.Equal("eventVersion", exception.ParamName);
    }
}

public sealed class IntegrationContractCatalogTests
{
    [Fact]
    public void Publishes_only_the_purchase_order_status_contract()
    {
        var published = Assert.Single(IntegrationContractCatalog.Published);

        Assert.Equal(IntegrationContractCatalog.PurchaseOrderStatusChanged, published);
    }

    [Fact]
    public void Subscribes_only_to_the_vendor_profile_contract()
    {
        var subscribed = Assert.Single(IntegrationContractCatalog.Subscribed);

        Assert.Equal(IntegrationContractCatalog.VendorProfileChanged, subscribed);
    }

    [Fact]
    public void Gives_every_catalogued_contract_a_routable_name_and_a_positive_version()
    {
        var contracts = IntegrationContractCatalog.Published
            .Concat(IntegrationContractCatalog.Subscribed)
            .ToList();

        Assert.All(contracts, contract =>
        {
            Assert.False(string.IsNullOrWhiteSpace(contract.Name));
            Assert.True(contract.Version >= 1);
            Assert.NotNull(contract.ContractType);
        });
    }

    [Fact]
    public void Keeps_published_and_subscribed_routing_keys_distinct()
    {
        var routingKeys = IntegrationContractCatalog.Published
            .Concat(IntegrationContractCatalog.Subscribed)
            .Select(contract => IntegrationContractRoutingKey.Create(contract.Name, contract.Version))
            .ToList();

        Assert.Equal(routingKeys.Count, routingKeys.Distinct(StringComparer.Ordinal).Count());
    }
}

public sealed class DisabledIntegrationEventPublisherTests
{
    [Fact]
    public async Task Completes_a_publish_request_synchronously_without_queueing_anything()
    {
        var publisher = new DisabledIntegrationEventPublisher();

        var enqueue = publisher.EnqueueAsync(
            IntegrationContractCatalog.VendorProfileChanged,
            new object(),
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(enqueue.IsCompletedSuccessfully);
        await enqueue;
    }

    [Fact]
    public async Task Does_not_validate_the_contract_against_the_payload()
    {
        var publisher = new DisabledIntegrationEventPublisher();

        var enqueue = publisher.EnqueueAsync(
            new IntegrationContractDescriptor(string.Empty, 0, typeof(string)),
            new object(),
            correlationId: null,
            causationId: null,
            cancellationToken: new CancellationToken(canceled: true));

        Assert.True(enqueue.IsCompletedSuccessfully);
        await enqueue;
    }
}

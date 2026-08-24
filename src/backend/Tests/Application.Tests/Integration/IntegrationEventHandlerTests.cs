using System.Text.Json;
using Application.Integration;
using Contracts.Events.VendorMaster;
using Contracts.Integration;

namespace Application.Tests;

public sealed class IntegrationEventHandlerTests
{
    private sealed class RecordingVendorHandler : IntegrationEventHandler<VendorProfileChangedV1>
    {
        public VendorProfileChangedV1? Payload { get; private set; }

        public IntegrationEventContext? Context { get; private set; }

        public override IntegrationContractDescriptor Contract =>
            IntegrationContractCatalog.VendorProfileChanged;

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

    private static IntegrationEventContext CreateContext() =>
        new(
            Guid.CreateVersion7(),
            IntegrationContractCatalog.VendorProfileChanged.Name,
            1,
            "vendor-master",
            DateTimeOffset.UtcNow,
            "correlation-1",
            null,
            "nie-template");

    private static JsonElement Element(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }

    [Fact]
    public async Task Deserialises_a_well_formed_payload_before_calling_the_typed_handler()
    {
        var handler = new RecordingVendorHandler();
        var json = """
            {"vendorCode":"V-001","name":"Acme","contactPerson":null,"email":"ops@acme.test",
             "phone":null,"address":null,"category":"Stationery","isActive":true,
             "changedAtUtc":"2026-03-04T05:06:07+00:00"}
            """;

        await handler.HandleAsync(Element(json), CreateContext(), TestContext.Current.CancellationToken);

        Assert.NotNull(handler.Payload);
        Assert.Equal("V-001", handler.Payload.VendorCode);
        Assert.Equal("Acme", handler.Payload.Name);
        Assert.True(handler.Payload.IsActive);
        Assert.NotNull(handler.Context);
    }

    [Fact]
    public async Task Rejects_a_payload_that_carries_an_unmapped_member()
    {
        var handler = new RecordingVendorHandler();
        var json = """{"vendorCode":"V-001","name":"Acme","isActive":true,"injected":"x"}""";

        var exception = await Assert.ThrowsAsync<PermanentIntegrationEventException>(async () =>
            await handler.HandleAsync(
                Element(json),
                CreateContext(),
                TestContext.Current.CancellationToken));

        Assert.IsType<JsonException>(exception.InnerException);
        Assert.Null(handler.Payload);
    }

    [Fact]
    public async Task Rejects_a_payload_that_uses_the_wrong_property_casing()
    {
        var handler = new RecordingVendorHandler();
        var json = """{"VendorCode":"V-001","Name":"Acme","IsActive":true}""";

        await Assert.ThrowsAsync<PermanentIntegrationEventException>(async () =>
            await handler.HandleAsync(
                Element(json),
                CreateContext(),
                TestContext.Current.CancellationToken));

        Assert.Null(handler.Payload);
    }

    [Fact]
    public async Task Rejects_a_payload_whose_field_has_the_wrong_json_type()
    {
        var handler = new RecordingVendorHandler();
        var json = """{"vendorCode":42,"name":"Acme","isActive":true}""";

        var exception = await Assert.ThrowsAsync<PermanentIntegrationEventException>(async () =>
            await handler.HandleAsync(
                Element(json),
                CreateContext(),
                TestContext.Current.CancellationToken));

        Assert.Contains(
            typeof(VendorProfileChangedV1).FullName!,
            exception.Message,
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task Rejects_an_empty_payload()
    {
        var handler = new RecordingVendorHandler();

        var exception = await Assert.ThrowsAsync<PermanentIntegrationEventException>(async () =>
            await handler.HandleAsync(
                Element("null"),
                CreateContext(),
                TestContext.Current.CancellationToken));

        Assert.Equal("The integration event payload was empty.", exception.Message);
        Assert.Null(exception.InnerException);
    }

    [Fact]
    public async Task Accepts_a_payload_that_omits_every_optional_field()
    {
        var handler = new RecordingVendorHandler();

        await handler.HandleAsync(
            Element("""{"vendorCode":"V-001","name":"Acme","isActive":false}"""),
            CreateContext(),
            TestContext.Current.CancellationToken);

        Assert.NotNull(handler.Payload);
        Assert.Null(handler.Payload.Category);
        Assert.False(handler.Payload.IsActive);
        Assert.Equal(default, handler.Payload.ChangedAtUtc);
    }

    [Fact]
    public void Publishes_the_contract_the_dispatcher_routes_on()
    {
        var handler = new RecordingVendorHandler();

        Assert.Equal("nie.vendor-master.vendor-profile.changed", handler.Contract.Name);
        Assert.Equal(1, handler.Contract.Version);
        Assert.Equal(typeof(VendorProfileChangedV1), handler.Contract.ContractType);
    }
}

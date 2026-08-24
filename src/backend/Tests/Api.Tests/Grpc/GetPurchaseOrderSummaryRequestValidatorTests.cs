using Api.Grpc.Validation;
using Contracts.Grpc.Procurement.V1;

namespace Api.Tests.Grpc;

/// <summary>
/// The inbound gRPC identifier is the only untrusted input on the service-integration
/// surface; it must be a real UUIDv7 before any query runs.
/// </summary>
public sealed class GetPurchaseOrderSummaryRequestValidatorTests
{
    private readonly GetPurchaseOrderSummaryRequestValidator _validator = new();

    [Fact]
    public void A_uuid_v7_identifier_is_accepted()
    {
        var request = new GetPurchaseOrderSummaryRequest
        {
            PurchaseOrderId = Guid.CreateVersion7().ToString("D"),
        };

        Assert.True(_validator.Validate(request).IsValid);
    }

    [Fact]
    public void A_uuid_v4_identifier_is_rejected()
    {
        var request = new GetPurchaseOrderSummaryRequest
        {
            PurchaseOrderId = Guid.NewGuid().ToString("D"),
        };

        Assert.False(_validator.Validate(request).IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-guid")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    public void Malformed_or_empty_identifiers_are_rejected(string purchaseOrderId)
    {
        var request = new GetPurchaseOrderSummaryRequest { PurchaseOrderId = purchaseOrderId };

        Assert.False(_validator.Validate(request).IsValid);
    }

    [Fact]
    public void An_identifier_longer_than_a_canonical_uuid_is_rejected()
    {
        var request = new GetPurchaseOrderSummaryRequest
        {
            PurchaseOrderId = Guid.CreateVersion7().ToString("D") + "0",
        };

        Assert.False(_validator.Validate(request).IsValid);
    }

    [Fact]
    public void Validation_stops_at_the_first_failure_so_one_message_is_returned()
    {
        var request = new GetPurchaseOrderSummaryRequest { PurchaseOrderId = string.Empty };

        Assert.Single(_validator.Validate(request).Errors);
    }
}

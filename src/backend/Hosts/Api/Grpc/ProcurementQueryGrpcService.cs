using System.Globalization;
using Application.Integration;
using Contracts.Grpc.Procurement.V1;
using FluentValidation;
using Grpc.Core;

namespace Api.Grpc;

public sealed class ProcurementQueryGrpcService(
    IProcurementIntegrationQuery query,
    IValidator<GetPurchaseOrderSummaryRequest> validator) : ProcurementQuery.ProcurementQueryBase
{
    private readonly IProcurementIntegrationQuery _query = query;
    private readonly IValidator<GetPurchaseOrderSummaryRequest> _validator = validator;

    public override async Task<PurchaseOrderSummaryReply> GetPurchaseOrderSummary(
        GetPurchaseOrderSummaryRequest request,
        ServerCallContext context)
    {
        var validation = await _validator.ValidateAsync(request, context.CancellationToken);
        if (!validation.IsValid)
        {
            throw new RpcException(new Status(
                StatusCode.InvalidArgument,
                "The request is invalid."));
        }

        var purchaseOrderId = Guid.Parse(request.PurchaseOrderId);

        var summary = await _query.GetPurchaseOrderSummaryAsync(
            purchaseOrderId,
            context.CancellationToken);
        if (summary is null)
        {
            throw new RpcException(new Status(
                StatusCode.NotFound,
                "The purchase order was not found."));
        }

        return new PurchaseOrderSummaryReply
        {
            PurchaseOrderId = summary.PurchaseOrderId.ToString("D"),
            PurchaseOrderNumber = summary.PurchaseOrderNumber,
            Status = summary.Status,
            VendorId = summary.VendorId.ToString("D"),
            VendorName = summary.VendorName,
            TotalAmount = summary.TotalAmount.ToString(CultureInfo.InvariantCulture),
            Currency = summary.Currency,
        };
    }
}

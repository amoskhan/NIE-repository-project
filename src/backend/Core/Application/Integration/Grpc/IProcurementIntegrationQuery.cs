namespace Application.Integration;

public sealed record ProcurementPurchaseOrderSummary(
    Guid PurchaseOrderId,
    string PurchaseOrderNumber,
    string Status,
    Guid VendorId,
    string VendorName,
    decimal TotalAmount,
    string Currency);

public interface IProcurementIntegrationQuery
{
    Task<ProcurementPurchaseOrderSummary?> GetPurchaseOrderSummaryAsync(
        Guid purchaseOrderId,
        CancellationToken cancellationToken);
}

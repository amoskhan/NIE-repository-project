namespace Contracts.Events.Procurement;

/// <summary>
/// Published when a procurement purchase order changes workflow status.
/// </summary>
public sealed record PurchaseOrderStatusChangedV1(
    Guid PurchaseOrderId,
    string PurchaseOrderNumber,
    string PreviousStatus,
    string Status,
    decimal TotalAmount,
    string Currency,
    DateTimeOffset ChangedAtUtc);

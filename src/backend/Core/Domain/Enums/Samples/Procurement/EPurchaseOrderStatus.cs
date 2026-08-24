namespace Domain.Enums;

public enum EPurchaseOrderStatus
{
    Draft = 0,
    Submitted = 1,
    PendingManagerApproval = 2,
    PendingFinanceApproval = 3,
    PendingProcurementApproval = 4,
    Approved = 5,
    Rejected = 6,
    Cancelled = 7
}

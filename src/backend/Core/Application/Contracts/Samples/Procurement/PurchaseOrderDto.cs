using Domain.Enums;

namespace Application.Contracts;

public class PurchaseOrderDto
{
    public Guid Id { get; set; }
    public string PoNumber { get; set; } = default!;
    public string RequestedBy { get; set; } = default!;
    public string? RequestedByName { get; set; }
    public DateTime RequestDate { get; set; }
    public string? DeliveryAddress { get; set; }
    public DateTime? ExpectedDeliveryDate { get; set; }
    public EPurchaseOrderStatus Status { get; set; }
    public string StatusName => Status.ToString();
    public string? Notes { get; set; }
    public decimal TotalAmount { get; set; }
    public string? RejectionReason { get; set; }
    public Guid VendorId { get; set; }
    public string? VendorName { get; set; }
    public List<PurchaseOrderLineDto> Lines { get; set; } = new();
    public List<PurchaseOrderApprovalDto> Approvals { get; set; } = new();
    public List<PurchaseOrderDocumentDto> Documents { get; set; } = new();
    public DateTime? CreatedOn { get; set; }
    public DateTime? UpdatedOn { get; set; }
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
}

public class PurchaseOrderLineDto
{
    public Guid Id { get; set; }
    public int LineNumber { get; set; }
    public string ItemName { get; set; } = default!;
    public string? Description { get; set; }
    public string? UnitOfMeasure { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
    public Guid? CatalogItemId { get; set; }
}

public class PurchaseOrderApprovalDto
{
    public Guid Id { get; set; }
    public EApprovalStage ApprovalStage { get; set; }
    public int StageOrder { get; set; }
    public string? ApproverId { get; set; }
    public string? ApproverName { get; set; }
    public EApprovalAction? Action { get; set; }
    public DateTime? ActionDate { get; set; }
    public string? Comments { get; set; }
}

public class PurchaseOrderDocumentDto
{
    public Guid Id { get; set; }
    public string FilePath { get; set; } = default!;
    public long FileSize { get; set; }
    public string UserFileName { get; set; } = default!;
    public string? DocumentType { get; set; }
}

public class ApprovalActionDto
{
    public Guid PurchaseOrderId { get; set; }
    public EApprovalAction Action { get; set; }
    public string? Comments { get; set; }
}

public class PurchaseOrderSearchDto : PagedSearchDto
{
    public string? Search { get; set; }
    public EPurchaseOrderStatus? Status { get; set; }
    public Guid? VendorId { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? SortBy { get; set; }
    public bool SortDescending { get; set; } = true;
}

public class SpendOverviewDto
{
    public int PendingApprovals { get; set; }
    public decimal MonthlySpend { get; set; }
    public int RecentOrders { get; set; }
    public int TotalVendors { get; set; }
    public int TotalOrders { get; set; }
    public decimal TotalSpend { get; set; }
    public List<MonthlySpendItem> MonthlySpendTrend { get; set; } = new();
    public List<StatusCountItem> StatusBreakdown { get; set; } = new();
    public List<TopVendorItem> TopVendors { get; set; } = new();
    public List<RecentOrderItem> RecentOrdersList { get; set; } = new();
}

public class MonthlySpendItem
{
    public string Month { get; set; } = default!;
    public decimal Amount { get; set; }
}

public class StatusCountItem
{
    public string Status { get; set; } = default!;
    public int Count { get; set; }
}

public class TopVendorItem
{
    public string VendorName { get; set; } = default!;
    public decimal TotalSpend { get; set; }
    public int OrderCount { get; set; }
}

public class RecentOrderItem
{
    public Guid Id { get; set; }
    public string PoNumber { get; set; } = default!;
    public string VendorName { get; set; } = default!;
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = default!;
    public DateTime RequestDate { get; set; }
}

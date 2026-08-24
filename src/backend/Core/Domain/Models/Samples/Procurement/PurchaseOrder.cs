using Domain.Enums;

namespace Domain.Models;

public class PurchaseOrder : TimestampedEntity
{
    public string PoNumber { get; set; } = default!;
    public string RequestedBy { get; set; } = default!;
    public string? RequestedByName { get; set; }
    public DateTime RequestDate { get; set; }
    public string? DeliveryAddress { get; set; }
    public DateTime? ExpectedDeliveryDate { get; set; }
    public EPurchaseOrderStatus Status { get; set; } = EPurchaseOrderStatus.Draft;
    public string? Notes { get; set; }
    public decimal TotalAmount { get; set; }
    public string? RejectionReason { get; set; }

    /// <summary>
    /// Workflow state used by the workflow engine.
    /// Mirrors EWorkflowState values. Default is Draft.
    /// </summary>
    [System.ComponentModel.DataAnnotations.MaxLength(50)]
    public string WorkflowState { get; set; } = EWorkflowState.Draft.ToString();

    public Guid VendorId { get; set; }
    public virtual Vendor Vendor { get; set; } = default!;

    public virtual ICollection<PurchaseOrderLine> Lines { get; set; } = new List<PurchaseOrderLine>();
    public virtual ICollection<PurchaseOrderApproval> Approvals { get; set; } = new List<PurchaseOrderApproval>();
    public virtual ICollection<PurchaseOrderDocument> Documents { get; set; } = new List<PurchaseOrderDocument>();
}

using Domain.Enums;

namespace Domain.Models;

public class PurchaseOrderApproval : TimestampedEntity
{
    public EApprovalStage ApprovalStage { get; set; }
    public int StageOrder { get; set; }
    public string? ApproverId { get; set; }
    public string? ApproverName { get; set; }
    public EApprovalAction? Action { get; set; }
    public DateTime? ActionDate { get; set; }
    public string? Comments { get; set; }

    public Guid PurchaseOrderId { get; set; }
    public virtual PurchaseOrder PurchaseOrder { get; set; } = default!;
}

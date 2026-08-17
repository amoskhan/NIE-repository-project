namespace Domain.Models;

public class PurchaseOrderDocument : TimestampedEntity
{
    public string FilePath { get; set; } = default!;
    public long FileSize { get; set; }
    public string UserFileName { get; set; } = default!;
    public string? DocumentType { get; set; }

    public int PurchaseOrderId { get; set; }
    public virtual PurchaseOrder PurchaseOrder { get; set; } = default!;
}

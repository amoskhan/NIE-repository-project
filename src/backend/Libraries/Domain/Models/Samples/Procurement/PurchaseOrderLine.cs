namespace Domain.Models;

public class PurchaseOrderLine : TimestampedEntity
{
    public int LineNumber { get; set; }
    public string ItemName { get; set; } = default!;
    public string? Description { get; set; }
    public string? UnitOfMeasure { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }

    public int PurchaseOrderId { get; set; }
    public virtual PurchaseOrder PurchaseOrder { get; set; } = default!;

    public int? CatalogItemId { get; set; }
    public virtual CatalogItem? CatalogItem { get; set; }
}

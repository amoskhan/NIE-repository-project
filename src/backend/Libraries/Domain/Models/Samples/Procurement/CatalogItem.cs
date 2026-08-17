namespace Domain.Models;

public class CatalogItem : TimestampedEntity
{
    public string Name { get; set; } = default!;
    public string? Sku { get; set; }
    public string? Description { get; set; }
    public string? Category { get; set; }
    public string? UnitOfMeasure { get; set; }
    public decimal UnitPrice { get; set; }
    public bool IsActive { get; set; } = true;

    public int VendorId { get; set; }
    public virtual Vendor Vendor { get; set; } = default!;
}

namespace Domain.Models;

public class Vendor : TimestampedEntity
{
    public string Name { get; set; } = default!;
    public string Code { get; set; } = default!;
    public string? ContactPerson { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? Category { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Notes { get; set; }

    public virtual ICollection<CatalogItem> CatalogItems { get; set; } = new List<CatalogItem>();
    public virtual ICollection<PurchaseOrder> PurchaseOrders { get; set; } = new List<PurchaseOrder>();
}

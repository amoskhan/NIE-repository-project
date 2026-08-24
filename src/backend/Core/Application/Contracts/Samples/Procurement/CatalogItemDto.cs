namespace Application.Contracts;

public class CatalogItemDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string? Sku { get; set; }
    public string? Description { get; set; }
    public string? Category { get; set; }
    public string? UnitOfMeasure { get; set; }
    public decimal UnitPrice { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid VendorId { get; set; }
    public string? VendorName { get; set; }
    public DateTime? CreatedOn { get; set; }
    public DateTime? UpdatedOn { get; set; }
}

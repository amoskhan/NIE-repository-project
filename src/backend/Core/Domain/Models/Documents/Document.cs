namespace Domain.Models;

public class Document : TimestampedEntity
{
    public string FilePath { get; set; } = default!;
    public long FileSize { get; set; } = default!;
    public string UserFileName { get; set; } = default!;

    /// <summary>
    /// Optional polymorphic owner type (e.g. "PurchaseOrder", "Profile"). Apps wire owner-specific
    /// linking entities (like PurchaseOrderDocument) when they need a hard FK; this pair is for
    /// loose attachments that do not require a relational FK.
    /// </summary>
    public string? OwnerType { get; set; }

    /// <summary>
    /// Optional polymorphic owner id paired with OwnerType. Null when the document is owner-less.
    /// </summary>
    public Guid? OwnerId { get; set; }
}

namespace Domain.Models;

/// <summary>
/// Immutable version of a channel template. Publishing a new version leaves the
/// previous version available for audit and rollback.
/// </summary>
public class NotificationTemplate : TimestampedEntity
{
    public string EventKey { get; set; } = default!;
    public string Channel { get; set; } = default!;
    public int Version { get; set; }
    public string Subject { get; set; } = default!;
    public string Content { get; set; } = default!;
    public bool IsPublished { get; set; }
    public string? PublishedBy { get; set; }
    public DateTime? PublishedOn { get; set; }
}

namespace Domain.Models;

/// <summary>
/// A durable in-app notification for one signed-in user.
/// </summary>
public class Notification : TimestampedEntity
{
    public string RecipientUserId { get; set; } = default!;
    public string? RecipientName { get; set; }
    public string Title { get; set; } = default!;
    public string Message { get; set; } = default!;
    public string Type { get; set; } = default!;
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }
    public string? Link { get; set; }
    public string? SourceEntityType { get; set; }
    public Guid? SourceEntityId { get; set; }
    public string? EventKey { get; set; }
    public string? CorrelationKey { get; set; }
    public bool IsActionRequired { get; set; }

    /// <summary>
    /// Idempotency key for scheduled reminders. Null for ad-hoc notifications.
    /// </summary>
    public string? DedupeKey { get; set; }
}

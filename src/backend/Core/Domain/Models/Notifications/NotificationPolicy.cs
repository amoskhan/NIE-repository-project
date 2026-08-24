namespace Domain.Models;

/// <summary>
/// System-admin controlled channel and timing policy for one notification event.
/// Recipient rules remain code-owned so configuration cannot bypass workflow
/// separation of duties.
/// </summary>
public class NotificationPolicy : TimestampedEntity
{
    public string EventKey { get; set; } = default!;
    public string DisplayName { get; set; } = default!;
    public string Description { get; set; } = default!;
    public string Category { get; set; } = default!;
    public bool InAppEnabled { get; set; } = true;
    public bool EmailEnabled { get; set; } = true;
    public bool PushEnabled { get; set; }
    public bool IsActive { get; set; } = true;
    public int? ReminderAfterHours { get; set; }
    public int? EscalationAfterHours { get; set; }
}

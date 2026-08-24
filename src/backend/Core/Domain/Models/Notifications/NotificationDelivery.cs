namespace Domain.Models;

/// <summary>One channel attempt for one recipient of an outbox event.</summary>
public class NotificationDelivery : TimestampedEntity
{
    public Guid NotificationOutboxId { get; set; }
    public NotificationOutbox NotificationOutbox { get; set; } = default!;
    public string RecipientUserId { get; set; } = default!;
    public string? RecipientName { get; set; }
    public string? RecipientEmail { get; set; }
    public string Channel { get; set; } = default!;
    public string Status { get; set; } = "Pending";
    public int Attempts { get; set; }
    public DateTime? NextAttemptOn { get; set; }
    public DateTime? SentOn { get; set; }
    public string? ProviderMessageId { get; set; }
    public string? LastError { get; set; }
}

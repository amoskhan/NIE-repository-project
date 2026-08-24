namespace Domain.Models;

/// <summary>
/// Transactional notification event. Workflow services add this row in the same
/// database transaction as the state change; background delivery is retried
/// independently.
/// </summary>
public class NotificationOutbox : TimestampedEntity
{
    public string EventKey { get; set; } = default!;
    public string CorrelationKey { get; set; } = default!;
    public Guid? ApplicationId { get; set; }
    public string ActorUserId { get; set; } = default!;
    public string PayloadJson { get; set; } = default!;
    public string Status { get; set; } = "Pending";
    public int Attempts { get; set; }
    public DateTime OccurredOn { get; set; }
    public DateTime? NextAttemptOn { get; set; }
    public DateTime? ProcessedOn { get; set; }
    public string? LastError { get; set; }
    public string DedupeKey { get; set; } = default!;

    public ICollection<NotificationDelivery> Deliveries { get; set; } =
        new List<NotificationDelivery>();
}

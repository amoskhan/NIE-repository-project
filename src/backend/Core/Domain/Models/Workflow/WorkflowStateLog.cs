using System.ComponentModel.DataAnnotations;

namespace Domain.Models;

/// <summary>
/// Workflow state transition audit log.
/// Tracks all state changes for compliance and history.
/// </summary>
public class WorkflowStateLog : TimestampedEntity
{
    [Required]
    [MaxLength(50)]
    public string FromState { get; set; } = default!;

    [Required]
    [MaxLength(50)]
    public string ToState { get; set; } = default!;

    [MaxLength(1000)]
    public string? Remarks { get; set; }

    [MaxLength(100)]
    public string? PerformedByUserId { get; set; }

    [MaxLength(200)]
    public string? PerformedByName { get; set; }

    [MaxLength(100)]
    public string? PerformedByRole { get; set; }

    public DateTime TransitionedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(200)]
    public string? IpAddress { get; set; }

    /// <summary>
    /// Polymorphic owner — links to any entity type that uses workflow.
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string OwnerType { get; set; } = default!; // e.g., "PurchaseOrder"

    public Guid OwnerId { get; set; }

    // Notification tracking
    public bool NotificationSent { get; set; }

    public DateTime? NotificationSentAt { get; set; }
}

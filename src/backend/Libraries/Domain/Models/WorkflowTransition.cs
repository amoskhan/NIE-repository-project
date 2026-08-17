using System.ComponentModel.DataAnnotations;

namespace Domain.Models;

/// <summary>
/// Configurable workflow transitions.
/// Defines valid state transitions and required permissions.
/// Table-driven — no code changes needed to add new states or transitions.
/// </summary>
public class WorkflowTransition : TimestampedEntity
{
    [Required]
    [MaxLength(50)]
    public string FromState { get; set; } = default!;

    [Required]
    [MaxLength(50)]
    public string ToState { get; set; } = default!;

    [Required]
    [MaxLength(50)]
    public string RequiredRole { get; set; } = default!; // Which role can trigger this transition

    [MaxLength(200)]
    public string? DisplayLabel { get; set; } // Button text in UI (e.g., "Submit for Review")

    public bool RequiresRemarks { get; set; }

    public bool IsActive { get; set; } = true;

    public int DisplayOrder { get; set; }

    /// <summary>
    /// JSON conditions for showing/hiding transition in UI.
    /// Example: {"requireDocumentUpload": true, "minAmount": 1000}
    /// </summary>
    [MaxLength(500)]
    public string? UiConditions { get; set; }
}

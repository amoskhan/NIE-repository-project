using System.ComponentModel.DataAnnotations;
using Domain.Enums;
using Domain.Identifiers;

namespace Domain.Models;

/// <summary>
/// Comprehensive audit log entity for tracking all changes, authentication events,
/// access control events, file operations, and system events.
/// </summary>
public class AuditLog
{
    [Key]
    public Guid Id { get; set; } = Uuid7.New();

    /// <summary>
    /// The name of the entity or module (e.g., "PurchaseOrder", "Authentication", "RoleManagement").
    /// </summary>
    [Required]
    [MaxLength(256)]
    public string EntityName { get; set; } = default!;

    /// <summary>
    /// The primary key of the entity that was changed, or a contextual identifier.
    /// </summary>
    [MaxLength(256)]
    public string? EntityId { get; set; }

    /// <summary>
    /// The type of action performed.
    /// </summary>
    public EAuditAction Action { get; set; }

    /// <summary>
    /// Category for grouping audit events (Data, Authentication, AccessControl, etc.).
    /// </summary>
    public EAuditCategory Category { get; set; }

    /// <summary>
    /// Severity level (Info, Warning, Error, Critical).
    /// </summary>
    public EAuditSeverity Severity { get; set; }

    /// <summary>
    /// JSON representation of the old values (for Update and Delete).
    /// </summary>
    public string? OldValues { get; set; }

    /// <summary>
    /// JSON representation of the new values (for Create and Update).
    /// </summary>
    public string? NewValues { get; set; }

    /// <summary>
    /// JSON representation of the changed properties (for Update only).
    /// </summary>
    public string? ChangedProperties { get; set; }

    /// <summary>
    /// The user who performed the action.
    /// </summary>
    [MaxLength(256)]
    public string? UserId { get; set; }

    /// <summary>
    /// The username of the user who performed the action.
    /// </summary>
    [MaxLength(256)]
    public string? UserName { get; set; }

    /// <summary>
    /// The timestamp when the action was performed.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// The IP address of the client.
    /// </summary>
    [MaxLength(50)]
    public string? IpAddress { get; set; }

    /// <summary>
    /// The user agent of the client.
    /// </summary>
    [MaxLength(512)]
    public string? UserAgent { get; set; }

    /// <summary>
    /// Additional context or correlation ID for tracing.
    /// </summary>
    [MaxLength(256)]
    public string? CorrelationId { get; set; }

    /// <summary>
    /// The session ID associated with this action.
    /// </summary>
    [MaxLength(256)]
    public string? SessionId { get; set; }

    /// <summary>
    /// HTTP method (GET, POST, PUT, DELETE, etc.) for request-based events.
    /// </summary>
    [MaxLength(10)]
    public string? RequestMethod { get; set; }

    /// <summary>
    /// The request URL/path that triggered this audit event.
    /// </summary>
    [MaxLength(2048)]
    public string? RequestUrl { get; set; }

    /// <summary>
    /// Duration in milliseconds for operations that are timed.
    /// </summary>
    public long? DurationMs { get; set; }

    /// <summary>
    /// Whether the action succeeded, failed, or partially succeeded.
    /// </summary>
    [MaxLength(20)]
    public string? Outcome { get; set; }

    /// <summary>
    /// JSON blob for any additional structured data not covered by other fields.
    /// </summary>
    public string? AdditionalData { get; set; }
}

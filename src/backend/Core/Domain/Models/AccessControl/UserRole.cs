using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Models;

/// <summary>
/// Represents the assignment of a role to a user.
/// </summary>
public class UserRole : TimestampedEntity
{
    /// <summary>
    /// The user ID (from external auth system).
    /// </summary>
    [Required]
    [MaxLength(256)]
    public string UserId { get; set; } = default!;

    /// <summary>
    /// The role ID.
    /// </summary>
    public Guid RoleId { get; set; }

    /// <summary>
    /// Navigation property to the role.
    /// </summary>
    [ForeignKey(nameof(RoleId))]
    public virtual Role Role { get; set; } = default!;

    /// <summary>
    /// When the role was assigned.
    /// </summary>
    public DateTime AssignedOn { get; set; }

    /// <summary>
    /// Who assigned the role.
    /// </summary>
    [MaxLength(256)]
    public string? AssignedBy { get; set; }

    /// <summary>
    /// Optional expiration date for the role assignment.
    /// </summary>
    public DateTime? ExpiresOn { get; set; }

    /// <summary>
    /// Whether this role assignment is active.
    /// </summary>
    public bool IsActive { get; set; } = true;
}

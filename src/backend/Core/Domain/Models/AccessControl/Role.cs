using System.ComponentModel.DataAnnotations;

namespace Domain.Models;

/// <summary>
/// Represents a role that can be assigned to users.
/// </summary>
public class Role : TimestampedEntity
{
    /// <summary>
    /// Stable machine-readable code for the role.
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Code { get; set; } = default!;

    /// <summary>
    /// The unique name of the role.
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = default!;

    /// <summary>
    /// Description of the role.
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Whether this role is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Whether this is a system role that cannot be deleted.
    /// </summary>
    public bool IsSystemRole { get; set; } = false;

    /// <summary>
    /// Display ordering in administration screens.
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// Navigation property to user roles.
    /// </summary>
    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();

    /// <summary>
    /// Navigation property to role access functions.
    /// </summary>
    public virtual ICollection<RoleAccessFunction> RoleAccessFunctions { get; set; } = new List<RoleAccessFunction>();

    /// <summary>
    /// Application-scoped assignments using this role.
    /// </summary>
    public virtual ICollection<ApplicationAccess> ApplicationAccesses { get; set; } = new List<ApplicationAccess>();
}

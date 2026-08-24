using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Models;

/// <summary>
/// Assigns a role to a user within one application boundary.
/// </summary>
public class ApplicationAccess : TimestampedEntity
{
    public Guid ApplicationId { get; set; }

    [ForeignKey(nameof(ApplicationId))]
    public virtual Application Application { get; set; } = default!;

    [Required]
    [MaxLength(256)]
    public string UserId { get; set; } = default!;

    public Guid RoleId { get; set; }

    [ForeignKey(nameof(RoleId))]
    public virtual Role Role { get; set; } = default!;

    public DateTime AssignedOn { get; set; }

    [MaxLength(256)]
    public string? AssignedBy { get; set; }

    public DateTime? ExpiresOn { get; set; }

    public bool IsActive { get; set; } = true;
}

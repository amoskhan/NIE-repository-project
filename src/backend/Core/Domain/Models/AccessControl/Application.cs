using System.ComponentModel.DataAnnotations;

namespace Domain.Models;

/// <summary>
/// An application that can receive application-scoped role assignments.
/// </summary>
public class Application : TimestampedEntity
{
    [Required]
    [MaxLength(120)]
    public string Name { get; set; } = default!;

    [MaxLength(500)]
    public string? Description { get; set; }

    [MaxLength(500)]
    public string? Repository { get; set; }

    [MaxLength(200)]
    public string? Branch { get; set; }

    [Required]
    [MaxLength(120)]
    public string ProjectKey { get; set; } = default!;

    public bool IsActive { get; set; } = true;

    public virtual ICollection<ApplicationAccess> AccessAssignments { get; set; } = new List<ApplicationAccess>();
}

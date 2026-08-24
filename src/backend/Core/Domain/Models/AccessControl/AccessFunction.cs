using System.ComponentModel.DataAnnotations;
using Domain.Enums;

namespace Domain.Models;

/// <summary>
/// Atomic access function that protects either a screen or an API capability.
/// Roles grant access by linking to one or more of these functions.
/// </summary>
public class AccessFunction : TimestampedEntity
{
    [Required]
    [MaxLength(120)]
    public string Code { get; set; } = default!;

    [Required]
    [MaxLength(120)]
    public string Name { get; set; } = default!;

    [MaxLength(500)]
    public string? Description { get; set; }

    [Required]
    [MaxLength(80)]
    public string Module { get; set; } = default!;

    public EAccessFunctionType Type { get; set; }

    [Required]
    [MaxLength(120)]
    public string ResourceName { get; set; } = default!;

    [MaxLength(200)]
    public string? Route { get; set; }

    [MaxLength(20)]
    public string? HttpMethod { get; set; }

    public bool IsActive { get; set; } = true;

    public bool IsSystemFunction { get; set; } = true;

    public int DisplayOrder { get; set; }

    public virtual ICollection<RoleAccessFunction> RoleAccessFunctions { get; set; } = new List<RoleAccessFunction>();
}

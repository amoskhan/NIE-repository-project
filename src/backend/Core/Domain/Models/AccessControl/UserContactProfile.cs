using System.ComponentModel.DataAnnotations;

namespace Domain.Models;

/// <summary>
/// Non-secret staff details cached after a directory lookup for access administration.
/// </summary>
public class UserContactProfile : TimestampedEntity
{
    [Required]
    [MaxLength(256)]
    public string UserId { get; set; } = default!;

    [MaxLength(200)]
    public string? DisplayName { get; set; }

    [MaxLength(254)]
    public string? Email { get; set; }

    [MaxLength(120)]
    public string? Department { get; set; }

    [MaxLength(300)]
    public string? DepartmentDescription { get; set; }

    [MaxLength(200)]
    public string? Designation { get; set; }

    [MaxLength(100)]
    public string? Title { get; set; }

    [Required]
    [MaxLength(100)]
    public string Source { get; set; } = "NIE";

    public DateTime LastVerifiedOn { get; set; }

    public bool IsActive { get; set; } = true;
}

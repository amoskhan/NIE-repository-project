using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Models;

/// <summary>
/// Link table between roles and access functions.
/// </summary>
public class RoleAccessFunction : TimestampedEntity
{
    public Guid RoleId { get; set; }

    [ForeignKey(nameof(RoleId))]
    public virtual Role Role { get; set; } = default!;

    public Guid AccessFunctionId { get; set; }

    [ForeignKey(nameof(AccessFunctionId))]
    public virtual AccessFunction AccessFunction { get; set; } = default!;
}

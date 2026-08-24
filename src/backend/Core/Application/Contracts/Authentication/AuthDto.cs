namespace Application.Contracts;

//NOTE: Do not change this dto, this is used for authentication purpose
public class AuthDto
{
    public string UserId { get; set; } = default!;
    public DateTime LastActive { get; set; }

    public string Name { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string Department { get; set; } = default!;

    /// <summary>
    /// The user's primary role ID (from ERole enum). Null if no role assigned.
    /// Kept for backward compatibility.
    /// </summary>
    public int? Role { get; set; }

    /// <summary>
    /// All active role IDs assigned to this user. Supports multiple role assignments.
    /// </summary>
    public List<int> Roles { get; set; } = new();

    /// <summary>
    /// All active role names assigned to this user.
    /// </summary>
    public List<string> RoleNames { get; set; } = new();
}

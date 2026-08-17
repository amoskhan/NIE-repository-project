namespace Auth.Models;

/// <summary>
/// Identity-only session stored in Valkey. No roles or permissions -
/// those are resolved by the Main API on each authenticated request.
/// </summary>
public class AuthSessionDto
{
    public string UserId { get; set; } = default!;
    public DateTime LastActive { get; set; }
    public string Name { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string Department { get; set; } = default!;
}

namespace Domain.Enums;

/// <summary>
/// Predefined system roles.
/// These roles are seeded in the database and cannot be deleted.
/// Enum values are stable business codes only; database primary and foreign keys use UUIDv7.
/// </summary>
public enum ERole
{
    /// <summary>
    /// Full system access - can manage all resources including users and roles.
    /// </summary>
    Administrator = 1,

    /// <summary>
    /// Standard user access - can view and edit assigned resources.
    /// </summary>
    User = 2,

    /// <summary>
    /// Manager access - can manage team resources and approve workflows.
    /// </summary>
    Manager = 3,

    /// <summary>
    /// Read-only access - can only view resources without modification.
    /// </summary>
    Viewer = 4
}

namespace Domain.Enums;

/// <summary>
/// Comprehensive audit action types covering data changes, authentication,
/// access control, file operations, and system events.
/// </summary>
public enum EAuditAction
{
    // ── Data Operations ──
    Create = 1,
    Update = 2,
    Delete = 3,
    Read = 4,
    BulkCreate = 5,
    BulkUpdate = 6,
    BulkDelete = 7,

    // ── Authentication ──
    Login = 10,
    Logout = 11,
    FailedLogin = 12,
    SessionCreated = 13,
    SessionExpired = 14,
    SessionRefreshed = 15,
    PasswordChanged = 16,

    // ── Access Control ──
    RoleAssigned = 20,
    RoleRemoved = 21,
    RoleCreated = 22,
    RoleUpdated = 23,
    RoleDeleted = 24,
    PermissionGranted = 25,
    PermissionRevoked = 26,
    PermissionUpdated = 27,
    AccessDenied = 28,

    // ── File Operations ──
    FileUpload = 30,
    FileDownload = 31,
    FileDelete = 32,

    // ── Data Export/Import ──
    Export = 40,
    Import = 41,

    // ── System Events ──
    SettingsChanged = 50,
    SystemEvent = 51,
    JobExecuted = 52,
    EmailSent = 53,
    DataMigration = 54
}

/// <summary>
/// Audit log severity levels.
/// </summary>
public enum EAuditSeverity
{
    Info = 0,
    Warning = 1,
    Error = 2,
    Critical = 3
}

/// <summary>
/// Audit log categories for grouping and filtering.
/// </summary>
public enum EAuditCategory
{
    Data = 0,
    Authentication = 1,
    AccessControl = 2,
    FileOperation = 3,
    DataTransfer = 4,
    System = 5
}

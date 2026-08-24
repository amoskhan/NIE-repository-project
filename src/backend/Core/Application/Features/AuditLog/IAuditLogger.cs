using Domain.Enums;

namespace Application.Features;

/// <summary>
/// Injectable service for creating manual audit log entries.
/// Use this to audit events that are not captured automatically by DbContext SaveChanges
/// (e.g., authentication, access control changes, file operations, exports, system events).
/// </summary>
public interface IAuditLogger
{
    // ── Generic Logging ──

    /// <summary>
    /// Logs a custom audit event with full control over all fields.
    /// </summary>
    Task LogAsync(
        EAuditAction action,
        EAuditCategory category,
        string entityName,
        string? entityId = null,
        string? oldValues = null,
        string? newValues = null,
        string? outcome = null,
        EAuditSeverity severity = EAuditSeverity.Info,
        string? additionalData = null);

    // ── Authentication Events ──

    /// <summary>
    /// Logs a successful login event.
    /// </summary>
    Task LogLoginAsync(string userId, string userName);

    /// <summary>
    /// Logs a failed login attempt.
    /// </summary>
    Task LogFailedLoginAsync(string userId, string? reason = null);

    /// <summary>
    /// Logs a user logout event.
    /// </summary>
    Task LogLogoutAsync(string userId, string userName);

    /// <summary>
    /// Logs a session refresh event.
    /// </summary>
    Task LogSessionRefreshedAsync(string userId);

    /// <summary>
    /// Logs a session expiration event.
    /// </summary>
    Task LogSessionExpiredAsync(string userId, string sessionId);

    // ── Access Control Events ──

    /// <summary>
    /// Logs a role assignment event.
    /// </summary>
    Task LogRoleAssignedAsync(string targetUserId, string roleName, string? assignedBy = null);

    /// <summary>
    /// Logs a role removal event.
    /// </summary>
    Task LogRoleRemovedAsync(string targetUserId, string roleName, string? removedBy = null);

    /// <summary>
    /// Logs a role access-function change event.
    /// </summary>
    Task LogRoleAccessChangedAsync(string roleCode, IEnumerable<string> accessFunctionCodes, string? oldValues = null, string? newValues = null);

    /// <summary>
    /// Logs an access denied event.
    /// </summary>
    Task LogAccessDeniedAsync(string userId, string accessFunctionCode, string? requestPath = null);

    // ── File Operation Events ──

    /// <summary>
    /// Logs a file upload event.
    /// </summary>
    Task LogFileUploadAsync(string fileName, long fileSize, string? entityName = null, string? entityId = null);

    /// <summary>
    /// Logs a file download event.
    /// </summary>
    Task LogFileDownloadAsync(string fileName, string? entityName = null, string? entityId = null);

    /// <summary>
    /// Logs a file deletion event.
    /// </summary>
    Task LogFileDeleteAsync(string fileName, string? entityName = null, string? entityId = null);

    // ── Data Transfer Events ──

    /// <summary>
    /// Logs a data export event.
    /// </summary>
    Task LogExportAsync(string entityName, int recordCount, string format = "CSV");

    /// <summary>
    /// Logs a data import event.
    /// </summary>
    Task LogImportAsync(string entityName, int recordCount, string format = "CSV");

    // ── System Events ──

    /// <summary>
    /// Logs a settings change event.
    /// </summary>
    Task LogSettingsChangedAsync(string settingName, string? oldValue = null, string? newValue = null);

    /// <summary>
    /// Logs a background job execution event.
    /// </summary>
    Task LogJobExecutedAsync(string jobName, long durationMs, string outcome = "Success", string? details = null);

    /// <summary>
    /// Logs an email sent event.
    /// </summary>
    Task LogEmailSentAsync(string recipient, string subject, string outcome = "Success");
}

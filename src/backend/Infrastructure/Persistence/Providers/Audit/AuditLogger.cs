using System.Text.Json;
using Application.Abstractions.Identity;
using Application.Features.Email;
using Application.Features.PushNotification;
using Domain.Enums;
using Domain.Models;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Providers.Audit;

/// <summary>
/// Implementation of IAuditLogger for creating manual audit log entries.
/// Injects HttpContext for request metadata and UserContextService for user identity.
/// </summary>
public class AuditLogger : IAuditLogger
{
    private const string SuccessOutcome = "Success";

    private readonly MainDbContext _context;
    private readonly IUserContextService _userContextService;
    private readonly IHttpContextAccessor? _httpContextAccessor;
    private readonly ILogger<AuditLogger> _logger;

    public AuditLogger(
        MainDbContext context,
        IUserContextService userContextService,
        IHttpContextAccessor? httpContextAccessor = null,
        ILogger<AuditLogger>? logger = null)
    {
        _context = context;
        _userContextService = userContextService;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<AuditLogger>.Instance;
    }

    // ── Generic Logging ──

    public async Task LogAsync(
        EAuditAction action,
        EAuditCategory category,
        string entityName,
        string? entityId = null,
        string? oldValues = null,
        string? newValues = null,
        string? outcome = null,
        EAuditSeverity severity = EAuditSeverity.Info,
        string? additionalData = null)
    {
        var entry = CreateBaseEntry(action, category, severity, entityName, entityId);
        entry.OldValues = oldValues;
        entry.NewValues = newValues;
        entry.Outcome = outcome ?? SuccessOutcome;
        entry.AdditionalData = additionalData;
        await SaveEntryAsync(entry);
    }

    // ── Authentication Events ──

    public async Task LogLoginAsync(string userId, string userName)
    {
        var entry = CreateBaseEntry(EAuditAction.Login, EAuditCategory.Authentication, EAuditSeverity.Info, "Authentication", userId);
        entry.UserId = userId;
        entry.UserName = userName;
        entry.Outcome = SuccessOutcome;
        entry.NewValues = JsonSerializer.Serialize(new { userId, userName, loginTime = BuildingBlocks.Helpers.DateTimeHelper.Now });
        await SaveEntryAsync(entry);
    }

    public async Task LogFailedLoginAsync(string userId, string? reason = null)
    {
        var entry = CreateBaseEntry(EAuditAction.FailedLogin, EAuditCategory.Authentication, EAuditSeverity.Warning, "Authentication", userId);
        entry.UserId = userId;
        entry.Outcome = "Failed";
        entry.AdditionalData = reason != null ? JsonSerializer.Serialize(new { reason }) : null;
        await SaveEntryAsync(entry);
    }

    public async Task LogLogoutAsync(string userId, string userName)
    {
        var entry = CreateBaseEntry(EAuditAction.Logout, EAuditCategory.Authentication, EAuditSeverity.Info, "Authentication", userId);
        entry.UserId = userId;
        entry.UserName = userName;
        entry.Outcome = SuccessOutcome;
        await SaveEntryAsync(entry);
    }

    public async Task LogSessionRefreshedAsync(string userId)
    {
        var entry = CreateBaseEntry(EAuditAction.SessionRefreshed, EAuditCategory.Authentication, EAuditSeverity.Info, "Session", userId);
        entry.UserId = userId;
        entry.Outcome = SuccessOutcome;
        await SaveEntryAsync(entry);
    }

    public async Task LogSessionExpiredAsync(string userId, string sessionId)
    {
        var entry = CreateBaseEntry(EAuditAction.SessionExpired, EAuditCategory.Authentication, EAuditSeverity.Info, "Session", sessionId);
        entry.UserId = userId;
        entry.Outcome = "Expired";
        await SaveEntryAsync(entry);
    }

    // ── Access Control Events ──

    public async Task LogRoleAssignedAsync(string targetUserId, string roleName, string? assignedBy = null)
    {
        var entry = CreateBaseEntry(EAuditAction.RoleAssigned, EAuditCategory.AccessControl, EAuditSeverity.Info, "UserRole", targetUserId);
        entry.UserId = assignedBy ?? _userContextService.UserId;
        entry.UserName = _userContextService.UserName;
        entry.Outcome = SuccessOutcome;
        entry.NewValues = JsonSerializer.Serialize(new { targetUserId, roleName, assignedBy = assignedBy ?? _userContextService.UserId });
        await SaveEntryAsync(entry);
    }

    public async Task LogRoleRemovedAsync(string targetUserId, string roleName, string? removedBy = null)
    {
        var entry = CreateBaseEntry(EAuditAction.RoleRemoved, EAuditCategory.AccessControl, EAuditSeverity.Info, "UserRole", targetUserId);
        entry.UserId = removedBy ?? _userContextService.UserId;
        entry.UserName = _userContextService.UserName;
        entry.Outcome = SuccessOutcome;
        entry.OldValues = JsonSerializer.Serialize(new { targetUserId, roleName, removedBy = removedBy ?? _userContextService.UserId });
        await SaveEntryAsync(entry);
    }

    public async Task LogRoleAccessChangedAsync(string roleCode, IEnumerable<string> accessFunctionCodes, string? oldValues = null, string? newValues = null)
    {
        var entry = CreateBaseEntry(EAuditAction.PermissionUpdated, EAuditCategory.AccessControl, EAuditSeverity.Info, "Role", roleCode);
        entry.Outcome = SuccessOutcome;
        entry.OldValues = oldValues;
        entry.NewValues = newValues ?? JsonSerializer.Serialize(new
        {
            roleCode,
            accessFunctionCodes = accessFunctionCodes.OrderBy(code => code).ToList()
        });
        await SaveEntryAsync(entry);
    }

    public async Task LogAccessDeniedAsync(string userId, string accessFunctionCode, string? requestPath = null)
    {
        var entry = CreateBaseEntry(EAuditAction.AccessDenied, EAuditCategory.AccessControl, EAuditSeverity.Warning, "Authorization", userId);
        entry.UserId = userId;
        entry.Outcome = "Denied";
        entry.AdditionalData = JsonSerializer.Serialize(new { accessFunctionCode, requestPath });
        await SaveEntryAsync(entry);
    }

    // ── File Operation Events ──

    public async Task LogFileUploadAsync(string fileName, long fileSize, string? entityName = null, string? entityId = null)
    {
        var entry = CreateBaseEntry(EAuditAction.FileUpload, EAuditCategory.FileOperation, EAuditSeverity.Info, entityName ?? "Document", entityId);
        entry.Outcome = SuccessOutcome;
        entry.NewValues = JsonSerializer.Serialize(new { fileName, fileSize });
        await SaveEntryAsync(entry);
    }

    public async Task LogFileDownloadAsync(string fileName, string? entityName = null, string? entityId = null)
    {
        var entry = CreateBaseEntry(EAuditAction.FileDownload, EAuditCategory.FileOperation, EAuditSeverity.Info, entityName ?? "Document", entityId);
        entry.Outcome = SuccessOutcome;
        entry.AdditionalData = JsonSerializer.Serialize(new { fileName });
        await SaveEntryAsync(entry);
    }

    public async Task LogFileDeleteAsync(string fileName, string? entityName = null, string? entityId = null)
    {
        var entry = CreateBaseEntry(EAuditAction.FileDelete, EAuditCategory.FileOperation, EAuditSeverity.Info, entityName ?? "Document", entityId);
        entry.Outcome = SuccessOutcome;
        entry.OldValues = JsonSerializer.Serialize(new { fileName });
        await SaveEntryAsync(entry);
    }

    // ── Data Transfer Events ──

    public async Task LogExportAsync(string entityName, int recordCount, string format = "CSV")
    {
        var entry = CreateBaseEntry(EAuditAction.Export, EAuditCategory.DataTransfer, EAuditSeverity.Info, entityName);
        entry.Outcome = SuccessOutcome;
        entry.AdditionalData = JsonSerializer.Serialize(new { recordCount, format });
        await SaveEntryAsync(entry);
    }

    public async Task LogImportAsync(string entityName, int recordCount, string format = "CSV")
    {
        var entry = CreateBaseEntry(EAuditAction.Import, EAuditCategory.DataTransfer, EAuditSeverity.Info, entityName);
        entry.Outcome = SuccessOutcome;
        entry.AdditionalData = JsonSerializer.Serialize(new { recordCount, format });
        await SaveEntryAsync(entry);
    }

    // ── System Events ──

    public async Task LogSettingsChangedAsync(string settingName, string? oldValue = null, string? newValue = null)
    {
        var entry = CreateBaseEntry(EAuditAction.SettingsChanged, EAuditCategory.System, EAuditSeverity.Info, "Settings", settingName);
        entry.OldValues = oldValue != null ? JsonSerializer.Serialize(new { value = oldValue }) : null;
        entry.NewValues = newValue != null ? JsonSerializer.Serialize(new { value = newValue }) : null;
        entry.Outcome = SuccessOutcome;
        await SaveEntryAsync(entry);
    }

    public async Task LogJobExecutedAsync(string jobName, long durationMs, string outcome = SuccessOutcome, string? details = null)
    {
        var severity = outcome == SuccessOutcome ? EAuditSeverity.Info : EAuditSeverity.Error;
        var entry = CreateBaseEntry(EAuditAction.JobExecuted, EAuditCategory.System, severity, "BackgroundJob", jobName);
        entry.DurationMs = durationMs;
        entry.Outcome = outcome;
        entry.AdditionalData = details;
        await SaveEntryAsync(entry);
    }

    public async Task LogEmailSentAsync(string recipient, string subject, string outcome = SuccessOutcome)
    {
        var entry = CreateBaseEntry(EAuditAction.EmailSent, EAuditCategory.System, EAuditSeverity.Info, "Email", recipient);
        entry.Outcome = outcome;
        entry.AdditionalData = JsonSerializer.Serialize(new { recipient, subject });
        await SaveEntryAsync(entry);
    }

    // ── Private Helpers ──

    private AuditLog CreateBaseEntry(EAuditAction action, EAuditCategory category, EAuditSeverity severity, string entityName, string? entityId = null)
    {
        var httpContext = _httpContextAccessor?.HttpContext;
        return new AuditLog
        {
            EntityName = entityName,
            EntityId = entityId,
            Action = action,
            Category = category,
            Severity = severity,
            UserId = _userContextService.UserId,
            UserName = _userContextService.UserName,
            Timestamp = BuildingBlocks.Helpers.DateTimeHelper.Now,
            IpAddress = httpContext?.Connection?.RemoteIpAddress?.ToString(),
            UserAgent = httpContext?.Request?.Headers["User-Agent"].ToString(),
            CorrelationId = httpContext?.TraceIdentifier,
            SessionId = _userContextService.SessionId,
            RequestMethod = httpContext?.Request?.Method,
            RequestUrl = httpContext?.Request?.Path.Value
        };
    }

    private async Task SaveEntryAsync(AuditLog entry)
    {
        try
        {
            _context.AuditLogs.Add(entry);
            // Use base SaveChanges to avoid recursive audit logging
            await _context.Database.ExecuteSqlRawAsync(
                @"INSERT INTO ""AuditLogs"" (""EntityName"", ""EntityId"", ""Action"", ""Category"", ""Severity"",
                  ""OldValues"", ""NewValues"", ""ChangedProperties"", ""UserId"", ""UserName"",
                  ""Timestamp"", ""IpAddress"", ""UserAgent"", ""CorrelationId"", ""SessionId"",
                  ""RequestMethod"", ""RequestUrl"", ""DurationMs"", ""Outcome"", ""AdditionalData"")
                VALUES ({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}, {10}, {11}, {12}, {13}, {14}, {15}, {16}, {17}, {18}, {19})",
                entry.EntityName, (object?)entry.EntityId ?? DBNull.Value,
                (int)entry.Action, (int)entry.Category, (int)entry.Severity,
                (object?)entry.OldValues ?? DBNull.Value, (object?)entry.NewValues ?? DBNull.Value,
                (object?)entry.ChangedProperties ?? DBNull.Value,
                (object?)entry.UserId ?? DBNull.Value, (object?)entry.UserName ?? DBNull.Value,
                BuildingBlocks.Helpers.DateTimeHelper.AsUnspecified(entry.Timestamp),
                (object?)entry.IpAddress ?? DBNull.Value, (object?)entry.UserAgent ?? DBNull.Value,
                (object?)entry.CorrelationId ?? DBNull.Value, (object?)entry.SessionId ?? DBNull.Value,
                (object?)entry.RequestMethod ?? DBNull.Value, (object?)entry.RequestUrl ?? DBNull.Value,
                (object?)entry.DurationMs ?? DBNull.Value,
                (object?)entry.Outcome ?? DBNull.Value, (object?)entry.AdditionalData ?? DBNull.Value);

            // Remove the tracked entity since we inserted via raw SQL
            _context.Entry(entry).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save audit log entry: {Action} on {Entity}", entry.Action, entry.EntityName);
        }
    }
}

using Application.Contracts;
using Domain.Enums;

namespace Application.Features;

/// <summary>
/// Service for querying audit logs with comprehensive filtering, search, and statistics.
/// </summary>
public interface IAuditLogService
{
    /// <summary>
    /// Gets paginated audit logs based on filter criteria.
    /// </summary>
    Task<AuditLogPagedResultDto> GetAuditLogsAsync(AuditLogFilterDto filter);
    Task<DataTablePageDto<AuditLogDto>> SearchTableAsync(DataTableRequestDto request, CancellationToken cancellationToken = default);
    Task<DataTableFilterOptionPageDto> GetFilterOptionsAsync(DataTableFilterOptionsRequestDto request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets audit logs for a specific entity.
    /// </summary>
    Task<List<AuditLogDto>> GetEntityHistoryAsync(string entityName, string entityId);

    /// <summary>
    /// Gets audit logs for a specific user.
    /// </summary>
    Task<List<AuditLogDto>> GetUserActivityAsync(string userId, int maxRecords = 100);

    /// <summary>
    /// Gets a single audit log entry by ID.
    /// </summary>
    Task<AuditLogDto?> GetByIdAsync(Guid id);

    /// <summary>
    /// Gets audit log summary/statistics for dashboard display.
    /// </summary>
    Task<AuditLogSummaryDto> GetSummaryAsync();

    /// <summary>
    /// Gets distinct entity names that exist in the audit log (for filter dropdowns).
    /// </summary>
    Task<List<string>> GetDistinctEntityNamesAsync();

    /// <summary>
    /// Gets audit logs by category (Authentication, AccessControl, etc.).
    /// </summary>
    Task<List<AuditLogDto>> GetByCategoryAsync(EAuditCategory category, int maxRecords = 100);

    /// <summary>
    /// Gets a count of failed login attempts for a specific user within a time period.
    /// </summary>
    Task<int> GetFailedLoginCountAsync(string userId, TimeSpan period);
}

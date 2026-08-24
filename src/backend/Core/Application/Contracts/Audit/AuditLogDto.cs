using Domain.Enums;

namespace Application.Contracts;

/// <summary>
/// DTO for audit log entries.
/// </summary>
public class AuditLogDto
{
    public Guid Id { get; set; }
    public string EntityName { get; set; } = default!;
    public string? EntityId { get; set; }
    public EAuditAction Action { get; set; }
    public string ActionName => Action.ToString();
    public EAuditCategory Category { get; set; }
    public string CategoryName => Category.ToString();
    public EAuditSeverity Severity { get; set; }
    public string SeverityName => Severity.ToString();
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string? ChangedProperties { get; set; }
    public string? UserId { get; set; }
    public string? UserName { get; set; }
    public DateTime Timestamp { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? CorrelationId { get; set; }
    public string? SessionId { get; set; }
    public string? RequestMethod { get; set; }
    public string? RequestUrl { get; set; }
    public long? DurationMs { get; set; }
    public string? Outcome { get; set; }
    public string? AdditionalData { get; set; }
}

/// <summary>
/// Filter for querying audit logs with comprehensive search options.
/// </summary>
public class AuditLogFilterDto
{
    public string? EntityName { get; set; }
    public string? EntityId { get; set; }
    public EAuditAction? Action { get; set; }
    public EAuditCategory? Category { get; set; }
    public EAuditSeverity? Severity { get; set; }
    public string? UserId { get; set; }
    public string? Keyword { get; set; }
    public string? SessionId { get; set; }
    public string? Outcome { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
    public string? SortBy { get; set; }
    public bool SortDescending { get; set; } = true;
}

/// <summary>
/// Paged result for audit logs.
/// </summary>
public class AuditLogPagedResultDto
{
    public List<AuditLogDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}

/// <summary>
/// Summary/statistics for audit log dashboard.
/// </summary>
public class AuditLogSummaryDto
{
    public int TotalRecords { get; set; }
    public int TodayRecords { get; set; }
    public int FailedLogins { get; set; }
    public int AccessDeniedEvents { get; set; }
    public int ErrorEvents { get; set; }
    public int CriticalEvents { get; set; }
    public Dictionary<string, int> ActionBreakdown { get; set; } = new();
    public Dictionary<string, int> CategoryBreakdown { get; set; } = new();
    public List<AuditLogDto> RecentCriticalEvents { get; set; } = new();
}

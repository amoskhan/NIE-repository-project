using Data.Data;
using Shared.Dto;
using Shared.Enum;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace Services.Services;

/// <summary>
/// Service for querying audit logs with comprehensive filtering, search, and statistics.
/// </summary>
public class AuditLogService : IAuditLogService
{
    private readonly MainDbContext _context;

    public AuditLogService(MainDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<AuditLogPagedResultDto> GetAuditLogsAsync(AuditLogFilterDto filter)
    {
        var query = BuildFilteredQuery(filter);

        // Get total count
        var totalCount = await query.CountAsync();

        // Apply sorting
        query = ApplySorting(query, filter.SortBy, filter.SortDescending);

        // Apply pagination
        var items = await query
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ProjectToType<AuditLogDto>()
            .ToListAsync();

        return new AuditLogPagedResultDto
        {
            Items = items,
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    /// <inheritdoc />
    public async Task<List<AuditLogDto>> GetEntityHistoryAsync(string entityName, string entityId)
    {
        return await _context.AuditLogs
            .AsNoTracking()
            .Where(a => a.EntityName == entityName && a.EntityId == entityId)
            .OrderByDescending(a => a.Timestamp)
            .ProjectToType<AuditLogDto>()
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<List<AuditLogDto>> GetUserActivityAsync(string userId, int maxRecords = 100)
    {
        return await _context.AuditLogs
            .AsNoTracking()
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.Timestamp)
            .Take(maxRecords)
            .ProjectToType<AuditLogDto>()
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<AuditLogDto?> GetByIdAsync(long id)
    {
        var entity = await _context.AuditLogs
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id);

        return entity?.Adapt<AuditLogDto>();
    }

    /// <inheritdoc />
    public async Task<AuditLogSummaryDto> GetSummaryAsync()
    {
        var now = Shared.Helpers.DateTimeHelper.Now;
        var todayStart = now.Date;

        var summary = new AuditLogSummaryDto
        {
            TotalRecords = await _context.AuditLogs.CountAsync(),
            TodayRecords = await _context.AuditLogs.CountAsync(a => a.Timestamp >= todayStart),
            FailedLogins = await _context.AuditLogs.CountAsync(a => a.Action == EAuditAction.FailedLogin && a.Timestamp >= todayStart),
            AccessDeniedEvents = await _context.AuditLogs.CountAsync(a => a.Action == EAuditAction.AccessDenied && a.Timestamp >= todayStart),
            ErrorEvents = await _context.AuditLogs.CountAsync(a => a.Severity == EAuditSeverity.Error && a.Timestamp >= todayStart),
            CriticalEvents = await _context.AuditLogs.CountAsync(a => a.Severity == EAuditSeverity.Critical && a.Timestamp >= todayStart)
        };

        // Action breakdown (last 7 days)
        var sevenDaysAgo = now.AddDays(-7);
        var actionCounts = await _context.AuditLogs
            .AsNoTracking()
            .Where(a => a.Timestamp >= sevenDaysAgo)
            .GroupBy(a => a.Action)
            .Select(g => new { Action = g.Key.ToString(), Count = g.Count() })
            .ToListAsync();
        summary.ActionBreakdown = actionCounts.ToDictionary(x => x.Action, x => x.Count);

        // Category breakdown (last 7 days)
        var categoryCounts = await _context.AuditLogs
            .AsNoTracking()
            .Where(a => a.Timestamp >= sevenDaysAgo)
            .GroupBy(a => a.Category)
            .Select(g => new { Category = g.Key.ToString(), Count = g.Count() })
            .ToListAsync();
        summary.CategoryBreakdown = categoryCounts.ToDictionary(x => x.Category, x => x.Count);

        // Recent critical events
        summary.RecentCriticalEvents = await _context.AuditLogs
            .AsNoTracking()
            .Where(a => a.Severity >= EAuditSeverity.Error)
            .OrderByDescending(a => a.Timestamp)
            .Take(10)
            .ProjectToType<AuditLogDto>()
            .ToListAsync();

        return summary;
    }

    /// <inheritdoc />
    public async Task<List<string>> GetDistinctEntityNamesAsync()
    {
        return await _context.AuditLogs
            .AsNoTracking()
            .Select(a => a.EntityName)
            .Distinct()
            .OrderBy(n => n)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<List<AuditLogDto>> GetByCategoryAsync(EAuditCategory category, int maxRecords = 100)
    {
        return await _context.AuditLogs
            .AsNoTracking()
            .Where(a => a.Category == category)
            .OrderByDescending(a => a.Timestamp)
            .Take(maxRecords)
            .ProjectToType<AuditLogDto>()
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<int> GetFailedLoginCountAsync(string userId, TimeSpan period)
    {
        var cutoff = Shared.Helpers.DateTimeHelper.Now.Subtract(period);
        return await _context.AuditLogs
            .AsNoTracking()
            .CountAsync(a => a.UserId == userId
                          && a.Action == EAuditAction.FailedLogin
                          && a.Timestamp >= cutoff);
    }

    // ── Private Helpers ──

    private IQueryable<Domain.Models.AuditLog> BuildFilteredQuery(AuditLogFilterDto filter)
    {
        var query = _context.AuditLogs.AsNoTracking();

        if (!string.IsNullOrEmpty(filter.EntityName))
            query = query.Where(a => a.EntityName == filter.EntityName);

        if (!string.IsNullOrEmpty(filter.EntityId))
            query = query.Where(a => a.EntityId == filter.EntityId);

        if (filter.Action.HasValue)
            query = query.Where(a => a.Action == filter.Action.Value);

        if (filter.Category.HasValue)
            query = query.Where(a => a.Category == filter.Category.Value);

        if (filter.Severity.HasValue)
            query = query.Where(a => a.Severity == filter.Severity.Value);

        if (!string.IsNullOrEmpty(filter.UserId))
            query = query.Where(a => a.UserId == filter.UserId);

        if (!string.IsNullOrEmpty(filter.SessionId))
            query = query.Where(a => a.SessionId == filter.SessionId);

        if (!string.IsNullOrEmpty(filter.Outcome))
            query = query.Where(a => a.Outcome == filter.Outcome);

        if (filter.FromDate.HasValue)
            query = query.Where(a => a.Timestamp >= filter.FromDate.Value);

        if (filter.ToDate.HasValue)
            query = query.Where(a => a.Timestamp <= filter.ToDate.Value);

        if (!string.IsNullOrEmpty(filter.Keyword))
        {
            var keyword = filter.Keyword.ToLower();
            query = query.Where(a =>
                (a.EntityName != null && a.EntityName.ToLower().Contains(keyword)) ||
                (a.UserName != null && a.UserName.ToLower().Contains(keyword)) ||
                (a.UserId != null && a.UserId.ToLower().Contains(keyword)) ||
                (a.Outcome != null && a.Outcome.ToLower().Contains(keyword)) ||
                (a.RequestUrl != null && a.RequestUrl.ToLower().Contains(keyword)));
        }

        return query;
    }

    private static IQueryable<Domain.Models.AuditLog> ApplySorting(IQueryable<Domain.Models.AuditLog> query, string? sortBy, bool descending)
    {
        return sortBy?.ToLower() switch
        {
            "entityname" => descending ? query.OrderByDescending(a => a.EntityName) : query.OrderBy(a => a.EntityName),
            "action" => descending ? query.OrderByDescending(a => a.Action) : query.OrderBy(a => a.Action),
            "category" => descending ? query.OrderByDescending(a => a.Category) : query.OrderBy(a => a.Category),
            "severity" => descending ? query.OrderByDescending(a => a.Severity) : query.OrderBy(a => a.Severity),
            "username" => descending ? query.OrderByDescending(a => a.UserName) : query.OrderBy(a => a.UserName),
            "userid" => descending ? query.OrderByDescending(a => a.UserId) : query.OrderBy(a => a.UserId),
            _ => descending ? query.OrderByDescending(a => a.Timestamp) : query.OrderBy(a => a.Timestamp)
        };
    }
}

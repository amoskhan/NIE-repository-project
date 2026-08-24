using Application.Abstractions;
using Application.Contracts;
using Application.Features.DataTable;
using Domain.Enums;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace Application.Features;

/// <summary>
/// Service for querying audit logs with comprehensive filtering, search, and statistics.
/// </summary>
public class AuditLogService : IAuditLogService
{
    private readonly IApplicationDbContext _context;

    public AuditLogService(IApplicationDbContext context)
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

    public async Task<DataTablePageDto<AuditLogDto>> SearchTableAsync(
        DataTableRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var query = ApplyTableQuery(_context.AuditLogs.AsNoTracking(), request);
        var ordered = new DataTableSortMap<Domain.Models.AuditLog>()
            .Add("timestamp", item => item.Timestamp)
            .Add("categoryname", item => item.Category)
            .Add("entityname", item => item.EntityName)
            .Add("entityid", item => item.EntityId)
            .Add("actionname", item => item.Action)
            .Add("username", item => item.UserName)
            .Apply(query, request, items => items.OrderByDescending(item => item.Timestamp), item => item.Id);

        return await ordered
            .ProjectToType<AuditLogDto>()
            .ToDataTablePageAsync(request, cancellationToken);
    }

    public Task<DataTableFilterOptionPageDto> GetFilterOptionsAsync(
        DataTableFilterOptionsRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var query = ApplyTableQuery(_context.AuditLogs.AsNoTracking(), request, request.ColumnKey);
        var values = request.ColumnKey.ToLowerInvariant() switch
        {
            "timestamp" => query.Select(item => item.Timestamp.ToString("yyyy-MM-dd")),
            "categoryname" => query.Select(item => item.Category.ToString()),
            "entityname" => query.Select(item => item.EntityName),
            "entityid" => query.Select(item => item.EntityId ?? string.Empty),
            "actionname" => query.Select(item => item.Action.ToString()),
            "username" => query.Select(item => item.UserName ?? item.UserId ?? "System"),
            _ => query.Where(_ => false).Select(item => item.EntityName),
        };
        return values.ToFilterOptionPageAsync(request, cancellationToken: cancellationToken);
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
    public async Task<AuditLogDto?> GetByIdAsync(Guid id)
    {
        var entity = await _context.AuditLogs
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id);

        return entity?.Adapt<AuditLogDto>();
    }

    /// <inheritdoc />
    public async Task<AuditLogSummaryDto> GetSummaryAsync()
    {
        var now = BuildingBlocks.Helpers.DateTimeHelper.Now;
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
        var cutoff = BuildingBlocks.Helpers.DateTimeHelper.Now.Subtract(period);
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

    private static IQueryable<Domain.Models.AuditLog> ApplyTableQuery(
        IQueryable<Domain.Models.AuditLog> query,
        DataTableRequestDto request,
        string? excludedFilter = null)
    {
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var pattern = $"%{request.Search.Trim()}%";
            query = query.Where(item =>
                EF.Functions.ILike(item.EntityName, pattern) ||
                (item.EntityId != null && EF.Functions.ILike(item.EntityId, pattern)) ||
                (item.UserName != null && EF.Functions.ILike(item.UserName, pattern)) ||
                (item.UserId != null && EF.Functions.ILike(item.UserId, pattern)) ||
                (item.RequestUrl != null && EF.Functions.ILike(item.RequestUrl, pattern)));
        }

        foreach (var filter in request.Filters.Where(filter => !filter.Key.Equals(excludedFilter, StringComparison.OrdinalIgnoreCase)))
        {
            var values = filter.Values.Where(value => !string.IsNullOrWhiteSpace(value)).ToList();
            if (values.Count == 0) continue;
            query = filter.Key.ToLowerInvariant() switch
            {
                "timestamp" => query.Where(item => values.Contains(item.Timestamp.ToString("yyyy-MM-dd"))),
                "categoryname" => ApplyCategoryFilter(query, values),
                "entityname" => query.Where(item => values.Contains(item.EntityName)),
                "entityid" => query.Where(item => item.EntityId != null && values.Contains(item.EntityId)),
                "actionname" => ApplyActionFilter(query, values),
                "username" => query.Where(item => values.Contains(item.UserName ?? item.UserId ?? "System")),
                _ => query,
            };
        }
        return query;
    }

    private static IQueryable<Domain.Models.AuditLog> ApplyCategoryFilter(
        IQueryable<Domain.Models.AuditLog> query,
        IEnumerable<string> values)
    {
        var categories = values
            .Select(value => Enum.TryParse<EAuditCategory>(value, true, out var category) ? category : (EAuditCategory?)null)
            .Where(value => value.HasValue)
            .Select(value => value!.Value)
            .ToList();
        return categories.Count == 0 ? query : query.Where(item => categories.Contains(item.Category));
    }

    private static IQueryable<Domain.Models.AuditLog> ApplyActionFilter(
        IQueryable<Domain.Models.AuditLog> query,
        IEnumerable<string> values)
    {
        var actions = values
            .Select(value => Enum.TryParse<EAuditAction>(value, true, out var action) ? action : (EAuditAction?)null)
            .Where(value => value.HasValue)
            .Select(value => value!.Value)
            .ToList();
        return actions.Count == 0 ? query : query.Where(item => actions.Contains(item.Action));
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

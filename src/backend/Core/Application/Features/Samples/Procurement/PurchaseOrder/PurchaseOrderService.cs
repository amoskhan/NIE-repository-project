using Application.Abstractions;
using Application.Contracts;
using Application.Features.DataTable;
using BuildingBlocks.Helpers;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Models = Domain.Models;

namespace Application.Features.PurchaseOrder;

public class PurchaseOrderService : BaseService<Models.PurchaseOrder>, IPurchaseOrderService
{
    private readonly IApplicationDbContext _context;

    public PurchaseOrderService(IApplicationDbContext context) : base(context)
    {
        _context = context;
    }

    public async Task<Models.PurchaseOrder?> GetByIdWithDetailsAsync(
        Guid id,
        bool asNoTracking = true,
        CancellationToken cancellationToken = default)
    {
        var query = Records
            .Include(po => po.Vendor)
            .Include(po => po.Lines).ThenInclude(l => l.CatalogItem)
            .Include(po => po.Approvals)
            .Include(po => po.Documents)
            .AsSplitQuery();

        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }

        return await query.FirstOrDefaultAsync(po => po.Id == id, cancellationToken);
    }

    public async Task<IList<Models.PurchaseOrder>> GetAllWithVendorAsync(CancellationToken cancellationToken = default)
    {
        return await Records
            .AsNoTracking()
            .Include(po => po.Vendor)
            .OrderByDescending(po => po.RequestDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<string> GeneratePoNumberAsync(CancellationToken cancellationToken = default)
    {
        var year = DateTimeHelper.Now.Year;
        var count = await Records.CountAsync(po => po.RequestDate.Year == year, cancellationToken);
        return $"PO-{year}-{(count + 1):D5}";
    }

    public async Task<(IList<Models.PurchaseOrder> Items, int TotalCount)> SearchAsync(
        PurchaseOrderSearchDto filter,
        CancellationToken cancellationToken = default)
    {
        var query = Records
            .AsNoTracking()
            .Include(po => po.Vendor)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = $"%{filter.Search.Trim()}%";
            query = query.Where(po =>
                EF.Functions.ILike(po.PoNumber, search) ||
                (po.Vendor.Name != null && EF.Functions.ILike(po.Vendor.Name, search)) ||
                (po.RequestedByName != null && EF.Functions.ILike(po.RequestedByName, search)));
        }

        if (filter.Status.HasValue)
            query = query.Where(po => po.Status == filter.Status.Value);

        if (filter.VendorId.HasValue)
            query = query.Where(po => po.VendorId == filter.VendorId.Value);

        if (filter.FromDate.HasValue)
            query = query.Where(po => po.RequestDate >= filter.FromDate.Value);

        if (filter.ToDate.HasValue)
            query = query.Where(po => po.RequestDate <= filter.ToDate.Value);

        var totalCount = await query.CountAsync(cancellationToken);

        query = filter.SortBy?.ToLower() switch
        {
            "ponumber" => filter.SortDescending ? query.OrderByDescending(po => po.PoNumber) : query.OrderBy(po => po.PoNumber),
            "totalamount" => filter.SortDescending ? query.OrderByDescending(po => po.TotalAmount) : query.OrderBy(po => po.TotalAmount),
            "status" => filter.SortDescending ? query.OrderByDescending(po => po.Status) : query.OrderBy(po => po.Status),
            _ => filter.SortDescending ? query.OrderByDescending(po => po.RequestDate) : query.OrderBy(po => po.RequestDate)
        };

        var items = await query
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<DataTablePageDto<PurchaseOrderDto>> SearchTableAsync(
        DataTableRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var query = ApplyTableQuery(Records.AsNoTracking(), request);
        var ordered = new DataTableSortMap<Models.PurchaseOrder>()
            .Add("ponumber", item => item.PoNumber)
            .Add("vendorname", item => item.Vendor.Name)
            .Add("totalamount", item => item.TotalAmount)
            .Add("statusname", item => item.Status)
            .Add("requestedbyname", item => item.RequestedByName)
            .Add("requestdate", item => item.RequestDate)
            .Apply(query, request, items => items.OrderByDescending(item => item.RequestDate), item => item.Id);

        return await ordered
            .Select(item => new PurchaseOrderDto
            {
                Id = item.Id,
                PoNumber = item.PoNumber,
                RequestedBy = item.RequestedBy,
                RequestedByName = item.RequestedByName,
                RequestDate = item.RequestDate,
                DeliveryAddress = item.DeliveryAddress,
                ExpectedDeliveryDate = item.ExpectedDeliveryDate,
                Status = item.Status,
                Notes = item.Notes,
                TotalAmount = item.TotalAmount,
                RejectionReason = item.RejectionReason,
                VendorId = item.VendorId,
                VendorName = item.Vendor.Name,
                CreatedOn = item.CreatedOn,
                UpdatedOn = item.UpdatedOn,
                CreatedBy = item.CreatedBy,
                UpdatedBy = item.UpdatedBy,
            })
            .ToDataTablePageAsync(request, cancellationToken);
    }

    public Task<DataTableFilterOptionPageDto> GetFilterOptionsAsync(
        DataTableFilterOptionsRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var query = ApplyTableQuery(Records.AsNoTracking(), request, request.ColumnKey);
        var values = request.ColumnKey.ToLowerInvariant() switch
        {
            "ponumber" => query.Select(item => item.PoNumber),
            "vendorname" => query.Select(item => item.Vendor.Name),
            "totalamount" => query.Select(item => item.TotalAmount.ToString()),
            "statusname" => query.Select(item => item.Status.ToString()),
            "requestedbyname" => query.Select(item => item.RequestedByName ?? item.RequestedBy),
            "requestdate" => query.Select(item => item.RequestDate.ToString("yyyy-MM-dd")),
            _ => query.Where(_ => false).Select(item => item.PoNumber),
        };
        return values.ToFilterOptionPageAsync(request, cancellationToken: cancellationToken);
    }

    private static IQueryable<Models.PurchaseOrder> ApplyTableQuery(
        IQueryable<Models.PurchaseOrder> query,
        DataTableRequestDto request,
        string? excludedFilter = null)
    {
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var pattern = $"%{request.Search.Trim()}%";
            query = query.Where(item =>
                EF.Functions.ILike(item.PoNumber, pattern) ||
                EF.Functions.ILike(item.Vendor.Name, pattern) ||
                (item.RequestedByName != null && EF.Functions.ILike(item.RequestedByName, pattern)) ||
                EF.Functions.ILike(item.RequestedBy, pattern));
        }

        foreach (var filter in request.Filters.Where(filter => !filter.Key.Equals(excludedFilter, StringComparison.OrdinalIgnoreCase)))
        {
            var values = filter.Values.Where(value => !string.IsNullOrWhiteSpace(value)).ToList();
            if (values.Count == 0) continue;
            query = filter.Key.ToLowerInvariant() switch
            {
                "ponumber" => query.Where(item => values.Contains(item.PoNumber)),
                "vendorname" => query.Where(item => values.Contains(item.Vendor.Name)),
                "totalamount" => query.Where(item => values.Contains(item.TotalAmount.ToString())),
                "statusname" => ApplyStatusFilter(query, values),
                "requestedbyname" => query.Where(item => values.Contains(item.RequestedByName ?? item.RequestedBy)),
                "requestdate" => query.Where(item => values.Contains(item.RequestDate.ToString("yyyy-MM-dd"))),
                _ => query,
            };
        }
        return query;
    }

    private static IQueryable<Models.PurchaseOrder> ApplyStatusFilter(
        IQueryable<Models.PurchaseOrder> query,
        IEnumerable<string> values)
    {
        var statuses = values
            .Select(value => Enum.TryParse<EPurchaseOrderStatus>(value, true, out var status) ? status : (EPurchaseOrderStatus?)null)
            .Where(status => status.HasValue)
            .Select(status => status!.Value)
            .ToList();
        return statuses.Count == 0 ? query : query.Where(item => statuses.Contains(item.Status));
    }

    public async Task<SpendOverviewDto> GetSpendOverviewAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeHelper.Now;
        var startOfMonth = new DateTime(now.Year, now.Month, 1);
        var sixMonthsAgo = startOfMonth.AddMonths(-5);
        var pendingStatuses = new[]
        {
            EPurchaseOrderStatus.PendingManagerApproval,
            EPurchaseOrderStatus.PendingFinanceApproval,
            EPurchaseOrderStatus.PendingProcurementApproval
        };

        var purchaseOrders = Records.AsNoTracking();

        var pendingApprovals = await purchaseOrders.CountAsync(po => pendingStatuses.Contains(po.Status), cancellationToken);

        var monthlySpend = await purchaseOrders
            .Where(po => po.RequestDate >= startOfMonth && po.Status == EPurchaseOrderStatus.Approved)
            .SumAsync(po => (decimal?)po.TotalAmount, cancellationToken) ?? 0m;

        var recentOrders = await purchaseOrders.CountAsync(po => po.RequestDate >= now.AddDays(-30), cancellationToken);

        var totalVendors = await _context.Set<Models.Vendor>()
            .AsNoTracking()
            .CountAsync(v => v.IsActive, cancellationToken);

        var totalOrders = await purchaseOrders.CountAsync(cancellationToken);
        var totalSpend = await purchaseOrders
            .Where(po => po.Status == EPurchaseOrderStatus.Approved)
            .SumAsync(po => (decimal?)po.TotalAmount, cancellationToken) ?? 0m;

        var monthlySpendRows = await purchaseOrders
            .Where(po => po.RequestDate >= sixMonthsAgo && po.Status == EPurchaseOrderStatus.Approved)
            .GroupBy(po => new { po.RequestDate.Year, po.RequestDate.Month })
            .Select(group => new
            {
                group.Key.Year,
                group.Key.Month,
                Amount = group.Sum(po => po.TotalAmount)
            })
            .ToListAsync(cancellationToken);

        var monthlySpendLookup = monthlySpendRows.ToDictionary(
            item => new DateTime(item.Year, item.Month, 1),
            item => item.Amount);

        var monthlyTrend = Enumerable.Range(0, 6)
            .Select(i => sixMonthsAgo.AddMonths(i))
            .Select(month => new MonthlySpendItem
            {
                Month = month.ToString("MMM yyyy"),
                Amount = monthlySpendLookup.GetValueOrDefault(month, 0m)
            })
            .ToList();

        var statusBreakdown = await purchaseOrders
            .GroupBy(po => po.Status)
            .Select(g => new StatusCountItem { Status = g.Key.ToString(), Count = g.Count() })
            .ToListAsync(cancellationToken);

        var topVendors = await purchaseOrders
            .Where(po => po.Status == EPurchaseOrderStatus.Approved)
            .GroupBy(po => po.Vendor.Name)
            .Select(g => new TopVendorItem { VendorName = g.Key, TotalSpend = g.Sum(po => po.TotalAmount), OrderCount = g.Count() })
            .OrderByDescending(v => v.TotalSpend)
            .Take(5)
            .ToListAsync(cancellationToken);

        var recentOrdersList = await purchaseOrders
            .OrderByDescending(po => po.RequestDate)
            .Take(10)
            .Select(po => new RecentOrderItem
            {
                Id = po.Id,
                PoNumber = po.PoNumber,
                VendorName = po.Vendor.Name,
                TotalAmount = po.TotalAmount,
                Status = po.Status.ToString(),
                RequestDate = po.RequestDate
            })
            .ToListAsync(cancellationToken);

        return new SpendOverviewDto
        {
            PendingApprovals = pendingApprovals,
            MonthlySpend = monthlySpend,
            RecentOrders = recentOrders,
            TotalVendors = totalVendors,
            TotalOrders = totalOrders,
            TotalSpend = totalSpend,
            MonthlySpendTrend = monthlyTrend,
            StatusBreakdown = statusBreakdown,
            TopVendors = topVendors,
            RecentOrdersList = recentOrdersList
        };
    }

    public async Task<IList<Models.PurchaseOrder>> GetPendingApprovalsAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        return await Records
            .AsNoTracking()
            .Include(po => po.Vendor)
            .Include(po => po.Approvals)
            .Where(po =>
                po.Status == EPurchaseOrderStatus.PendingManagerApproval ||
                po.Status == EPurchaseOrderStatus.PendingFinanceApproval ||
                po.Status == EPurchaseOrderStatus.PendingProcurementApproval)
            .OrderByDescending(po => po.RequestDate)
            .ToListAsync(cancellationToken);
    }
}

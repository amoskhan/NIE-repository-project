using Models = Domain.Models;
using Data.Data;
using Shared.Dto;
using Shared.Enum;
using Microsoft.EntityFrameworkCore;
using Shared.Helpers;

namespace Services.Services.PurchaseOrder;

public class PurchaseOrderService : BaseService<Models.PurchaseOrder>, IPurchaseOrderService
{
    private readonly MainDbContext _context;

    public PurchaseOrderService(MainDbContext context) : base(context)
    {
        _context = context;
    }

    public async Task<Models.PurchaseOrder?> GetByIdWithDetailsAsync(
        int id,
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

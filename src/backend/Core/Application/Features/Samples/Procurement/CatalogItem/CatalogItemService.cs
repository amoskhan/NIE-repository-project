using Application.Abstractions;
using Application.Contracts;
using Application.Features.DataTable;
using Microsoft.EntityFrameworkCore;
using Models = Domain.Models;

namespace Application.Features.CatalogItem;

public class CatalogItemService : BaseService<Models.CatalogItem>, ICatalogItemService
{
    public CatalogItemService(IApplicationDbContext context) : base(context)
    { }

    public async Task<IList<Models.CatalogItem>> GetByVendorAsync(Guid vendorId)
    {
        return await Records.Where(c => c.VendorId == vendorId).Include(c => c.Vendor).ToListAsync();
    }

    public async Task<DataTablePageDto<CatalogItemDto>> SearchTableAsync(
        DataTableRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var query = ApplyQuery(Records.AsNoTracking(), request);
        var ordered = new DataTableSortMap<Models.CatalogItem>()
            .Add("sku", item => item.Sku)
            .Add("name", item => item.Name)
            .Add("vendorname", item => item.Vendor.Name)
            .Add("category", item => item.Category)
            .Add("unitofmeasure", item => item.UnitOfMeasure)
            .Add("unitprice", item => item.UnitPrice)
            .Add("isactive", item => item.IsActive)
            .Apply(query, request, items => items.OrderBy(item => item.Name), item => item.Id);

        return await ordered
            .Select(item => new CatalogItemDto
            {
                Id = item.Id,
                Name = item.Name,
                Sku = item.Sku,
                Description = item.Description,
                Category = item.Category,
                UnitOfMeasure = item.UnitOfMeasure,
                UnitPrice = item.UnitPrice,
                IsActive = item.IsActive,
                VendorId = item.VendorId,
                VendorName = item.Vendor.Name,
                CreatedOn = item.CreatedOn,
                UpdatedOn = item.UpdatedOn,
            })
            .ToDataTablePageAsync(request, cancellationToken);
    }

    public Task<DataTableFilterOptionPageDto> GetFilterOptionsAsync(
        DataTableFilterOptionsRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var query = ApplyQuery(Records.AsNoTracking(), request, request.ColumnKey);
        var values = request.ColumnKey.ToLowerInvariant() switch
        {
            "sku" => query.Select(item => item.Sku ?? string.Empty),
            "name" => query.Select(item => item.Name),
            "vendorname" => query.Select(item => item.Vendor.Name),
            "category" => query.Select(item => item.Category ?? string.Empty),
            "unitofmeasure" => query.Select(item => item.UnitOfMeasure ?? string.Empty),
            "unitprice" => query.Select(item => item.UnitPrice.ToString()),
            "isactive" => query.Select(item => item.IsActive ? "true" : "false"),
            _ => query.Where(_ => false).Select(item => item.Name),
        };
        Func<string, string>? labelFactory = request.ColumnKey.Equals("isActive", StringComparison.OrdinalIgnoreCase)
            ? value => string.Equals(value, "true", StringComparison.OrdinalIgnoreCase) ? "Active" : "Inactive"
            : null;
        return values.ToFilterOptionPageAsync(request, labelFactory, cancellationToken);
    }

    private static IQueryable<Models.CatalogItem> ApplyQuery(
        IQueryable<Models.CatalogItem> query,
        DataTableRequestDto request,
        string? excludedFilter = null)
    {
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var pattern = $"%{request.Search.Trim()}%";
            query = query.Where(item =>
                (item.Sku != null && EF.Functions.ILike(item.Sku, pattern)) ||
                EF.Functions.ILike(item.Name, pattern) ||
                EF.Functions.ILike(item.Vendor.Name, pattern) ||
                (item.Category != null && EF.Functions.ILike(item.Category, pattern)) ||
                (item.UnitOfMeasure != null && EF.Functions.ILike(item.UnitOfMeasure, pattern)));
        }

        foreach (var filter in request.Filters.Where(filter => !filter.Key.Equals(excludedFilter, StringComparison.OrdinalIgnoreCase)))
        {
            var values = filter.Values.Where(value => !string.IsNullOrWhiteSpace(value)).ToList();
            if (values.Count == 0) continue;
            query = filter.Key.ToLowerInvariant() switch
            {
                "sku" => query.Where(item => item.Sku != null && values.Contains(item.Sku)),
                "name" => query.Where(item => values.Contains(item.Name)),
                "vendorname" => query.Where(item => values.Contains(item.Vendor.Name)),
                "category" => query.Where(item => item.Category != null && values.Contains(item.Category)),
                "unitofmeasure" => query.Where(item => item.UnitOfMeasure != null && values.Contains(item.UnitOfMeasure)),
                "unitprice" => query.Where(item => values.Contains(item.UnitPrice.ToString())),
                "isactive" => query.Where(item => values.Contains(item.IsActive ? "true" : "false")),
                _ => query,
            };
        }
        return query;
    }
}

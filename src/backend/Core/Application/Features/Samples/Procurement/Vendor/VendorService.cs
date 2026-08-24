using Application.Abstractions;
using Application.Contracts;
using Application.Features.DataTable;
using Microsoft.EntityFrameworkCore;
using Models = Domain.Models;

namespace Application.Features.Vendor;

public class VendorService : BaseService<Models.Vendor>, IVendorService
{
    public VendorService(IApplicationDbContext context) : base(context)
    { }

    public async Task<IList<Models.Vendor>> GetAllWithCatalogCountAsync()
    {
        return await Records.AsNoTracking().Include(v => v.CatalogItems).ToListAsync();
    }

    public async Task<DataTablePageDto<VendorDto>> SearchTableAsync(
        DataTableRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var query = ApplyQuery(Records.AsNoTracking(), request);
        var ordered = new DataTableSortMap<Models.Vendor>()
            .Add("code", item => item.Code)
            .Add("name", item => item.Name)
            .Add("contactperson", item => item.ContactPerson)
            .Add("email", item => item.Email)
            .Add("phone", item => item.Phone)
            .Add("category", item => item.Category)
            .Add("isactive", item => item.IsActive)
            .Add("catalogitemcount", item => item.CatalogItems.Count)
            .Apply(query, request, items => items.OrderBy(item => item.Name), item => item.Id);

        return await ordered
            .Select(item => new VendorDto
            {
                Id = item.Id,
                Name = item.Name,
                Code = item.Code,
                ContactPerson = item.ContactPerson,
                Email = item.Email,
                Phone = item.Phone,
                Address = item.Address,
                Category = item.Category,
                IsActive = item.IsActive,
                Notes = item.Notes,
                CatalogItemCount = item.CatalogItems.Count,
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
        var query = ApplyQuery(Records.AsNoTracking(), request, request.ColumnKey);
        var values = request.ColumnKey.ToLowerInvariant() switch
        {
            "code" => query.Select(item => item.Code),
            "name" => query.Select(item => item.Name),
            "contactperson" => query.Select(item => item.ContactPerson ?? string.Empty),
            "email" => query.Select(item => item.Email ?? string.Empty),
            "phone" => query.Select(item => item.Phone ?? string.Empty),
            "category" => query.Select(item => item.Category ?? string.Empty),
            "isactive" => query.Select(item => item.IsActive ? "true" : "false"),
            "catalogitemcount" => query.Select(item => item.CatalogItems.Count.ToString()),
            _ => query.Where(_ => false).Select(item => item.Name),
        };
        Func<string, string>? labelFactory = request.ColumnKey.Equals("isActive", StringComparison.OrdinalIgnoreCase)
            ? value => string.Equals(value, "true", StringComparison.OrdinalIgnoreCase) ? "Active" : "Inactive"
            : null;
        return values.ToFilterOptionPageAsync(request, labelFactory, cancellationToken);
    }

    private static IQueryable<Models.Vendor> ApplyQuery(
        IQueryable<Models.Vendor> query,
        DataTableRequestDto request,
        string? excludedFilter = null)
    {
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var pattern = $"%{request.Search.Trim()}%";
            query = query.Where(item =>
                EF.Functions.ILike(item.Code, pattern) ||
                EF.Functions.ILike(item.Name, pattern) ||
                (item.ContactPerson != null && EF.Functions.ILike(item.ContactPerson, pattern)) ||
                (item.Email != null && EF.Functions.ILike(item.Email, pattern)) ||
                (item.Phone != null && EF.Functions.ILike(item.Phone, pattern)) ||
                (item.Category != null && EF.Functions.ILike(item.Category, pattern)));
        }

        foreach (var filter in request.Filters.Where(filter => !filter.Key.Equals(excludedFilter, StringComparison.OrdinalIgnoreCase)))
        {
            var values = filter.Values.Where(value => !string.IsNullOrWhiteSpace(value)).ToList();
            if (values.Count == 0) continue;
            query = filter.Key.ToLowerInvariant() switch
            {
                "code" => query.Where(item => values.Contains(item.Code)),
                "name" => query.Where(item => values.Contains(item.Name)),
                "contactperson" => query.Where(item => item.ContactPerson != null && values.Contains(item.ContactPerson)),
                "email" => query.Where(item => item.Email != null && values.Contains(item.Email)),
                "phone" => query.Where(item => item.Phone != null && values.Contains(item.Phone)),
                "category" => query.Where(item => item.Category != null && values.Contains(item.Category)),
                "isactive" => query.Where(item => values.Contains(item.IsActive ? "true" : "false")),
                "catalogitemcount" => query.Where(item => values.Contains(item.CatalogItems.Count.ToString())),
                _ => query,
            };
        }
        return query;
    }
}

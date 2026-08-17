using Models = Domain.Models;
using Data.Data;
using Microsoft.EntityFrameworkCore;

namespace Services.Services.CatalogItem;

public class CatalogItemService : BaseService<Models.CatalogItem>, ICatalogItemService
{
    public CatalogItemService(MainDbContext context) : base(context)
    { }

    public async Task<IList<Models.CatalogItem>> GetByVendorAsync(int vendorId)
    {
        return await Records.Where(c => c.VendorId == vendorId).Include(c => c.Vendor).ToListAsync();
    }
}

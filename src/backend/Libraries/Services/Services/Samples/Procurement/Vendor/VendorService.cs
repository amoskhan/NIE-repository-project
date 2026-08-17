using Models = Domain.Models;
using Data.Data;
using Microsoft.EntityFrameworkCore;

namespace Services.Services.Vendor;

public class VendorService : BaseService<Models.Vendor>, IVendorService
{
    public VendorService(MainDbContext context) : base(context)
    { }

    public async Task<IList<Models.Vendor>> GetAllWithCatalogCountAsync()
    {
        return await Records.Include(v => v.CatalogItems).ToListAsync();
    }
}

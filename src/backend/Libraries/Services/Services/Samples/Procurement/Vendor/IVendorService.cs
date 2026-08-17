using Models = Domain.Models;
using Domain.Models;

namespace Services.Services.Vendor;

public interface IVendorService : IBaseService<Models.Vendor>
{
    Task<IList<Models.Vendor>> GetAllWithCatalogCountAsync();
}

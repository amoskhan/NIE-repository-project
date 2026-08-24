using Application.Contracts;
using Domain.Models;
using Models = Domain.Models;

namespace Application.Features.Vendor;

public interface IVendorService : IBaseService<Models.Vendor>
{
    Task<IList<Models.Vendor>> GetAllWithCatalogCountAsync();
    Task<DataTablePageDto<VendorDto>> SearchTableAsync(DataTableRequestDto request, CancellationToken cancellationToken = default);
    Task<DataTableFilterOptionPageDto> GetFilterOptionsAsync(DataTableFilterOptionsRequestDto request, CancellationToken cancellationToken = default);
}

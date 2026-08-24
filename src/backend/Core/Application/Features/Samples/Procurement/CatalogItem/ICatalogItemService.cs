using Application.Contracts;
using Models = Domain.Models;
namespace Application.Features.CatalogItem;

public interface ICatalogItemService : IBaseService<Models.CatalogItem>
{
    Task<IList<Models.CatalogItem>> GetByVendorAsync(Guid vendorId);
    Task<DataTablePageDto<CatalogItemDto>> SearchTableAsync(DataTableRequestDto request, CancellationToken cancellationToken = default);
    Task<DataTableFilterOptionPageDto> GetFilterOptionsAsync(DataTableFilterOptionsRequestDto request, CancellationToken cancellationToken = default);
}

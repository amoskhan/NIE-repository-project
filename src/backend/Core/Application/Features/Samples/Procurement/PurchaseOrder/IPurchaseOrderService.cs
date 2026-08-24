using Application.Contracts;
using Domain.Enums;
using Models = Domain.Models;

namespace Application.Features.PurchaseOrder;

public interface IPurchaseOrderService : IBaseService<Models.PurchaseOrder>
{
    Task<Models.PurchaseOrder?> GetByIdWithDetailsAsync(
        Guid id,
        bool asNoTracking = true,
        CancellationToken cancellationToken = default);

    Task<IList<Models.PurchaseOrder>> GetAllWithVendorAsync(CancellationToken cancellationToken = default);

    Task<string> GeneratePoNumberAsync(CancellationToken cancellationToken = default);

    Task<(IList<Models.PurchaseOrder> Items, int TotalCount)> SearchAsync(
        PurchaseOrderSearchDto filter,
        CancellationToken cancellationToken = default);

    Task<DataTablePageDto<PurchaseOrderDto>> SearchTableAsync(
        DataTableRequestDto request,
        CancellationToken cancellationToken = default);

    Task<DataTableFilterOptionPageDto> GetFilterOptionsAsync(
        DataTableFilterOptionsRequestDto request,
        CancellationToken cancellationToken = default);

    Task<SpendOverviewDto> GetSpendOverviewAsync(CancellationToken cancellationToken = default);

    Task<IList<Models.PurchaseOrder>> GetPendingApprovalsAsync(
        string userId,
        CancellationToken cancellationToken = default);
}

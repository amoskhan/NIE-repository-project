using Models = Domain.Models;
using Shared.Dto;
using Shared.Enum;

namespace Services.Services.PurchaseOrder;

public interface IPurchaseOrderService : IBaseService<Models.PurchaseOrder>
{
    Task<Models.PurchaseOrder?> GetByIdWithDetailsAsync(
        int id,
        bool asNoTracking = true,
        CancellationToken cancellationToken = default);

    Task<IList<Models.PurchaseOrder>> GetAllWithVendorAsync(CancellationToken cancellationToken = default);

    Task<string> GeneratePoNumberAsync(CancellationToken cancellationToken = default);

    Task<(IList<Models.PurchaseOrder> Items, int TotalCount)> SearchAsync(
        PurchaseOrderSearchDto filter,
        CancellationToken cancellationToken = default);

    Task<SpendOverviewDto> GetSpendOverviewAsync(CancellationToken cancellationToken = default);

    Task<IList<Models.PurchaseOrder>> GetPendingApprovalsAsync(
        string userId,
        CancellationToken cancellationToken = default);
}

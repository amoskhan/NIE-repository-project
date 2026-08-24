using Application.Abstractions;
using Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Application.Integration;

public sealed class ProcurementIntegrationQuery(
    IApplicationDbContext dbContext) : IProcurementIntegrationQuery
{
    private readonly IApplicationDbContext _dbContext = dbContext;

    public Task<ProcurementPurchaseOrderSummary?> GetPurchaseOrderSummaryAsync(
        Guid purchaseOrderId,
        CancellationToken cancellationToken)
    {
        if (purchaseOrderId == Guid.Empty || purchaseOrderId.Version != 7)
        {
            throw new ArgumentException("The purchase-order ID must be UUIDv7.", nameof(purchaseOrderId));
        }

        return _dbContext.Set<PurchaseOrder>()
            .AsNoTracking()
            .Where(order => order.Id == purchaseOrderId)
            .Select(order => new ProcurementPurchaseOrderSummary(
                order.Id,
                order.PoNumber,
                order.Status.ToString(),
                order.VendorId,
                order.Vendor.Name,
                order.TotalAmount,
                "SGD"))
            .SingleOrDefaultAsync(cancellationToken);
    }
}

using Application.Abstractions;
using Models = Domain.Models;

namespace Application.Features.PurchaseOrderDocument;

public class PurchaseOrderDocumentService : BaseService<Models.PurchaseOrderDocument>, IPurchaseOrderDocumentService
{
    public PurchaseOrderDocumentService(IApplicationDbContext context) : base(context)
    { }
}

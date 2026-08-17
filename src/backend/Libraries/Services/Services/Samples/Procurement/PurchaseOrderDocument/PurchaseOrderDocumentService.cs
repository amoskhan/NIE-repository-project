using Models = Domain.Models;
using Data.Data;

namespace Services.Services.PurchaseOrderDocument;

public class PurchaseOrderDocumentService : BaseService<Models.PurchaseOrderDocument>, IPurchaseOrderDocumentService
{
    public PurchaseOrderDocumentService(MainDbContext context) : base(context)
    { }
}

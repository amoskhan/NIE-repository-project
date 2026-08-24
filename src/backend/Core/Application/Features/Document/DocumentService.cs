using Application.Abstractions;

namespace Application.Features.Document;

public class DocumentService : BaseService<Domain.Models.Document>, IDocumentService
{
    public DocumentService(IApplicationDbContext context)
        : base(context)
    { }
}

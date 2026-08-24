using Application.Abstractions;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Code;

public class CodeService : BaseService<Domain.Models.Code>, ICodeService
{
    public CodeService(IApplicationDbContext context) : base(context)
    { }

    public async Task<IList<Domain.Models.Code>> GetAllByCodeType(ECodeType codeType)
    {
        return await Records.Where(x => x.Type == codeType.ToString()).ToListAsync();
    }
}

using Data.Data;
using Shared.Enum;
using Microsoft.EntityFrameworkCore;

namespace Services.Services.Code;

public class CodeService : BaseService<Domain.Models.Code>, ICodeService
{
    public CodeService(MainDbContext context) : base(context)
    { }

    public async Task<IList<Domain.Models.Code>> GetAllByCodeType(ECodeType codeType)
    {
        return await Records.Where(x => x.Type == codeType.ToString()).ToListAsync();
    }
}

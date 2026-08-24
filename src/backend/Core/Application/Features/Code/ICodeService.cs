using Domain.Enums;

namespace Application.Features.Code;

public interface ICodeService : IBaseService<Domain.Models.Code>
{
    Task<IList<Domain.Models.Code>> GetAllByCodeType(ECodeType codeType);
}

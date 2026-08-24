using Application.Contracts;

namespace Application.Features;

public interface IApplicationService
{
    Task<List<ApplicationDto>> GetActiveAsync();
}

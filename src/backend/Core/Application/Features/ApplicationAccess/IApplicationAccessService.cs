using Application.Contracts;

namespace Application.Features;

public interface IApplicationAccessService
{
    Task<ApplicationAccessDto?> GetByIdAsync(Guid id);
    Task<List<ApplicationAccessDto>> GetForApplicationAsync(Guid applicationId);
    Task<List<ApplicationAccessDto>> AssignManyAsync(AssignAccessDto dto, string? assignedBy);
    Task<ApplicationAccessDto> AssignAsync(AssignApplicationAccessDto dto, string? assignedBy);
    Task<bool> RemoveAsync(Guid id);
    Task<List<Guid>> GetAccessibleApplicationIdsAsync(string userId);
}

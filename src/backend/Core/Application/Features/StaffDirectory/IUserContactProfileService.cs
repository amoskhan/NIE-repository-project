using Application.Contracts;

namespace Application.Features;

public interface IUserContactProfileService
{
    Task UpsertFromStaffAsync(StaffDetailsDto staff, CancellationToken cancellationToken = default);
}

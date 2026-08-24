using Application.Contracts;

namespace Application.Features;

/// <summary>
/// Service for managing user-role assignments.
/// </summary>
public interface IUserRoleService
{
    /// <summary>
    /// Gets all user role assignments.
    /// </summary>
    Task<List<UserRoleListDto>> GetAllUserRolesAsync();

    /// <summary>
    /// Gets aggregated user rows for the access-control screen.
    /// </summary>
    Task<List<UserAccessSummaryDto>> GetAccessControlUsersAsync();
    Task<DataTablePageDto<UserAccessSummaryDto>> SearchAccessControlUsersAsync(DataTableRequestDto request, CancellationToken cancellationToken = default);
    Task<DataTableFilterOptionPageDto> GetAccessControlUserFilterOptionsAsync(DataTableFilterOptionsRequestDto request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all roles for a user.
    /// </summary>
    Task<List<UserRoleDto>> GetUserRolesAsync(string userId);

    /// <summary>
    /// Gets the primary role for a user by username.
    /// </summary>
    Task<Guid?> GetUserRoleByUsernameAsync(string username);

    /// <summary>
    /// Gets all active role IDs and names for a user. Used during login to populate AuthDto.
    /// </summary>
    Task<List<(Guid RoleId, string RoleName)>> GetActiveUserRolesAsync(string userId);

    /// <summary>
    /// Gets all users with a specific role.
    /// </summary>
    Task<List<UserRoleDto>> GetUsersInRoleAsync(Guid roleId);

    /// <summary>
    /// Assigns a role to a user.
    /// </summary>
    Task<UserRoleDto> AssignRoleAsync(AssignRoleDto dto);

    /// <summary>
    /// Assigns multiple global roles as one unit of work.
    /// </summary>
    Task<List<UserRoleDto>> AssignRolesAsync(AssignAccessDto dto);

    /// <summary>
    /// Removes a role from a user.
    /// </summary>
    Task<bool> RemoveRoleAsync(string userId, Guid roleId);

    /// <summary>
    /// Deletes a user role by ID.
    /// </summary>
    Task<bool> DeleteUserRoleAsync(Guid id);

    /// <summary>
    /// Updates a user role assignment.
    /// </summary>
    Task<UserRoleDto?> UpdateAssignmentAsync(Guid userRoleId, DateTime? expiresOn, bool isActive);
}

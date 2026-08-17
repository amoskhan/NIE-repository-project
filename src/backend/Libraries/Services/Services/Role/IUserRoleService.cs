using Shared.Dto;

namespace Services.Services;

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

    /// <summary>
    /// Gets all roles for a user.
    /// </summary>
    Task<List<UserRoleDto>> GetUserRolesAsync(string userId);

    /// <summary>
    /// Gets the primary role for a user by username.
    /// </summary>
    Task<int?> GetUserRoleByUsernameAsync(string username);

    /// <summary>
    /// Gets all active role IDs and names for a user. Used during login to populate AuthDto.
    /// </summary>
    Task<List<(int RoleId, string RoleName)>> GetActiveUserRolesAsync(string userId);

    /// <summary>
    /// Gets all users with a specific role.
    /// </summary>
    Task<List<UserRoleDto>> GetUsersInRoleAsync(int roleId);

    /// <summary>
    /// Assigns a role to a user.
    /// </summary>
    Task<UserRoleDto> AssignRoleAsync(AssignRoleDto dto);

    /// <summary>
    /// Removes a role from a user.
    /// </summary>
    Task<bool> RemoveRoleAsync(string userId, int roleId);

    /// <summary>
    /// Deletes a user role by ID.
    /// </summary>
    Task<bool> DeleteUserRoleAsync(int id);

    /// <summary>
    /// Updates a user role assignment.
    /// </summary>
    Task<UserRoleDto?> UpdateAssignmentAsync(int userRoleId, DateTime? expiresOn, bool isActive);
}

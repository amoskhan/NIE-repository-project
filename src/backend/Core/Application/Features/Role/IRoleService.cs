using Application.Contracts;

namespace Application.Features;

/// <summary>
/// Service for managing roles.
/// </summary>
public interface IRoleService
{
    /// <summary>
    /// Gets all roles.
    /// </summary>
    Task<List<RoleDto>> GetAllAsync();

    /// <summary>
    /// Gets a role by ID with its permissions.
    /// </summary>
    Task<RoleDto?> GetByIdAsync(Guid id);

    /// <summary>
    /// Gets a role by name.
    /// </summary>
    Task<RoleDto?> GetByNameAsync(string name);

    /// <summary>
    /// Creates a new role.
    /// </summary>
    Task<RoleDto> CreateAsync(CreateRoleDto dto);

    /// <summary>
    /// Updates an existing role.
    /// </summary>
    Task<RoleDto?> UpdateAsync(UpdateRoleDto dto);

    /// <summary>
    /// Deletes a role (only if it's not a system role).
    /// </summary>
    Task<bool> DeleteAsync(Guid id);

}

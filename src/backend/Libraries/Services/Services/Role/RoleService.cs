using Data.Data;
using Shared.Dto;
using Domain.Models;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace Services.Services;

/// <summary>
/// Service for managing roles.
/// </summary>
public class RoleService : IRoleService
{
    private readonly MainDbContext _context;
    private readonly IAccessFunctionService _accessFunctionService;

    public RoleService(MainDbContext context, IAccessFunctionService accessFunctionService)
    {
        _context = context;
        _accessFunctionService = accessFunctionService;
    }

    /// <inheritdoc />
    public async Task<List<RoleDto>> GetAllAsync()
    {
        var roles = await _context.Roles
            .AsNoTracking()
            .Include(role => role.RoleAccessFunctions)
                .ThenInclude(link => link.AccessFunction)
            .Include(role => role.UserRoles)
            .OrderBy(r => r.Name)
            .ToListAsync();

        return roles.Select(ToDto).ToList();
    }

    /// <inheritdoc />
    public async Task<RoleDto?> GetByIdAsync(int id)
    {
        var role = await _context.Roles
            .AsNoTracking()
            .Include(r => r.RoleAccessFunctions)
                .ThenInclude(link => link.AccessFunction)
            .Include(r => r.UserRoles)
            .FirstOrDefaultAsync(r => r.Id == id);

        return role == null ? null : ToDto(role);
    }

    /// <inheritdoc />
    public async Task<RoleDto?> GetByNameAsync(string name)
    {
        var role = await _context.Roles
            .AsNoTracking()
            .Include(r => r.RoleAccessFunctions)
                .ThenInclude(link => link.AccessFunction)
            .Include(r => r.UserRoles)
            .FirstOrDefaultAsync(r => r.Name == name);

        return role == null ? null : ToDto(role);
    }

    /// <inheritdoc />
    public async Task<RoleDto> CreateAsync(CreateRoleDto dto)
    {
        var normalizedCode = NormalizeRoleCode(dto.Code);
        var normalizedName = dto.Name.Trim();
        var accessFunctionIds = dto.AccessFunctionIds.Distinct().ToList();

        await ValidateRoleAsync(0, normalizedCode, normalizedName, accessFunctionIds);

        var role = new Role
        {
            Code = normalizedCode,
            Name = normalizedName,
            Description = NormalizeOptional(dto.Description),
            IsActive = dto.IsActive,
            IsSystemRole = false,
            DisplayOrder = await GetNextDisplayOrderAsync()
        };

        _context.Roles.Add(role);
        await _context.SaveChangesAsync();

        _context.RoleAccessFunctions.AddRange(accessFunctionIds.Select(accessFunctionId => new RoleAccessFunction
        {
            RoleId = role.Id,
            AccessFunctionId = accessFunctionId
        }));

        await _context.SaveChangesAsync();

        return (await GetByIdAsync(role.Id))!;
    }

    /// <inheritdoc />
    public async Task<RoleDto?> UpdateAsync(UpdateRoleDto dto)
    {
        var role = await _context.Roles
            .Include(r => r.RoleAccessFunctions)
            .Include(r => r.UserRoles)
            .FirstOrDefaultAsync(r => r.Id == dto.Id);

        if (role == null)
            return null;

        var normalizedCode = NormalizeRoleCode(dto.Code);
        var normalizedName = dto.Name.Trim();
        var accessFunctionIds = dto.AccessFunctionIds.Distinct().ToList();

        await ValidateRoleAsync(role.Id, normalizedCode, normalizedName, accessFunctionIds);

        var affectedUsers = role.UserRoles.Select(userRole => userRole.UserId).ToList();

        role.Code = normalizedCode;
        role.Name = normalizedName;
        role.Description = NormalizeOptional(dto.Description);
        role.IsActive = dto.IsActive;

        var existingLinks = role.RoleAccessFunctions.ToList();
        var existingIds = existingLinks.Select(link => link.AccessFunctionId).ToHashSet();

        foreach (var link in existingLinks.Where(link => !accessFunctionIds.Contains(link.AccessFunctionId)))
        {
            _context.RoleAccessFunctions.Remove(link);
        }

        foreach (var accessFunctionId in accessFunctionIds.Where(id => !existingIds.Contains(id)))
        {
            role.RoleAccessFunctions.Add(new RoleAccessFunction
            {
                RoleId = role.Id,
                AccessFunctionId = accessFunctionId
            });
        }

        await _context.SaveChangesAsync();
        await _accessFunctionService.InvalidateUsersAsync(affectedUsers);

        return await GetByIdAsync(role.Id);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(int id)
    {
        var role = await _context.Roles
            .Include(r => r.UserRoles)
            .Include(r => r.RoleAccessFunctions)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (role == null)
            return false;

        // Prevent deletion of system roles
        if (role.IsSystemRole)
            throw new InvalidOperationException("Cannot delete system roles");

        var affectedUsers = role.UserRoles.Select(userRole => userRole.UserId).ToList();

        _context.Roles.Remove(role);
        await _context.SaveChangesAsync();
        await _accessFunctionService.InvalidateUsersAsync(affectedUsers);

        return true;
    }

    private async Task ValidateRoleAsync(int existingRoleId, string roleCode, string roleName, IReadOnlyCollection<int> accessFunctionIds)
    {
        if (string.IsNullOrWhiteSpace(roleCode))
            throw new InvalidOperationException("Role code is required.");

        if (string.IsNullOrWhiteSpace(roleName))
            throw new InvalidOperationException("Role name is required.");

        if (accessFunctionIds.Count == 0)
            throw new InvalidOperationException("At least one access function must be assigned to a role.");

        var duplicateCodeExists = await _context.Roles.AnyAsync(role =>
            role.Id != existingRoleId && role.Code.ToLower() == roleCode.ToLower());

        if (duplicateCodeExists)
            throw new InvalidOperationException($"Role code '{roleCode}' already exists.");

        var duplicateNameExists = await _context.Roles.AnyAsync(role =>
            role.Id != existingRoleId && role.Name.ToLower() == roleName.ToLower());

        if (duplicateNameExists)
            throw new InvalidOperationException($"Role name '{roleName}' already exists.");

        var validAccessFunctionCount = await _context.AccessFunctions
            .Where(accessFunction => accessFunctionIds.Contains(accessFunction.Id) && accessFunction.IsActive)
            .CountAsync();

        if (validAccessFunctionCount != accessFunctionIds.Count)
            throw new InvalidOperationException("One or more selected access functions are invalid or inactive.");
    }

    private async Task<int> GetNextDisplayOrderAsync()
    {
        return ((await _context.Roles.MaxAsync(role => (int?)role.DisplayOrder)) ?? 0) + 10;
    }

    private static string NormalizeRoleCode(string code)
    {
        return code.Trim().ToUpperInvariant();
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static RoleDto ToDto(Role role)
    {
        var activeUserCount = role.UserRoles.Count(userRole =>
            userRole.IsActive &&
            (userRole.ExpiresOn == null || userRole.ExpiresOn > Shared.Helpers.DateTimeHelper.Now));

        var orderedAccessFunctions = role.RoleAccessFunctions
            .Select(link => link.AccessFunction)
            .Where(accessFunction => accessFunction.IsActive)
            .OrderBy(accessFunction => accessFunction.Module)
            .ThenBy(accessFunction => accessFunction.DisplayOrder)
            .ToList();

        return new RoleDto
        {
            Id = role.Id,
            Code = role.Code,
            Name = role.Name,
            Description = role.Description,
            IsActive = role.IsActive,
            IsSystemRole = role.IsSystemRole,
            DisplayOrder = role.DisplayOrder,
            AssignedUserCount = activeUserCount,
            AccessFunctions = orderedAccessFunctions.Adapt<List<AccessFunctionDto>>(),
            AccessFunctionIds = orderedAccessFunctions.Select(accessFunction => accessFunction.Id).ToList()
        };
    }
}

using Data.Data;
using Shared.Dto;
using Domain.Models;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Shared.Services.UserContext;
using Services.Services.Email;
using Services.Services.PushNotification;

namespace Services.Services;

/// <summary>
/// Service for managing user-role assignments.
/// </summary>
public class UserRoleService : IUserRoleService
{
    private readonly MainDbContext _context;
    private readonly IUserContextService _userContextService;
    private readonly IAccessFunctionService _accessFunctionService;

    public UserRoleService(
        MainDbContext context,
        IUserContextService userContextService,
        IAccessFunctionService accessFunctionService)
    {
        _context = context;
        _userContextService = userContextService;
        _accessFunctionService = accessFunctionService;
    }

    /// <inheritdoc />
    public async Task<List<UserRoleListDto>> GetAllUserRolesAsync()
    {
        return await _context.UserRoles
            .AsNoTracking()
            .Include(ur => ur.Role)
            .OrderBy(ur => ur.UserId)
            .ThenBy(ur => ur.Role.DisplayOrder)
            .Select(ur => new UserRoleListDto
            {
                Id = ur.Id,
                Username = ur.UserId,
                Role = ur.RoleId,
                CreatedAt = ur.AssignedOn,
                RoleCode = ur.Role.Code,
                RoleName = ur.Role.Name,
                AssignedBy = ur.AssignedBy,
                ExpiresOn = ur.ExpiresOn,
                IsActive = ur.IsActive
            })
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<List<UserAccessSummaryDto>> GetAccessControlUsersAsync()
    {
        var assignments = await _context.UserRoles
            .AsNoTracking()
            .Include(userRole => userRole.Role)
            .OrderBy(userRole => userRole.UserId)
            .ThenBy(userRole => userRole.Role.DisplayOrder)
            .ToListAsync();

        var result = new List<UserAccessSummaryDto>();

        foreach (var userGroup in assignments.GroupBy(assignment => assignment.UserId))
        {
            result.Add(new UserAccessSummaryDto
            {
                UserId = userGroup.Key,
                Assignments = userGroup
                    .Select(ToDto)
                    .OrderBy(assignment => assignment.RoleName)
                    .ToList(),
                AccessFunctionCodes = await _accessFunctionService.GetUserAccessFunctionCodesAsync(userGroup.Key)
            });
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<int?> GetUserRoleByUsernameAsync(string username)
    {
        var userRole = await _context.UserRoles
            .AsNoTracking()
            .Where(ur => ur.UserId == username && ur.IsActive)
            .OrderBy(ur => ur.RoleId) // Return highest priority role (Administrator = 1)
            .FirstOrDefaultAsync();

        return userRole?.RoleId;
    }

    /// <inheritdoc />
    public async Task<List<(int RoleId, string RoleName)>> GetActiveUserRolesAsync(string userId)
    {
        return await _context.UserRoles
            .AsNoTracking()
            .Include(ur => ur.Role)
            .Where(ur => ur.UserId == userId && ur.IsActive)
            .Where(ur => ur.ExpiresOn == null || ur.ExpiresOn > Shared.Helpers.DateTimeHelper.Now)
            .Select(ur => new ValueTuple<int, string>(ur.RoleId, ur.Role.Name))
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<bool> DeleteUserRoleAsync(int id)
    {
        var userRole = await _context.UserRoles.FindAsync(id);
        if (userRole == null)
            return false;

        _context.UserRoles.Remove(userRole);
        await _context.SaveChangesAsync();
        await _accessFunctionService.InvalidateUsersAsync(new[] { userRole.UserId });
        return true;
    }

    /// <inheritdoc />
    public async Task<List<UserRoleDto>> GetUserRolesAsync(string userId)
    {
        return await _context.UserRoles
            .AsNoTracking()
            .Include(ur => ur.Role)
            .Where(ur => ur.UserId == userId)
            .Select(ur => new UserRoleDto
            {
                Id = ur.Id,
                UserId = ur.UserId,
                RoleId = ur.RoleId,
                RoleCode = ur.Role.Code,
                RoleName = ur.Role.Name,
                AssignedOn = ur.AssignedOn,
                AssignedBy = ur.AssignedBy,
                ExpiresOn = ur.ExpiresOn,
                IsActive = ur.IsActive
            })
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<List<UserRoleDto>> GetUsersInRoleAsync(int roleId)
    {
        return await _context.UserRoles
            .AsNoTracking()
            .Include(ur => ur.Role)
            .Where(ur => ur.RoleId == roleId)
            .Select(ur => new UserRoleDto
            {
                Id = ur.Id,
                UserId = ur.UserId,
                RoleId = ur.RoleId,
                RoleCode = ur.Role.Code,
                RoleName = ur.Role.Name,
                AssignedOn = ur.AssignedOn,
                AssignedBy = ur.AssignedBy,
                ExpiresOn = ur.ExpiresOn,
                IsActive = ur.IsActive
            })
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<UserRoleDto> AssignRoleAsync(AssignRoleDto dto)
    {
        // Check if assignment already exists
        var existing = await _context.UserRoles
            .Include(ur => ur.Role)
            .FirstOrDefaultAsync(ur => ur.UserId == dto.UserId && ur.RoleId == dto.RoleId);

        if (existing != null)
        {
            existing.IsActive = true;
            existing.ExpiresOn = dto.ExpiresOn;
            existing.AssignedOn = Shared.Helpers.DateTimeHelper.Now;
            existing.AssignedBy = _userContextService.UserId;
            await _context.SaveChangesAsync();
            await _accessFunctionService.InvalidateUsersAsync(new[] { dto.UserId });
            return ToDto(existing);
        }

        var userRole = new UserRole
        {
            UserId = dto.UserId,
            RoleId = dto.RoleId,
            AssignedOn = Shared.Helpers.DateTimeHelper.Now,
            AssignedBy = _userContextService.UserId,
            ExpiresOn = dto.ExpiresOn,
            IsActive = true
        };

        _context.UserRoles.Add(userRole);
        await _context.SaveChangesAsync();
        await _accessFunctionService.InvalidateUsersAsync(new[] { dto.UserId });

        // Load role for DTO
        await _context.Entry(userRole).Reference(ur => ur.Role).LoadAsync();
        return ToDto(userRole);
    }

    /// <inheritdoc />
    public async Task<bool> RemoveRoleAsync(string userId, int roleId)
    {
        var userRole = await _context.UserRoles
            .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId);

        if (userRole == null)
            return false;

        _context.UserRoles.Remove(userRole);
        await _context.SaveChangesAsync();
        await _accessFunctionService.InvalidateUsersAsync(new[] { userId });

        return true;
    }

    /// <inheritdoc />
    public async Task<UserRoleDto?> UpdateAssignmentAsync(int userRoleId, DateTime? expiresOn, bool isActive)
    {
        var userRole = await _context.UserRoles
            .Include(ur => ur.Role)
            .FirstOrDefaultAsync(ur => ur.Id == userRoleId);

        if (userRole == null)
            return null;

        userRole.ExpiresOn = expiresOn;
        userRole.IsActive = isActive;
        await _context.SaveChangesAsync();
        await _accessFunctionService.InvalidateUsersAsync(new[] { userRole.UserId });

        return ToDto(userRole);
    }

    private static UserRoleDto ToDto(UserRole userRole)
    {
        return new UserRoleDto
        {
            Id = userRole.Id,
            UserId = userRole.UserId,
            RoleId = userRole.RoleId,
            RoleCode = userRole.Role.Code,
            RoleName = userRole.Role.Name,
            AssignedOn = userRole.AssignedOn,
            AssignedBy = userRole.AssignedBy,
            ExpiresOn = userRole.ExpiresOn,
            IsActive = userRole.IsActive
        };
    }
}

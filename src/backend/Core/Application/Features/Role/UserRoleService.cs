using Application.Abstractions;
using Application.Abstractions.Identity;
using Application.Contracts;
using Application.Features.DataTable;
using Application.Features.Email;
using Application.Features.PushNotification;
using Domain.Models;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace Application.Features;

/// <summary>
/// Service for managing user-role assignments.
/// </summary>
public class UserRoleService : IUserRoleService
{
    private readonly IApplicationDbContext _context;
    private readonly IUserContextService _userContextService;
    private readonly IAccessFunctionService _accessFunctionService;

    public UserRoleService(
        IApplicationDbContext context,
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
        var userIds = await _context.UserRoles
            .AsNoTracking()
            .Select(item => item.UserId)
            .Union(_context.ApplicationAccesses.AsNoTracking().Select(item => item.UserId))
            .Distinct()
            .OrderBy(userId => userId)
            .ToListAsync();
        return await BuildAccessControlUsersAsync(userIds);
    }

    public async Task<DataTablePageDto<UserAccessSummaryDto>> SearchAccessControlUsersAsync(
        DataTableRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var userIds = ApplyAccessControlUserQuery(BuildAccessControlUserIds(), request);
        var totalCount = await userIds.CountAsync(cancellationToken);
        var ordered = new DataTableSortMap<string>()
            .Add("displaylabel", userId => _context.UserContactProfiles
                .Where(profile => profile.UserId == userId)
                .Select(profile => profile.DisplayName)
                .FirstOrDefault() ?? userId)
            .Add("departmentlabel", userId => _context.UserContactProfiles
                .Where(profile => profile.UserId == userId)
                .Select(profile => profile.DepartmentDescription ?? profile.Department)
                .FirstOrDefault())
            .Add("rolenames", userId => _context.UserRoles
                .Where(item => item.UserId == userId)
                .OrderBy(item => item.Role.Name)
                .Select(item => item.Role.Name)
                .FirstOrDefault())
            .Add("applicationnames", userId => _context.ApplicationAccesses
                .Where(item => item.UserId == userId)
                .OrderBy(item => item.Application.Name)
                .Select(item => item.Application.Name)
                .FirstOrDefault())
            .Apply(userIds, request, items => items.OrderBy(userId => userId), userId => userId);
        var pageUserIds = await ordered
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return new DataTablePageDto<UserAccessSummaryDto>
        {
            Items = await BuildAccessControlUsersAsync(pageUserIds, cancellationToken),
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize,
        };
    }

    public Task<DataTableFilterOptionPageDto> GetAccessControlUserFilterOptionsAsync(
        DataTableFilterOptionsRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var userIds = ApplyAccessControlUserQuery(BuildAccessControlUserIds(), request, request.ColumnKey);
        var values = request.ColumnKey.ToLowerInvariant() switch
        {
            "displaylabel" => userIds.Select(userId => _context.UserContactProfiles.Where(profile => profile.UserId == userId).Select(profile => profile.DisplayName).FirstOrDefault() ?? userId),
            "departmentlabel" => userIds.Select(userId => _context.UserContactProfiles.Where(profile => profile.UserId == userId).Select(profile => profile.DepartmentDescription ?? profile.Department).FirstOrDefault() ?? "Not available"),
            "rolenames" => _context.UserRoles.AsNoTracking().Where(item => userIds.Contains(item.UserId)).Select(item => item.Role.Name),
            "applicationnames" => _context.ApplicationAccesses.AsNoTracking().Where(item => userIds.Contains(item.UserId)).Select(item => item.Application.Name),
            _ => userIds.Where(_ => false),
        };
        return values.ToFilterOptionPageAsync(request, cancellationToken: cancellationToken);
    }

    private IQueryable<string> BuildAccessControlUserIds() =>
        _context.UserRoles
            .AsNoTracking()
            .Select(item => item.UserId)
            .Union(_context.ApplicationAccesses.AsNoTracking().Select(item => item.UserId))
            .Distinct();

    private IQueryable<string> ApplyAccessControlUserQuery(
        IQueryable<string> userIds,
        DataTableRequestDto request,
        string? excludedFilter = null)
    {
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var pattern = $"%{request.Search.Trim()}%";
            userIds = userIds.Where(userId =>
                EF.Functions.ILike(userId, pattern) ||
                _context.UserContactProfiles.Any(profile => profile.UserId == userId &&
                    ((profile.DisplayName != null && EF.Functions.ILike(profile.DisplayName, pattern)) ||
                     (profile.Email != null && EF.Functions.ILike(profile.Email, pattern)) ||
                     (profile.Department != null && EF.Functions.ILike(profile.Department, pattern)) ||
                     (profile.DepartmentDescription != null && EF.Functions.ILike(profile.DepartmentDescription, pattern)) ||
                     (profile.Designation != null && EF.Functions.ILike(profile.Designation, pattern)))) ||
                _context.UserRoles.Any(item => item.UserId == userId &&
                    (EF.Functions.ILike(item.Role.Name, pattern) || EF.Functions.ILike(item.Role.Code, pattern))) ||
                _context.ApplicationAccesses.Any(item => item.UserId == userId &&
                    (EF.Functions.ILike(item.Application.Name, pattern) || EF.Functions.ILike(item.Role.Name, pattern))));
        }

        foreach (var filter in request.Filters.Where(filter => !filter.Key.Equals(excludedFilter, StringComparison.OrdinalIgnoreCase)))
        {
            var values = filter.Values.Where(value => !string.IsNullOrWhiteSpace(value)).ToList();
            if (values.Count == 0) continue;
            userIds = filter.Key.ToLowerInvariant() switch
            {
                "displaylabel" => userIds.Where(userId => values.Contains(_context.UserContactProfiles.Where(profile => profile.UserId == userId).Select(profile => profile.DisplayName).FirstOrDefault() ?? userId)),
                "departmentlabel" => userIds.Where(userId => values.Contains(_context.UserContactProfiles.Where(profile => profile.UserId == userId).Select(profile => profile.DepartmentDescription ?? profile.Department).FirstOrDefault() ?? "Not available")),
                "rolenames" => userIds.Where(userId => _context.UserRoles.Any(item => item.UserId == userId && values.Contains(item.Role.Name))),
                "applicationnames" => userIds.Where(userId => _context.ApplicationAccesses.Any(item => item.UserId == userId && values.Contains(item.Application.Name))),
                _ => userIds,
            };
        }
        return userIds;
    }

    private async Task<List<UserAccessSummaryDto>> BuildAccessControlUsersAsync(
        IReadOnlyCollection<string> userIds,
        CancellationToken cancellationToken = default)
    {
        if (userIds.Count == 0) return [];
        var assignments = await _context.UserRoles
            .AsNoTracking()
            .Include(userRole => userRole.Role)
                .ThenInclude(role => role.RoleAccessFunctions)
                    .ThenInclude(link => link.AccessFunction)
            .OrderBy(userRole => userRole.UserId)
            .ThenBy(userRole => userRole.Role.DisplayOrder)
            .Where(item => userIds.Contains(item.UserId))
            .ToListAsync(cancellationToken);
        var applicationAccesses = await _context.ApplicationAccesses
            .AsNoTracking()
            .Include(item => item.Application)
            .Include(item => item.Role)
                .ThenInclude(role => role.RoleAccessFunctions)
                    .ThenInclude(link => link.AccessFunction)
            .OrderBy(item => item.UserId)
            .ThenBy(item => item.Application.Name)
            .ThenBy(item => item.Role.DisplayOrder)
            .Where(item => userIds.Contains(item.UserId))
            .ToListAsync(cancellationToken);
        var profiles = await _context.UserContactProfiles
            .AsNoTracking()
            .Where(profile => profile.IsActive && userIds.Contains(profile.UserId))
            .ToDictionaryAsync(profile => profile.UserId, StringComparer.OrdinalIgnoreCase, cancellationToken);

        var assignmentsByUser = assignments
            .GroupBy(item => item.UserId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.OrdinalIgnoreCase);
        var applicationAccessesByUser = applicationAccesses
            .GroupBy(item => item.UserId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.OrdinalIgnoreCase);
        var now = BuildingBlocks.Helpers.DateTimeHelper.Now;

        return userIds.Select(userId =>
        {
            assignmentsByUser.TryGetValue(userId, out var userAssignments);
            applicationAccessesByUser.TryGetValue(userId, out var userApplicationAccesses);
            profiles.TryGetValue(userId, out var profile);
            userAssignments ??= [];
            userApplicationAccesses ??= [];

            var effectiveRoles = userAssignments
                .Where(item => item.IsActive && item.Role.IsActive && (item.ExpiresOn == null || item.ExpiresOn > now))
                .Select(item => item.Role)
                .Concat(userApplicationAccesses
                    .Where(item => item.IsActive && item.Role.IsActive && (item.ExpiresOn == null || item.ExpiresOn > now))
                    .Select(item => item.Role));

            return new UserAccessSummaryDto
            {
                UserId = userId,
                DisplayName = profile?.DisplayName,
                Email = profile?.Email,
                Department = profile?.Department,
                DepartmentDescription = profile?.DepartmentDescription,
                Designation = profile?.Designation,
                Title = profile?.Title,
                ProfileSource = profile?.Source,
                Assignments = userAssignments.Select(ToDto).OrderBy(item => item.RoleName).ToList(),
                ApplicationAccesses = userApplicationAccesses.Select(ToDto).ToList(),
                AccessFunctionCodes = effectiveRoles
                    .SelectMany(role => role.RoleAccessFunctions)
                    .Select(link => link.AccessFunction)
                    .Where(accessFunction => accessFunction.IsActive)
                    .Select(accessFunction => accessFunction.Code)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(code => code)
                    .ToList()
            };
        }).ToList();
    }

    /// <inheritdoc />
    public async Task<Guid?> GetUserRoleByUsernameAsync(string username)
    {
        var userRole = await _context.UserRoles
            .AsNoTracking()
            .Where(ur => ur.UserId == username && ur.IsActive)
            .OrderBy(ur => ur.Role.DisplayOrder)
            .FirstOrDefaultAsync();

        return userRole?.RoleId;
    }

    /// <inheritdoc />
    public async Task<List<(Guid RoleId, string RoleName)>> GetActiveUserRolesAsync(string userId)
    {
        return await _context.UserRoles
            .AsNoTracking()
            .Include(ur => ur.Role)
            .Where(ur => ur.UserId == userId && ur.IsActive)
            .Where(ur => ur.ExpiresOn == null || ur.ExpiresOn > BuildingBlocks.Helpers.DateTimeHelper.Now)
            .Select(ur => new ValueTuple<Guid, string>(ur.RoleId, ur.Role.Name))
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<bool> DeleteUserRoleAsync(Guid id)
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
    public async Task<List<UserRoleDto>> GetUsersInRoleAsync(Guid roleId)
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
        var results = await AssignRolesAsync(new AssignAccessDto
        {
            UserId = dto.UserId,
            Scope = AccessAssignmentScope.Global,
            RoleIds = [dto.RoleId],
            ExpiresOn = dto.ExpiresOn
        });
        return results.Single();
    }

    /// <inheritdoc />
    public async Task<List<UserRoleDto>> AssignRolesAsync(AssignAccessDto dto)
    {
        var userId = (dto.UserId ?? string.Empty).Trim().ToLowerInvariant();
        var roleIds = dto.RoleIds.Distinct().ToList();
        var validRoleCount = await _context.Roles.CountAsync(role => roleIds.Contains(role.Id) && role.IsActive);
        if (validRoleCount != roleIds.Count)
        {
            throw new InvalidOperationException("One or more roles were not found or are inactive.");
        }

        var existingAssignments = await _context.UserRoles
            .Where(item => item.UserId == userId && roleIds.Contains(item.RoleId))
            .ToDictionaryAsync(item => item.RoleId);

        foreach (var roleId in roleIds)
        {
            if (existingAssignments.TryGetValue(roleId, out var existing))
            {
                existing.IsActive = true;
                existing.ExpiresOn = dto.ExpiresOn;
                existing.AssignedOn = BuildingBlocks.Helpers.DateTimeHelper.Now;
                existing.AssignedBy = _userContextService.UserId;
                continue;
            }

            _context.UserRoles.Add(new UserRole
            {
                UserId = userId,
                RoleId = roleId,
                AssignedOn = BuildingBlocks.Helpers.DateTimeHelper.Now,
                AssignedBy = _userContextService.UserId,
                ExpiresOn = dto.ExpiresOn,
                IsActive = true
            });
        }

        await _context.SaveChangesAsync();
        await _accessFunctionService.InvalidateUsersAsync([userId]);

        return await _context.UserRoles
            .AsNoTracking()
            .Include(item => item.Role)
            .Where(item => item.UserId == userId && roleIds.Contains(item.RoleId))
            .OrderBy(item => item.Role.DisplayOrder)
            .Select(item => new UserRoleDto
            {
                Id = item.Id,
                UserId = item.UserId,
                RoleId = item.RoleId,
                RoleCode = item.Role.Code,
                RoleName = item.Role.Name,
                AssignedOn = item.AssignedOn,
                AssignedBy = item.AssignedBy,
                ExpiresOn = item.ExpiresOn,
                IsActive = item.IsActive
            })
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<bool> RemoveRoleAsync(string userId, Guid roleId)
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
    public async Task<UserRoleDto?> UpdateAssignmentAsync(Guid userRoleId, DateTime? expiresOn, bool isActive)
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

    private static ApplicationAccessDto ToDto(ApplicationAccess access)
    {
        return new ApplicationAccessDto
        {
            Id = access.Id,
            ApplicationId = access.ApplicationId,
            ApplicationName = access.Application.Name,
            ApplicationProjectKey = access.Application.ProjectKey,
            UserId = access.UserId,
            RoleId = access.RoleId,
            RoleCode = access.Role.Code,
            RoleName = access.Role.Name,
            AssignedOn = access.AssignedOn,
            AssignedBy = access.AssignedBy,
            ExpiresOn = access.ExpiresOn,
            IsActive = access.IsActive
        };
    }
}

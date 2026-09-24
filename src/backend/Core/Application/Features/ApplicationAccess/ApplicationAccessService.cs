using Application.Abstractions;
using Application.Contracts;
using BuildingBlocks.Helpers;
using Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Application.Features;

public sealed class ApplicationAccessService : IApplicationAccessService
{
    private readonly IApplicationDbContext _context;
    private readonly IAccessFunctionService _accessFunctionService;

    public ApplicationAccessService(
        IApplicationDbContext context,
        IAccessFunctionService accessFunctionService)
    {
        _context = context;
        _accessFunctionService = accessFunctionService;
    }

    public async Task<ApplicationAccessDto?> GetByIdAsync(Guid id)
    {
        var assignment = await AssignmentQuery().FirstOrDefaultAsync(item => item.Id == id);
        return assignment is null ? null : ToDto(assignment);
    }

    public async Task<List<ApplicationAccessDto>> GetForApplicationAsync(Guid applicationId) =>
        (await AssignmentQuery()
            .Where(item => item.ApplicationId == applicationId)
            .OrderBy(item => item.UserId)
            .ThenBy(item => item.Role.DisplayOrder)
            .ToListAsync())
        .Select(ToDto)
        .ToList();

    public async Task<ApplicationAccessDto> AssignAsync(
        AssignApplicationAccessDto dto,
        string? assignedBy)
    {
        var request = new AssignAccessDto
        {
            UserId = dto.UserId,
            Scope = AccessAssignmentScope.Application,
            RoleIds = [dto.RoleId],
            ApplicationIds = [dto.ApplicationId],
            ExpiresOn = dto.ExpiresOn
        };

        return (await AssignManyAsync(request, assignedBy)).Single();
    }

    public async Task<List<ApplicationAccessDto>> AssignManyAsync(
        AssignAccessDto dto,
        string? assignedBy)
    {
        var userId = NormalizeUserId(dto.UserId);
        var roleIds = dto.RoleIds.Distinct().ToList();
        var applicationIds = dto.ApplicationIds.Distinct().ToList();

        var validRoles = await _context.Roles
            .Where(role => roleIds.Contains(role.Id) && role.IsActive)
            .Select(role => role.Id)
            .ToListAsync();
        if (validRoles.Count != roleIds.Count)
        {
            throw new InvalidOperationException("One or more roles were not found or are inactive.");
        }

        var validApplications = await _context.Applications
            .Where(application => applicationIds.Contains(application.Id) && application.IsActive)
            .Select(application => application.Id)
            .ToListAsync();
        if (validApplications.Count != applicationIds.Count)
        {
            throw new InvalidOperationException("One or more applications were not found or are inactive.");
        }

        var existing = await _context.ApplicationAccesses
            .Where(item =>
                item.UserId == userId &&
                applicationIds.Contains(item.ApplicationId) &&
                roleIds.Contains(item.RoleId))
            .ToDictionaryAsync(item => (item.ApplicationId, item.RoleId));

        foreach (var applicationId in applicationIds)
        {
            foreach (var roleId in roleIds)
            {
                if (existing.TryGetValue((applicationId, roleId), out var assignment))
                {
                    assignment.IsActive = true;
                    assignment.ExpiresOn = dto.ExpiresOn;
                    assignment.AssignedOn = DateTimeHelper.Now;
                    assignment.AssignedBy = assignedBy;
                    continue;
                }

                _context.ApplicationAccesses.Add(new ApplicationAccess
                {
                    ApplicationId = applicationId,
                    UserId = userId,
                    RoleId = roleId,
                    AssignedOn = DateTimeHelper.Now,
                    AssignedBy = assignedBy,
                    ExpiresOn = dto.ExpiresOn,
                    IsActive = true
                });
            }
        }

        await _context.SaveChangesAsync();
        await _accessFunctionService.InvalidateUsersAsync([userId]);

        return (await AssignmentQuery()
            .Where(item =>
                item.UserId == userId &&
                applicationIds.Contains(item.ApplicationId) &&
                roleIds.Contains(item.RoleId))
            .OrderBy(item => item.Application.Name)
            .ThenBy(item => item.Role.DisplayOrder)
            .ToListAsync())
        .Select(ToDto)
        .ToList();
    }

    public async Task<bool> RemoveAsync(Guid id)
    {
        var assignment = await _context.ApplicationAccesses.FindAsync(id);
        if (assignment is null)
        {
            return false;
        }

        _context.ApplicationAccesses.Remove(assignment);
        await _context.SaveChangesAsync();
        await _accessFunctionService.InvalidateUsersAsync([assignment.UserId]);
        return true;
    }

    public async Task<List<Guid>> GetAccessibleApplicationIdsAsync(string userId)
    {
        var normalizedUserId = NormalizeUserId(userId);
        var now = DateTimeHelper.Now;
        var hasGlobalRole = await _context.UserRoles
            .AsNoTracking()
            .AnyAsync(item =>
                item.UserId == normalizedUserId &&
                item.IsActive &&
                item.Role.IsActive &&
                (item.ExpiresOn == null || item.ExpiresOn > now));

        if (hasGlobalRole)
        {
            return await _context.Applications
                .AsNoTracking()
                .Where(application => application.IsActive)
                .OrderBy(application => application.Name)
                .Select(application => application.Id)
                .ToListAsync();
        }

        return await _context.ApplicationAccesses
            .AsNoTracking()
            .Where(item =>
                item.UserId == normalizedUserId &&
                item.IsActive &&
                item.Application.IsActive &&
                item.Role.IsActive &&
                (item.ExpiresOn == null || item.ExpiresOn > now))
            .Select(item => item.ApplicationId)
            .Distinct()
            .ToListAsync();
    }

    private IQueryable<ApplicationAccess> AssignmentQuery() =>
        _context.ApplicationAccesses
            .AsNoTracking()
            .Include(item => item.Application)
            .Include(item => item.Role);

    private static string NormalizeUserId(string userId) =>
        string.IsNullOrWhiteSpace(userId)
            ? throw new InvalidOperationException("User ID is required.")
            : userId.Trim().ToLowerInvariant();

    private static ApplicationAccessDto ToDto(ApplicationAccess assignment) => new()
    {
        Id = assignment.Id,
        ApplicationId = assignment.ApplicationId,
        ApplicationName = assignment.Application.Name,
        ApplicationProjectKey = assignment.Application.ProjectKey,
        UserId = assignment.UserId,
        RoleId = assignment.RoleId,
        RoleCode = assignment.Role.Code,
        RoleName = assignment.Role.Name,
        AssignedOn = assignment.AssignedOn,
        AssignedBy = assignment.AssignedBy,
        ExpiresOn = assignment.ExpiresOn,
        IsActive = assignment.IsActive
    };
}

using System.Text.Json;
using System.Text.RegularExpressions;
using Api.Authorization;
using Application.Contracts;
using Application.Features;
using Application.Security;
using Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

/// <summary>
/// Manages access functions, roles, and user-role assignments.
/// Access is modeled through screen-level and API-level access functions.
/// </summary>
public partial class AccessControlController : BaseController
{
    private const int MaxEmailLength = 254;
    private readonly IUserRoleService _userRoleService;
    private readonly IAccessFunctionService _accessFunctionService;
    private readonly IRoleService _roleService;
    private readonly IAuditLogger _auditLogger;
    private readonly IApplicationService _applicationService;
    private readonly IApplicationAccessService _applicationAccessService;
    private readonly IStaffDirectoryService _staffDirectoryService;
    private readonly IUserContactProfileService _userContactProfileService;

    public AccessControlController(
        IUserRoleService userRoleService,
        IAccessFunctionService accessFunctionService,
        IRoleService roleService,
        IAuditLogger auditLogger,
        IApplicationService applicationService,
        IApplicationAccessService applicationAccessService,
        IStaffDirectoryService staffDirectoryService,
        IUserContactProfileService userContactProfileService)
    {
        _userRoleService = userRoleService;
        _accessFunctionService = accessFunctionService;
        _roleService = roleService;
        _auditLogger = auditLogger;
        _applicationService = applicationService;
        _applicationAccessService = applicationAccessService;
        _staffDirectoryService = staffDirectoryService;
        _userContactProfileService = userContactProfileService;
    }

    /// <summary>
    /// Returns the complete access-control snapshot used by the administration screen.
    /// </summary>
    [HttpGet]
    [RequireAccessFunction(AccessFunctionCodes.Api.AccessControlRead)]
    public async Task<ActionResult<AccessControlOverviewDto>> GetOverview()
    {
        var overview = new AccessControlOverviewDto
        {
            Users = [],
            Roles = await _roleService.GetAllAsync(),
            AccessFunctions = await _accessFunctionService.GetAllAsync(),
            Applications = await _applicationService.GetActiveAsync()
        };

        return Ok(overview);
    }

    [HttpPost]
    [RequireAccessFunction(AccessFunctionCodes.Api.AccessControlRead)]
    public async Task<ActionResult<DataTablePageDto<UserAccessSummaryDto>>> SearchUsers(
        [FromBody] DataTableRequestDto request,
        CancellationToken cancellationToken) =>
        Ok(await _userRoleService.SearchAccessControlUsersAsync(request, cancellationToken));

    [HttpPost]
    [RequireAccessFunction(AccessFunctionCodes.Api.AccessControlRead)]
    public async Task<ActionResult<DataTableFilterOptionPageDto>> GetUserFilterOptions(
        [FromBody] DataTableFilterOptionsRequestDto request,
        CancellationToken cancellationToken) =>
        Ok(await _userRoleService.GetAccessControlUserFilterOptionsAsync(request, cancellationToken));

    /// <summary>
    /// Returns the current user's role and access profile for screen-level checks.
    /// </summary>
    [HttpGet]
    [RequireAccessFunction(AccessFunctionCodes.Api.AccessProfileRead)]
    public async Task<ActionResult<CurrentAccessProfileDto>> GetCurrentAccessProfile()
    {
        if (string.IsNullOrWhiteSpace(UserId))
        {
            return Unauthorized();
        }

        var userRoles = await _userRoleService.GetUserRolesAsync(UserId);
        var accessFunctionCodes = await _accessFunctionService.GetUserAccessFunctionCodesAsync(UserId);
        var applicationIds = await _applicationAccessService.GetAccessibleApplicationIdsAsync(UserId);

        return Ok(new CurrentAccessProfileDto
        {
            UserId = UserId,
            RoleCodes = userRoles
                .Where(role => role.IsActive)
                .Select(role => role.RoleCode)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(code => code)
                .ToList(),
            RoleNames = userRoles
                .Where(role => role.IsActive)
                .Select(role => role.RoleName)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(name => name)
                .ToList(),
            AccessFunctionCodes = accessFunctionCodes,
            ApplicationIds = applicationIds
        });
    }

    /// <summary>
    /// Creates a new role with its granted access functions.
    /// </summary>
    [HttpPost]
    [RequireAccessFunction(AccessFunctionCodes.Api.AccessControlRolesManage)]
    public async Task<ActionResult<RoleDto>> CreateRole([FromBody] CreateRoleDto dto)
    {
        var role = await _roleService.CreateAsync(dto);

        await _auditLogger.LogAsync(
            EAuditAction.RoleCreated,
            EAuditCategory.AccessControl,
            "Role",
            role.Id.ToString(),
            newValues: JsonSerializer.Serialize(role));

        await _auditLogger.LogRoleAccessChangedAsync(
            role.Code,
            role.AccessFunctions.Select(accessFunction => accessFunction.Code),
            newValues: JsonSerializer.Serialize(role.AccessFunctions.Select(accessFunction => accessFunction.Code)));

        return Ok(role);
    }

    /// <summary>
    /// Updates an existing role and its granted access functions.
    /// </summary>
    [HttpPost]
    [RequireAccessFunction(AccessFunctionCodes.Api.AccessControlRolesManage)]
    public async Task<ActionResult<RoleDto>> UpdateRole([FromBody] UpdateRoleDto dto)
    {
        var existing = await _roleService.GetByIdAsync(dto.Id);
        if (existing == null)
        {
            return NotFound("Role not found.");
        }

        var role = await _roleService.UpdateAsync(dto);
        if (role == null)
        {
            return NotFound("Role not found.");
        }

        await _auditLogger.LogAsync(
            EAuditAction.RoleUpdated,
            EAuditCategory.AccessControl,
            "Role",
            role.Id.ToString(),
            oldValues: JsonSerializer.Serialize(existing),
            newValues: JsonSerializer.Serialize(role));

        await _auditLogger.LogRoleAccessChangedAsync(
            role.Code,
            role.AccessFunctions.Select(accessFunction => accessFunction.Code),
            oldValues: JsonSerializer.Serialize(existing.AccessFunctions.Select(accessFunction => accessFunction.Code)),
            newValues: JsonSerializer.Serialize(role.AccessFunctions.Select(accessFunction => accessFunction.Code)));

        return Ok(role);
    }

    /// <summary>
    /// Deletes a non-system role.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [RequireAccessFunction(AccessFunctionCodes.Api.AccessControlRolesManage)]
    public async Task<ActionResult> DeleteRole(Guid id)
    {
        var existing = await _roleService.GetByIdAsync(id);
        if (existing == null)
        {
            return NotFound("Role not found.");
        }

        await _roleService.DeleteAsync(id);

        await _auditLogger.LogAsync(
            EAuditAction.RoleDeleted,
            EAuditCategory.AccessControl,
            "Role",
            id.ToString(),
            oldValues: JsonSerializer.Serialize(existing));

        return NoContent();
    }

    /// <summary>
    /// Assigns a role to a user.
    /// </summary>
    [HttpPost]
    [RequireAccessFunction(AccessFunctionCodes.Api.AccessControlAssignmentsManage)]
    public async Task<ActionResult<UserRoleDto>> AssignRole([FromBody] AssignRoleDto dto)
    {
        var assignment = await _userRoleService.AssignRoleAsync(dto);
        await _auditLogger.LogRoleAssignedAsync(dto.UserId, assignment.RoleName, UserId);
        return Ok(assignment);
    }

    /// <summary>
    /// Assigns one or more roles globally or to one or more applications.
    /// </summary>
    [HttpPost]
    [RequireAccessFunction(
        AccessFunctionCodes.Api.AccessControlAssignmentsManage,
        AccessFunctionCodes.Api.ApplicationAccessManage)]
    public async Task<ActionResult<AccessAssignmentResultDto>> AssignAccess([FromBody] AssignAccessDto dto)
    {
        if (string.IsNullOrWhiteSpace(UserId))
        {
            return Unauthorized();
        }

        var requiredAccessFunction = dto.Scope == AccessAssignmentScope.Application
            ? AccessFunctionCodes.Api.ApplicationAccessManage
            : AccessFunctionCodes.Api.AccessControlAssignmentsManage;
        if (!await _accessFunctionService.HasAccessAsync(UserId, requiredAccessFunction))
        {
            return Forbid();
        }

        var result = new AccessAssignmentResultDto();
        if (dto.Scope == AccessAssignmentScope.Application)
        {
            result.ApplicationAssignments = await _applicationAccessService.AssignManyAsync(dto, UserId);
            foreach (var assignment in result.ApplicationAssignments)
            {
                await _auditLogger.LogRoleAssignedAsync(
                    assignment.UserId,
                    $"{assignment.RoleName} ({assignment.ApplicationName})",
                    UserId);
            }
        }
        else
        {
            result.GlobalAssignments = await _userRoleService.AssignRolesAsync(dto);
            foreach (var assignment in result.GlobalAssignments)
            {
                await _auditLogger.LogRoleAssignedAsync(assignment.UserId, assignment.RoleName, UserId);
            }
        }

        return Ok(result);
    }

    /// <summary>
    /// Resolves and caches a staff profile before access is assigned.
    /// </summary>
    [HttpGet]
    [RequireAccessFunction(
        AccessFunctionCodes.Api.AccessControlAssignmentsManage,
        AccessFunctionCodes.Api.ApplicationAccessManage)]
    public async Task<ActionResult<StaffDetailsDto>> LookupStaff([FromQuery] string email)
    {
        var normalizedEmail = (email ?? string.Empty).Trim();
        if (normalizedEmail.Length > MaxEmailLength || !EmailPattern().IsMatch(normalizedEmail))
        {
            return BadRequest("A valid email address is required.");
        }

        try
        {
            var staff = await _staffDirectoryService.GetStaffDetailsByEmailAsync(normalizedEmail);
            if (staff is null)
            {
                return NotFound("No staff record matches that email address.");
            }

            await _userContactProfileService.UpsertFromStaffAsync(staff, HttpContext.RequestAborted);
            await _auditLogger.LogAsync(
                EAuditAction.Read,
                EAuditCategory.AccessControl,
                "StaffDirectory",
                normalizedEmail,
                outcome: "Success",
                additionalData: JsonSerializer.Serialize(new { resolvedUserId = staff.UserId }));
            return Ok(staff);
        }
        catch (StaffDirectoryUnavailableException)
        {
            await _auditLogger.LogAsync(
                EAuditAction.Read,
                EAuditCategory.AccessControl,
                "StaffDirectory",
                normalizedEmail,
                outcome: "Unavailable",
                severity: EAuditSeverity.Warning);
            return StatusCode(StatusCodes.Status502BadGateway, "The staff directory did not respond.");
        }
    }

    /// <summary>
    /// Returns assignments for an application.
    /// </summary>
    [HttpGet("{applicationId:guid}")]
    [RequireAccessFunction(AccessFunctionCodes.Api.ApplicationAccessManage)]
    public async Task<ActionResult<List<ApplicationAccessDto>>> GetApplicationAccess(Guid applicationId)
    {
        return Ok(await _applicationAccessService.GetForApplicationAsync(applicationId));
    }

    /// <summary>
    /// Removes one application-scoped assignment.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [RequireAccessFunction(AccessFunctionCodes.Api.ApplicationAccessManage)]
    public async Task<ActionResult> RemoveApplicationAccess(Guid id)
    {
        var existing = await _applicationAccessService.GetByIdAsync(id);
        if (existing is null)
        {
            return NotFound("Application access assignment not found.");
        }

        if (!await _applicationAccessService.RemoveAsync(id))
        {
            return NotFound("Application access assignment not found.");
        }

        await _auditLogger.LogRoleRemovedAsync(
            existing.UserId,
            $"{existing.RoleName} ({existing.ApplicationName})",
            UserId);
        return NoContent();
    }

    /// <summary>
    /// Removes a user-role assignment by assignment ID.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [RequireAccessFunction(AccessFunctionCodes.Api.AccessControlAssignmentsManage)]
    public async Task<ActionResult> RemoveAssignment(Guid id)
    {
        var existing = (await _userRoleService.GetAllUserRolesAsync()).FirstOrDefault(assignment => assignment.Id == id);
        if (existing == null)
        {
            return NotFound("Assignment not found.");
        }

        var deleted = await _userRoleService.DeleteUserRoleAsync(id);
        if (!deleted)
        {
            return NotFound("Assignment not found.");
        }

        await _auditLogger.LogRoleRemovedAsync(existing.Username, existing.RoleName ?? existing.Role.ToString(), UserId);
        return NoContent();
    }

    [GeneratedRegex(@"^[^\s@]+@[^\s@]+\.[^\s@]+$", RegexOptions.CultureInvariant)]
    private static partial Regex EmailPattern();
}

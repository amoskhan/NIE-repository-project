using System.Text.Json;
using API.Authorization;
using API.Sessions;
using Shared.Dto;
using Shared.Enum;
using Shared.Security;
using Services.Services;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

/// <summary>
/// Manages access functions, roles, user-role assignments, and the local accounts those
/// assignments point at.
/// Access is modeled through screen-level and API-level access functions.
/// </summary>
public class AccessControlController : BaseController
{
    private readonly IUserRoleService _userRoleService;
    private readonly IAccessFunctionService _accessFunctionService;
    private readonly IRoleService _roleService;
    private readonly IUserAccountService _userAccountService;
    private readonly ISessionRevocationService _sessionRevocationService;
    private readonly IAuditLogger _auditLogger;

    public AccessControlController(
        IUserRoleService userRoleService,
        IAccessFunctionService accessFunctionService,
        IRoleService roleService,
        IUserAccountService userAccountService,
        ISessionRevocationService sessionRevocationService,
        IAuditLogger auditLogger)
    {
        _userRoleService = userRoleService;
        _accessFunctionService = accessFunctionService;
        _roleService = roleService;
        _userAccountService = userAccountService;
        _sessionRevocationService = sessionRevocationService;
        _auditLogger = auditLogger;
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
            Users = await _userRoleService.GetAccessControlUsersAsync(),
            Roles = await _roleService.GetAllAsync(),
            AccessFunctions = await _accessFunctionService.GetAllAsync()
        };

        return Ok(overview);
    }

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
            AccessFunctionCodes = accessFunctionCodes
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
    [HttpDelete("{id:int}")]
    [RequireAccessFunction(AccessFunctionCodes.Api.AccessControlRolesManage)]
    public async Task<ActionResult> DeleteRole(int id)
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
    /// Removes a user-role assignment by assignment ID.
    /// </summary>
    [HttpDelete("{id:int}")]
    [RequireAccessFunction(AccessFunctionCodes.Api.AccessControlAssignmentsManage)]
    public async Task<ActionResult> RemoveAssignment(int id)
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

    // ── Local accounts ──
    // Roles above are assignments; the endpoints below own the accounts they are assigned TO.
    // Sign-in against these accounts is the Auth API's job — nothing here checks a password.

    /// <summary>
    /// Lists every local account for the administration Users screen.
    /// </summary>
    /// <remarks>
    /// <see cref="GetOverview"/> is keyed on the login name and only knows about users who already
    /// hold a role assignment. This is the account table itself, so a freshly created user appears
    /// here before anyone has granted them anything — and it is where the numeric <c>id</c> that
    /// <see cref="ApproveUser"/> and <see cref="DeactivateUser"/> take comes from.
    /// </remarks>
    [HttpGet]
    [RequireAccessFunction(AccessFunctionCodes.Api.AccessControlRead)]
    public async Task<ActionResult<List<UserAccountDto>>> GetUserAccounts()
    {
        return Ok(await _userAccountService.GetAllAsync(HttpContext.RequestAborted));
    }

    /// <summary>
    /// Creates a local account on a user's behalf with an administrator-issued password.
    /// </summary>
    /// <remarks>
    /// The new account is active and flagged <c>MustChangePassword</c>, so the initial password is
    /// a handover value rather than a credential the administrator keeps. This is separate from
    /// self-registration on the Auth API and ignores <c>LocalIdentity:AllowSelfRegistration</c>.
    /// </remarks>
    [HttpPost]
    [RequireAccessFunction(AccessFunctionCodes.Api.AccessControlUsersManage)]
    public async Task<ActionResult<UserAccountDto>> RegisterUser([FromBody] RegisterUserAccountDto dto)
    {
        var (ok, error, account) = await _userAccountService.RegisterAsync(dto, HttpContext.RequestAborted);

        if (!ok || account == null)
        {
            return BadRequest(error ?? "The account could not be created.");
        }

        await _auditLogger.LogAsync(
            EAuditAction.Create,
            EAuditCategory.AccessControl,
            "UserAccount",
            account.Id.ToString(),
            newValues: JsonSerializer.Serialize(account));

        return Ok(account);
    }

    /// <summary>
    /// Approves an account so it can sign in, clearing any lockout left over from failed attempts.
    /// </summary>
    [HttpPost]
    [RequireAccessFunction(AccessFunctionCodes.Api.AccessControlUsersManage)]
    public async Task<ActionResult<UserAccountDto>> ApproveUser([FromQuery] int id)
    {
        var existing = await _userAccountService.GetByIdAsync(id, HttpContext.RequestAborted);
        if (existing == null)
        {
            return NotFound("User account not found.");
        }

        var account = await _userAccountService.SetActiveAsync(id, true, HttpContext.RequestAborted);
        if (account == null)
        {
            return NotFound("User account not found.");
        }

        await _auditLogger.LogAsync(
            EAuditAction.Update,
            EAuditCategory.AccessControl,
            "UserAccount",
            account.Id.ToString(),
            oldValues: JsonSerializer.Serialize(existing),
            newValues: JsonSerializer.Serialize(account));

        return Ok(account);
    }

    /// <summary>
    /// Deactivates an account and revokes its live sessions.
    /// </summary>
    /// <remarks>
    /// Clearing <c>IsActive</c> only stops the NEXT sign-in — a session already minted into Valkey
    /// would keep working until it expired. Both halves have to happen for a deactivation to mean
    /// anything, so the response reports how many sessions were actually revoked, and says so
    /// when Valkey could not be reached rather than implying a clean revocation.
    /// </remarks>
    [HttpPost]
    [RequireAccessFunction(AccessFunctionCodes.Api.AccessControlUsersManage)]
    public async Task<ActionResult<DeactivateUserResultDto>> DeactivateUser([FromQuery] int id)
    {
        var existing = await _userAccountService.GetByIdAsync(id, HttpContext.RequestAborted);
        if (existing == null)
        {
            return NotFound("User account not found.");
        }

        // Without this an administrator can revoke their own session mid-request and lock the
        // last admin out of the screen that would undo it.
        if (string.Equals(existing.UserId, UserId, StringComparison.Ordinal))
        {
            return BadRequest("You cannot deactivate your own account.");
        }

        var account = await _userAccountService.SetActiveAsync(id, false, HttpContext.RequestAborted);
        if (account == null)
        {
            return NotFound("User account not found.");
        }

        var sessionsRevoked = await _sessionRevocationService.RevokeUserSessionsAsync(
            account.UserId,
            HttpContext.RequestAborted);

        await _auditLogger.LogAsync(
            EAuditAction.Update,
            EAuditCategory.AccessControl,
            "UserAccount",
            account.Id.ToString(),
            oldValues: JsonSerializer.Serialize(existing),
            newValues: JsonSerializer.Serialize(account),
            outcome: sessionsRevoked.HasValue ? "Success" : "PartialSuccess",
            severity: sessionsRevoked.HasValue ? EAuditSeverity.Info : EAuditSeverity.Warning,
            additionalData: JsonSerializer.Serialize(new { sessionsRevoked }));

        return Ok(new DeactivateUserResultDto
        {
            Account = account,
            SessionsRevoked = sessionsRevoked,
            Warning = sessionsRevoked.HasValue
                ? null
                : "The account was deactivated, but its live sessions could not be revoked because "
                  + "the session store was unreachable. They will stop working when they expire."
        });
    }
}

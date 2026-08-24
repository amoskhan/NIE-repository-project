using Application.Security;
using BuildingBlocks.Globals;
using Domain.Abstractions;
using Domain.Models;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

/// <summary>
/// Base controller providing common functionality for all API controllers.
/// </summary>
[ApiController]
[Route("api/[controller]/[action]")]
public abstract class BaseController : ControllerBase
{
    protected string? UserId => HttpContext.Items[Constants.KeySessionUserId]?.ToString();
    protected string? UserName => HttpContext.Items[Constants.KeySessionUserName]?.ToString();
    protected string? UserEmail => HttpContext.Items[Constants.KeySessionUserEmail]?.ToString();
    protected string? SessionId => HttpContext.Items[Constants.KeySessionSessionId]?.ToString();
    protected string? UserDepartment => HttpContext.Items[Constants.KeySessionUserDept]?.ToString();

    protected List<string> UserRoles
    {
        get
        {
            var roles = HttpContext.Items[Constants.KeySessionUserRoles] as List<string>;
            return roles ?? new List<string>();
        }
    }

    protected bool IsInRole(string role)
    {
        return UserRoles.Contains(role, StringComparer.OrdinalIgnoreCase);
    }

    protected List<string> UserAccessFunctions
    {
        get
        {
            var accessFunctions = HttpContext.Items[Constants.KeySessionUserAccessFunctions] as List<string>;
            return accessFunctions ?? new List<string>();
        }
    }

    protected bool IsAdmin =>
        UserAccessFunctions.Contains(AccessFunctionCodes.Api.AccessControlRead, StringComparer.OrdinalIgnoreCase);

    protected object GetCurrentUser()
    {
        return new
        {
            UserId,
            UserName,
            UserEmail,
            Department = UserDepartment,
            Roles = UserRoles,
            SessionId
        };
    }

    /// <summary>
    /// Per-record ownership guard for entities implementing <see cref="IOwnedEntity"/>.
    /// Closes OWASP API1 (BOLA). Returns:
    /// <list type="bullet">
    ///   <item><c>null</c> — caller is allowed (record exists and user owns it, OR user is admin)</item>
    ///   <item><see cref="NotFoundResult"/> — no record with that id</item>
    ///   <item><see cref="ForbidResult"/> — record belongs to another user and caller is not admin</item>
    /// </list>
    /// Use as: <c>var guard = await EnsureOwnedAsync(id, _service); if (guard != null) return guard;</c>
    /// </summary>
    protected async Task<IActionResult?> EnsureOwnedAsync<TEntity>(Guid id, Application.Features.IBaseService<TEntity> service)
        where TEntity : BaseEntity, IOwnedEntity
    {
        if (IsAdmin) return null;

        var entity = await service.GetByIdAsync(id);
        if (entity == null) return NotFound();
        if (!string.Equals(entity.OwnerUserId, UserId, StringComparison.Ordinal))
            return Forbid();

        return null;
    }
}

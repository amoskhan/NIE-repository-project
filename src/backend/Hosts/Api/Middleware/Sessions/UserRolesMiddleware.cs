using Application.Features;
using BuildingBlocks.Globals;

namespace Api.Middleware;

/// <summary>
/// Middleware that runs after SessionValidationMiddleware to load user roles
/// from the database and set them in HttpContext.Items for use by BaseController.
/// This keeps SessionValidationMiddleware unchanged while enabling role-based access.
/// </summary>
public class UserRolesMiddleware
{
    private readonly RequestDelegate _next;

    public UserRolesMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(
        HttpContext context,
        IUserRoleService userRoleService,
        IAccessFunctionService accessFunctionService)
    {
        // Only load roles if a user is already authenticated (set by SessionValidationMiddleware)
        var userId = context.Items[Constants.KeySessionUserId]?.ToString();
        if (!string.IsNullOrEmpty(userId))
        {
            var userRoles = await userRoleService.GetUserRolesAsync(userId);
            var roleNames = userRoles
                .Where(r => r.IsActive)
                .Select(r => r.RoleName)
                .ToList();

            context.Items[Constants.KeySessionUserRoles] = roleNames;
            context.Items[Constants.KeySessionUserAccessFunctions] =
                await accessFunctionService.GetUserAccessFunctionCodesAsync(userId);
        }

        await _next(context);
    }
}

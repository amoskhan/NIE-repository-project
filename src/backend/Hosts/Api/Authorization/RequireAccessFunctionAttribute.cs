using Application.Features;
using BuildingBlocks.Globals;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Api.Authorization;

/// <summary>
/// Ensures the current user has one of the specified access functions.
/// Uses explicit access-function codes instead of controller/action pattern matching.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class RequireAccessFunctionAttribute : Attribute, IAsyncAuthorizationFilter
{
    private readonly string[] _accessFunctionCodes;

    public RequireAccessFunctionAttribute(params string[] accessFunctionCodes)
    {
        _accessFunctionCodes = accessFunctionCodes
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        if (_accessFunctionCodes.Length == 0)
        {
            context.Result = new ForbidResult();
            return;
        }

        var httpContext = context.HttpContext;
        var userId = httpContext.Items[Constants.KeySessionUserId]?.ToString();
        if (string.IsNullOrWhiteSpace(userId))
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        var grantedCodes = httpContext.Items[Constants.KeySessionUserAccessFunctions] as List<string>;
        var hasAccess = grantedCodes != null && grantedCodes.Any(code =>
            _accessFunctionCodes.Contains(code, StringComparer.OrdinalIgnoreCase));

        if (!hasAccess)
        {
            var accessFunctionService = httpContext.RequestServices.GetRequiredService<IAccessFunctionService>();
            foreach (var accessFunctionCode in _accessFunctionCodes)
            {
                if (await accessFunctionService.HasAccessAsync(userId, accessFunctionCode))
                {
                    hasAccess = true;
                    break;
                }
            }
        }

        if (hasAccess)
        {
            return;
        }

        var auditLogger = httpContext.RequestServices.GetRequiredService<IAuditLogger>();
        await auditLogger.LogAccessDeniedAsync(
            userId,
            string.Join(", ", _accessFunctionCodes),
            httpContext.Request.Path.Value);

        context.Result = new ForbidResult();
    }
}

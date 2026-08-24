using BuildingBlocks.Globals;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;

namespace Api.Tests.TestSupport;

/// <summary>
/// Hand-built MVC plumbing (HttpContext / ActionContext / filter contexts) so the
/// authorization and action filters can be exercised without a host or TestServer.
/// </summary>
internal static class MvcTestContext
{
    public static DefaultHttpContext CreateHttpContext(
        string? userId = null,
        IEnumerable<string>? accessFunctions = null,
        IEnumerable<string>? roles = null,
        IServiceProvider? services = null)
    {
        var httpContext = new DefaultHttpContext();

        if (services is not null)
        {
            httpContext.RequestServices = services;
        }

        if (userId is not null)
        {
            httpContext.Items[Constants.KeySessionUserId] = userId;
        }

        if (accessFunctions is not null)
        {
            httpContext.Items[Constants.KeySessionUserAccessFunctions] = accessFunctions.ToList();
        }

        if (roles is not null)
        {
            httpContext.Items[Constants.KeySessionUserRoles] = roles.ToList();
        }

        return httpContext;
    }

    public static ActionContext CreateActionContext(HttpContext httpContext, RouteData? routeData = null) =>
        new(httpContext, routeData ?? new RouteData(), new ActionDescriptor());

    public static AuthorizationFilterContext CreateAuthorizationContext(HttpContext httpContext) =>
        new(CreateActionContext(httpContext), []);

    public static RouteData CreateRouteData(string name, object? value)
    {
        var routeData = new RouteData();
        routeData.Values[name] = value;
        return routeData;
    }
}

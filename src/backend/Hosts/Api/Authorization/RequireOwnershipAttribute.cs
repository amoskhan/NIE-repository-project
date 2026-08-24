using Application.Security;
using BuildingBlocks.Globals;
using Domain.Abstractions;
using Domain.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace Api.Authorization;

/// <summary>
/// Action attribute that enforces per-record ownership for entities implementing
/// <see cref="IOwnedEntity"/>. Pair it with a <c>[RequireAccessFunction(...)]</c> attribute
/// on the same action — function-level authorization is necessary but not sufficient
/// (OWASP API1 — BOLA).
///
/// Usage:
/// <code>
/// [HttpGet("{id}")]
/// [RequireAccessFunction(AccessFunctionCodes.Api.FooRead)]
/// [RequireOwnership(typeof(Foo), "id")]
/// public async Task&lt;ActionResult&lt;FooDto&gt;&gt; Get(Guid id) { /* ... */ }
/// </code>
///
/// The filter resolves the entity by primary key, compares <see cref="IOwnedEntity.OwnerUserId"/>
/// to the session user id, and short-circuits with <see cref="ForbidResult"/> if the user is
/// not the owner and not an admin (<c>UserAccessFunctions</c> contains
/// <see cref="AccessFunctionCodes.Api.AccessControlRead"/>).
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class RequireOwnershipAttribute : Attribute, IFilterFactory
{
    public Type EntityType { get; }
    public string RouteParameterName { get; }

    public bool IsReusable => false;

    public RequireOwnershipAttribute(Type entityType, string routeParameterName = "id")
    {
        if (!typeof(IOwnedEntity).IsAssignableFrom(entityType))
            throw new ArgumentException(
                $"{entityType.Name} must implement {nameof(IOwnedEntity)} to be used with {nameof(RequireOwnershipAttribute)}.",
                nameof(entityType));

        EntityType = entityType;
        RouteParameterName = routeParameterName;
    }

    public IFilterMetadata CreateInstance(IServiceProvider serviceProvider)
    {
        var filterType = typeof(OwnedEntityActionFilter<>).MakeGenericType(EntityType);
        return (IFilterMetadata)ActivatorUtilities.CreateInstance(serviceProvider, filterType, RouteParameterName)!;
    }
}

internal sealed class OwnedEntityActionFilter<TEntity> : IAsyncActionFilter
    where TEntity : class, IOwnedEntity
{
    private readonly DbContext _dbContext;
    private readonly string _routeParameterName;

    public OwnedEntityActionFilter(DbContext dbContext, string routeParameterName)
    {
        _dbContext = dbContext;
        _routeParameterName = routeParameterName;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var http = context.HttpContext;

        // Admin bypass — same definition BaseController.IsAdmin uses.
        var accessFunctions = http.Items[Constants.KeySessionUserAccessFunctions] as List<string> ?? new List<string>();
        var isAdmin = accessFunctions.Contains(AccessFunctionCodes.Api.AccessControlRead, StringComparer.OrdinalIgnoreCase);
        if (isAdmin)
        {
            await next();
            return;
        }

        if (!context.RouteData.Values.TryGetValue(_routeParameterName, out var rawId)
            || rawId == null
            || !int.TryParse(rawId.ToString(), out var id))
        {
            context.Result = new BadRequestObjectResult(
                $"Route parameter '{_routeParameterName}' is required and must be an integer for ownership-checked endpoints.");
            return;
        }

        var entity = await _dbContext.Set<TEntity>().FindAsync(new object[] { id }, http.RequestAborted);
        if (entity == null)
        {
            context.Result = new NotFoundResult();
            return;
        }

        var currentUserId = http.Items[Constants.KeySessionUserId]?.ToString();
        if (string.IsNullOrEmpty(currentUserId)
            || !string.Equals(entity.OwnerUserId, currentUserId, StringComparison.Ordinal))
        {
            context.Result = new ForbidResult();
            return;
        }

        await next();
    }
}

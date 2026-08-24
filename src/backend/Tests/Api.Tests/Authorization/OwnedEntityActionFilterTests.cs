using Api.Authorization;
using Api.Tests.TestSupport;
using Application.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Api.Tests.Authorization;

/// <summary>
/// Per-record ownership enforcement (OWASP API1 — BOLA). The filter is produced through
/// <see cref="RequireOwnershipAttribute.CreateInstance"/> exactly as MVC would create it.
/// </summary>
public sealed class OwnedEntityActionFilterTests
{
    private const string AdminCode = AccessFunctionCodes.Api.AccessControlRead;

    [Fact]
    public async Task Administrators_bypass_the_ownership_check_without_touching_the_record()
    {
        using var dbContext = OwnedRecordDbContext.Create();
        var httpContext = MvcTestContext.CreateHttpContext(
            userId: "someone-else",
            accessFunctions: [AdminCode]);

        var (context, next) = CreateInvocation(httpContext, MvcTestContext.CreateRouteData("id", "404"));
        await CreateFilter(dbContext).OnActionExecutionAsync(context, next.InvokeAsync);

        Assert.True(next.WasCalled);
        Assert.Null(context.Result);
    }

    [Fact]
    public async Task Administrator_detection_ignores_case()
    {
        using var dbContext = OwnedRecordDbContext.Create();
        var httpContext = MvcTestContext.CreateHttpContext(
            userId: "someone-else",
            accessFunctions: [AdminCode.ToUpperInvariant()]);

        var (context, next) = CreateInvocation(httpContext, MvcTestContext.CreateRouteData("id", "404"));
        await CreateFilter(dbContext).OnActionExecutionAsync(context, next.InvokeAsync);

        Assert.True(next.WasCalled);
    }

    [Fact]
    public async Task Missing_route_parameter_is_rejected_before_the_action_runs()
    {
        using var dbContext = OwnedRecordDbContext.Create();
        var httpContext = MvcTestContext.CreateHttpContext(userId: "user-1");

        var (context, next) = CreateInvocation(httpContext, new RouteData());
        await CreateFilter(dbContext).OnActionExecutionAsync(context, next.InvokeAsync);

        Assert.False(next.WasCalled);
        var result = Assert.IsType<BadRequestObjectResult>(context.Result);
        Assert.Contains("'id'", Assert.IsType<string>(result.Value), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("not-a-number")]
    [InlineData("")]
    [InlineData("7.5")]
    [InlineData("019fc374-e85b-7001-8000-000000000001")]
    public async Task Non_integer_route_parameters_are_rejected(string rawId)
    {
        using var dbContext = OwnedRecordDbContext.Create();
        var httpContext = MvcTestContext.CreateHttpContext(userId: "user-1");

        var (context, next) = CreateInvocation(httpContext, MvcTestContext.CreateRouteData("id", rawId));
        await CreateFilter(dbContext).OnActionExecutionAsync(context, next.InvokeAsync);

        Assert.False(next.WasCalled);
        Assert.IsType<BadRequestObjectResult>(context.Result);
    }

    [Fact]
    public async Task Null_route_parameter_value_is_rejected()
    {
        using var dbContext = OwnedRecordDbContext.Create();
        var httpContext = MvcTestContext.CreateHttpContext(userId: "user-1");

        var (context, next) = CreateInvocation(httpContext, MvcTestContext.CreateRouteData("id", null));
        await CreateFilter(dbContext).OnActionExecutionAsync(context, next.InvokeAsync);

        Assert.False(next.WasCalled);
        Assert.IsType<BadRequestObjectResult>(context.Result);
    }

    [Fact]
    public async Task The_owner_may_execute_the_action()
    {
        using var dbContext = OwnedRecordDbContext.Create();
        dbContext.Attach(new OwnedRecord { Id = 7, OwnerUserId = "user-1" });
        var httpContext = MvcTestContext.CreateHttpContext(userId: "user-1");

        var (context, next) = CreateInvocation(httpContext, MvcTestContext.CreateRouteData("id", "7"));
        await CreateFilter(dbContext).OnActionExecutionAsync(context, next.InvokeAsync);

        Assert.True(next.WasCalled);
        Assert.Null(context.Result);
    }

    [Fact]
    public async Task A_record_owned_by_another_user_is_forbidden()
    {
        using var dbContext = OwnedRecordDbContext.Create();
        dbContext.Attach(new OwnedRecord { Id = 7, OwnerUserId = "user-2" });
        var httpContext = MvcTestContext.CreateHttpContext(userId: "user-1");

        var (context, next) = CreateInvocation(httpContext, MvcTestContext.CreateRouteData("id", "7"));
        await CreateFilter(dbContext).OnActionExecutionAsync(context, next.InvokeAsync);

        Assert.False(next.WasCalled);
        Assert.IsType<ForbidResult>(context.Result);
    }

    [Fact]
    public async Task Owner_matching_is_case_sensitive()
    {
        using var dbContext = OwnedRecordDbContext.Create();
        dbContext.Attach(new OwnedRecord { Id = 7, OwnerUserId = "User-1" });
        var httpContext = MvcTestContext.CreateHttpContext(userId: "user-1");

        var (context, next) = CreateInvocation(httpContext, MvcTestContext.CreateRouteData("id", "7"));
        await CreateFilter(dbContext).OnActionExecutionAsync(context, next.InvokeAsync);

        Assert.False(next.WasCalled);
        Assert.IsType<ForbidResult>(context.Result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task Callers_without_a_session_user_are_forbidden(string? userId)
    {
        using var dbContext = OwnedRecordDbContext.Create();
        dbContext.Attach(new OwnedRecord { Id = 7, OwnerUserId = "user-1" });
        var httpContext = MvcTestContext.CreateHttpContext(userId: userId);

        var (context, next) = CreateInvocation(httpContext, MvcTestContext.CreateRouteData("id", "7"));
        await CreateFilter(dbContext).OnActionExecutionAsync(context, next.InvokeAsync);

        Assert.False(next.WasCalled);
        Assert.IsType<ForbidResult>(context.Result);
    }

    [Fact]
    public async Task A_custom_route_parameter_name_is_honoured()
    {
        using var dbContext = OwnedRecordDbContext.Create();
        dbContext.Attach(new OwnedRecord { Id = 7, OwnerUserId = "user-1" });
        var httpContext = MvcTestContext.CreateHttpContext(userId: "user-1");

        var (context, next) = CreateInvocation(httpContext, MvcTestContext.CreateRouteData("recordId", "7"));
        await CreateFilter(dbContext, "recordId").OnActionExecutionAsync(context, next.InvokeAsync);

        Assert.True(next.WasCalled);
    }

    [Fact]
    public async Task A_custom_route_parameter_name_is_reported_when_it_is_absent()
    {
        using var dbContext = OwnedRecordDbContext.Create();
        var httpContext = MvcTestContext.CreateHttpContext(userId: "user-1");

        var (context, next) = CreateInvocation(httpContext, MvcTestContext.CreateRouteData("id", "7"));
        await CreateFilter(dbContext, "recordId").OnActionExecutionAsync(context, next.InvokeAsync);

        Assert.False(next.WasCalled);
        var result = Assert.IsType<BadRequestObjectResult>(context.Result);
        Assert.Contains("'recordId'", Assert.IsType<string>(result.Value), StringComparison.Ordinal);
    }

    private static IAsyncActionFilter CreateFilter(DbContext dbContext, string routeParameterName = "id")
    {
        var services = new ServiceCollection()
            .AddSingleton<DbContext>(dbContext)
            .BuildServiceProvider();

        var attribute = new RequireOwnershipAttribute(typeof(OwnedRecord), routeParameterName);
        return Assert.IsAssignableFrom<IAsyncActionFilter>(attribute.CreateInstance(services));
    }

    private static (ActionExecutingContext Context, NextRecorder Next) CreateInvocation(
        HttpContext httpContext,
        RouteData routeData)
    {
        var actionContext = MvcTestContext.CreateActionContext(httpContext, routeData);
        var context = new ActionExecutingContext(
            actionContext,
            [],
            new Dictionary<string, object?>(),
            new object());
        return (context, new NextRecorder(actionContext));
    }

    private sealed class NextRecorder
    {
        private readonly ActionContext _actionContext;

        public NextRecorder(ActionContext actionContext) => _actionContext = actionContext;

        public bool WasCalled { get; private set; }

        public Task<ActionExecutedContext> InvokeAsync()
        {
            WasCalled = true;
            return Task.FromResult(new ActionExecutedContext(_actionContext, [], new object()));
        }
    }
}

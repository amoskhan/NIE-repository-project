using Api.Authorization;
using Api.Tests.TestSupport;
using Application.Features;
using BuildingBlocks.Globals;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Api.Tests.Authorization;

/// <summary>
/// Function-level authorization is the first gate every API action passes through.
/// Deny paths (no codes, no session, no grant) matter far more than the allow path.
/// </summary>
public sealed class RequireAccessFunctionAttributeTests
{
    private const string ReadCode = "api.sample.read";
    private const string ManageCode = "api.sample.manage";

    private readonly IAccessFunctionService _accessFunctions = Substitute.For<IAccessFunctionService>();
    private readonly IAuditLogger _auditLogger = Substitute.For<IAuditLogger>();

    [Fact]
    public async Task Attribute_declaring_no_access_function_denies_every_caller()
    {
        var httpContext = CreateHttpContext(userId: "user-1");

        var context = MvcTestContext.CreateAuthorizationContext(httpContext);
        await new RequireAccessFunctionAttribute().OnAuthorizationAsync(context);

        Assert.IsType<ForbidResult>(context.Result);
        await _accessFunctions.DidNotReceive().HasAccessAsync(Arg.Any<string>(), Arg.Any<string>());
        await _auditLogger.DidNotReceive().LogAccessDeniedAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string?>());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    public async Task Blank_access_function_codes_are_discarded_and_deny_the_caller(string blankCode)
    {
        var httpContext = CreateHttpContext(userId: "user-1");

        var context = MvcTestContext.CreateAuthorizationContext(httpContext);
        await new RequireAccessFunctionAttribute(blankCode).OnAuthorizationAsync(context);

        Assert.IsType<ForbidResult>(context.Result);
        await _accessFunctions.DidNotReceive().HasAccessAsync(Arg.Any<string>(), Arg.Any<string>());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Requests_without_a_validated_session_user_are_unauthorized(string? userId)
    {
        var httpContext = CreateHttpContext(userId: userId);

        var context = MvcTestContext.CreateAuthorizationContext(httpContext);
        await new RequireAccessFunctionAttribute(ReadCode).OnAuthorizationAsync(context);

        Assert.IsType<UnauthorizedResult>(context.Result);
        await _accessFunctions.DidNotReceive().HasAccessAsync(Arg.Any<string>(), Arg.Any<string>());
        await _auditLogger.DidNotReceive().LogAccessDeniedAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string?>());
    }

    [Fact]
    public async Task Access_function_already_loaded_onto_the_request_allows_without_a_service_call()
    {
        var httpContext = CreateHttpContext(userId: "user-1", accessFunctions: [ManageCode, ReadCode]);

        var context = MvcTestContext.CreateAuthorizationContext(httpContext);
        await new RequireAccessFunctionAttribute(ReadCode).OnAuthorizationAsync(context);

        Assert.Null(context.Result);
        await _accessFunctions.DidNotReceive().HasAccessAsync(Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task Access_function_comparison_ignores_case()
    {
        var httpContext = CreateHttpContext(userId: "user-1", accessFunctions: ["API.SAMPLE.READ"]);

        var context = MvcTestContext.CreateAuthorizationContext(httpContext);
        await new RequireAccessFunctionAttribute(ReadCode).OnAuthorizationAsync(context);

        Assert.Null(context.Result);
    }

    [Fact]
    public async Task Access_functions_stored_in_an_unexpected_shape_fall_back_to_the_service()
    {
        var httpContext = CreateHttpContext(userId: "user-1");
        httpContext.Items[Constants.KeySessionUserAccessFunctions] = new[] { ReadCode };
        _accessFunctions.HasAccessAsync("user-1", ReadCode).Returns(true);

        var context = MvcTestContext.CreateAuthorizationContext(httpContext);
        await new RequireAccessFunctionAttribute(ReadCode).OnAuthorizationAsync(context);

        Assert.Null(context.Result);
        await _accessFunctions.Received(1).HasAccessAsync("user-1", ReadCode);
    }

    [Fact]
    public async Task Any_one_of_the_declared_access_functions_is_enough()
    {
        var httpContext = CreateHttpContext(userId: "user-1", accessFunctions: []);
        _accessFunctions.HasAccessAsync("user-1", ReadCode).Returns(false);
        _accessFunctions.HasAccessAsync("user-1", ManageCode).Returns(true);

        var context = MvcTestContext.CreateAuthorizationContext(httpContext);
        await new RequireAccessFunctionAttribute(ReadCode, ManageCode).OnAuthorizationAsync(context);

        Assert.Null(context.Result);
        await _accessFunctions.Received(1).HasAccessAsync("user-1", ReadCode);
        await _accessFunctions.Received(1).HasAccessAsync("user-1", ManageCode);
    }

    [Fact]
    public async Task Evaluation_stops_at_the_first_granted_access_function()
    {
        var httpContext = CreateHttpContext(userId: "user-1", accessFunctions: []);
        _accessFunctions.HasAccessAsync("user-1", ReadCode).Returns(true);

        var context = MvcTestContext.CreateAuthorizationContext(httpContext);
        await new RequireAccessFunctionAttribute(ReadCode, ManageCode).OnAuthorizationAsync(context);

        Assert.Null(context.Result);
        await _accessFunctions.DidNotReceive().HasAccessAsync("user-1", ManageCode);
    }

    [Fact]
    public async Task Duplicate_access_function_codes_are_evaluated_only_once()
    {
        var httpContext = CreateHttpContext(userId: "user-1", accessFunctions: []);
        _accessFunctions.HasAccessAsync(Arg.Any<string>(), Arg.Any<string>()).Returns(false);

        var context = MvcTestContext.CreateAuthorizationContext(httpContext);
        await new RequireAccessFunctionAttribute(ReadCode, "API.SAMPLE.READ", ReadCode)
            .OnAuthorizationAsync(context);

        Assert.IsType<ForbidResult>(context.Result);
        await _accessFunctions.Received(1).HasAccessAsync("user-1", Arg.Any<string>());
    }

    [Fact]
    public async Task Denied_requests_are_audited_with_every_required_code_and_the_request_path()
    {
        var httpContext = CreateHttpContext(userId: "user-1", accessFunctions: ["api.other.read"]);
        httpContext.Request.Path = "/api/sample/list";
        _accessFunctions.HasAccessAsync(Arg.Any<string>(), Arg.Any<string>()).Returns(false);

        var context = MvcTestContext.CreateAuthorizationContext(httpContext);
        await new RequireAccessFunctionAttribute(ReadCode, ManageCode).OnAuthorizationAsync(context);

        Assert.IsType<ForbidResult>(context.Result);
        await _auditLogger.Received(1).LogAccessDeniedAsync(
            "user-1",
            $"{ReadCode}, {ManageCode}",
            "/api/sample/list");
    }

    [Fact]
    public async Task Granted_requests_are_not_audited_as_denials()
    {
        var httpContext = CreateHttpContext(userId: "user-1", accessFunctions: [ReadCode]);

        var context = MvcTestContext.CreateAuthorizationContext(httpContext);
        await new RequireAccessFunctionAttribute(ReadCode).OnAuthorizationAsync(context);

        await _auditLogger.DidNotReceive().LogAccessDeniedAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string?>());
    }

    private DefaultHttpContext CreateHttpContext(
        string? userId,
        IEnumerable<string>? accessFunctions = null)
    {
        var services = new ServiceCollection()
            .AddSingleton(_accessFunctions)
            .AddSingleton(_auditLogger)
            .BuildServiceProvider();

        return MvcTestContext.CreateHttpContext(userId, accessFunctions, services: services);
    }
}

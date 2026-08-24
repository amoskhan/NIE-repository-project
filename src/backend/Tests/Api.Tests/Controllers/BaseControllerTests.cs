using Api.Controllers;
using Api.Tests.TestSupport;
using Application.Features;
using Application.Security;
using BuildingBlocks.Globals;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace Api.Tests.Controllers;

/// <summary>
/// Every controller in the host inherits its session accessors and the BOLA ownership
/// guard from <see cref="BaseController"/>, so its deny paths are worth covering directly.
/// </summary>
public sealed class BaseControllerTests
{
    private readonly IBaseService<SampleOwnedEntity> _service =
        Substitute.For<IBaseService<SampleOwnedEntity>>();

    [Fact]
    public void Session_values_are_projected_from_the_request_items()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Items[Constants.KeySessionUserId] = "user-1";
        httpContext.Items[Constants.KeySessionUserName] = "Ada Lovelace";
        httpContext.Items[Constants.KeySessionUserEmail] = "ada@example.edu.sg";
        httpContext.Items[Constants.KeySessionSessionId] = "session-9";
        httpContext.Items[Constants.KeySessionUserDept] = "Research";

        var controller = CreateController(httpContext);

        Assert.Equal("user-1", controller.CurrentUserId);
        Assert.Equal("Ada Lovelace", controller.CurrentUserName);
        Assert.Equal("ada@example.edu.sg", controller.CurrentUserEmail);
        Assert.Equal("session-9", controller.CurrentSessionId);
        Assert.Equal("Research", controller.CurrentUserDepartment);
    }

    [Fact]
    public void An_unauthenticated_request_exposes_no_session_values()
    {
        var controller = CreateController(new DefaultHttpContext());

        Assert.Null(controller.CurrentUserId);
        Assert.Null(controller.CurrentUserName);
        Assert.Null(controller.CurrentUserEmail);
        Assert.Null(controller.CurrentSessionId);
        Assert.Null(controller.CurrentUserDepartment);
        Assert.Empty(controller.CurrentUserRoles);
        Assert.Empty(controller.CurrentUserAccessFunctions);
        Assert.False(controller.CurrentUserIsAdmin);
    }

    [Fact]
    public void Roles_stored_in_an_unexpected_shape_degrade_to_an_empty_list()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Items[Constants.KeySessionUserRoles] = new[] { "Administrator" };
        httpContext.Items[Constants.KeySessionUserAccessFunctions] = "not-a-list";

        var controller = CreateController(httpContext);

        Assert.Empty(controller.CurrentUserRoles);
        Assert.Empty(controller.CurrentUserAccessFunctions);
        Assert.False(controller.CurrentUserIsAdmin);
    }

    [Theory]
    [InlineData("Approver", true)]
    [InlineData("APPROVER", true)]
    [InlineData("approver", true)]
    [InlineData("Requester", false)]
    [InlineData("", false)]
    public void Role_membership_ignores_case(string role, bool expected)
    {
        var controller = CreateController(MvcTestContext.CreateHttpContext(roles: ["Approver", "Finance"]));

        Assert.Equal(expected, controller.HasRole(role));
    }

    [Theory]
    [InlineData(AccessFunctionCodes.Api.AccessControlRead, true)]
    [InlineData("API.ACCESS-CONTROL.READ", true)]
    [InlineData(AccessFunctionCodes.Api.AuditRead, false)]
    public void Administrator_status_is_derived_from_the_access_control_read_function(
        string grantedCode,
        bool expected)
    {
        var controller = CreateController(MvcTestContext.CreateHttpContext(accessFunctions: [grantedCode]));

        Assert.Equal(expected, controller.CurrentUserIsAdmin);
    }

    [Fact]
    public async Task The_ownership_guard_lets_administrators_through_without_loading_the_record()
    {
        var controller = CreateController(MvcTestContext.CreateHttpContext(
            userId: "admin-1",
            accessFunctions: [AccessFunctionCodes.Api.AccessControlRead]));

        var guard = await controller.GuardAsync(Guid.CreateVersion7(), _service);

        Assert.Null(guard);
        await _service.DidNotReceive().GetByIdAsync(Arg.Any<Guid>());
    }

    [Fact]
    public async Task The_ownership_guard_reports_an_unknown_record_as_not_found()
    {
        var id = Guid.CreateVersion7();
        _service.GetByIdAsync(id).Returns((SampleOwnedEntity?)null);
        var controller = CreateController(MvcTestContext.CreateHttpContext(userId: "user-1"));

        var guard = await controller.GuardAsync(id, _service);

        Assert.IsType<NotFoundResult>(guard);
    }

    [Fact]
    public async Task The_ownership_guard_forbids_a_record_owned_by_someone_else()
    {
        var id = Guid.CreateVersion7();
        _service.GetByIdAsync(id).Returns(new SampleOwnedEntity { OwnerUserId = "user-2" });
        var controller = CreateController(MvcTestContext.CreateHttpContext(userId: "user-1"));

        var guard = await controller.GuardAsync(id, _service);

        Assert.IsType<ForbidResult>(guard);
    }

    [Fact]
    public async Task The_ownership_guard_admits_the_owner()
    {
        var id = Guid.CreateVersion7();
        _service.GetByIdAsync(id).Returns(new SampleOwnedEntity { OwnerUserId = "user-1" });
        var controller = CreateController(MvcTestContext.CreateHttpContext(userId: "user-1"));

        Assert.Null(await controller.GuardAsync(id, _service));
    }

    [Fact]
    public async Task The_ownership_guard_compares_owners_ordinally()
    {
        var id = Guid.CreateVersion7();
        _service.GetByIdAsync(id).Returns(new SampleOwnedEntity { OwnerUserId = "User-1" });
        var controller = CreateController(MvcTestContext.CreateHttpContext(userId: "user-1"));

        Assert.IsType<ForbidResult>(await controller.GuardAsync(id, _service));
    }

    [Fact]
    public async Task The_ownership_guard_forbids_a_caller_with_no_session_user()
    {
        var id = Guid.CreateVersion7();
        _service.GetByIdAsync(id).Returns(new SampleOwnedEntity { OwnerUserId = "user-1" });
        var controller = CreateController(new DefaultHttpContext());

        Assert.IsType<ForbidResult>(await controller.GuardAsync(id, _service));
    }

    private static ProbeController CreateController(HttpContext httpContext) =>
        new()
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext },
        };

    /// <summary>Surfaces the protected <see cref="BaseController"/> members for assertion.</summary>
    private sealed class ProbeController : BaseController
    {
        public string? CurrentUserId => UserId;

        public string? CurrentUserName => UserName;

        public string? CurrentUserEmail => UserEmail;

        public string? CurrentSessionId => SessionId;

        public string? CurrentUserDepartment => UserDepartment;

        public List<string> CurrentUserRoles => UserRoles;

        public List<string> CurrentUserAccessFunctions => UserAccessFunctions;

        public bool CurrentUserIsAdmin => IsAdmin;

        public bool HasRole(string role) => IsInRole(role);

        public Task<IActionResult?> GuardAsync(Guid id, IBaseService<SampleOwnedEntity> service) =>
            EnsureOwnedAsync(id, service);
    }
}

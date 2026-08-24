using Api.Controllers;
using Api.Tests.TestSupport;
using Application.Contracts;
using Application.Features.DataTablePreferences;
using Application.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Api.Tests.Controllers;

public sealed class UserDataTablePreferenceControllerTests
{
    private readonly IUserDataTablePreferenceService _service =
        Substitute.For<IUserDataTablePreferenceService>();

    [Fact]
    public async Task Every_endpoint_rejects_a_request_without_a_session_user()
    {
        var controller = CreateController(userId: null);
        var token = TestContext.Current.CancellationToken;

        Assert.IsType<UnauthorizedResult>(await controller.GetAll(token));
        Assert.IsType<UnauthorizedResult>(await controller.Get("vendor.list", token));
        Assert.IsType<UnauthorizedResult>(
            await controller.Upsert("vendor.list", new UpsertUserDataTablePreferenceDto(), token));
        Assert.IsType<UnauthorizedResult>(await controller.Delete("vendor.list", token));

        Assert.Empty(_service.ReceivedCalls());
    }

    [Theory]
    [InlineData("ab")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(".vendor")]
    [InlineData("vendor.")]
    [InlineData("-vendor-")]
    [InlineData("vendor list")]
    [InlineData("vendor/list")]
    [InlineData("vendor_list")]
    public async Task Table_keys_outside_the_allowed_shape_are_rejected(string tableKey)
    {
        var controller = CreateController();

        var result = await controller.Get(tableKey, TestContext.Current.CancellationToken);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var problem = Assert.IsType<ValidationProblemDetails>(badRequest.Value);
        Assert.Equal(StatusCodes.Status400BadRequest, problem.Status);
        Assert.True(problem.Errors.ContainsKey("tableKey"));
        Assert.Equal("/api/userdatatablepreference/get", problem.Instance);
        Assert.Empty(_service.ReceivedCalls());
    }

    [Theory]
    [InlineData("vendor.list", "vendor.list")]
    [InlineData("  Vendor.List  ", "vendor.list")]
    [InlineData("PURCHASE-ORDER.TABLE", "purchase-order.table")]
    public async Task Table_keys_are_trimmed_and_lowercased_before_they_reach_the_service(
        string tableKey,
        string expected)
    {
        var controller = CreateController();

        await controller.Get(tableKey, TestContext.Current.CancellationToken);

        await _service.Received(1).GetAsync(
            SystemApplicationIds.Core,
            "user-1",
            expected,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task A_user_without_a_stored_preference_gets_no_content()
    {
        _service
            .GetAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((UserDataTablePreferenceDto?)null);
        var controller = CreateController();

        var result = await controller.Get("vendor.list", TestContext.Current.CancellationToken);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task A_stored_preference_is_returned_to_its_owner()
    {
        var stored = new UserDataTablePreferenceDto { TableKey = "vendor.list", Revision = 4 };
        _service
            .GetAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(stored);
        var controller = CreateController();

        var result = await controller.Get("vendor.list", TestContext.Current.CancellationToken);

        Assert.Same(stored, Assert.IsType<OkObjectResult>(result).Value);
    }

    [Fact]
    public async Task Preferences_are_always_scoped_to_the_calling_user()
    {
        var controller = CreateController(userId: "someone-else");

        await controller.GetAll(TestContext.Current.CancellationToken);

        await _service.Received(1).GetAllAsync(
            SystemApplicationIds.Core,
            "someone-else",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task A_concurrent_edit_is_reported_as_a_conflict_problem()
    {
        _service
            .UpsertAsync(
                Arg.Any<Guid>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<UpsertUserDataTablePreferenceDto>(),
                Arg.Any<CancellationToken>())
            .ThrowsAsync(new DataTablePreferenceConflictException());
        var controller = CreateController();

        var result = await controller.Upsert(
            "vendor.list",
            new UpsertUserDataTablePreferenceDto(),
            TestContext.Current.CancellationToken);

        var conflict = Assert.IsType<ConflictObjectResult>(result);
        var problem = Assert.IsType<ProblemDetails>(conflict.Value);
        Assert.Equal(StatusCodes.Status409Conflict, problem.Status);
        Assert.Equal("Table preference conflict", problem.Title);
        Assert.Equal("/api/userdatatablepreference/get", problem.Instance);
    }

    [Fact]
    public async Task A_successful_upsert_returns_the_saved_preference()
    {
        var saved = new UserDataTablePreferenceDto { TableKey = "vendor.list", Revision = 5 };
        var request = new UpsertUserDataTablePreferenceDto { DefinitionVersion = 2, Revision = 4 };
        _service
            .UpsertAsync(
                SystemApplicationIds.Core,
                "user-1",
                "vendor.list",
                request,
                Arg.Any<CancellationToken>())
            .Returns(saved);
        var controller = CreateController();

        var result = await controller.Upsert(
            " Vendor.List ",
            request,
            TestContext.Current.CancellationToken);

        Assert.Same(saved, Assert.IsType<OkObjectResult>(result).Value);
    }

    [Fact]
    public async Task Deleting_a_preference_normalises_the_key_and_returns_no_content()
    {
        var controller = CreateController();

        var result = await controller.Delete("  Vendor.List ", TestContext.Current.CancellationToken);

        Assert.IsType<NoContentResult>(result);
        await _service.Received(1).DeleteAsync(
            SystemApplicationIds.Core,
            "user-1",
            "vendor.list",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task An_invalid_key_is_rejected_before_any_delete_reaches_the_service()
    {
        var controller = CreateController();

        var result = await controller.Delete("no", TestContext.Current.CancellationToken);

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Empty(_service.ReceivedCalls());
    }

    private UserDataTablePreferenceController CreateController(string? userId = "user-1")
    {
        var httpContext = MvcTestContext.CreateHttpContext(userId);
        httpContext.Request.Path = "/api/userdatatablepreference/get";
        return new UserDataTablePreferenceController(_service)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext },
        };
    }
}

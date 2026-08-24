using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Validation;

namespace Validation.Tests;

public sealed class FluentValidationActionFilterTests
{
    [Fact]
    public async Task OnActionExecutionAsync_InvalidRequest_ReturnsProblemWithoutExecutingAction()
    {
        var validator = new TestRequestValidator();
        var httpContext = CreateHttpContext(validator);
        var actionContext = CreateActionContext(httpContext);
        var context = new ActionExecutingContext(
            actionContext,
            [],
            new Dictionary<string, object?> { ["request"] = new TestRequest("") },
            new object());
        var executed = false;

        await new FluentValidationActionFilter().OnActionExecutionAsync(
            context,
            () =>
            {
                executed = true;
                return Task.FromResult(new ActionExecutedContext(actionContext, [], new object()));
            });

        Assert.False(executed);
        var result = Assert.IsType<BadRequestObjectResult>(context.Result);
        var problem = Assert.IsType<ValidationProblemDetails>(result.Value);
        Assert.Equal(StatusCodes.Status400BadRequest, problem.Status);
        Assert.Equal(["Name is required."], problem.Errors["name"]);
        Assert.True(validator.AsyncRuleExecuted);
        Assert.Equal(httpContext.RequestAborted, validator.ObservedCancellationToken);
    }

    [Fact]
    public async Task OnActionExecutionAsync_ValidRequest_ExecutesAction()
    {
        var validator = new TestRequestValidator();
        var httpContext = CreateHttpContext(validator);
        var actionContext = CreateActionContext(httpContext);
        var context = new ActionExecutingContext(
            actionContext,
            [],
            new Dictionary<string, object?> { ["request"] = new TestRequest("Valid") },
            new object());
        var executed = false;

        await new FluentValidationActionFilter().OnActionExecutionAsync(
            context,
            () =>
            {
                executed = true;
                return Task.FromResult(new ActionExecutedContext(actionContext, [], new object()));
            });

        Assert.True(executed);
        Assert.Null(context.Result);
    }

    private static DefaultHttpContext CreateHttpContext(IValidator<TestRequest> validator)
    {
        var services = new ServiceCollection()
            .AddSingleton(validator)
            .BuildServiceProvider();
        return new DefaultHttpContext
        {
            RequestServices = services
        };
    }

    private static ActionContext CreateActionContext(HttpContext httpContext) =>
        new(httpContext, new RouteData(), new ActionDescriptor());

    private sealed record TestRequest(string Name);

    private sealed class TestRequestValidator : AbstractValidator<TestRequest>
    {
        public TestRequestValidator()
        {
            RuleFor(request => request.Name)
                .MustAsync(async (name, cancellationToken) =>
                {
                    AsyncRuleExecuted = true;
                    ObservedCancellationToken = cancellationToken;
                    await Task.Yield();
                    return !string.IsNullOrWhiteSpace(name);
                })
                .WithMessage("Name is required.");
        }

        public bool AsyncRuleExecuted { get; private set; }

        public CancellationToken ObservedCancellationToken { get; private set; }
    }
}

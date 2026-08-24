using System.Net;
using System.Text.Json;
using Api.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Api.Tests.Middleware;

/// <summary>
/// The global handler is the only thing standing between an unhandled exception and the
/// caller, so both the status mapping and the "do not leak internals" rule are covered.
/// </summary>
public sealed class ExceptionHandlingMiddlewareTests
{
    [Fact]
    public async Task Successful_requests_are_left_untouched()
    {
        var context = CreateHttpContext();
        var body = ReplaceBody(context);
        var middleware = CreateMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        Assert.Equal(0, body.Length);
    }

    [Fact]
    public async Task A_missing_argument_is_reported_as_bad_request_without_naming_the_parameter()
    {
        var (statusCode, _, problem) = await HandleAsync(new ArgumentNullException("secretParameterName"));

        Assert.Equal((int)HttpStatusCode.BadRequest, statusCode);
        Assert.Equal("Bad Request", problem.GetProperty("title").GetString());
        Assert.Equal("Required parameter is missing.", problem.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task An_invalid_argument_is_reported_as_bad_request_with_its_message()
    {
        var (statusCode, _, problem) = await HandleAsync(new ArgumentException("Quantity must be positive."));

        Assert.Equal((int)HttpStatusCode.BadRequest, statusCode);
        Assert.Equal("Quantity must be positive.", problem.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task A_missing_key_is_reported_as_not_found_with_a_generic_detail()
    {
        var (statusCode, _, problem) = await HandleAsync(new KeyNotFoundException("purchase-order 42 is missing"));

        Assert.Equal((int)HttpStatusCode.NotFound, statusCode);
        Assert.Equal("Not Found", problem.GetProperty("title").GetString());
        Assert.Equal("The requested resource was not found.", problem.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task An_unauthorized_access_attempt_is_reported_as_unauthorized()
    {
        var (statusCode, _, problem) = await HandleAsync(new UnauthorizedAccessException("token expired"));

        Assert.Equal((int)HttpStatusCode.Unauthorized, statusCode);
        Assert.Equal("You are not authorized to perform this action.", problem.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task An_invalid_operation_is_reported_as_bad_request_with_its_message()
    {
        var (statusCode, _, problem) = await HandleAsync(new InvalidOperationException("Only draft orders can be edited"));

        Assert.Equal((int)HttpStatusCode.BadRequest, statusCode);
        Assert.Equal("Only draft orders can be edited", problem.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task An_unexpected_failure_is_reported_as_500_without_leaking_the_message()
    {
        var (statusCode, _, problem) = await HandleAsync(
            new InvalidCastException("Npgsql connection string password=hunter2"));

        Assert.Equal((int)HttpStatusCode.InternalServerError, statusCode);
        Assert.Equal("Internal Server Error", problem.GetProperty("title").GetString());
        Assert.Equal("An unexpected error occurred.", problem.GetProperty("detail").GetString());
        Assert.DoesNotContain("hunter2", problem.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Production_responses_never_include_the_exception_dump()
    {
        var (_, _, problem) = await HandleAsync(new InvalidCastException("boom"), "Production");

        Assert.False(problem.TryGetProperty("exception", out _));
    }

    [Fact]
    public async Task Development_responses_include_the_exception_dump()
    {
        var (_, _, problem) = await HandleAsync(new InvalidCastException("boom"), "Development");

        Assert.True(problem.TryGetProperty("exception", out var dump));
        Assert.Contains("InvalidCastException", dump.GetString() ?? string.Empty, StringComparison.Ordinal);
    }

    [Fact]
    public async Task The_problem_response_carries_the_request_path_and_a_status_type_link()
    {
        var (statusCode, _, problem) = await HandleAsync(new ArgumentException("nope"));

        Assert.Equal("/api/purchaseorder/edit", problem.GetProperty("instance").GetString());
        Assert.Equal($"https://httpstatuses.io/{statusCode}", problem.GetProperty("type").GetString());
        Assert.Equal(statusCode, problem.GetProperty("status").GetInt32());
    }

    [Fact]
    public async Task The_correlation_id_is_taken_from_the_request_when_one_was_assigned()
    {
        var context = CreateHttpContext();
        context.Items["CorrelationId"] = "correlation-77";

        var problem = await HandleAsync(context, new ArgumentException("nope"));

        Assert.Equal("correlation-77", problem.GetProperty("correlationId").GetString());
        Assert.Equal("trace-1", problem.GetProperty("traceId").GetString());
    }

    [Fact]
    public async Task The_correlation_id_falls_back_to_the_trace_identifier()
    {
        var problem = await HandleAsync(CreateHttpContext(), new ArgumentException("nope"));

        Assert.Equal("trace-1", problem.GetProperty("correlationId").GetString());
    }

    private static async Task<(int StatusCode, string? ContentType, JsonElement Problem)> HandleAsync(
        Exception exception,
        string environmentName = "Production")
    {
        var context = CreateHttpContext();
        var problem = await HandleAsync(context, exception, environmentName);
        return (context.Response.StatusCode, context.Response.ContentType, problem);
    }

    private static async Task<JsonElement> HandleAsync(
        DefaultHttpContext context,
        Exception exception,
        string environmentName = "Production")
    {
        var body = ReplaceBody(context);
        var middleware = CreateMiddleware(_ => throw exception, environmentName);

        await middleware.InvokeAsync(context);

        body.Position = 0;
        using var document = await JsonDocument.ParseAsync(
            body,
            cancellationToken: TestContext.Current.CancellationToken);
        return document.RootElement.Clone();
    }

    private static ExceptionHandlingMiddleware CreateMiddleware(
        RequestDelegate next,
        string environmentName = "Production")
    {
        var environment = Substitute.For<IHostEnvironment>();
        environment.EnvironmentName.Returns(environmentName);
        return new ExceptionHandlingMiddleware(
            next,
            NullLogger<ExceptionHandlingMiddleware>.Instance,
            environment);
    }

    private static DefaultHttpContext CreateHttpContext()
    {
        var context = new DefaultHttpContext { TraceIdentifier = "trace-1" };
        context.Request.Path = "/api/purchaseorder/edit";
        return context;
    }

    private static MemoryStream ReplaceBody(HttpContext context)
    {
        var body = new MemoryStream();
        context.Response.Body = body;
        return body;
    }
}

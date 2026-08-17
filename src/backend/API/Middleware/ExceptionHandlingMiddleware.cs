using System.Net;
using Microsoft.AspNetCore.Mvc;

namespace API.Middleware;

/// <summary>
/// Global exception handling middleware that catches unhandled exceptions
/// and returns a standardized RFC 9457 ProblemDetails response.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var correlationId = context.Items["CorrelationId"]?.ToString() ?? context.TraceIdentifier;

        _logger.LogError(exception,
            "Unhandled exception. TraceId: {TraceId}, CorrelationId: {CorrelationId}",
            context.TraceIdentifier, correlationId);

        var (statusCode, title, detail) = exception switch
        {
            ArgumentNullException => (HttpStatusCode.BadRequest, "Bad Request", "Required parameter is missing."),
            ArgumentException => (HttpStatusCode.BadRequest, "Bad Request", exception.Message),
            KeyNotFoundException => (HttpStatusCode.NotFound, "Not Found", "The requested resource was not found."),
            UnauthorizedAccessException => (HttpStatusCode.Unauthorized, "Unauthorized", "You are not authorized to perform this action."),
            InvalidOperationException => (HttpStatusCode.BadRequest, "Bad Request", exception.Message),
            _ => (HttpStatusCode.InternalServerError, "Internal Server Error", "An unexpected error occurred.")
        };

        var problemDetails = new ProblemDetails
        {
            Status = (int)statusCode,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path,
            Type = $"https://httpstatuses.io/{(int)statusCode}"
        };

        problemDetails.Extensions["traceId"] = context.TraceIdentifier;
        problemDetails.Extensions["correlationId"] = correlationId;

        if (_environment.IsDevelopment())
        {
            problemDetails.Extensions["exception"] = exception.ToString();
        }

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = (int)statusCode;

        await context.Response.WriteAsJsonAsync(problemDetails);
    }
}

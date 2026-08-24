using System.Text.Json;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Validation;

/// <summary>
/// Produces one stable RFC 7807 validation response for model binding and
/// FluentValidation failures.
/// </summary>
public static class ValidationProblemFactory
{
    private const string ValidationProblemType =
        "https://datatracker.ietf.org/doc/html/rfc9110#section-15.5.1";

    public static IActionResult FromModelState(ActionContext context)
    {
        var errors = context.ModelState
            .Where(entry => entry.Value?.ValidationState == ModelValidationState.Invalid)
            .ToDictionary(
                entry => ToJsonPath(entry.Key),
                entry => entry.Value!.Errors
                    .Select(error => string.IsNullOrWhiteSpace(error.ErrorMessage)
                        ? "The supplied value is invalid."
                        : error.ErrorMessage)
                    .Distinct(StringComparer.Ordinal)
                    .ToArray(),
                StringComparer.Ordinal);

        return CreateResult(context.HttpContext, errors);
    }

    public static IActionResult FromFailures(
        HttpContext httpContext,
        IEnumerable<ValidationFailure> failures)
    {
        var errors = failures
            .Where(failure => failure is not null)
            .GroupBy(failure => ToJsonPath(failure.PropertyName), StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group
                    .Select(failure => failure.ErrorMessage)
                    .Where(message => !string.IsNullOrWhiteSpace(message))
                    .Distinct(StringComparer.Ordinal)
                    .ToArray(),
                StringComparer.Ordinal);

        return CreateResult(httpContext, errors);
    }

    private static BadRequestObjectResult CreateResult(
        HttpContext httpContext,
        IDictionary<string, string[]> errors)
    {
        var problem = new ValidationProblemDetails(errors)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "One or more validation errors occurred.",
            Type = ValidationProblemType,
            Instance = httpContext.Request.Path
        };
        problem.Extensions["traceId"] = httpContext.TraceIdentifier;

        return new BadRequestObjectResult(problem)
        {
            ContentTypes = { "application/problem+json" }
        };
    }

    private static string ToJsonPath(string propertyPath)
    {
        if (string.IsNullOrWhiteSpace(propertyPath))
        {
            return "$";
        }

        return string.Join(
            '.',
            propertyPath.Split('.', StringSplitOptions.RemoveEmptyEntries)
                .Select(segment =>
                {
                    var bracketIndex = segment.IndexOf('[');
                    var propertyName = bracketIndex < 0 ? segment : segment[..bracketIndex];
                    var suffix = bracketIndex < 0 ? string.Empty : segment[bracketIndex..];
                    return JsonNamingPolicy.CamelCase.ConvertName(propertyName) + suffix;
                }));
    }
}

using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Validation;

namespace Validation.Tests;

public sealed class ValidationProblemFactoryTests
{
    [Fact]
    public void FromFailures_NestedPascalCasePath_ReturnsCanonicalProblemDetails()
    {
        var httpContext = new DefaultHttpContext
        {
            TraceIdentifier = "trace-123"
        };
        httpContext.Request.Path = "/api/PurchaseOrder/Save";
        var failures = new[]
        {
            new ValidationFailure("Lines[0].ItemName", "Item name is required."),
            new ValidationFailure("VendorId", "Vendor is required.")
        };

        var result = Assert.IsType<BadRequestObjectResult>(
            ValidationProblemFactory.FromFailures(httpContext, failures));
        var problem = Assert.IsType<ValidationProblemDetails>(result.Value);

        Assert.Equal(StatusCodes.Status400BadRequest, result.StatusCode);
        Assert.Contains("application/problem+json", result.ContentTypes);
        Assert.Equal("/api/PurchaseOrder/Save", problem.Instance);
        Assert.Equal("trace-123", problem.Extensions["traceId"]);
        Assert.Equal(["Item name is required."], problem.Errors["lines[0].itemName"]);
        Assert.Equal(["Vendor is required."], problem.Errors["vendorId"]);
    }
}

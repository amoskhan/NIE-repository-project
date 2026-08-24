using FluentValidation;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Validation;

/// <summary>
/// Runs registered FluentValidation validators asynchronously after model binding and
/// before a controller action executes.
/// </summary>
public sealed class FluentValidationActionFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next)
    {
        foreach (var (argumentName, argumentValue) in context.ActionArguments)
        {
            if (argumentValue is null)
            {
                continue;
            }

            var parameter = context.ActionDescriptor.Parameters
                .OfType<ControllerParameterDescriptor>()
                .FirstOrDefault(candidate => candidate.Name == argumentName);
            var modelType = parameter?.ParameterInfo.ParameterType ?? argumentValue.GetType();
            modelType = Nullable.GetUnderlyingType(modelType) ?? modelType;

            var validatorType = typeof(IValidator<>).MakeGenericType(modelType);
            if (context.HttpContext.RequestServices.GetService(validatorType) is not IValidator validator)
            {
                continue;
            }

            var validationContext = new ValidationContext<object>(argumentValue);
            var result = await validator.ValidateAsync(
                validationContext,
                context.HttpContext.RequestAborted);

            if (!result.IsValid)
            {
                context.Result = ValidationProblemFactory.FromFailures(
                    context.HttpContext,
                    result.Errors);
                return;
            }
        }

        await next();
    }
}

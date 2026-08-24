using System.Reflection;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Validation;

public static class ValidationServiceCollectionExtensions
{
    /// <summary>
    /// Registers validators from the supplied assemblies and adds the asynchronous
    /// request-validation filter to all controller actions.
    /// </summary>
    public static IMvcBuilder AddNieRequestValidation(
        this IMvcBuilder mvcBuilder,
        params Assembly[] validatorAssemblies)
    {
        ArgumentNullException.ThrowIfNull(mvcBuilder);

        if (validatorAssemblies.Length == 0)
        {
            throw new ArgumentException(
                "At least one validator assembly must be supplied.",
                nameof(validatorAssemblies));
        }

        mvcBuilder.Services.AddValidatorsFromAssemblies(
            validatorAssemblies.Distinct(),
            includeInternalTypes: true,
            lifetime: ServiceLifetime.Scoped);
        mvcBuilder.Services.AddScoped<FluentValidationActionFilter>();
        mvcBuilder.Services.Configure<MvcOptions>(options =>
            options.Filters.AddService<FluentValidationActionFilter>());
        mvcBuilder.Services.Configure<ApiBehaviorOptions>(options =>
            options.InvalidModelStateResponseFactory = ValidationProblemFactory.FromModelState);

        return mvcBuilder;
    }
}

using Api.Middleware;

namespace Api.Extensions;

/// <summary>
/// Extension methods for registering custom middleware.
/// </summary>
public static class MiddlewareExtensions
{
    /// <summary>
    /// Adds session validation middleware to the request pipeline.
    /// </summary>
    public static IApplicationBuilder UseSessionValidation(this IApplicationBuilder builder)
    {
        builder.UseMiddleware<SessionValidationMiddleware>();
        // Load user roles after session is validated
        builder.UseMiddleware<UserRolesMiddleware>();
        return builder;
    }

    /// <summary>
    /// Adds global exception handling middleware to the request pipeline.
    /// </summary>
    public static IApplicationBuilder UseGlobalExceptionHandling(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ExceptionHandlingMiddleware>();
    }

    /// <summary>
    /// Adds session validation services to the service collection.
    /// </summary>
    public static IServiceCollection AddSessionValidation(this IServiceCollection services, IConfiguration configuration)
    {
        // Add HttpClient for calling auth service
        services.AddHttpClient<SessionValidationMiddleware>(client =>
        {
            var authServiceUrl = configuration["AuthService:BaseUrl"]
                ?? throw new InvalidOperationException("AuthService:BaseUrl is not configured.");
            client.BaseAddress = new Uri(authServiceUrl);
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        return services;
    }
}

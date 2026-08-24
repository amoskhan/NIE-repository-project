using Auth.Controllers;
using Auth.Extensions;
using Auth.Models;
using Auth.Services;
using StackExchange.Redis;
using Validation;

namespace Auth;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        var configuration = builder.Configuration;

        builder.AddObservability();

        // Auth API connects to IDP + Valkey only. No database, no service-to-service calls.
        // Role/permission resolution happens in the Main API, fetched by the frontend after login.

        builder.Services.AddSingleton<IConnectionMultiplexer>(provider =>
        {
            var connectionString = builder.Configuration["Valkey:ConnectionString"]
                ?? throw new InvalidOperationException("Valkey:ConnectionString configuration is required.");
            return ConnectionMultiplexer.Connect(connectionString);
        });

        builder.Services.AddStackExchangeRedisCache(options =>
        {
            var valkeyConnectionString = builder.Configuration["Valkey:ConnectionString"]
                ?? throw new InvalidOperationException("Valkey:ConnectionString configuration is required.");
            options.ConfigurationOptions = ConfigurationOptions.Parse(valkeyConnectionString);
            options.ConfigurationOptions.AbortOnConnectFail = false;
            options.InstanceName = builder.Configuration["Valkey:InstanceName"];
        });

        builder.Services.AddCors(options =>
        {
            var allowedOrigins = configuration.GetSection("AllowedCORSOrigin").Get<string[]>()
                ?? throw new InvalidOperationException("AllowedCORSOrigin configuration is required.");
            options.AddPolicy("AllowSpecificOrigin",
                policy => policy
                    .WithOrigins(allowedOrigins)
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials());
        });

        builder.Services.AddHttpClient<AuthController>();
        builder.Services.Configure<PortalSsoOptions>(configuration.GetSection(PortalSsoOptions.SectionName));
        builder.Services.AddScoped<IAuthSessionService, AuthSessionService>();
        builder.Services.AddScoped<IPortalSsoService, PortalSsoService>();
        builder.Services
            .AddControllers()
            .AddNieRequestValidation(typeof(Program).Assembly);
        builder.Services.AddHealthChecks()
            .AddRedis(configuration["Valkey:ConnectionString"]!, name: "valkey");
        builder.Services.AddOpenApi();

        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        app.UseCors("AllowSpecificOrigin");
        app.MapHealthChecks("/health");
        app.MapHealthChecks("/health/ready");
        app.MapGet("/health/live", () => Results.Ok("ok"));
        app.MapControllers();
        app.Run();
    }
}

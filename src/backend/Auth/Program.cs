using Auth.Extensions;
using Auth.Models;
using Auth.Services;
using Data.Data;
using Domain.Models;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

namespace Auth;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        var configuration = builder.Configuration;

        builder.AddObservability();

        // The Auth API owns CREDENTIALS and SESSIONS, and nothing else.
        //   - Credentials live in the UserAccounts table (PostgreSQL), read through MainDbContext.
        //   - Sessions live in Valkey under session:{token}.
        //   - Roles and permissions are NOT resolved here. The Main API owns those, and the
        //     frontend fetches them after sign-in.
        // An optional external OIDC provider can be switched on via the ExternalIdp section;
        // it ships disabled and empty.

        // Database: the Auth API only ever touches UserAccounts, but it shares the application's
        // single DbContext so the model stays in one place. Migrations are owned by the Main API.
        builder.Services.AddDbContext<MainDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("MainDbConnection")
                    ?? throw new InvalidOperationException("ConnectionStrings:MainDbConnection configuration is required."),
                npgsql => npgsql.MigrationsAssembly("Data")));

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

        // Local identity provider: password policy + the hasher used to store and verify passwords.
        // PasswordHasher<UserAccount> is ASP.NET Core's PBKDF2 implementation; MainDbContextSeeder
        // hashes the development demo accounts with exactly the same type.
        builder.Services.Configure<LocalIdentityOptions>(configuration.GetSection(LocalIdentityOptions.SectionName));
        builder.Services.AddSingleton<IPasswordHasher<UserAccount>, PasswordHasher<UserAccount>>();
        builder.Services.AddScoped<ILocalIdentityService, LocalIdentityService>();

        // Optional external OIDC slot. Registering it is harmless while ExternalIdp:Enabled is false -
        // every endpoint checks the flag and answers 503.
        builder.Services.Configure<ExternalIdpOptions>(configuration.GetSection(ExternalIdpOptions.SectionName));
        builder.Services.AddHttpClient();
        builder.Services.AddScoped<IExternalIdpService, ExternalIdpService>();

        builder.Services.AddScoped<IAuthSessionService, AuthSessionService>();
        builder.Services.AddControllers();

        // Health checks are split by TAG so that liveness and readiness can answer different
        // questions from one registration (see the MapHealthChecks calls at the bottom):
        //   - untagged / liveness -> nothing runs, the 200 only proves the process is serving.
        //   - "ready"             -> the dependencies this API needs to do real work.
        // Dependency probes MUST carry the "ready" tag and nothing else.
        string[] readyTags = ["ready"];
        builder.Services.AddHealthChecks()
            .AddNpgSql(configuration.GetConnectionString("MainDbConnection")!, name: "postgresql", tags: readyTags)
            .AddRedis(configuration["Valkey:ConnectionString"]!, name: "valkey", tags: readyTags);
        builder.Services.AddOpenApi();

        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        app.UseCors("AllowSpecificOrigin");

        // Three endpoints, same convention as the Main API:
        //
        //   /health       LIVENESS. Runs NO checks at all (Predicate = _ => false) — it answers
        //                 "this process is up and serving HTTP", nothing more.
        //
        //                 WHY THIS MUST NOT TOUCH POSTGRES OR VALKEY: in an NIE Ignite workspace
        //                 this is the URL the Coder `auth-api` coder_app healthcheck polls
        //                 (build/coder/template/v1/main.tf -> http://localhost:15001/health, every
        //                 10s, 30 failures allowed). supervisord starts auth-api and main-api at
        //                 the SAME priority, and the application database is created by the MAIN
        //                 API's Database.Migrate() — so on a cold first boot Postgres has no
        //                 database for the Auth API to connect to yet. If liveness probed Postgres,
        //                 a slow (or broken) Main API build would park the whole workspace at
        //                 Degraded and the Ignite UI would show a dead preview tab, even though the
        //                 Auth API itself is perfectly healthy. Dependency state belongs on
        //                 readiness, not liveness.
        //
        //   /health/ready READINESS. Runs the "ready"-tagged checks (Postgres + Valkey) — use this
        //                 for load-balancer/rollout gating, not for process liveness.
        //
        //   /health/live  Cheap plain-text liveness for uptime monitors that want a body.
        app.MapHealthChecks("/health", new HealthCheckOptions { Predicate = _ => false });
        app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") });
        app.MapGet("/health/live", () => Results.Ok("ok"));
        app.MapControllers();
        app.Run();
    }
}

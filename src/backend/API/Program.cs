using API.Authorization;
using API.Extensions;
using API.Mapping;
using API.Middleware;
using API.Sessions;
using Data.Data;
using Domain.Models;
using Mapster;
using MapsterMapper;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using AppTemplate.AI;
using Services.Services;
using Services.Services.CatalogItem;
using Services.Services.Code;
using Services.Services.Document;
using Services.Services.Email;
using Services.Services.FileStorage;
using Services.Services.PurchaseOrder;
using Services.Services.PurchaseOrderDocument;
using Services.Services.PushNotification;
using Services.Services.Reports;
using Services.Services.Vendor;
using Services.Services.Workflow;
using Shared.Helpers;
using Shared.Models;
using Shared.Services.UserContext;
using StackExchange.Redis;

namespace API;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        var configuration = builder.Configuration;

        // Regional defaults. "Application:TimeZone" drives every DateTimeHelper.Now
        // call, the TickerQ scheduler, and the cron monitors. It ships as
        // "Asia/Singapore" — change it in appsettings.json for your own project.
        // This must run before anything reads the clock.
        DateTimeHelper.Configure(configuration["Application:TimeZone"]);

        // Add observability (Sentry + OpenTelemetry)
        builder.AddObservability(
            activitySources:
            [
                "AppTemplate.AI.Chat",
                "AppTemplate.AI.AzureOpenAI",
                "AppTemplate.AI.AgentFramework",
                "AppTemplate.AI.Embeddings",
                "AppTemplate.AI.Orchestrator",
                "AppTemplate.AI.Rag",
                "AppTemplate.AI.Tools"
            ],
            meters:
            [
                "AppTemplate.AI",
                "AppTemplate.API"
            ]);

        // Add HttpContextAccessor for user context
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddScoped<IUserContextService, UserContextService>();

        // Add database context. To enable pgvector-backed RAG, install the
        // pgvector OS package and add `.UseVector()` here (see
        // Libraries/AI/Services/Rag/PgVectorRagService.cs).
        builder.Services.AddDbContext<MainDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("MainDbConnection"),
                b => b.MigrationsAssembly("Data"))
            .UseSeeding((context, _) => MainDbContextSeeder.Seed((MainDbContext)context))
            .UseAsyncSeeding((context, _, cancellationToken) =>
                MainDbContextSeeder.SeedAsync((MainDbContext)context, cancellationToken: cancellationToken)));

        // Resolve generic DbContext to MainDbContext so generic action filters
        // (e.g. OwnedEntityActionFilter<TEntity>) can use it without coupling to MainDbContext.
        builder.Services.AddScoped<DbContext>(sp => sp.GetRequiredService<MainDbContext>());

        // Bind SecurityHeadersOptions so SecurityHeadersMiddleware can apply CSP/HSTS/etc.
        builder.Services.Configure<API.Middleware.SecurityHeadersOptions>(
            configuration.GetSection(API.Middleware.SecurityHeadersOptions.SectionName));


        // Add Valkey connection
        builder.Services.AddSingleton<IConnectionMultiplexer>(provider =>
        {
            var connectionString = builder.Configuration["Valkey:ConnectionString"]
                ?? throw new InvalidOperationException("Valkey connection string is not configured.");
            return ConnectionMultiplexer.Connect(connectionString);
        });

        // Add distributed cache for session (required for IDistributedCache)
        builder.Services.AddStackExchangeRedisCache(options =>
        {
            var valkeyConnectionString = builder.Configuration["Valkey:ConnectionString"]
                ?? throw new InvalidOperationException("Valkey connection string is not configured.");
            options.ConfigurationOptions = ConfigurationOptions.Parse(valkeyConnectionString);
            options.ConfigurationOptions.AbortOnConnectFail = false;
            options.InstanceName = builder.Configuration["Valkey:InstanceName"];
        });

        // Add health checks.
        // Tagged by intent so one registration can serve both liveness and readiness (see the
        // MapHealthChecks calls further down):
        //   - untagged / liveness -> nothing runs, the 200 only proves the process is serving.
        //   - "ready"             -> the dependencies this API needs to do real work.
        // Dependency probes MUST carry the "ready" tag and nothing else.
        string[] readyTags = ["ready"];
        builder.Services.AddHealthChecks()
            .AddNpgSql(configuration.GetConnectionString("MainDbConnection")!, name: "postgresql", tags: readyTags)
            .AddRedis(configuration["Valkey:ConnectionString"]!, name: "valkey", tags: readyTags);

        // Add services to the container
        builder.Services.AddScoped<ICodeService, CodeService>();
        builder.Services.AddScoped<IDocumentService, DocumentService>();
        // Select the file-storage backend by FileStorage:Provider ("S3" or local default).
        if (string.Equals(configuration["FileStorage:Provider"], "S3", StringComparison.OrdinalIgnoreCase))
            builder.Services.AddScoped<IFileStorageService, S3FileStorageService>();
        else
            builder.Services.AddScoped<IFileStorageService, FileStorageService>();

        // === SAMPLE: procurement services (removable via task 0003) ===
        builder.Services.AddScoped<IVendorService, VendorService>();
        builder.Services.AddScoped<ICatalogItemService, CatalogItemService>();
        builder.Services.AddScoped<IPurchaseOrderService, PurchaseOrderService>();
        builder.Services.AddScoped<IPurchaseOrderDocumentService, PurchaseOrderDocumentService>();
        // === END SAMPLE ===

        // Add workflow service
        builder.Services.AddScoped<IWorkflowService, WorkflowService>();
        builder.Services.AddScoped<IPdfGenerationService, PlaywrightPdfGenerationService>();

        // AI library (Agent Framework orchestrator, Azure OpenAI client, rate
        // limit, RAG). No credentials are loaded here — configure AzureOpenAI:*
        // via user-secrets, env vars, or Key Vault.
        builder.Services.AddAppTemplateAi(configuration);

        // Add optional feature services when their Copier-gated files are present.
        RegisterOptionalScopedService(
            builder.Services,
            "Services.Services.Chat.IChatService, Services",
            "Services.Services.Chat.ChatService, Services");

        // Add audit and role management services
        builder.Services.AddScoped<IAuditLogService, AuditLogService>();
        builder.Services.AddScoped<IAuditLogger, AuditLogger>();
        builder.Services.AddScoped<IAccessFunctionService, AccessFunctionService>();
        builder.Services.AddScoped<IRoleService, RoleService>();
        builder.Services.AddScoped<IUserRoleService, UserRoleService>();

        // Administrative lifecycle for local accounts (create / approve / deactivate). The hasher
        // must be the SAME type the Auth API verifies with and the seeder writes with, otherwise
        // an account created here could not sign in.
        builder.Services.AddSingleton<IPasswordHasher<UserAccount>, PasswordHasher<UserAccount>>();
        builder.Services.AddScoped<IUserAccountService, UserAccountService>();
        // Deactivating an account has to kill its live sessions too; that store is Valkey.
        builder.Services.AddScoped<ISessionRevocationService, SessionRevocationService>();

        // Add email service
        builder.Services.Configure<EmailSettings>(configuration.GetSection("EmailSettings"));
        builder.Services.AddScoped<IEmailService>(sp =>
            new EmailService(
                sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<EmailSettings>>(),
                sp.GetRequiredService<ILogger<EmailService>>(),
                builder.Environment.ContentRootPath));

        // Add push notification service (OneSignal)
        builder.Services.Configure<OneSignalSettings>(configuration.GetSection("OneSignal"));
        builder.Services.AddHttpClient<IPushNotificationService, OneSignalPushNotificationService>();

        // Configure Mapster
        MappingConfig.RegisterMappings();
        builder.Services.AddSingleton(TypeAdapterConfig.GlobalSettings);
        builder.Services.AddScoped<IMapper, ServiceMapper>();

        // Add CORS policy
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowSpecificOrigin",
                policy => policy
                    .WithOrigins(configuration.GetSection("AllowedCORSOrigin").Get<string[]>() ?? Array.Empty<string>())
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials());
        });

        // Add rate limiting
        builder.Services.AddRateLimiting(configuration);

        // Add response caching
        builder.Services.AddResponseCaching();

        // Add anti-forgery (CSRF protection for session-based auth)
        builder.Services.AddAntiforgery(options =>
        {
            options.HeaderName = "X-XSRF-TOKEN";
            options.Cookie.Name = "XSRF-TOKEN";
            options.Cookie.HttpOnly = false; // Frontend needs to read the cookie
            options.Cookie.SameSite = SameSiteMode.Strict;
        });

        // Add API versioning
        builder.Services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new Asp.Versioning.ApiVersion(1, 0);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions = true;
        }).AddApiExplorer(options =>
        {
            options.GroupNameFormat = "'v'VVV";
            options.SubstituteApiVersionInUrl = true;
        });

        // Add session validation
        builder.Services.AddSessionValidation(configuration);

        // Add TickerQ for background job processing
        builder.Services.AddTickerQServices(configuration, builder.Environment);

        builder.Services.AddControllers();
        builder.Services.AddOpenApi();

        var app = builder.Build();

        // Handle "dotnet run -- seed" for database seeding
        if (args.Contains("seed", StringComparer.OrdinalIgnoreCase))
        {
            using var scope = app.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<MainDbContext>();
            await db.Database.MigrateAsync();
            await DatabaseSeeder.SeedAsync(db);
            Console.WriteLine("Database seeded successfully.");
            return;
        }

        // Handle "dotnet run -- seed-reports" — runs ONLY the report-showcase
        // data on top of whatever is already in the DB. Use this when the
        // base seed has already run but reports look sparse.
        if (args.Contains("seed-reports", StringComparer.OrdinalIgnoreCase))
        {
            using var scope = app.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<MainDbContext>();
            await db.Database.MigrateAsync();
            await DatabaseSeeder.SeedReportShowcaseAsync(db);
            Console.WriteLine("Report showcase data seeded successfully.");
            return;
        }

        // Correlation ID middleware (must be first)
        app.UseMiddleware<CorrelationIdMiddleware>();

        // Use global exception handling middleware
        app.UseGlobalExceptionHandling();

        // Rate limiting
        app.UseRateLimiter();

        // Configure the HTTP request pipeline
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        // Enable CORS before other middleware
        app.UseCors("AllowSpecificOrigin");

        // Security response headers (CSP / HSTS / X-Frame-Options / etc.)
        app.UseSecurityHeaders();

        // Response caching + ETag support
        app.UseResponseCaching();
        app.UseMiddleware<ETagMiddleware>();

        // Map health check endpoints (used by uptime monitoring / Sentry Crons).
        //
        //   /health       LIVENESS. Runs NO checks at all (Predicate = _ => false) — it answers
        //                 "this process is up and serving HTTP", nothing more.
        //
        //                 WHY THIS MUST NOT TOUCH POSTGRES OR VALKEY: in an NIE Ignite workspace
        //                 this is the URL the Coder `main-api` coder_app healthcheck polls
        //                 (build/coder/template/v1/main.tf -> http://localhost:15002/health, every
        //                 10s, 30 failures allowed). A transient dependency blip — Postgres or
        //                 Valkey restarting under supervisord — would otherwise flip the workspace
        //                 to Degraded and leave the Ignite UI showing a dead preview tab while the
        //                 API process is fine. Dependency state belongs on readiness.
        //
        //   /health/ready READINESS. Runs the "ready"-tagged checks (Postgres + Valkey) — use this
        //                 for load-balancer/rollout gating, not for process liveness.
        //
        //   /health/live  Cheap plain-text liveness for uptime monitors that want a body.
        app.MapHealthChecks("/health", new HealthCheckOptions { Predicate = _ => false });
        app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") });
        app.MapGet("/health/live", () => Results.Ok("ok"));

        // Start the TickerQ job host before session validation.
        app.UseTickerQServices();

        // Use session validation middleware
        app.UseSessionValidation();

        // Use authorization middleware
        app.UseAuthorization();

        app.MapControllers();

        using (var scope = app.Services.CreateScope())
        {
            var services = scope.ServiceProvider;
            var context = services.GetRequiredService<MainDbContext>();

            context.Database.Migrate();
        }

        await app.RunAsync();
    }

    /// <summary>
    /// Registers a service only when its type is actually present in the build.
    /// Optional features (for example the AI chat service) can be switched off
    /// when the template is generated, which removes their files — resolving the
    /// types by name keeps Program.cs compiling either way.
    /// </summary>
    private static void RegisterOptionalScopedService(
        IServiceCollection services,
        string serviceTypeName,
        string implementationTypeName)
    {
        var serviceType = Type.GetType(serviceTypeName, throwOnError: false);
        var implementationType = Type.GetType(implementationTypeName, throwOnError: false);
        if (serviceType is null || implementationType is null)
        {
            return;
        }

        services.AddScoped(serviceType, implementationType);
    }
}

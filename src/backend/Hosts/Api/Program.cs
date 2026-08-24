using AI;
using Api.Authorization;
using Api.Extensions;
using Api.Grpc;
using Api.Hubs;
using Api.Mapping;
using Api.Middleware;
using Application.Abstractions;
using Application.Abstractions.Identity;
using Application.Features;
using Application.Features.CatalogItem;
using Application.Features.Code;
using Application.Features.DataTablePreferences;
using Application.Features.Document;
using Application.Features.Email;
using Application.Features.FileStorage;
using Application.Features.Notifications;
using Application.Features.PurchaseOrder;
using Application.Features.PurchaseOrderDocument;
using Application.Features.PushNotification;
using Application.Features.Reports;
using Application.Features.Vendor;
using Application.Features.Workflow;
using Application.Integration;
using BuildingBlocks.Helpers;
using Infrastructure.Identity;
using Infrastructure.Integration;
using Infrastructure.Integration.Observability;
using Infrastructure.Integration.Options;
using Infrastructure.Integrations;
using Infrastructure.Options;
using Infrastructure.Persistence;
using Infrastructure.Providers.Audit;
using Infrastructure.Providers.Email;
using Infrastructure.Providers.FileStorage;
using Infrastructure.Providers.PdfGeneration;
using Infrastructure.Providers.PushNotification;
using Infrastructure.Providers.StaffDirectory;
using Mapster;
using MapsterMapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;
using Validation;

namespace Api;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        var configuration = builder.Configuration;
        var serviceIntegration = configuration
            .GetSection(ServiceIntegrationOptions.SectionName)
            .Get<ServiceIntegrationOptions>() ?? new ServiceIntegrationOptions();

        // Add observability (Sentry + OpenTelemetry)
        builder.AddObservability(
            activitySources:
            [
                "AI.Chat",
                "AI.AzureOpenAI",
                "AI.AgentFramework",
                "AI.Embeddings",
                "AI.Orchestrator",
                "AI.Rag",
                "AI.Tools",
                ServiceIntegrationTelemetry.ActivitySourceName
            ],
            meters:
            [
                "AI",
                "Api",
                ServiceIntegrationTelemetry.MeterName
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
                b => b.MigrationsAssembly("Persistence"))
            .UseSeeding((context, _) => MainDbContextSeeder.Seed((MainDbContext)context))
            .UseAsyncSeeding((context, _, cancellationToken) =>
                MainDbContextSeeder.SeedAsync((MainDbContext)context, cancellationToken: cancellationToken)));

        // Resolve generic DbContext to MainDbContext so generic action filters
        // (e.g. OwnedEntityActionFilter<TEntity>) can use it without coupling to MainDbContext.
        builder.Services.AddScoped<DbContext>(sp => sp.GetRequiredService<MainDbContext>());
        builder.Services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<MainDbContext>());

        builder.Services.AddServiceIntegration(configuration);
        builder.Services.AddScoped<IProcurementIntegrationQuery, ProcurementIntegrationQuery>();
        builder.Services.AddScoped<IIntegrationEventDispatcher, IntegrationEventDispatcher>();
        builder.Services.AddScoped<IIntegrationEventHandler, VendorProfileChangedIntegrationEventHandler>();
        builder.Services.AddScoped<IIntegrationEventProcessor, EfIntegrationEventProcessor>();
        builder.Services.AddScoped<IIntegrationOutboxStore, EfIntegrationOutboxStore>();
        builder.Services.AddScoped<IIntegrationMessageRetentionStore, EfIntegrationMessageRetentionStore>();
        if (serviceIntegration.Enabled && serviceIntegration.RabbitMq.Enabled)
        {
            builder.Services.AddScoped<IIntegrationEventPublisher>(serviceProvider =>
                new EfIntegrationEventPublisher(
                    serviceProvider.GetRequiredService<MainDbContext>(),
                    serviceIntegration.ApplicationKey));
        }
        else
        {
            builder.Services.AddSingleton<IIntegrationEventPublisher, DisabledIntegrationEventPublisher>();
        }

        if (serviceIntegration.Enabled && serviceIntegration.Grpc.Enabled)
        {
            builder.Services.AddGrpc(options =>
            {
                options.MaxReceiveMessageSize = serviceIntegration.Grpc.MaximumMessageBytes;
                options.MaxSendMessageSize = serviceIntegration.Grpc.MaximumMessageBytes;
                options.EnableDetailedErrors = builder.Environment.IsDevelopment();
            });
            builder.Services.AddGrpcHealthChecks();
            builder.Services.Configure<HealthCheckPublisherOptions>(options =>
            {
                options.Delay = TimeSpan.Zero;
                options.Period = TimeSpan.FromSeconds(10);
            });

            if (serviceIntegration.Grpc.RequireAuthentication)
            {
                builder.Services
                    .AddAuthentication()
                    .AddJwtBearer("ServiceIntegration", options =>
                    {
                        options.Authority = serviceIntegration.Grpc.Authority;
                        options.Audience = serviceIntegration.Grpc.Audience;
                        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
                        options.MapInboundClaims = false;
                    });
                builder.Services.AddAuthorization(options =>
                {
                    options.AddPolicy("ServiceIntegration", policy =>
                    {
                        policy.AddAuthenticationSchemes("ServiceIntegration");
                        policy.RequireAuthenticatedUser();
                        policy.RequireAssertion(context =>
                            ServiceIntegrationAuthorization.HasRequiredScope(
                                context.User,
                                serviceIntegration.Grpc.RequiredInboundScope));
                    });
                });
            }
        }

        // Bind SecurityHeadersOptions so SecurityHeadersMiddleware can apply CSP/HSTS/etc.
        builder.Services.Configure<Api.Middleware.SecurityHeadersOptions>(
            configuration.GetSection(Api.Middleware.SecurityHeadersOptions.SectionName));


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

        // Add health checks
        builder.Services.AddHealthChecks()
            .AddNpgSql(
                configuration.GetConnectionString("MainDbConnection")!,
                name: "postgresql",
                tags: ["ready"])
            .AddRedis(
                configuration["Valkey:ConnectionString"]!,
                name: "valkey",
                tags: ["ready"]);

        // Add services to the container
        builder.Services.AddScoped<ICodeService, CodeService>();
        builder.Services.AddScoped<IDocumentService, DocumentService>();
        // Select the file-storage backend by FileStorage:Provider ("S3" or local default).
        if (string.Equals(configuration["FileStorage:Provider"], "S3", StringComparison.OrdinalIgnoreCase))
            builder.Services.AddScoped<IFileStorageService, S3FileStorageService>();
        else
            builder.Services.AddScoped<IFileStorageService, FileStorageService>();

        // === SAMPLE: procurement services (reference vertical; remove only after approved replacement) ===
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
        builder.Services.AddAiInfrastructure(configuration);

        // Add optional feature services when their Copier-gated files are present.
        RegisterOptionalScopedService(
            builder.Services,
            "Application.Features.Chat.IChatService, Application",
            "Application.Features.Chat.ChatService, Application");

        // Add audit and role management services
        builder.Services.AddScoped<IAuditLogService, AuditLogService>();
        builder.Services.AddScoped<IAuditLogger, AuditLogger>();
        builder.Services.AddScoped<IAccessFunctionService, AccessFunctionService>();
        builder.Services.AddScoped<IRoleService, RoleService>();
        builder.Services.AddScoped<IUserRoleService, UserRoleService>();
        builder.Services.AddScoped<IApplicationService, ApplicationService>();
        builder.Services.AddScoped<IApplicationAccessService, ApplicationAccessService>();
        builder.Services.AddScoped<IUserContactProfileService, UserContactProfileService>();
        builder.Services.AddScoped<IUserDataTablePreferenceService, UserDataTablePreferenceService>();
        builder.Services.AddHttpClient<IStaffDirectoryService, StaffDirectoryService>();
        builder.Services.AddSingleton<INotificationTemplateRenderer, NotificationTemplateRenderer>();
        builder.Services.AddScoped<INotificationOutboxService, NotificationOutboxService>();

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
        builder.Services.AddOptions<NotificationSettings>()
            .Bind(configuration.GetSection("Notifications"))
            .Validate(settings => settings.RetentionDays >= 183, "Notification retention must be at least 183 days.")
            .Validate(settings => settings.MaxDeliveryAttempts is >= 1 and <= 10, "Notification delivery attempts must be between 1 and 10.")
            .Validate(settings => settings.DispatchBatchSize is >= 1 and <= 500, "Notification dispatch batch size must be between 1 and 500.")
            .ValidateOnStart();

        // Add optional MyInfo service when the Copier-gated files are present.
        RegisterOptionalMyInfoService(builder.Services);

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

        builder.Services
            .AddControllers()
            .AddNieRequestValidation(
                typeof(Program).Assembly,
                typeof(VendorProfileChangedIntegrationEventHandler).Assembly);
        builder.Services.AddSignalR();
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

        app.UseRouting();

        // Enable CORS before other middleware
        app.UseCors("AllowSpecificOrigin");

        // Security response headers (CSP / HSTS / X-Frame-Options / etc.)
        app.UseSecurityHeaders();

        // Response caching + ETag support
        app.UseResponseCaching();
        app.UseMiddleware<ETagMiddleware>();

        // Map health check endpoints (used by uptime monitoring / Sentry Crons)
        app.MapHealthChecks("/health");
        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = registration => registration.Tags.Contains("ready"),
        });
        app.MapGet("/health/live", () => Results.Ok("ok"));

        // Start the TickerQ job host before session validation.
        app.UseTickerQServices();

        if (serviceIntegration.Enabled
            && serviceIntegration.Grpc.Enabled
            && serviceIntegration.Grpc.RequireAuthentication)
        {
            app.UseAuthentication();
        }

        // Use session validation middleware
        app.UseSessionValidation();

        // Use authorization middleware
        app.UseAuthorization();

        app.MapControllers();
        app.MapHub<NotificationHub>("/hubs/notifications");
        if (serviceIntegration.Enabled && serviceIntegration.Grpc.Enabled)
        {
            var grpcEndpoint = app.MapGrpcService<ProcurementQueryGrpcService>()
                .WithMetadata(new ServiceIntegrationEndpointMetadata());
            app.MapGrpcHealthChecksService()
                .WithMetadata(new ServiceIntegrationEndpointMetadata())
                .AllowAnonymous();
            if (serviceIntegration.Grpc.RequireAuthentication)
            {
                grpcEndpoint.RequireAuthorization("ServiceIntegration");
            }
        }

        await using (var scope = app.Services.CreateAsyncScope())
        {
            var services = scope.ServiceProvider;
            var context = services.GetRequiredService<MainDbContext>();

            await context.Database.MigrateAsync();
        }

        await app.RunAsync();
    }

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

    private static void RegisterOptionalMyInfoService(IServiceCollection services)
    {
        var serviceType = Type.GetType("Application.Features.MyInfo.IMyInfoService, Application", throwOnError: false);
        var implementationType = Type.GetType("Infrastructure.Providers.MyInfo.MyInfoService, Persistence", throwOnError: false);
        if (serviceType is null || implementationType is null)
        {
            return;
        }

        services.AddMemoryCache();
        services.AddHttpClient();
        services.AddScoped(serviceType, serviceProvider =>
        {
            var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
            var httpClient = httpClientFactory.CreateClient("MyInfo");
            return ActivatorUtilities.CreateInstance(serviceProvider, implementationType, httpClient);
        });
    }
}

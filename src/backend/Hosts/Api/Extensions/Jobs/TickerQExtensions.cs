using Api.Jobs;
using BuildingBlocks.Helpers;
using Infrastructure.Persistence;
using TickerQ.DependencyInjection;
using TickerQ.EntityFrameworkCore.Customizer;
using TickerQ.EntityFrameworkCore.DependencyInjection;

namespace Api.Extensions;

/// <summary>
/// Extension methods for configuring TickerQ.
/// </summary>
public static class TickerQExtensions
{
    /// <summary>
    /// Adds TickerQ services to the service collection.
    /// </summary>
    public static IServiceCollection AddTickerQServices(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        services.Configure<AuditLogSettings>(configuration.GetSection("AuditLog"));
        services.AddScoped<AuditLogPurgeJob>();

        services.AddTickerQ(options =>
        {
            options.ConfigureScheduler(schedulerOptions =>
            {
                schedulerOptions.MaxConcurrency = 2;
                schedulerOptions.NodeIdentifier = $"{environment.ApplicationName}-{environment.EnvironmentName}-{Environment.MachineName}";
                schedulerOptions.IdleWorkerTimeOut = TimeSpan.FromMinutes(1);
                schedulerOptions.FallbackIntervalChecker = TimeSpan.FromMinutes(1);
                schedulerOptions.SchedulerTimeZone = DateTimeHelper.SingaporeTimeZone;
            });

            options.AddOperationalStore(efOptions =>
            {
                efOptions.UseApplicationDbContext<MainDbContext>(ConfigurationType.UseModelCustomizer);
                efOptions.SetDbContextPoolSize(32);
            });

        });

        return services;
    }

    /// <summary>
    /// Configures the TickerQ host.
    /// </summary>
    public static WebApplication UseTickerQServices(this WebApplication app)
    {
        app.UseTickerQ();
        return app;
    }
}

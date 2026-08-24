using Api.Observability;
using Application.Features;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TickerQ.Utilities.Base;

namespace Api.Jobs;

/// <summary>
/// Configuration for audit log retention
/// </summary>
public class AuditLogSettings
{
    /// <summary>
    /// Number of months to retain audit logs before purging
    /// </summary>
    public int RetentionMonths { get; set; } = 6;

    /// <summary>
    /// Maximum number of records to delete per batch (to avoid long-running transactions)
    /// </summary>
    public int BatchSize { get; set; } = 1000;
}

/// <summary>
/// TickerQ job to purge old audit logs based on configurable retention period
/// </summary>
public class AuditLogPurgeJob
{
    private const string MonitorSlug = "application-audit-log-purge";

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AuditLogPurgeJob> _logger;
    private readonly AuditLogSettings _settings;

    public AuditLogPurgeJob(
        IServiceScopeFactory scopeFactory,
        ILogger<AuditLogPurgeJob> logger,
        IOptions<AuditLogSettings> settings)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _settings = settings.Value;
    }

    /// <summary>
    /// Executes the purge job on the daily TickerQ schedule.
    /// </summary>
    [TickerFunction("AuditLogPurge", cronExpression: "0 0 2 * * *")]
    public async Task ExecuteAsync(TickerFunctionContext context, CancellationToken cancellationToken)
    {
        await SentryCronMonitor.TrackAsync(
            MonitorSlug,
            ExecuteCoreAsync,
            ConfigureSentryMonitor,
            cancellationToken);
    }

    private async Task ExecuteCoreAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting audit log purge job. Retention period: {Months} months", _settings.RetentionMonths);

        var cutoffDate = BuildingBlocks.Helpers.DateTimeHelper.Now.AddMonths(-_settings.RetentionMonths);
        var totalDeleted = 0;
        var startTime = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<MainDbContext>();

            // Delete in batches to avoid long-running transactions
            int deletedInBatch;
            do
            {
                deletedInBatch = await dbContext.AuditLogs
                    .Where(a => a.Timestamp < cutoffDate)
                    .Take(_settings.BatchSize)
                    .ExecuteDeleteAsync(cancellationToken);

                totalDeleted += deletedInBatch;

                if (deletedInBatch > 0)
                {
                    _logger.LogDebug("Deleted {Count} audit log records in batch", deletedInBatch);
                }

            } while (deletedInBatch == _settings.BatchSize);

            startTime.Stop();

            _logger.LogInformation(
                "Audit log purge completed. Total records deleted: {Count}. Cutoff date: {CutoffDate:yyyy-MM-dd}",
                totalDeleted,
                cutoffDate);

            // Log the job execution to audit trail
            try
            {
                var auditLogger = scope.ServiceProvider.GetRequiredService<IAuditLogger>();
                await auditLogger.LogJobExecutedAsync(
                    "AuditLogPurge",
                    startTime.ElapsedMilliseconds,
                    "Success",
                    $"Deleted {totalDeleted} records older than {cutoffDate:yyyy-MM-dd}");
            }
            catch
            {
                // Don't fail the purge job if audit logging fails
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during audit log purge job");
            throw; // Re-throw to let TickerQ retry
        }
    }

    private static void ConfigureSentryMonitor(Sentry.SentryMonitorOptions options)
    {
        options.Interval("0 2 * * *");
        options.CheckInMargin = TimeSpan.FromMinutes(30);
        options.MaxRuntime = TimeSpan.FromMinutes(60);
        options.FailureIssueThreshold = 1;
        options.RecoveryThreshold = 1;
        options.TimeZone = "Asia/Singapore";
    }
}

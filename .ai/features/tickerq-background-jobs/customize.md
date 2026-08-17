# TickerQ Background Jobs — Customize

## 1. Add a new scheduled job (e.g. `WeeklyReportJob` every Monday at 6 AM)

1. Create a new file `src/backend/API/Jobs/WeeklyReportJob.cs`:
   ```csharp
   using TickerQ.Utilities.Base;

   namespace API.Jobs;

   public class WeeklyReportJob
   {
       private readonly IServiceScopeFactory _scopeFactory;
       private readonly ILogger<WeeklyReportJob> _logger;

       public WeeklyReportJob(IServiceScopeFactory scopeFactory, ILogger<WeeklyReportJob> logger)
       {
           _scopeFactory = scopeFactory;
           _logger = logger;
       }

       [TickerFunction("WeeklyReport", cronExpression: "0 0 6 * * MON")]
       public async Task ExecuteAsync(TickerFunctionContext context, CancellationToken cancellationToken)
       {
           using var scope = _scopeFactory.CreateScope();
           var auditLogger = scope.ServiceProvider.GetRequiredService<IAuditLogger>();
           var stopwatch = System.Diagnostics.Stopwatch.StartNew();

           // ... your work ...

           stopwatch.Stop();
           await auditLogger.LogJobExecutedAsync(
               "WeeklyReport",
               stopwatch.ElapsedMilliseconds,
               "Success");
       }
   }
   ```
2. Register the class in `src/backend/API/Extensions/TickerQExtensions.cs:26-27` (alongside the existing `services.AddScoped<AuditLogPurgeJob>()` registration):
   ```csharp
   services.AddScoped<WeeklyReportJob>();
   ```
3. Restart the API. The scheduler scans `[TickerFunction]` on boot and inserts the schedule row. Open `/tickerq/dashboard` to confirm the new job appears with the next-fire time.
4. No migration needed — TickerQ stores cron metadata in tables already shipped.

## 2. Change a cron expression

1. Edit the `[TickerFunction(...)]` attribute on the method. The cron string lives in source so it's diffable.
2. Restart the API. On boot, TickerQ updates the existing schedule row. Confirm in the dashboard that the next-fire time matches the new expression.
3. Singapore timezone is implicit (`schedulerOptions.SchedulerTimeZone`) — write your cron in SGT, NOT UTC.

## 3. Change the operational concurrency cap

1. Edit `src/backend/API/Extensions/TickerQExtensions.cs:33`:
   ```csharp
   schedulerOptions.MaxConcurrency = 4; // was 2
   ```
2. This is GLOBAL across all jobs on the node. Going above the database's connection-pool size causes failures; keep `MaxConcurrency` < `MainDbContext` pool size. The pool size for TickerQ specifically is set at line 43: `efOptions.SetDbContextPoolSize(32)`.

## 4. Lock down the dashboard for staging / production

1. Generate a strong random key (e.g. `openssl rand -hex 32`).
2. Edit the environment-specific `appsettings.{Env}.json` (or use ASP.NET secrets):
   ```json
   "TickerQ": {
     "DashboardApiKey": "<your-strong-key>"
   }
   ```
3. Re-deploy. The boot code at `TickerQExtensions.cs:55-62` throws `InvalidOperationException` when the key is missing outside Development.
4. Pass the key as an `Authorization: Bearer` (or query string `?apikey=`) when accessing the dashboard URL. Document the key in the team's secret store, never in source.

## 5. Move the dashboard to a different base path

1. Edit `TickerQExtensions.cs:48`:
   ```csharp
   dashboardOptions.SetBasePath("/internal/jobs/dashboard");
   ```
2. Edit `src/backend/API/Middleware/SessionValidationMiddleware.cs:95-101` — change the `/tickerq` entry in `skipPaths` to your new prefix (`/internal/jobs`). If you forget this step the dashboard becomes session-gated.

## 6. Pause / resume a single job at runtime

The dashboard supports this directly — open the job, click Pause / Resume. The state persists in the operational store and survives restarts. There is no code change needed.

## 7. Trigger a job manually for testing

```bash
# Open the dashboard, find the job, click "Trigger Now".
# In Development the dashboard is unauthenticated.
# In other environments include the API key as Authorization header.

# Verify the run inserted a JobExecuted audit row:
psql "$DATABASE_URL" -c \
  "SELECT \"EntityId\", \"DurationMs\", \"Outcome\"
   FROM \"AuditLogs\"
   WHERE \"Action\" = 52
   ORDER BY \"Timestamp\" DESC LIMIT 5;"
```

## 8. Remove a job

1. Delete the job class file (`src/backend/API/Jobs/WeeklyReportJob.cs`).
2. Remove the `services.AddScoped<WeeklyReportJob>()` line from `TickerQExtensions.cs`.
3. Restart the API. On next boot the missing-attribute job is removed from the active scheduler. Existing rows in the operational store remain (they are append-only history); to clear them, either truncate the relevant TickerQ tables manually or wait for retention.

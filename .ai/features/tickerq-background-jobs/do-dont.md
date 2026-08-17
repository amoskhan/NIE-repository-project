# TickerQ Background Jobs — Do and Don't

## DO ✅

1. **DO** declare jobs as plain DI-registered classes. Use constructor injection for `IServiceScopeFactory`, `ILogger<T>`, and any options POCO. `AuditLogPurgeJob` is the canonical example.
2. **DO** decorate the executor method with `[TickerFunction("<UniqueName>", cronExpression: "<cron>")]`. The name MUST be globally unique across the API; TickerQ persists it as a primary key on the schedule row.
3. **DO** use `IServiceScopeFactory` and create your own scope inside the job (`using var scope = _scopeFactory.CreateScope()`). The default DI lifetime when TickerQ resolves the job is singleton-friendly; scoped services like `MainDbContext` MUST be resolved per execution.
4. **DO** use Quartz-style 6-field cron expressions (`<sec> <min> <hour> <day> <month> <dow>`). The shipped job uses `0 0 2 * * *` (2 AM daily). Verify your expression against the dashboard preview before deploying.
5. **DO** use `ExecuteDeleteAsync` / `ExecuteUpdateAsync` for batch operations — the purge job uses these to keep transactions short.
6. **DO** re-throw exceptions so TickerQ retries automatically. The framework's retry/dead-letter behavior depends on seeing the exception bubble up.
7. **DO** call `IAuditLogger.LogJobExecutedAsync` after every job (success and failure paths). The audit log is the source of truth for ops "did this job run?" questions.
8. **DO** set `TickerQ:DashboardApiKey` to a strong random value in non-Development environments. The boot code throws `InvalidOperationException` if the key is missing outside Development.
9. **DO** keep the dashboard at the `/tickerq` base path (specifically `/tickerq/dashboard` per `dashboardOptions.SetBasePath`). The session middleware skip list in `SessionValidationMiddleware.ShouldSkipValidation` is keyed off this prefix.
10. **DO** trust the Singapore timezone. `schedulerOptions.SchedulerTimeZone = DateTimeHelper.SingaporeTimeZone` is set in `TickerQExtensions`; cron expressions read against this zone, NOT against UTC. Don't second-guess it by adding hour offsets.
11. **DO** wrap every recurring job with `SentryCronMonitor`. Sentry Cron is mandatory for uptime-equivalent visibility of scheduled work.

## DON'T ❌

1. **DON'T** use `BackgroundService` / `IHostedService` for new scheduled work. The project standardized on TickerQ for visibility (dashboard) and durability (operational store). Mixing both creates ambiguity in ops.
2. **DON'T** call `MainDbContext` directly from the constructor or instance fields of a TickerQ job class. The framework keeps the job alive across executions; a captured DbContext becomes invalid almost immediately. Always resolve from a fresh scope.
3. **DON'T** use long-running database transactions inside a job. The shipped purge uses `ExecuteDeleteAsync` in batches of `BatchSize` precisely to avoid this. Long transactions block other jobs and stall the scheduler.
4. **DON'T** swallow exceptions silently inside `[TickerFunction]` methods. The retry / dead-letter mechanism only fires when an exception escapes; eaten errors look like "successful runs" in the dashboard.
5. **DON'T** seed jobs by calling `_context.CronTickers.Add(...)`. TickerQ discovers jobs by attribute scan on boot — adding rows manually is fragile and de-syncs from the code.
6. **DON'T** disable `app.UseTickerQ()` "for local dev". The dashboard is a development tool; the scheduler also fires the audit purge. If you want to skip a single job for local, comment out the `[TickerFunction]` attribute on that method or set the job's row to paused via the dashboard.
7. **DON'T** put a TickerQ job inside the Auth API. Auth is deliberately narrow — credentials, sessions, and nothing else — so its attack surface stays small and reviewable. Background jobs belong in the Main API.
8. **DON'T** reference `MaxConcurrency = N` thinking N controls parallelism per job. It's the global worker count for the whole node — increasing it means more CPU contention across all jobs.
9. **DON'T** put secrets in the job's `[TickerFunction]` arguments or in the dashboard's "Trigger now" payload. The dashboard renders payloads in cleartext and the operational store keeps them indefinitely.
10. **DON'T** allow session-validation to wrap `/tickerq/*` — the dashboard authenticates by API key. Removing the `/tickerq` entry from `SessionValidationMiddleware.ShouldSkipValidation` will break the dashboard.
11. **DON'T** create a scheduled job with no way to notice it stopped running. If you use Sentry cron monitors, name the slug `<applicationSlug>-<job-slug>`; if you don't, at minimum log a heartbeat the job's absence would make obvious. A silent scheduled job that has not fired in three weeks is the classic production surprise.

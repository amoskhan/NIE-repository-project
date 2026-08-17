# Audit Logging — Customize

## 1. Make a new entity participate in automatic audit

1. In your entity file, change `: BaseEntity` to `: TimestampedEntity`. That alone enrolls it in the change-tracker scan inside `MainDbContext.OnBeforeSaveChanges`.
2. Add a migration: `dotnet ef migrations add Add<EntityName>Timestamps --project src/backend/Libraries/Data --startup-project src/backend/API`. The migration adds the four `CreatedOn`, `CreatedBy`, `UpdatedOn`, `UpdatedBy` columns.
3. Apply with `dotnet ef database update`.
4. Make any change to the entity through `MainDbContext.SaveChangesAsync` and confirm a row appears in `AuditLogs` with `EntityName = "<EntityName>"`, `Action = Create | Update | Delete`, and `Category = Data` (the default for the auto path).
5. The diff captures scalar properties only. Navigation collections (e.g. `RoleAccessFunctions` on `Role`) are NOT included; only the FK column on the parent appears.

## 2. Add a new manual audit helper for a domain event (e.g. "report exported")

1. Edit `src/backend/Libraries/Services/Services/AuditLog/IAuditLogger.cs` — add a method signature:
   ```csharp
   Task LogReportExportedAsync(string reportName, int recordCount, string? format = "PDF");
   ```
2. Edit `src/backend/Libraries/Services/Services/AuditLog/AuditLogger.cs` — implement it in the "Data Transfer Events" region. Reuse `CreateBaseEntry(EAuditAction.Export, EAuditCategory.DataTransfer, EAuditSeverity.Info, "Report", reportName)` then set `AdditionalData = JsonSerializer.Serialize(new { reportName, recordCount, format })` and call `SaveEntryAsync`.
3. Inject `IAuditLogger` into your reporting service / controller (it's already DI-registered). Call the new helper after the export completes.
4. No DB migration needed — same `AuditLogs` table, same shape.

## 3. Change the retention window

1. Edit `src/backend/API/appsettings.json`:
   ```json
   "AuditLog": {
     "RetentionMonths": 12,
     "BatchSize": 1000
   }
   ```
2. Restart the API. The next nightly TickerQ run (`AuditLogPurgeJob.ExecuteAsync`, cron `0 0 2 * * *`) uses the new cutoff date computed from `DateTimeHelper.Now.AddMonths(-RetentionMonths)`.
3. To force-run now without waiting, hit the TickerQ dashboard at `http://localhost:5002/tickerq/dashboard` (Development uses `WithNoAuth`; production uses `TickerQ:DashboardApiKey`) and trigger `AuditLogPurge` manually.

## 4. Increase the purge batch size for very large tables

1. Edit `appsettings.json` `AuditLog:BatchSize` — default 1000.
2. The job loop in `AuditLogPurgeJob.ExecuteAsync` continues until `deletedInBatch < BatchSize`. Larger batches mean fewer round-trips but longer transactions; pick a value that fits your DB's lock budget (typically 5_000 for Postgres).

## 5. Filter and view audit entries from the admin UI

The page (`src/frontend/main/src/staff/pages/admin/AuditLog.vue`) already supports:

- Date range filter (uses `Timestamp`)
- Category dropdown (`EAuditCategory`)
- Action dropdown (`EAuditAction`)
- Severity filter
- User text-match
- Pagination (driven by `auditLogService.getAuditLogs(...)`)

To add a new filter:

1. Add the filter field to the `AuditLogFilterDto` on the BE — it lives in `src/backend/Libraries/Shared/Dto/AuditLogDto.cs`, not in a file of its own.
2. Apply it inside `AuditLogService.GetPagedAsync` as an `query.Where(...)` clause.
3. Add the FE input in `AuditLog.vue` and pass it through `auditLogService.getAuditLogs`.
4. Mapster mapping does not need a touch unless you also added a column to the DTO.

## 6. Disable automatic audit for one specific TimestampedEntity

If for some reason you want a `TimestampedEntity` that does NOT auto-audit (rare):

1. Open `src/backend/Libraries/Data/Data/MainDbContext.cs`.
2. Find the `IsTracked(EntityEntry entry)` helper near the bottom of the class.
3. Add a clause: `entry.Entity is YourEntity ||` to the `return` chain so the scanner skips it.
4. Document the exception with an ADR in `agents/NNN-skip-audit-for-X.md` — auditing-by-default is a load-bearing convention.

## 7. Send a copy of every Critical-severity audit entry to Sentry / pager

1. Add a `severity == EAuditSeverity.Critical` short-circuit at the end of `AuditLogger.SaveEntryAsync` after the SQL insert succeeds.
2. Inject `Microsoft.Extensions.Logging.ILogger<AuditLogger>` (already there) and call `_logger.LogCritical(...)` — the Sentry sink configured in `ObservabilityExtensions.cs` will capture it.
3. Do NOT inject `Sentry.IHub` directly; route everything through `ILogger` so observability stays swappable.

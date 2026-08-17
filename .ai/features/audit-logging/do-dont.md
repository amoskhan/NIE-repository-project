# Audit Logging — Do and Don't

## DO ✅

1. **DO** inherit from `TimestampedEntity` (not `BaseEntity`) for any entity that should be audited automatically. The change-tracker scan in `OnBeforeSaveChanges` checks `entry.Entity is TimestampedEntity`; `BaseEntity`-only models are silently skipped.
2. **DO** use `IAuditLogger.LogXxxAsync(...)` for every event that is NOT an entity mutation — login, logout, file upload/download, job execution, settings change, denial, role grant change. The typed helpers on `IAuditLogger` cover every category; pick the closest one rather than calling `LogAsync` directly.
3. **DO** set `EAuditCategory` correctly. Read-side filtering in the admin UI groups by category; getting it wrong hides the entry from the wrong filter pane.
4. **DO** set `EAuditSeverity` based on the operational impact: routine = `Info`, suspicious / partial failure = `Warning`, error = `Error`, security incident = `Critical`. The audit log retention job does NOT distinguish severity, but the FE filter does.
5. **DO** use `Shared.Helpers.DateTimeHelper.Now` for timestamps. `Timestamp` is stored as Singapore wall-clock unspecified time — the audit logger calls `DateTimeHelper.AsUnspecified` before insert.
6. **DO** include both `OldValues` and `NewValues` JSON for any update — `AuditEntry.ToAuditLog()` does this for automatic entries, and `AuditLogger.LogAsync` accepts both. Diffs are computed by serializing scalar properties only (no nav properties).
7. **DO** keep `AuditLogger.SaveEntryAsync` on its raw-SQL path. The whole point of the dual-path (auto via `SaveChanges`, manual via raw SQL) is to avoid recursion when the manual logger writes a row that would otherwise re-trigger the SaveChanges hook.
8. **DO** attach `AdditionalData` JSON for context that doesn't fit any column — denied access path, exported record count, file name, etc. The admin UI renders this as collapsible JSON.
9. **DO** keep retention realistic in `AuditLog:RetentionMonths`. 6 months is the default; set it deliberately, because the window you pick is exactly how far back you can investigate an incident. The purge job runs daily and uses `ExecuteDeleteAsync` in batches of `BatchSize` so the transaction is short-lived.
10. **DO** set `Outcome` ("Success" / "Failed" / "Denied" / "Expired") on every entry. The admin filter and Sentry dashboards key off this string.

## DON'T ❌

1. **DON'T** call `_context.AuditLogs.Add(entry); await _context.SaveChangesAsync();` from anywhere. That goes through the SaveChanges interceptor and triggers an infinite loop. Always use `IAuditLogger`.
2. **DON'T** add `OldValues` / `NewValues` columns to your business entity for "history" purposes. The audit log already does this — duplicating the history denormalizes truth.
3. **DON'T** read or filter `AuditLogs` table from a service. Use `IAuditLogService` (it enforces the paging cap, returns an `AuditLogDto`, and respects the `Api.AuditRead` access function).
4. **DON'T** delete rows from `AuditLogs` outside of `AuditLogPurgeJob`. An audit trail with hand-edited gaps is worth less than no audit trail, because you can no longer trust what remains; if you really must, log the deletion itself via `IAuditLogger.LogAsync(EAuditAction.SystemEvent, ...)` first.
5. **DON'T** log secrets (passwords, session tokens, API keys, PII beyond what's already in the user record) to `AdditionalData`. The audit log is read by ops; treat it as a non-secret store.
6. **DON'T** disable the `SaveChanges` audit interceptor "for performance". The diff is shallow (scalars only) and the insert is part of the same transaction. If you really need a fast path, exclude that one entity by switching it from `TimestampedEntity` to `BaseEntity` (and lose its automatic audit).
7. **DON'T** assume `UserId` will be populated for system-triggered events — `AuditLogger.CreateBaseEntry` reads from `IUserContextService` which can be null when there is no HTTP context (e.g. inside the TickerQ purge job). Pass an explicit `userId` argument when one is available.
8. **DON'T** invent new audit actions as ad-hoc strings. Add a value to `EAuditAction` enum (in `EAuditAction.cs`) and pass that. The DB stores the int; the FE reverse-maps to the enum name.
9. **DON'T** rely on `CorrelationId` to be set if `CorrelationIdMiddleware` hasn't run yet. The TickerQ background pipeline does NOT have an HTTP context; `CorrelationId` is null for those rows by design.
10. **DON'T** swallow `SaveEntryAsync` exceptions in calling code. The logger already catches and `_logger.LogError`s — that's the correct contract. Wrapping it in another try/catch obscures the real failure.

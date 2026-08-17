# Audit Logging — File Map

## Owned files

### Backend — domain

| Path                                                               | Layer       | Purpose                                                                                                                                                                                                                                                                                                                                                                   |
| ------------------------------------------------------------------ | ----------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `src/backend/Libraries/Domain/Models/AuditLog.cs`                  | Entity      | The audit row entity (19 columns including `EntityName`, `EntityId`, `Action`, `Category`, `Severity`, `OldValues`, `NewValues`, `ChangedProperties`, `UserId`, `UserName`, `Timestamp`, `IpAddress`, `UserAgent`, `CorrelationId`, `SessionId`, `RequestMethod`, `RequestUrl`, `DurationMs`, `Outcome`, `AdditionalData`)                                                |
| `src/backend/Libraries/Domain/Models/TimestampedEntity.cs`         | Entity base | Marker base class. Only descendants of this class trigger automatic audit on `SaveChangesAsync` — `BaseEntity`-only models are explicitly skipped in the change tracker scan                                                                                                                                                                                              |
| `src/backend/Libraries/Shared/Enum/EAuditAction.cs`                | Enum        | `Create`, `Update`, `Delete`, `Read`, `BulkCreate/Update/Delete`, `Login`, `Logout`, `FailedLogin`, `SessionCreated/Expired/Refreshed`, `RoleAssigned/Removed/Created/Updated/Deleted`, `PermissionGranted/Revoked/Updated`, `AccessDenied`, `FileUpload/Download/Delete`, `Export/Import`, `SettingsChanged`, `SystemEvent`, `JobExecuted`, `EmailSent`, `DataMigration` |
| `src/backend/Libraries/Shared/Enum/EAuditAction.cs` (also defines) | Enum        | `EAuditSeverity` — `Info`, `Warning`, `Error`, `Critical`                                                                                                                                                                                                                                                                                                                 |
| `src/backend/Libraries/Shared/Enum/EAuditAction.cs` (also defines) | Enum        | `EAuditCategory` — `Data`, `Authentication`, `AccessControl`, `FileOperation`, `DataTransfer`, `System`                                                                                                                                                                                                                                                                   |

### Backend — data + services

| Path                                                                   | Layer     | Purpose                                                                                                                                                                                               |
| ---------------------------------------------------------------------- | --------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `src/backend/Libraries/Data/Data/MainDbContext.cs`                     | DbContext | Hosts the auto-audit hook (`SaveChanges` / `SaveChangesAsync` overrides + `OnBeforeSaveChanges` + `OnAfterSaveChanges` + `AuditEntry` helper struct). Also seeds `AuditLog` index + Fluent API config |
| `src/backend/Libraries/Services/Services/AuditLog/IAuditLogger.cs`     | Interface | Typed manual-logging contract (`LogLoginAsync`, `LogAccessDeniedAsync`, `LogFileUploadAsync`, `LogJobExecutedAsync`, `LogRoleAccessChangedAsync`, etc.)                                               |
| `src/backend/Libraries/Services/Services/AuditLog/AuditLogger.cs`      | Service   | Raw-SQL implementation that bypasses the `SaveChanges` interceptor to avoid recursion                                                                                                                 |
| `src/backend/Libraries/Services/Services/AuditLog/IAuditLogService.cs` | Interface | Read-side contract (paged + filtered queries)                                                                                                                                                         |
| `src/backend/Libraries/Services/Services/AuditLog/AuditLogService.cs`  | Service   | Implementation of paged audit-log queries                                                                                                                                                             |

### Backend — API + jobs

| Path                                                | Layer       | Purpose                                                                                                                                                         |
| --------------------------------------------------- | ----------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `src/backend/API/Controllers/AuditLogController.cs` | Controller  | `GetAuditLogs` (filtered + paged), `GetAuditLogById`, summary / category breakdown endpoints — guarded by `AccessFunctionCodes.Api.AuditRead`                   |
| `src/backend/API/Jobs/AuditLogPurgeJob.cs`          | TickerQ job | Daily cron `0 0 2 * * *` — deletes rows older than `AuditLogSettings.RetentionMonths` in batches; logs its own execution via `IAuditLogger.LogJobExecutedAsync` |

### Frontend

| Path                                                   | Layer   | Purpose                                                                                                   |
| ------------------------------------------------------ | ------- | --------------------------------------------------------------------------------------------------------- |
| `src/frontend/main/src/staff/pages/admin/AuditLog.vue` | Page    | Audit log explorer — filter by category, severity, action, user, date range; expand row to view JSON diff |
| `src/frontend/main/src/services/auditLogService.ts`    | Service | API client for `AuditLogController`                                                                       |

## Touched files

| Path                                                              | What it contains                                                                                                                | Why must be touched                                                                                                                         |
| ----------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------- |
| `src/backend/API/Program.cs`                                      | `builder.Services.AddScoped<IAuditLogService, AuditLogService>()` and `builder.Services.AddScoped<IAuditLogger, AuditLogger>()` | DI wiring; remove the matching line if you remove the feature                                                                               |
| `src/backend/API/Extensions/TickerQExtensions.cs`                 | `services.Configure<AuditLogSettings>(configuration.GetSection("AuditLog"))` and `services.AddScoped<AuditLogPurgeJob>()`       | Required for the purge job to be discoverable by TickerQ at runtime                                                                         |
| `src/backend/API/appsettings.json`                                | `"AuditLog": { "RetentionMonths": 6, "BatchSize": 1000 }`                                                                       | The retention window and batch size that drive `AuditLogPurgeJob`                                                                           |
| `src/backend/API/Mapping/MappingProfile.cs`                       | Mapster maps for `AuditLog ↔ AuditLogDto` and any filter DTOs                                                                   | Any new column on `AuditLog` must be projected into the DTO                                                                                 |
| `src/backend/Libraries/Shared/Security/AccessFunctionCatalog.cs`  | `AccessFunctionCodes.Api.AuditRead`, `AccessFunctionCodes.Screen.AuditView` and their seed definitions                          | Required so the audit page is discoverable via access control                                                                               |
| `src/frontend/main/src/router/index.ts`                           | Route entry for `/audit` with `meta.requiredAccessFunction = 'screen.audit.view'`                                               | Without this the page is unreachable                                                                                                        |
| `src/frontend/main/src/composables/usePermissions.ts`             | Sidebar nav entry guarded by `screen.audit.view`                                                                                | Without this the menu link is missing                                                                                                       |
| `src/backend/API/Controllers/AccessControlController.cs`          | Calls `IAuditLogger.LogRoleAccessChangedAsync`, `LogRoleAssignedAsync`, `LogRoleRemovedAsync`                                   | These are the manual audit hooks for access-control mutations — keep them in sync with the controller actions                               |
| `src/backend/API/Authorization/RequireAccessFunctionAttribute.cs` | Calls `IAuditLogger.LogAccessDeniedAsync` on 403                                                                                | The denial audit row originates here                                                                                                        |
| `src/backend/Auth/Controllers/AuthController.cs`                  | (intended) `LogLoginAsync`, `LogFailedLoginAsync`, `LogLogoutAsync` callsites                                                   | Authentication audit entries (note: the Auth API may delegate this to the Main API via a side channel — check the controller before adding) |

## Migrations

| Migration                                                            | What it does                                                                          |
| -------------------------------------------------------------------- | ------------------------------------------------------------------------------------- |
| `<timestamp>_AddAuditLogs.cs`                                        | Creates `AuditLogs` table with indices on `Timestamp`, `UserId`, `Category`, `Action` |
| `<timestamp>_AlterAuditLogsRetention.cs` (only if you change schema) | Adds new columns or partial indices for retention performance                         |

## External dependencies

None — implemented with EF Core, raw `ExecuteSqlRawAsync` for the manual logger, and `System.Text.Json` for diff serialization.

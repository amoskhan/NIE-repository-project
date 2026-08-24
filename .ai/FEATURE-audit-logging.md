# Audit Logging

Automatic create/update/delete audit capture for TimestampedEntity changes, plus manual IAuditLogger events, with queryable audit history.

Rules version: 2026.08.07.1
Feature key: audit-logging  
Adoption: **mandatory**

## Adoption and navigation

- Menu or entry point: required at **Administration > Audit Logs**.
- Visibility: Audit screen/API access only.
- If the feature is not adopted, report not-applicable with the product or architecture reason; do not silently omit it.

## Required libraries and minimums

| Library | Package/runtime | Minimum | Ecosystem |
| --- | --- | --- | --- |
| .NET / ASP.NET Core | net | 10.0.0 | runtime |
| Entity Framework Core | Microsoft.EntityFrameworkCore | 10.0.5 | nuget |
| Npgsql EF Core Provider | Npgsql.EntityFrameworkCore.PostgreSQL | 10.0.1 | nuget |
| PostgreSQL | postgres | 17.0.0 | service |
| Mapster | Mapster | 10.0.4 | nuget |
| TickerQ | TickerQ | 10.2.5 | nuget |
| Vue | vue | 3.5.30 | npm |
| Vue Router | vue-router | 4.5.1 | npm |

Versions are floors, not forced pins for derived applications. Stable upgrades are allowed when compatibility, build, tests, security, and deployment evidence pass. The template itself keeps exact tested pins and lockfiles.

## Rules

| Rule | Severity | Area | Requirement | Required evidence |
| --- | --- | --- | --- | --- |
| NIE-AUDIT-001 | error | coverage | Audit successful and failed create/update/delete operations and all security, access-control, file, export, job, and integration events; do not audit read-only POST searches as mutations. | tests |
| NIE-AUDIT-002 | error | data | Store a stable JSON contract for oldValues, newValues, changes, and additionalData; do not mix incompatible shapes between applications. | contract-tests |
| NIE-AUDIT-003 | error | data | Keep audit rows append-only; permit deletion only through the owned retention purge path. | architecture-tests |
| NIE-AUDIT-004 | error | retention | Retain audit data for at least six months and run a monitored daily purge in bounded batches using Singapore-time scheduling. | configuration, job, and retention tests |
| NIE-AUDIT-005 | error | privacy | Never store secrets, tokens, credentials, full file contents, or unnecessary personal data in audit payloads. | security-review |
| NIE-AUDIT-006 | error | frontend | Use the shared DataTable with server paging (maximum 100), date range, category, action, severity, outcome, user, entity, and correlation filters. | browser-tests |
| NIE-AUDIT-007 | error | frontend | Default the detail view to Differences and provide Before, After, and Raw JSON tabs using the same JSON field names in every application. | browser-tests |
| NIE-AUDIT-008 | error | operations | Capture timestamp, actor or system identity, action, category, severity, outcome, entity, request method/path, IP/user-agent where permitted, session, and correlation ID when available. | contract-tests |
| NIE-AUDIT-009 | error | authorization | Guard the Audit Logs menu, route, list endpoint, detail endpoint, and export if present with audit-specific access functions. | tests |
| NIE-AUDIT-010 | error | verification | Test mutation capture, failed/denied events, JSON diff correctness, filtering/paging, retention cutoff, purge idempotency, and secret redaction. | tests |

## Canonical reference footprint

Use these files as working examples and comparison targets. Procurement is a reference vertical, not a runtime dependency.

- src/backend/Core/Domain/Models/AuditLog.cs
- src/backend/Core/Domain/Models/TimestampedEntity.cs
- src/backend/Core/Domain/Enums/EAuditAction.cs
- src/backend/Infrastructure/Persistence/Data/MainDbContext.cs
- src/backend/Core/Application/Features/AuditLog/IAuditLogger.cs
- src/backend/Infrastructure/Persistence/Providers/Audit/AuditLogger.cs
- src/backend/Core/Application/Features/AuditLog/IAuditLogService.cs
- src/backend/Core/Application/Features/AuditLog/AuditLogService.cs
- src/backend/Hosts/Api/Controllers/AuditLogController.cs
- src/backend/Hosts/Api/Jobs/AuditLogPurgeJob.cs
- src/frontend/apps/main/src/staff/pages/admin/AuditLog.vue
- src/frontend/apps/main/src/services/auditLogService.ts

## AI implementation and verification

The implementing AI must:

1. Assess every rule above before editing and identify affected backend, frontend, data, security, navigation, and test surfaces.
2. Compare the application implementation with the pinned canonical template reference. Classify each shared file as identical, behind, customized, ahead, conflict, or not applicable.
3. Implement missing behavior directly. Preserve domain behavior and integrate shared updates through extension points rather than overwriting valid customization.
4. Add or update focused unit, architecture, integration, component, API, and browser tests according to the evidence type for each rule.
5. Run the standard repository gates and record exact commands, exit status, and meaningful test counts or artifacts.
6. Report each rule as pass, fail, not-applicable, manual-review, or approved-exception with file/line and test evidence.

A separate AI verifier must review material changes from a fresh context, inspect the diff and evidence, rerun risk-relevant gates, and issue an independent verdict. A claim without traceable evidence is not a pass.

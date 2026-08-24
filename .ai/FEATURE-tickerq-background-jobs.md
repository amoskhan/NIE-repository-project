# TickerQ Background Jobs

Scheduled and background jobs via TickerQ, such as the audit-log purge job.

Rules version: 2026.08.07.1
Feature key: tickerq-background-jobs  
Adoption: **conditional**

## Adoption and navigation

- Menu or entry point: not independently required. Administration > Jobs (conditional)
- Visibility: Link only when operators need a protected job dashboard; do not expose an unauthenticated production dashboard.
- If the feature is not adopted, report not-applicable with the product or architecture reason; do not silently omit it.

## Required libraries and minimums

| Library | Package/runtime | Minimum | Ecosystem |
| --- | --- | --- | --- |
| .NET / ASP.NET Core | net | 10.0.0 | runtime |
| TickerQ | TickerQ | 10.2.5 | nuget |
| Entity Framework Core | Microsoft.EntityFrameworkCore | 10.0.5 | nuget |
| Npgsql EF Core Provider | Npgsql.EntityFrameworkCore.PostgreSQL | 10.0.1 | nuget |
| PostgreSQL | postgres | 17.0.0 | service |
| Sentry for ASP.NET Core | Sentry.AspNetCore | 6.4.1 | nuget |

Versions are floors, not forced pins for derived applications. Stable upgrades are allowed when compatibility, build, tests, security, and deployment evidence pass. The template itself keeps exact tested pins and lockfiles.

## Rules

| Rule | Severity | Area | Requirement | Required evidence |
| --- | --- | --- | --- | --- |
| NIE-JOB-001 | error | scheduler | Use TickerQ for persistent scheduled/background jobs; do not add a competing BackgroundService scheduler for the same workload. | architecture-tests |
| NIE-JOB-002 | error | scheduling | Use unique stable job names, six-field cron expressions, an explicit Singapore timezone, and bounded concurrency below database pool capacity. | tests |
| NIE-JOB-003 | error | execution | Create a DI scope per execution, pass cancellation, make work idempotent, and bound retries/timeouts. | integration-tests |
| NIE-JOB-004 | error | observability | Audit success/failure/duration and monitor every scheduled job with Sentry Cron or an approved exception. | operations-tests |
| NIE-JOB-005 | error | security | Protect production dashboard/trigger endpoints with dedicated authorization and never place dashboard credentials in URLs or source. | security-tests |
| NIE-JOB-006 | error | verification | Test duplicate execution, retry/idempotency, cancellation, failure telemetry, schedule reconciliation, and dashboard authorization. | tests |

## Canonical reference footprint

Use these files as working examples and comparison targets. Procurement is a reference vertical, not a runtime dependency.

- src/backend/Hosts/Api/Extensions/TickerQExtensions.cs
- src/backend/Hosts/Api/Jobs/AuditLogPurgeJob.cs
- src/backend/Hosts/Api/Observability/SentryCronMonitor.cs

## AI implementation and verification

The implementing AI must:

1. Assess every rule above before editing and identify affected backend, frontend, data, security, navigation, and test surfaces.
2. Compare the application implementation with the pinned canonical template reference. Classify each shared file as identical, behind, customized, ahead, conflict, or not applicable.
3. Implement missing behavior directly. Preserve domain behavior and integrate shared updates through extension points rather than overwriting valid customization.
4. Add or update focused unit, architecture, integration, component, API, and browser tests according to the evidence type for each rule.
5. Run the standard repository gates and record exact commands, exit status, and meaningful test counts or artifacts.
6. Report each rule as pass, fail, not-applicable, manual-review, or approved-exception with file/line and test evidence.

A separate AI verifier must review material changes from a fresh context, inspect the diff and evidence, rerun risk-relevant gates, and issue an independent verdict. A claim without traceable evidence is not a pass.

# Health & Observability

Health checks, OpenTelemetry tracing/metrics, and Sentry error capture across the API and Auth services.

Rules version: 2026.08.07.1
Feature key: health-observability  
Adoption: **mandatory**

## Adoption and navigation

- Menu or entry point: not independently required. No dedicated menu is required.
- Visibility: Do not expose a template Monitoring page or sidebar item; operate the feature through backend configuration, telemetry destinations, and platform health probes.
- If the feature is not adopted, report not-applicable with the product or architecture reason; do not silently omit it.

## Required libraries and minimums

| Library | Package/runtime | Minimum | Ecosystem |
| --- | --- | --- | --- |
| .NET / ASP.NET Core | net | 10.0.0 | runtime |
| Sentry for ASP.NET Core | Sentry.AspNetCore | 6.4.1 | nuget |
| OpenTelemetry .NET hosting | OpenTelemetry.Extensions.Hosting | 1.15.3 | nuget |
| ASP.NET Core PostgreSQL Health Checks | AspNetCore.HealthChecks.NpgSql | 9.0.0 | nuget |
| ASP.NET Core Redis Health Checks | AspNetCore.HealthChecks.Redis | 9.0.0 | nuget |
| Sentry for Vue | @sentry/vue | 9.47.1 | npm |
| OpenTelemetry JavaScript core | @opentelemetry/core | 2.8.0 | npm |
| OpenTelemetry JavaScript OTLP HTTP exporter | @opentelemetry/exporter-trace-otlp-http | 0.219.0 | npm |

Versions are floors, not forced pins for derived applications. Stable upgrades are allowed when compatibility, build, tests, security, and deployment evidence pass. The template itself keeps exact tested pins and lockfiles.

## Rules

| Rule | Severity | Area | Requirement | Required evidence |
| --- | --- | --- | --- | --- |
| NIE-OBS-001 | error | health | Expose /health, /health/live, and /health/ready with liveness limited to process health and readiness covering required dependencies. | integration-tests |
| NIE-OBS-002 | error | telemetry | Use structured logs, correlation IDs, OpenTelemetry traces/metrics, and Sentry error capture with stable service/environment/release tags. | integration-tests |
| NIE-OBS-003 | error | privacy | Redact secrets, tokens, credentials, request bodies, and unnecessary personal data before telemetry leaves the process. | security-tests |
| NIE-OBS-004 | error | frontend | Initialize frontend telemetry from non-secret runtime config and tag each frontend surface without creating duplicate error reports. | browser-tests |
| NIE-OBS-005 | error | operations | Monitor every externally reachable backend health endpoint and every scheduled job, or document an owned exception. | operations-review |
| NIE-OBS-006 | error | verification | Test degraded dependencies, liveness/readiness behavior, correlation propagation, redaction, and trace export failure without requiring an application Monitoring screen. | tests |

## Canonical reference footprint

Use these files as working examples and comparison targets. Procurement is a reference vertical, not a runtime dependency.

- src/backend/Hosts/Api/Extensions/ObservabilityExtensions.cs
- src/backend/Hosts/Auth/Extensions/ObservabilityExtensions.cs
- src/backend/Hosts/Api/Middleware/CorrelationIdMiddleware.cs
- src/backend/Hosts/Api/Observability/SentryCronMonitor.cs
- src/frontend/packages/platform/src/utils/sentry.ts
- src/frontend/packages/platform/src/config/constants.ts
- src/backend/Hosts/Api/Program.cs
- src/backend/Hosts/Auth/Program.cs
- src/frontend/apps/main/src/main.ts
- src/frontend/apps/auth/src/main.ts
- build/appsettings.api.json
- build/appsettings.auth.json

## AI implementation and verification

The implementing AI must:

1. Assess every rule above before editing and identify affected backend, frontend, data, security, navigation, and test surfaces.
2. Compare the application implementation with the pinned canonical template reference. Classify each shared file as identical, behind, customized, ahead, conflict, or not applicable.
3. Implement missing behavior directly. Preserve domain behavior and integrate shared updates through extension points rather than overwriting valid customization.
4. Add or update focused unit, architecture, integration, component, API, and browser tests according to the evidence type for each rule.
5. Run the standard repository gates and record exact commands, exit status, and meaningful test counts or artifacts.
6. Report each rule as pass, fail, not-applicable, manual-review, or approved-exception with file/line and test evidence.

A separate AI verifier must review material changes from a fresh context, inspect the diff and evidence, rerun risk-relevant gates, and issue an independent verdict. A claim without traceable evidence is not a pass.

# Runtime Configuration and Observability Dossier

## Purpose

This dossier records the Launchpad v2 patterns reviewed and the NIE Template changes made so the frontend can be built once and deployed under different application paths without rebuilding for each environment. It also records the Sentry, OpenTelemetry, health, metrics, and uptime-monitoring setup added to the template.

No Sentry DSNs, OneSignal app IDs, OpenTelemetry collector endpoints, or other keys are embedded in this template.

## Launchpad v2 Reference Points

Reviewed local Launchpad v2 files under `temp/launchpad-v2`:

- `src/frontend/packages/platform/src/config/constants.ts`
- `src/frontend/packages/platform/src/utils/sentry.ts`
- `src/frontend/apps/main/src/services/core/api.ts`
- `src/backend/Hosts/Api/Program.cs`
- `src/backend/Hosts/Auth/Program.cs`
- `src/backend/Hosts/Api/Jobs/Notifications/NotificationDispatcherJob.cs`

Relevant Launchpad v2 patterns:

- Frontend API roots are centralized in shared constants, with app services using constants instead of per-call environment variables.
- Frontend Sentry and browser OpenTelemetry are initialized through a shared utility.
- Backends use Sentry for errors, performance, logs, metrics, and OpenTelemetry trace correlation.
- Backends expose health endpoints for uptime monitoring.
- Scheduled background jobs report to Sentry Cron Monitoring with an explicit monitor slug, cron interval, margin, max runtime, and Singapore timezone.

## NIE Template Runtime URL Strategy

The NIE Template now centralizes frontend runtime URLs in:

- `src/frontend/packages/platform/src/config/constants.ts`

The frontend derives its application base path from `window.location.pathname`.

Examples:

```text
https://domain.example/MYAPP/        -> app base /MYAPP
https://domain.example/MYAPP/login/  -> app base /MYAPP
https://domain.example/              -> app base /
```

Derived backend URLs:

```text
https://domain.example/MYAPP/        -> /MYAPP/api-main
https://domain.example/MYAPP/login/  -> /MYAPP/api-auth
```

The exported constants include:

- `FRONTEND_CONSTANTS.backend.auth`
- `FRONTEND_CONSTANTS.backend.main`
- `FRONTEND_CONSTANTS.api.auth`
- `FRONTEND_CONSTANTS.api.main`
- `FRONTEND_CONSTANTS.apps.auth`
- `FRONTEND_CONSTANTS.apps.main`
- `FRONTEND_CONSTANTS.cookies`
- `FRONTEND_CONSTANTS.sentry`
- `FRONTEND_CONSTANTS.openTelemetry`
- `FRONTEND_CONSTANTS.oneSignal`

Vite remains local-development only for backend proxying:

```text
/api-auth/api -> http://localhost:5001
/api-main     -> http://localhost:5002
```

Production deployment paths were aligned in:

- `build/nginx.conf`
- `build/appsettings.api.json`
- `build/appsettings.auth.json`

## Runtime Configuration Slots

The frontend reads non-secret runtime values from either `window.__NIE_APPLICATION_CONFIG__` or matching meta tags.

Supported keys:

- `cookieDomain`
- `oneSignalAppId`
- `openTelemetryExporterEndpoint`
- `portalSsoEnabled`
- `sentryDsn`
- `sentryEnvironment`
- `sentryTracesSampleRate`

Meta tag form:

```html
<meta name="nie:sentryDsn" content="">
<meta name="nie:openTelemetryExporterEndpoint" content="">
<meta name="nie:portalSsoEnabled" content="true">
```

Runtime global form:

```html
<script>
  window.__NIE_APPLICATION_CONFIG__ = {
    sentryDsn: "",
    sentryEnvironment: "stg",
    openTelemetryExporterEndpoint: "",
    portalSsoEnabled: true,
    oneSignalAppId: "",
  };
</script>
```

## Frontend Observability

Frontend observability is implemented in:

- `src/frontend/packages/platform/src/utils/sentry.ts`
- `src/frontend/apps/main/src/main.ts`
- `src/frontend/apps/auth/src/main.ts`

Behavior:

- Initializes Sentry Vue only when a runtime DSN is present.
- Initializes browser OpenTelemetry only when not running on localhost and an OTLP endpoint is present.
- Adds browser tracing integration for Vue Router.
- Adds optional replay support, disabled for normal sessions by default.
- Scrubs request cookies from Sentry events.
- Uses W3C trace context and baggage propagation.
- Instruments document load, fetch, and XHR.
- Ignores Sentry ingestion URLs during browser telemetry capture.

## Backend Observability

Backend observability was aligned across API and Auth:

- `src/backend/Hosts/Api/Extensions/Observability/ObservabilityExtensions.cs`
- `src/backend/Hosts/Auth/Extensions/ObservabilityExtensions.cs`
- `src/backend/Hosts/Api/Program.cs`
- `src/backend/Hosts/Auth/Program.cs`

Behavior:

- Configures Sentry only when `Sentry:Dsn` is present.
- Enables Sentry logs, metrics, tracing, profiling, stack traces, and OpenTelemetry correlation.
- Keeps `SendDefaultPii` disabled.
- Adds service tags for each backend.
- Configures OpenTelemetry independently from Sentry so OTLP can run without a Sentry DSN.
- Adds ASP.NET Core, HttpClient, EF Core where applicable, runtime metrics, AI activity sources, and Npgsql sources.
- Adds OTLP exporters only when `OpenTelemetry:ExporterEndpoint` or `OTEL_EXPORTER_OTLP_ENDPOINT` is configured.
- Adds OpenTelemetry logs with scopes, formatted messages, and parsed state values.

Template appsettings placeholders were added in:

- `build/appsettings.api.json`
- `build/appsettings.auth.json`

## Health and Uptime Monitoring

Health endpoints:

- API: `/health` and `/health/ready`
- Auth: `/health` and `/health/ready`

Readiness now uses health checks instead of a static OK response.

Auth readiness includes Redis when configured. API readiness includes the existing database and Redis health checks.

For external uptime monitoring, point Sentry Uptime, Azure Monitor, or another monitor at the deployed health endpoint:

```text
https://domain.example/MYAPP/api-auth/health
https://domain.example/MYAPP/api-main/health
```

## Fleet Sentry Creation Rules

The fleet setup must be generated from project metadata, not from a fixed `sentry.uptime.json` copied between applications.

Rules for derived projects:

- Every application entry must have an explicit `applicationSlug` and `pathPrefix`.
- Backend Sentry project slug is `<applicationSlug>-backend`. All backend services in the same app use that DSN.
- Frontend Sentry project slug is `<applicationSlug>-frontend`. All frontend SPAs in the same app use that DSN.
- Backend service tags differentiate `api-main`, `api-auth`, `api-access`, and other backend service slugs.
- Frontend service/app tags differentiate `main`, `login`, `public`, and other frontend surfaces.
- Optional extra Sentry projects are allowed only for separate runtimes such as `<applicationSlug>-cms`, `<applicationSlug>-worker`, `<applicationSlug>-maps-backend`, or `<applicationSlug>-maps-frontend`.
- Sentry Uptime monitors target `/health` only. Do not create uptime checks against `/health/ready` unless a project explicitly needs a separate load-balancer readiness probe.
- Cron monitors use `<applicationSlug>-<monitorSlug>` and Asia/Singapore timezone.
- DSNs are runtime configuration values. Backend DSNs live under `Sentry:Dsn`; frontend DSNs are injected through `window.__NIE_APPLICATION_CONFIG__.sentryDsn` or `<meta name="nie:sentryDsn">`. Environment values and tags separate dev, staging, production, and service surfaces.
- Worker services without public HTTP uptime, such as an MCP server, still get Sentry error monitoring but can set `uptime: false`.

The local fleet config shape used by tools should mirror `Nie.SentrySetup/projects/fleet.json`: one folder per project, one service list per project, optional `cronMonitors`, and generated lock files under `Nie.SentrySetup/projects/<folderName>/`.

## Cron Monitoring

Sentry Cron Monitoring support was added for the API audit purge job:

- `src/backend/Hosts/Api/Observability/SentryCronMonitor.cs`
- `src/backend/Hosts/Api/Jobs/Audit/AuditLogPurgeJob.cs`

Monitor:

```text
slug: application-audit-log-purge
interval: 0 2 * * *
timezone: Asia/Singapore
```

The helper reports in-progress, ok, and error check-ins to Sentry when Sentry is configured. It remains a no-op when Sentry is not configured.

## Operational Notes

- Do not add `.env`-only frontend URL dependencies for deployed API roots.
- Keep deploy-specific values injected at runtime by the hosting page, reverse proxy, or secret provider.
- Keep the frontend build artifact reusable across dev, stg, and prd.
- Do not copy Launchpad v2 DSNs, OneSignal app IDs, API keys, or collector endpoints into the template.
- For new frontend API clients, import `FRONTEND_CONSTANTS` or `getBackendUrl()` from the shared package.
- For new backend services, use the `AddObservability` pattern and tag the correct service name.

## Validation

Validation commands run during this change:

```text
pnpm --filter @nie/platform type-check
pnpm --filter main type-check
pnpm --filter auth type-check
dotnet restore src/backend/Backend.sln
dotnet build src/backend/Hosts/Auth/Auth.csproj -p:OutDir=temp/verify-auth/
dotnet build src/backend/Hosts/Api/Api.csproj -p:OutDir=temp/verify-api/
pnpm --filter main build:production
pnpm --filter auth build:production
```

Known existing warning:

```text
NU1608: Npgsql.EntityFrameworkCore.PostgreSQL 9.0.1 requires Microsoft.EntityFrameworkCore >= 9.0.0 && < 10.0.0, but Microsoft.EntityFrameworkCore 10.0.5 was resolved.
```

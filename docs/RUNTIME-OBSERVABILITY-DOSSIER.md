# Runtime Configuration and Observability

## Purpose

This document records two related things:

1. How the frontend is built **once** and deployed under different application paths and environments without a rebuild.
2. How Sentry, OpenTelemetry, health endpoints, metrics, and uptime monitoring are wired into the template.

No Sentry DSNs, OneSignal app IDs, OpenTelemetry collector endpoints, or other keys are embedded in this template. Every one of them is an empty placeholder that you fill in at deploy time.

## Design principles

- Frontend API roots are centralized in a shared constants module. Application services import those constants instead of reading a per-call environment variable.
- Frontend Sentry and browser OpenTelemetry are initialized through one shared utility, not per app.
- Backends use Sentry for errors, performance, logs, and metrics, with OpenTelemetry trace correlation.
- Every backend exposes health endpoints so an external monitor can watch it.
- Scheduled background jobs report to Sentry Cron Monitoring with an explicit monitor slug, cron expression, margin, max runtime, and timezone.

## Runtime URL strategy

Frontend runtime URLs live in:

- `src/frontend/packages/shared/src/config/constants.ts`

The frontend derives its application base path from `window.location.pathname` at runtime, so the same build artifact works at the domain root or under a path prefix.

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

Vite proxying is local-development only:

```text
/api-auth/api -> http://localhost:5001
/api-main     -> http://localhost:5002
```

Deployment paths are aligned in:

- `build/nginx.conf`
- `build/appsettings.api.json`
- `build/appsettings.auth.json`

## Runtime configuration slots

The frontend reads non-secret runtime values from either the `window.__APP_TEMPLATE_CONFIG__` global or matching meta tags. The hosting page, reverse proxy, or entry-point template injects them at serve time — they are **not** baked into the bundle.

Supported keys:

- `cookieDomain`
- `oneSignalAppId`
- `openTelemetryExporterEndpoint`
- `sentryDsn`
- `sentryEnvironment`
- `sentryTracesSampleRate`

Runtime global form:

```html
<script>
  window.__APP_TEMPLATE_CONFIG__ = {
    sentryDsn: "",
    sentryEnvironment: "stg",
    openTelemetryExporterEndpoint: "",
    oneSignalAppId: "",
  };
</script>
```

Meta-tag form (the fallback when no global is present):

```html
<meta name="app:sentryDsn" content="" />
<meta name="app:openTelemetryExporterEndpoint" content="" />
```

> The meta-tag name prefix is defined by `getMetaContent()` in `src/frontend/packages/shared/src/config/constants.ts`. If you change it there, change it in your hosting page too.

**Only non-secret values belong here.** Anything the browser downloads is public. A Sentry DSN and a OneSignal app ID are designed to be public; an API key or a client secret is not.

## Frontend observability

Implemented in:

- `src/frontend/packages/shared/src/utils/sentry.ts`
- `src/frontend/main/src/main.ts`
- `src/frontend/auth/src/main.ts`

Behavior:

- Initializes Sentry Vue only when a runtime DSN is present. With no DSN, the app runs normally and reports nothing.
- Initializes browser OpenTelemetry only when not running on localhost and an OTLP endpoint is present.
- Adds browser tracing integration for Vue Router.
- Adds optional replay support, disabled for normal sessions by default.
- Scrubs request cookies from Sentry events.
- Uses W3C trace context and baggage propagation.
- Instruments document load, fetch, and XHR.
- Ignores Sentry ingestion URLs during browser telemetry capture.

## Backend observability

Aligned across both services:

- `src/backend/API/Extensions/ObservabilityExtensions.cs`
- `src/backend/Auth/Extensions/ObservabilityExtensions.cs`
- `src/backend/API/Program.cs`
- `src/backend/Auth/Program.cs`

Behavior:

- Configures Sentry only when `Sentry:Dsn` is present.
- Enables Sentry logs, metrics, tracing, profiling, stack traces, and OpenTelemetry correlation.
- Keeps `SendDefaultPii` disabled. Do not turn it on to debug something — you will ship personal data to a third party.
- Adds a service tag per backend so `api-main` and `api-auth` are distinguishable in one project.
- Configures OpenTelemetry independently from Sentry, so OTLP can run without a Sentry DSN and vice versa.
- Instruments ASP.NET Core, HttpClient, EF Core where applicable, runtime metrics, AI activity sources, and Npgsql sources.
- Adds OTLP exporters only when `OpenTelemetry:ExporterEndpoint` or `OTEL_EXPORTER_OTLP_ENDPOINT` is configured.
- Adds OpenTelemetry logs with scopes, formatted messages, and parsed state values.

Deployment placeholders live in `build/appsettings.api.json` and `build/appsettings.auth.json`.

## Health and uptime monitoring

Health endpoints:

- Main API: `/health` and `/health/ready`
- Auth API: `/health` and `/health/ready`

Readiness uses real health checks, not a static OK. Auth readiness includes Valkey when configured; Main API readiness includes the database and Valkey checks.

For external uptime monitoring, point Sentry Uptime, Better Stack, Azure Monitor, GitHub Actions on a schedule, or any other monitor at the deployed health endpoints:

```text
https://domain.example/MYAPP/api-auth/health
https://domain.example/MYAPP/api-main/health
```

## Sentry project conventions

When a project sets Sentry up, generate the configuration from project metadata rather than copying another project's file.

- Give every application an explicit `applicationSlug` and `pathPrefix`.
- Backend Sentry project slug is `<applicationSlug>-backend`. All backend services in the same application share that DSN.
- Frontend Sentry project slug is `<applicationSlug>-frontend`. All frontend SPAs in the same application share that DSN.
- Backend **service tags** differentiate `api-main`, `api-auth`, and any other backend service. Do not create a second Sentry project for a second service.
- Frontend service/app tags differentiate `main`, `login`, and any other frontend surface.
- Extra Sentry projects are justified only for a genuinely separate runtime, for example `<applicationSlug>-worker`.
- Uptime monitors target `/health` only. Do not monitor `/health/ready` unless you specifically need a separate load-balancer readiness probe.
- Cron monitors use `<applicationSlug>-<monitorSlug>` and the application's configured timezone.
- DSNs are runtime configuration. Backend DSNs live under `Sentry:Dsn`; frontend DSNs arrive through `window.__APP_TEMPLATE_CONFIG__.sentryDsn` or the matching meta tag. Environment values and tags separate dev, staging, and production.
- A worker with no public HTTP surface still gets error monitoring but no uptime check.

## Cron monitoring

Sentry Cron Monitoring is wired up for the audit-log purge job as the reference implementation:

- `src/backend/API/Observability/SentryCronMonitor.cs`
- `src/backend/API/Jobs/AuditLogPurgeJob.cs`

Monitor:

```text
slug:     apptemplate-audit-log-purge
interval: 0 2 * * *
timezone: Asia/Singapore   (the shipped default — configurable per project)
```

The helper reports in-progress, ok, and error check-ins when Sentry is configured, and is a no-op when it is not. Copy this pattern for every scheduled job you add: a job that fails silently at 2am is indistinguishable from a job that never ran.

## Regional defaults

Timezone and locale are configuration, not constants. The template ships `Asia/Singapore` and `en-SG` as defaults because it has to ship _something_. Change them in your project's configuration if your users are elsewhere — nothing in the code assumes those values beyond the defaults.

## Operational notes

- Do not add `.env`-only frontend URL dependencies for deployed API roots.
- Keep deploy-specific values injected at runtime by the hosting page, reverse proxy, or secret provider.
- Keep the frontend build artifact reusable across dev, staging, and production.
- For a new frontend API client, import `FRONTEND_CONSTANTS` or `getBackendUrl()` from `@apptemplate/shared`.
- For a new backend service, use the `AddObservability` pattern and tag the correct service name.

## Validation

Commands worth running after any change in this area:

```text
pnpm --filter @apptemplate/shared type-check
pnpm --filter main type-check
pnpm --filter auth type-check
dotnet restore src/backend/AppTemplate.sln
dotnet build src/backend/Auth/Auth.csproj
dotnet build src/backend/API/API.csproj
pnpm --filter main build:production
pnpm --filter auth build:production
```

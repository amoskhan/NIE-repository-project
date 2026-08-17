# App Template Feature Adoption Policy

Generated for template maintainers and agents applying the template to existing projects.

## Purpose

Projects built on this template are not all at the same maturity. Some were scaffolded from the template, some only adopted selected runtime patterns, and some predate the template version they are being compared against. The analyze flow must therefore separate mandatory baseline drift from optional feature adoption.

This policy is the source of truth for that separation.

## Policy Classes

| Policy                  | Meaning                                                                                      | Analyze behavior                                                                |
| ----------------------- | -------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------- |
| Mandatory baseline      | Required in every application built on this template unless an exception is documented.      | Report missing or partial markers as drift and ask for approval to repair.      |
| Conditional mandatory   | Required only when the project has the trigger.                                              | Report missing or partial markers only when the trigger is present.             |
| Default-on feature pack | Included by default for new template projects, but older projects may intentionally skip it. | Ask the user whether to adopt, complete, or document as intentionally disabled. |
| Opt-in feature pack     | Only added when the product needs it.                                                        | Ask the user before creating or completing it. Never apply silently.            |
| Project-specific        | Valid project-owned code that is not a template feature.                                     | Document it, but do not force it into the template.                             |

## Mandatory Baseline

These must be present in new projects and should be restored in existing projects unless there is a documented exception:

- Template governance: `.app-template-version.json`, `.ai/ALIGN.md`, `.ai/ANALYZE.md`, `.ai/features/`, and `.ai/tasks/`.
- API/Auth service split and deployment runtime files.
- Runtime frontend configuration: path-prefix-aware API URLs, non-secret runtime config, and reusable frontend build output.
- Health and observability: backend Sentry, OpenTelemetry logs/traces/metrics, frontend Sentry, and `/health` uptime endpoints.
- Security middleware: exception handling, session validation, correlation ID, security headers, and ETag where applicable.
- Authentication and authorization: Auth app/API with the local identity provider, hashed password storage, session handling, access functions, and guarded controllers.
- Audit logging and retention job.
- Valkey/Redis caching integration where sessions or distributed cache are used.
- App shell/navigation boundary: project data must live in `src/frontend/main/src/app-config/*`.
- Shared frontend utilities and UI component package.
- Code tables, document management, email notification plumbing, feedback widget, OneSignal push plumbing, and PWA/service-worker support when the project has a frontend.

## Conditional Mandatory Rules

- Every scheduled job must either report to Sentry Cron Monitoring or have a documented monitoring exception.
- Every app with backend services must have a `<applicationSlug>-backend` Sentry project/DSN slot; each externally reachable backend service must expose `/health`, have an uptime monitor, and set a service tag such as `api-main` or `api-auth`.
- Every app with frontend surfaces must have a `<applicationSlug>-frontend` Sentry browser project/DSN slot unless it is explicitly non-user-facing; individual frontends must be separated by service/app tags, not extra Sentry projects.
- Every outbound HTTP integration whose target URL is influenced by user input or configuration must use the SSRF allowlist guard.
- Every project that enables the optional external OIDC provider must keep the client secret out of committed config and validate `state` and `nonce` on the callback.
- Every paginated list endpoint must use the shared page-size cap pattern.
- Every record that has per-user or per-department ownership must use the ownership/BOLA pattern.

## Non-Mandatory Template Tools

`tools/template-audit/`, `tools/template-align/`, and `tools/template-versioning/` are template-maintainer tools. They are not required inside derived application repositories and should be run from the central App Template checkout when needed.

Derived applications may keep project-owned tools under `tools/`, but copied template governance tools must not be treated as runtime, build, or deployment dependencies.

## Default-On Feature Packs

These are included by default by `copier.yml` for new projects, but older projects must choose explicitly during analysis:

- `workflow-engine`
- `cloud-storage`

If a project skips one of these, record the reason in its drift report or analysis note.

## Opt-In Feature Packs

These are adopted only when the product needs them:

- `ai-chatbot`
- `pdf-generation`

Analyze must present these as choices and wait for a user decision before applying any task or adding new files.

## Analyze Gate

Any analyze run must ask these questions before editing:

1. Which mandatory and conditional drift items should be repaired now?
2. Which default-on feature packs should be adopted, completed, or documented as intentionally disabled?
3. Which opt-in feature packs should be added, if any?

If the user answers with a subset, apply only that subset. If the user does not choose an optional feature, leave it absent and document it as skipped.

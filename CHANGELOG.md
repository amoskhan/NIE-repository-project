# Changelog

## [2026.07.31.1] — 2026-07-31

**Type:** baseline

First public baseline of App Template — a .NET 10 + Vue 3 full-stack starter for student and
course projects. This is the single version every derived project starts from; there is no
release history before it.

Tasks: 0001

### Stack

- Backend: .NET 10 solution (`src/backend/AppTemplate.sln`) with `API`, `Auth`, and shared libraries
- Frontend: Vue 3 monorepo with `main`, `auth`, `@apptemplate/ui`, and `@apptemplate/shared`
- Data: PostgreSQL 18 (`postgres:18-alpine`) with EF Core migrations; Valkey for caching and
  session storage
- Background jobs: TickerQ
- Tests: Playwright API and E2E suites

### Included capabilities

- **Authentication** — self-contained local identity provider (users table + ASP.NET Core
  `PasswordHasher`, self-service registration and password reset, seeded demo accounts).
  `POST /api/Auth/Login {userid, pd}` returns a Valkey-backed session token carried in the
  `X-Session-Id` header and checked by session validation middleware. A config-driven
  external OIDC slot (Google / Microsoft / GitHub) ships disabled.
- **Authorization** — access-function RBAC, plus audit logging and code tables
- **Content** — document management, file storage (local and S3-compatible), and PDF
  generation/reporting via Playwright
- **Productivity** — AI chatbot (Azure OpenAI + pgvector), workflow engine, email
  notifications, OneSignal push notifications, feedback widget
- **Frontend platform** — app shell and navigation, `@apptemplate/ui` component library,
  progressive web app support, i18n
- **Operations** — health and observability endpoints with Sentry, Docker Compose on public
  base images, a build-only GitHub Actions CI workflow (`.github/workflows/ci.yml` — no image
  push, no deploy), and a neutral Helm chart under `deploy/helm/app-template/`
- **Sample domain** — procurement, kept under `.ai/features/_samples/` as a removable
  learning reference with fictional seed data

### Governance

- Task-oriented alignment machinery: `.ai/tasks/` (schema in `.ai/tasks/_TEMPLATE/`),
  `.ai/ALIGN.md` for adopting template updates, `.ai/ANALYZE.md` for drift reports
- Tooling: `tools/template-align/`, `tools/template-audit/`, `tools/template-versioning/`
- Copier scaffold (`copier.yml`) with optional feature toggles
- Version marker `.app-template-version.json` records `templateVersion` and `appliedTasks`

### Regional defaults

`Asia/Singapore` and `en-SG` ship as configurable defaults, including the `YYYY.MM.DD.N`
release-version stamp. Nothing in the template hardcodes a locale.

- Detailed notes: [docs/template-releases/2026.07.31.1.md](docs/template-releases/2026.07.31.1.md)

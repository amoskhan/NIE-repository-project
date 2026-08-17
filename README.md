# App Template

A full-stack application template for student and course projects: a .NET 10 backend, a Vue 3 + TypeScript frontend, PostgreSQL, and Valkey — wired together with the parts real applications need (login, roles, audit trail, background jobs, file uploads, reporting) already working on day one.

You clone it, run it, and start building your own feature instead of spending your first week on plumbing.

**New here? Start with [`docs/GETTING-STARTED.md`](docs/GETTING-STARTED.md)** — a guided first hour that ends with you shipping one entity end to end.

---

## What you get

| Area                  | What is already built                                                                                      |
| --------------------- | ---------------------------------------------------------------------------------------------------------- |
| Authentication        | Self-contained local identity provider: users table, hashed passwords, registration, password reset        |
| Sessions              | Valkey-backed session store, `X-Session-Id` header contract, session-validation middleware                 |
| Authorization         | Roles + fine-grained access functions, enforced on the API and used to show/hide UI                        |
| Sample domain         | A complete procurement example (vendors, catalog, purchase orders, approvals) you can learn from or delete |
| UI component library  | `@apptemplate/ui` — buttons, inputs, modals, data tables, filter bars, toasts, state panels                |
| App shell             | Sidebar navigation, layouts, theming, and a project-owned config folder for your own routes and menu items |
| Audit logging         | Automatic audit trail with a retention purge job                                                           |
| Background jobs       | TickerQ scheduler for cron and one-off jobs                                                                |
| File handling         | Upload/download with a pluggable storage provider (local disk or S3-compatible)                            |
| Workflow engine       | State machine with transitions, guards, and a state log                                                    |
| Reporting             | Server-side PDF generation via Playwright                                                                  |
| AI chatbot (optional) | Retrieval-augmented chat backed by pgvector                                                                |
| Notifications         | Email plumbing plus OneSignal web push                                                                     |
| Observability         | Sentry (errors, traces, cron monitors) and OpenTelemetry logs/traces/metrics                               |
| PWA and i18n          | Installable service worker, offline shell, and a shared translation layer                                  |
| Tests                 | Vitest unit tests plus Playwright API and end-to-end suites                                                |
| Deployment            | Docker images, Docker Compose, a neutral Helm chart, and GitHub Actions CI                                 |

Optional features (AI chatbot, PDF generation, file storage, workflow engine) are toggled when you scaffold with Copier — see [Start a new project](#start-a-new-project-from-this-template).

---

## Prerequisites

| Tool           | Version | Notes                                             |
| -------------- | ------- | ------------------------------------------------- |
| .NET SDK       | 10.0    | https://dotnet.microsoft.com/download/dotnet/10.0 |
| Node.js        | 22.12+  | https://nodejs.org                                |
| pnpm           | 10.33+  | `npm install -g pnpm@10`                          |
| Docker Desktop | current | Runs PostgreSQL and Valkey locally                |

A VS Code dev container is included (`.devcontainer/`). If you open the repo in a container, .NET, Node, pnpm, PostgreSQL, Valkey, Mailpit, and an S3 emulator are all provisioned and dependencies are installed for you — run `pnpm build` in `src/frontend` once, then go straight to step 4 below.

---

## Quick start

```bash
# 1. Start infrastructure (PostgreSQL 5432, Valkey 6379, Mailpit 1025/8025)
docker compose -f .devcontainer/docker-compose.yml up -d postgres valkey mailpit

# 2. Install and build the frontend workspace
#    (the shared + ui packages must be built before the apps can run)
cd src/frontend
pnpm install
pnpm build

# 3. Restore the backend
dotnet restore src/backend/AppTemplate.sln

# 4. Run everything (each line in its own terminal, from the repo root)
dotnet watch run --project src/backend/Auth      # Auth API  -> http://localhost:5001
dotnet watch run --project src/backend/API       # Main API  -> http://localhost:5002
pnpm --dir src/frontend dev                      # Both SPAs -> 8001 (auth) and 8002 (main)
```

Steps 1 and 3 run from the repository root; step 2 runs from `src/frontend`. If you use VS Code, step 4 has a one-click equivalent: **Run and Debug -> "All Services (Hot Reload)"**, defined in `.vscode/launch.json`.

**Migrations and seed data run automatically.** On startup the Main API applies any pending EF Core migrations and seeds code tables, roles, access functions, workflow transitions, and the demo accounts. You do not need to run `dotnet ef database update` by hand for a first run — see [`docs/MIGRATIONS.md`](docs/MIGRATIONS.md) for when you do.

Then open <http://localhost:8002>. You will be redirected to the login app at <http://localhost:8001>.

---

## Access points

| Service          | URL                                   | Notes                                                    |
| ---------------- | ------------------------------------- | -------------------------------------------------------- |
| Main app (SPA)   | http://localhost:8002                 | The application you build on                             |
| Auth app (SPA)   | http://localhost:8001                 | Login, registration, password reset                      |
| Main API         | http://localhost:5002                 | Business endpoints, `api/{Controller}/{Action}`          |
| Main API OpenAPI | http://localhost:5002/openapi/v1.json | Machine-readable API document                            |
| Auth API         | http://localhost:5001                 | Login, session issue/verify/logout                       |
| Auth API OpenAPI | http://localhost:5001/openapi/v1.json | Machine-readable API document                            |
| Main API health  | http://localhost:5002/health          | Also `/health/ready`                                     |
| Mailpit inbox    | http://localhost:8025                 | Every email the app sends locally, incl. password resets |
| PostgreSQL       | localhost:5432                        | db `AppTemplate`, user/password `postgres`               |
| Valkey           | localhost:6379                        | Session and cache store                                  |

The template ships an OpenAPI document rather than a bundled Swagger UI page. Point any OpenAPI viewer (Scalar, Swagger UI, Postman, Bruno, your IDE's REST client) at the `/openapi/v1.json` URLs, or use a `.http` file such as `src/backend/API/Reports.http`.

---

## Signing in

Authentication is fully self-contained: there is no external identity provider to register with and no VPN to be on. The Auth API owns a users table, hashes passwords with the ASP.NET Core `PasswordHasher`, and issues a session token that the Main API validates against Valkey.

**Demo accounts.** The seeder creates a small set of demo users so the app is usable the moment it boots. Two of them, `alice` and `bob`, are seeded with the **Administrator** role in Development so you can reach the admin screens (Users & Roles, Access Functions, Audit Logs) immediately.

The demo user IDs and their passwords are defined in `src/backend/Libraries/Data/Data/MainDbContextSeeder.cs`. Read them there rather than trusting a value copied into a document.

Three rules before you show your project to anyone:

1. Change or delete the demo accounts. They exist for local development only.
2. Set a real connection string and Valkey password; do not ship the `postgres`/`postgres` defaults.
3. If you enable the optional external OIDC provider (Google, Microsoft, or GitHub), keep its client secret out of git — it belongs in user secrets or an environment variable.

You can also register a new account from the login app and reset a forgotten password there. Outgoing mail goes to the local Mailpit sink — open <http://localhost:8025> to read the reset link. See [`docs/security-model.md`](docs/security-model.md) for the full threat model.

---

## Tech stack

| Layer            | Technology                                                                                |
| ---------------- | ----------------------------------------------------------------------------------------- |
| Backend          | .NET 10, ASP.NET Core, Entity Framework Core, Mapster                                     |
| Frontend         | Vue 3 (Composition API), TypeScript, Vite, Tailwind CSS                                   |
| Database         | PostgreSQL 18 (`postgres:18-alpine`; swap to `pgvector/pgvector:pg18` for the AI chatbot) |
| Cache / sessions | Valkey (Redis-compatible)                                                                 |
| Background jobs  | TickerQ                                                                                   |
| Reporting        | Playwright (HTML to PDF)                                                                  |
| Observability    | Sentry, OpenTelemetry                                                                     |
| Testing          | Vitest (frontend units), Playwright (API + E2E)                                           |
| Packaging        | Docker, Docker Compose, Helm                                                              |
| CI               | GitHub Actions                                                                            |

---

## Project structure

```text
app-template/
|-- .ai/                        # Instructions for AI coding agents (start at .ai/README.md)
|-- .devcontainer/              # Dev container + local PostgreSQL/Valkey/S3 emulator
|-- build/                      # Dockerfiles, nginx config, deployment appsettings
|-- deploy/helm/app-template/   # Helm chart
|-- docs/                       # Documentation (this folder)
|-- src/
|   |-- backend/                # .NET 10 solution (AppTemplate.sln)
|   |   |-- API/                # Main API: controllers, middleware, jobs, mapping
|   |   |-- Auth/               # Auth API: local identity provider + session issuing
|   |   `-- Libraries/
|   |       |-- Domain/         # Entities only
|   |       |-- Data/           # MainDbContext, migrations, seeder
|   |       |-- Services/       # Business logic
|   |       |-- Shared/         # DTOs, enums, helpers, access-function catalog
|   |       `-- AI/             # Chat + embedding support
|   `-- frontend/               # pnpm workspace
|       |-- main/               # Main SPA (port 8002)
|       |-- auth/               # Login SPA (port 8001)
|       `-- packages/
|           |-- ui/             # @apptemplate/ui  — shared components
|           `-- shared/         # @apptemplate/shared — constants, i18n, utils
|-- tests/                      # Playwright API and E2E suites
`-- tools/                      # Template maintenance scripts (rename, align, release)
```

The two folders you will edit most: `src/backend/API/Controllers/` and `src/frontend/main/src/`. Inside the frontend, `src/frontend/main/src/app-config/` is explicitly **yours** — routes, navigation, branding, and access-function codes for your project live there, not in the shell components.

---

## Common commands

### Backend

```bash
# Build the solution
dotnet build src/backend/AppTemplate.sln

# Add and apply a migration
dotnet ef migrations add AddYourEntity --project src/backend/Libraries/Data --startup-project src/backend/API
dotnet ef database update --project src/backend/Libraries/Data --startup-project src/backend/API

# Run a single service with hot reload
dotnet watch run --project src/backend/API
```

### Frontend

```bash
cd src/frontend
pnpm install          # install the whole workspace
pnpm build            # build ui + shared, then both apps
pnpm dev              # run both SPAs in parallel
pnpm dev:main         # run only the main SPA
pnpm type-check       # TypeScript across every package
pnpm test:unit        # Vitest unit tests
pnpm lint
```

### Tests

```bash
# Frontend unit tests (Vitest)
pnpm --dir src/frontend test:unit

# API and end-to-end tests (Playwright)
cd tests
pnpm install
pnpm run install-browsers
pnpm run test:api     # API-level specs
pnpm run test:e2e     # browser end-to-end specs
pnpm test             # everything
```

Authenticated test runs read credentials from `tests/.env.dev.local`. The committed `tests/.env.dev` intentionally leaves `TEST_USERNAME` and `TEST_PASSWORD` blank so no shared credentials live in the repository.

### Template maintenance

```bash
python tools/template-versioning/release.py validate   # check release metadata is consistent
python tools/template-align/align.py                   # list template tasks not yet applied here
```

---

## Start a new project from this template

The recommended path is [Copier](https://copier.readthedocs.io/), which clones the template, renames the namespace, and can pull future template updates into your project with a three-way merge:

```bash
pip install --user copier
copier copy --trust gh:your-org/app-template ./my-app
cd ./my-app
git init && git add . && git commit -m "chore: scaffold from App Template"
```

Copier asks for your project name, title, and .NET root namespace, then runs `tools/template-rename/rename.py` to substitute `AppTemplate` throughout the source. `--trust` is required because Copier will not run that post-copy task otherwise.

To pull in a later template release:

```bash
copier update --trust
git diff            # review the merge, resolve any conflicts like a normal git merge
```

Prefer to do it by hand? Clone the repo, delete `.git`, run `python tools/template-rename/rename.py --to MyApp`, and keep `.app-template-version.json` so your project still records which template release it started from. Full details in [`docs/template-distribution.md`](docs/template-distribution.md).

---

## Documentation

| Document                                                   | Description                                                               |
| ---------------------------------------------------------- | ------------------------------------------------------------------------- |
| [Getting Started](docs/GETTING-STARTED.md)                 | **Read this first.** First hour, then your first feature end to end       |
| [`.ai/README.md`](.ai/README.md)                           | **AI-agent entry point.** Unified instructions for every AI agent         |
| [`.ai/ALIGN.md`](.ai/ALIGN.md)                             | Paste-into-any-agent self-check for projects derived from this template   |
| [`.ai/features/`](.ai/features/)                           | One dossier per feature: files map, do/don't, customize, verify           |
| [`.ai/tasks/`](.ai/tasks/)                                 | Executable upgrade tasks for derived repos                                |
| [Contributing](docs/CONTRIBUTING.md)                       | Code style, do's and don'ts, PR checklist                                 |
| [API Reference](docs/API-REFERENCE.md)                     | Routing conventions and the built-in endpoints                            |
| [Error Handling](docs/error-handling.md)                   | The standard error and response patterns                                  |
| [Migrations](docs/MIGRATIONS.md)                           | Database migration commands and troubleshooting                           |
| [Security Model](docs/security-model.md)                   | Threat model, session rules, access functions                             |
| [Architecture](docs/architecture.md)                       | Your project's architecture (a stub for you to fill in)                   |
| [Data Model](docs/data-model.md)                           | Your project's entities and lifecycle (a stub for you to fill in)         |
| [Design Spec](docs/design-spec.md)                         | Your project's service/DTO/UI design (a stub for you to fill in)          |
| [Requirements](docs/requirements/README.md)                | Your project's requirements (a folder for you to fill in)                 |
| [Doc guides](docs/templates/)                              | How to write each of the documents above                                  |
| [Observability](docs/RUNTIME-OBSERVABILITY-DOSSIER.md)     | Runtime configuration, Sentry, and OpenTelemetry setup                    |
| [Feature Adoption Policy](docs/FEATURE-ADOPTION-POLICY.md) | Which parts are mandatory, default-on, or opt-in                          |
| [Template Distribution](docs/template-distribution.md)     | How the template is distributed and updated                               |
| [Ignite Integration](docs/IGNITE-INTEGRATION.md)           | How this template runs inside an NIE Ignite workspace, and what breaks it |
| [Change Log](CHANGELOG.md)                                 | Template release history                                                  |

---

## Working with AI agents

This repository is written to be readable by AI coding assistants. Point your agent at [`.ai/README.md`](.ai/README.md) — it lists the rules, coding standards, and per-feature dossiers in the order an agent should read them. When you ask an agent to add a feature, tell it which dossier in `.ai/features/` matches the pattern you want copied; that is usually the difference between code that fits the template and code that fights it.

---

## License

MIT. Use it for your coursework, your capstone, your hackathon, or anything else.

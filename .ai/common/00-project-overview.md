# 00 — Project Overview

App Template is a production-grade, full-stack monorepo template for building web applications. It is aimed at student and course project teams who want a real, working baseline — authentication, authorization, auditing, background jobs, observability, and deployment — instead of starting from an empty folder. Every new project clones this template, adopts its versioning contract, then trims and customizes via the task system in `.ai/tasks/`.

## Stack at a glance

| Layer            | Technology                                                                                                                                                                                                                                              |
| ---------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Backend API      | .NET 10, ASP.NET Core, Entity Framework Core, Mapster                                                                                                                                                                                                   |
| Auth API         | .NET 10, local identity provider (users table + ASP.NET Core `PasswordHasher`) with Valkey-backed sessions; optional external OIDC slot, shipped disabled                                                                                               |
| Frontend         | Vue 3 + Composition API + TypeScript + Vite + Tailwind CSS, pnpm monorepo                                                                                                                                                                               |
| Database         | PostgreSQL 18 (ships as `postgres:18-alpine`). The AI chatbot feature additionally needs pgvector — stock `postgres:18-alpine` does **not** include it, so swap the Compose image to `pgvector/pgvector:pg18` before running `CREATE EXTENSION vector`. |
| Cache / sessions | Valkey (Redis-compatible)                                                                                                                                                                                                                               |
| Background jobs  | TickerQ                                                                                                                                                                                                                                                 |
| File storage     | S3-compatible object storage (LocalStack for local dev)                                                                                                                                                                                                 |
| Email            | MailKit over SMTP (Mailpit sink for local dev)                                                                                                                                                                                                          |
| AI chatbot       | Azure OpenAI + pgvector similarity search                                                                                                                                                                                                               |
| Reporting        | Playwright-rendered PDFs                                                                                                                                                                                                                                |
| Observability    | OpenTelemetry + Sentry                                                                                                                                                                                                                                  |
| Testing          | Playwright (API + E2E)                                                                                                                                                                                                                                  |
| Packaging        | Docker + nginx (Docker Compose for local infrastructure)                                                                                                                                                                                                |
| CI/CD            | GitHub Actions                                                                                                                                                                                                                                          |

## Local service ports

| Service                            | Port                 | URL                                   |
| ---------------------------------- | -------------------- | ------------------------------------- |
| Main API                           | 5002                 | http://localhost:5002/openapi/v1.json |
| Auth API                           | 5001                 | http://localhost:5001/openapi/v1.json |
| Main app (frontend)                | 8002                 | http://localhost:8002                 |
| Auth app (frontend)                | 8001                 | http://localhost:8001                 |
| PostgreSQL                         | 5432                 | —                                     |
| Valkey                             | 6379                 | —                                     |
| LocalStack (S3-compatible storage) | 4566                 | http://localhost:4566                 |
| Mailpit (local SMTP sink)          | 1025 SMTP / 8025 web | http://localhost:8025                 |

Long-running services start via `.vscode/launch.json` → `🚀 All Services (Hot Reload)`. Do not invent ad-hoc commands.

Infrastructure comes from Docker Compose:

- `.devcontainer/docker-compose.yml` — local development: PostgreSQL, Valkey, Mailpit, and LocalStack, all published on the ports above.
- `build/docker-compose.yml` — deployment-shaped: the three built images (UI, Auth API, Main API) behind nginx, plus PostgreSQL, Valkey, and Mailpit on a private network.

## Regional defaults

Timezone and locale are **configuration**, not constants. The template ships with `Asia/Singapore` and `en-SG` as defaults; a derived project changes them in configuration without touching shell code. Never hardcode a timezone or locale in feature code.

## Reference samples

The template ships with a **Procurement** reference sample (vendors, catalog items, purchase orders) so cloned projects have working CRUD, audit, file-upload, and approval-workflow examples to learn from. Once a derived project has built its own real entities, it removes procurement by following [`features/_samples/procurement/remove.md`](../features/_samples/procurement/remove.md) (released as a removal task in `.ai/tasks/`). Procurement stays in the template itself.

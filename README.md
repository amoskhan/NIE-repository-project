# NIE Template

A production-ready .NET 10 and Vue 3 enterprise application template with PostgreSQL, UUIDv7 identities, Valkey, TickerQ, optional RabbitMQ pub/sub and authenticated gRPC, shared UI, access control, audit, notifications, and a complete Procurement reference vertical.

The repository uses a rules-only AI development model. Detailed Markdown rules describe the required architecture and behavior; AI agents implement and independently verify changes; standard language and security tools provide deterministic evidence. There are no numbered source-migration tasks or custom Python governance verdicts.

## Quick start

### Prerequisites

- .NET SDK 10.0.102 or newer compatible 10.x version
- Node.js 22.12 or newer and pnpm 10.33 or newer
- Docker Desktop for PostgreSQL, Valkey, and optional RabbitMQ integration tests
- Copier 9.2 or newer when scaffolding a derived application

### Local development

```bash
docker compose -f build/docker-compose.yml up -d postgres valkey
pnpm --dir src/frontend install
pnpm --dir src/frontend build
dotnet ef database update --project src/backend/Infrastructure/Persistence --startup-project src/backend/Hosts/Api
```

Run the API, Auth service, and frontends with the checked-in VS Code launch configurations or their project commands.

| Service | URL |
| --- | --- |
| Main application | http://localhost:8002 |
| Auth application | http://localhost:8001 |
| Main API OpenAPI | http://localhost:5002/openapi/v1.json |
| Auth API OpenAPI | http://localhost:5001/openapi/v1.json |
| pgAdmin | http://localhost:5050 |

## Architecture

- Frontend: Vue 3 Composition API workspace with `apps/main`, `apps/auth`, `@nie/contracts`, `@nie/platform`, and `@nie/ui`
- Backend: .NET 10 clean dependency direction across Hosts, Core, Infrastructure, and focused BuildingBlocks using FluentValidation, EF Core, and Npgsql
- Data: PostgreSQL 17+ with RFC 9562 UUIDv7 application-managed primary and foreign keys
- Validation: FluentValidation and RFC 7807 in the backend; Zod, VeeValidate, and `@vee-validate/zod` in Vue
- Security: session authentication, access-function authorization, application-scoped roles, secure error handling, and audit logging
- Operations: Valkey, TickerQ, OpenTelemetry, Sentry integration, health checks, containers, Helm, and pipelines
- Service integration: provider-neutral versioned contracts, RabbitMQ transactional outbox/inbox pub/sub, and authenticated gRPC for bounded synchronous service queries
- Testing: xUnit v3, architecture tests, Vitest, Playwright API/E2E tests, analyzers, and coverage evidence

## Repository structure

```text
nie-template/
|-- .ai/                       # Global and per-feature Markdown rules
|-- build/                     # Docker, nginx, and pipeline assets
|-- deploy/                    # Helm and deployment pipeline scaffold
|-- docs/                      # Architecture, API, data, security, and operations docs
|-- src/backend/               # Stable Hosts/Core/Infrastructure/BuildingBlocks solution
|-- src/frontend/              # Stable apps and @nie contracts/platform/UI packages
|-- tests/                     # Playwright API and E2E tests
`-- copier.yml                 # Script-free scaffold selection
```

## AI development contract

Start with [`.ai/README.md`](.ai/README.md). It links:

- [global numbered rules](.ai/GLOBAL-RULES.md);
- [minimum library versions](.ai/LIBRARIES.md);
- one `FEATURE-*.md` dossier per feature, including menus, libraries, reference paths, backend/frontend/data/security rules, tests, and evidence;
- the [implementer and independent verifier workflow](.ai/WORKFLOW.md);
- the [evidence report template](.ai/REPORT.md); and
- application-owned context in [`.ai/APPLICATION.md`](.ai/APPLICATION.md).

Procurement remains in the template as a realistic example for shared patterns. Derived applications replace its domain behavior while retaining applicable platform rules and regression evidence.

Dependency choices are also governed: use the .NET runtime/BCL and official Microsoft packages first for .NET platform capabilities, then the technology owner's official open-source package, then a mature leading open-source package. External vendor SDKs stay behind provider-neutral adapters with an approved alternative and exit plan; popularity alone is never sufficient evidence.

## Standard quality commands

```bash
pnpm lint:frontend
pnpm typecheck:frontend
pnpm build:frontend
pnpm test:frontend
pnpm coverage:frontend
pnpm audit:frontend
pnpm format:backend
pnpm build:backend
pnpm test:backend
pnpm coverage:backend
pnpm audit:backend
pnpm test:e2e
```

The pre-commit hook runs staged C#/Vue/TypeScript lint verification plus full frontend lint and a warnings-as-errors .NET build. It does not rewrite files or ask a custom script for an architectural verdict.

## Scaffold a new application

```bash
copier copy https://niegithub.nie.edu.sg/NIE/nie-template ./my-app
cd ./my-app
git init
```

Copier copies the complete buildable reference and records feature decisions, but executes no trusted post-copy scripts. Give the generated repository to an AI agent and ask it to follow `AGENTS.md`: it will retain the stable generic source identities, apply product naming through configuration and branding, adopt or remove optional domain capabilities without breaking shared dependencies, record decisions in `.ai/APPLICATION.md`, run standard gates, and produce evidence.

For future updates, the AI pins a canonical commit, compares rule and source changes, triages security/bug/library/feature impact, and merges common Vue/.NET updates through documented extension points. See [`docs/template-distribution.md`](docs/template-distribution.md).

## Documentation

| Document | Purpose |
| --- | --- |
| [Architecture](docs/architecture.md) | System structure and boundaries |
| [API reference](docs/API-REFERENCE.md) | API contracts |
| [Data model](docs/data-model.md) | Entities and lifecycle |
| [Design specification](docs/design-spec.md) | Service and UI design |
| [Security model](docs/security-model.md) | Threats and controls |
| [Migrations](docs/MIGRATIONS.md) | EF Core and PostgreSQL migration guidance |
| [Template distribution](docs/template-distribution.md) | Copier, version pinning, AI update triage, and shared-code merging |
| [Service integration](docs/service-integration.md) | RabbitMQ, gRPC, contracts, reliability, security, ECS deployment, and operations |
| [Contributing](docs/CONTRIBUTING.md) | Repository contribution guidance |
| [Change log](CHANGELOG.md) | Template release history |

## Credentials

Keep local test credentials only in ignored local files such as `tests/.env.dev.local`. Committed configuration must contain placeholders or non-secret defaults, never shared credentials.

## License

Internal NIE application template.

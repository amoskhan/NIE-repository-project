# Contributing

All material work follows `AGENTS.md`, `.ai/WORKFLOW.md`, the global rules, and the affected feature dossiers. Preserve intentional user changes and do not weaken a quality or security gate to make a change pass.

## Stable identities

Do not rename `Backend.sln`, the generic .NET projects/namespaces, the `apps/main` and `apps/auth` folders, or the `@nie/*` workspace packages for a product. Configure product identity through branding, runtime configuration, deployment values, observability attributes, and catalog metadata. Stable source identities are what allow canonical template changes to merge predictably.

## Backend changes

- Put entities, value types, and domain rules in `Core/Domain`; keep that project dependency-free.
- Put use cases, DTO contracts, and provider interfaces in `Core/Application`.
- Implement database and provider adapters in `Infrastructure/Persistence` or a focused infrastructure project.
- Keep `Hosts/Api` and `Hosts/Auth` as composition roots with thin controllers/endpoints.
- Keep `BuildingBlocks/BuildingBlocks` small, dependency-free, and domain-neutral.
- Validate external request DTOs with FluentValidation through `BuildingBlocks/Validation`.
- Use constructor injection, async I/O, cancellation tokens, structured logging, typed options, and RFC 7807 errors.
- Use UUIDv7 `Guid` values for application-managed relational identifiers.
- Add EF Core migrations only under `Infrastructure/Persistence/Migrations` and test provider-specific behavior against PostgreSQL.
- Add package versions once in `src/backend/Directory.Packages.props`; package references in project files remain versionless.

The direction is `Host -> Application <- Infrastructure`, with `Application -> Domain`. Domain never references outer layers, and application never references infrastructure.

## Frontend changes

- Keep route views focused on composition and put business behavior in a traceable application feature area.
- Use Vue 3 Composition API with `<script setup lang="ts">`, typed props/emits, and focused composables.
- Use `@nie/ui` for visual primitives and composites; it must not import application APIs, routes, or entities.
- Use `@nie/platform` for domain-neutral runtime, API, i18n, observability, and browser capabilities.
- Put dependency-free cross-application DTO/type contracts in `@nie/contracts`.
- Keep routes, navigation, access codes, role labels, and branding in the application's `app-config` boundary.
- Use VeeValidate with Zod for form state and client feedback. Never treat client validation as an authorization or server-validation boundary.
- Add dependencies at the narrowest owning workspace and do not duplicate packages across shared concerns without a documented reason.

## Dependency selection

Use the platform or .NET shared framework first, official Microsoft packages for .NET platform concerns, the technology owner's official open-source package next, and then a mature leading open-source project. Verify publisher, repository, license, maintenance, security, adoption, transitive risk, interoperability, and replacement cost. Proprietary or provider-exclusive dependencies require approval, an adapter boundary, and an exit plan.

## Template updates

Compare the pinned canonical commit with the desired commit, read the changelog, and classify each affected rule and shared file. Merge compatible changes at their stable paths; preserve intentional domain behavior. Never overwrite a customized source folder or advance `.nie-template-version.json` based only on matching instruction files.

## Required checks

Run the affected subset and, before commit, the repository gates:

```bash
pnpm lint:frontend
pnpm typecheck:frontend
pnpm build:frontend
pnpm coverage:frontend
pnpm format:backend
pnpm build:backend
pnpm coverage:backend
pnpm typecheck:e2e
pnpm audit:frontend
pnpm audit:backend
```

`coverage:frontend` and `coverage:backend` are used in place of the bare `test:*` scripts because they enforce the coverage floors required by NIE-TEST-002. The bare `test:frontend` and `test:backend` scripts run the same suites without the gate and are for fast inner-loop work only.

The root backend quality commands select the matching checked-in entry point automatically: `.ps1` through PowerShell on Windows and `.sh` through Bash on Linux. Direct invocation is also supported, for example `pwsh -File build/Invoke-BackendCoverage.ps1` or `bash build/Invoke-BackendCoverage.sh`. Keep the paired interfaces and failure behavior in sync whenever either script changes. The Linux CRAP scorer uses Python 3's standard library to parse OpenCover XML.

These same gates run in `.github/workflows/ci.yml` on every push and pull request, so a local pass and a CI pass mean the same thing. `.husky/pre-commit` runs format, lint and build; `.husky/pre-push` runs the suites.

### Running the service-backed tests

PostgreSQL and RabbitMQ integration tests skip with a stated reason when their services are absent, so the bare test suite is green on a laptop with nothing running. The required `pnpm coverage:backend` gate is calibrated to the complete service-backed suite, so start both services and export both variables before running that gate:

```bash
export NIE_TEST_POSTGRES_ADMIN_CONNECTION="Host=localhost;Port=5432;Database=postgres;Username=nie_test;Password=nie_test"
export NIE_TEST_RABBITMQ_CONNECTION="amqp://nie_test:nie_test@localhost:5672"
```

A skipped test is not a passing test. Before claiming provider evidence, confirm the run reports zero skips.

### Deep quality signals

`.github/workflows/quality-deep.yml` runs weekly and on demand: mutation testing (does each test actually assert anything?) and CRAP risk hotspots (which complex code is least tested?). Run them locally with `pnpm mutation:backend` and `pnpm crap:backend`. Both select the host platform's script and use the exact .NET tool versions in `.config/dotnet-tools.json`. See `docs/test-strategy.md` for the current thresholds and how they ratchet.

Provider changes require integration evidence; critical journeys require browser evidence; material work requires the rule-by-rule report and a separate AI verifier. Do not claim that a build alone proves authorization, audit, accessibility, security, or business correctness.

## Commits and reviews

Keep commits cohesive and use an imperative message such as `refactor: stabilize template source identities`. Include the request, affected rules, migration/rollback impact, commands and results, coverage evidence where risk-relevant, and any residual risk in the review report. Do not commit secrets, local environment files, `bin`, `obj`, `node_modules`, `dist`, logs, or generated temporary output.

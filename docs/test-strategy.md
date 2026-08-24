# Test Strategy

This is the application-level test plan required by `.ai/GLOBAL-RULES.md` (NIE-TEST-001 through NIE-TEST-006) and described by `docs/templates/test-strategy-guide.md`. It records what is tested, where the gates live, what the current floors are, and how they move.

Derived applications inherit this file and should edit the matrices and floors to match their own domain. Do not delete a layer without recording the reason.

## Principle

A gate that is not executed is not a gate. Every rule below maps to a command that runs automatically, and every number below is a measured value rather than an aspiration. Where a floor is lower than we want, it is written as the real number with a ratchet plan — not rounded up to look better.

## Test categories

| Category | Tool | Location | Scope | Gate |
| --- | --- | --- | --- | --- |
| Domain unit | xUnit v3 | `src/backend/Tests/Domain.Tests` | Enum/persistence contracts, entity behaviour, invariants | `ci.yml` backend job |
| Application unit | xUnit v3 + NSubstitute | `src/backend/Tests/Application.Tests` | Business rules, validation, whitelisting, rendering, policies | `ci.yml` backend job |
| API unit | xUnit v3 + NSubstitute | `src/backend/Tests/Api.Tests` | Authorization filters, ownership guards, controller seams, error mapping | `ci.yml` backend job |
| Auth unit | xUnit v3 + NSubstitute | `src/backend/Tests/Auth.Tests` | Session lifecycle, logout/revocation, redirect validation, cookie options | `ci.yml` backend job |
| Validation unit | xUnit v3 | `src/backend/Tests/Validation.Tests` | FluentValidation pipeline, ProblemDetails shape | `ci.yml` backend job |
| Architecture | xUnit v3 (reflection) | `src/backend/Tests/Architecture.Tests` | Layer boundaries, UUIDv7 identity, source layout, deployment scaffold | `ci.yml` backend job |
| Provider integration | xUnit v3 + real PostgreSQL/RabbitMQ | `src/backend/Tests/Integration.Tests` | EF provider behaviour, outbox atomicity, broker topology | `ci.yml` backend job (services attached) |
| Component/unit (Vue) | Vitest + @vue/test-utils | `src/frontend/**/__tests__` | Components, composables, services | `ci.yml` frontend job |
| API/E2E (browser) | Playwright | `tests/specs` | Auth flows, critical journeys, responsive UI | `e2e.yml` (deployed environment) |
| Mutation | Stryker.NET / StrykerJS | `quality-deep.yml` | Do the tests actually assert? | Weekly + on demand |
| CRAP hotspots | coverlet + ReportGenerator | `quality-deep.yml` | Complex code that is undertested | Weekly + on demand |

## Where each gate runs

| Gate | Local | CI |
| --- | --- | --- |
| Format, lint, warnings-as-errors build | `.husky/pre-commit` | `ci.yml` |
| Unit/component/architecture/integration suites | `.husky/pre-push` | `ci.yml` |
| Coverage floors | `pnpm coverage:backend`, `pnpm coverage:frontend` | `ci.yml` (fails below floor) |
| E2E specs compile | `pnpm typecheck:e2e` | `ci.yml` |
| E2E specs execute | `pnpm test:e2e` against a running stack | `e2e.yml` |
| Mutation score | `pnpm mutation:backend` | `quality-deep.yml` |
| CRAP hotspots | `pnpm crap:backend` | `quality-deep.yml` |
| CodeQL | — | `codeql.yml` |

## Coverage floors

Floors ratchet upward only (NIE-TEST-002). Lowering one to make a change pass also violates NIE-GOV-003.

### Backend

Enforced by ReportGenerator in `.github/workflows/ci.yml` via `minimumCoverageThresholds`. ReportGenerator exits non-zero when a floor is unmet, which is what makes this a gate rather than a report.

| Metric | Floor | Notes |
| --- | --- | --- |
| Line | `COVERAGE_MIN_LINE` in `ci.yml` | Raise after each sustained increase |
| Branch | `COVERAGE_MIN_BRANCH` in `ci.yml` | Branch coverage is the honest signal; prioritise it |
| Method | `COVERAGE_MIN_METHOD` in `ci.yml` | |

Excluded from measurement by `src/backend/coverage.runsettings`: EF migrations, generated `.g.cs`, protobuf output, and test assemblies. `CompilerGeneratedAttribute` is deliberately **not** excluded — excluding it would drop every async method and auto-property and inflate the number rather than measure it.

### Frontend

Enforced by `thresholds` in each package's `vitest.config.ts`; Vitest fails the run when unmet.

| Package | Config |
| --- | --- |
| `apps/main` | `src/frontend/apps/main/vitest.config.ts` |
| `packages/ui` | `src/frontend/packages/ui/vitest.config.ts` |
| `packages/platform` | `src/frontend/packages/platform/vitest.config.ts` |

Every config sets an explicit `coverage.include`. Without it Vitest instruments only files a test happens to import, which reports a high percentage over a small subset of the package and hides everything untested. Treat any config without `coverage.include` as a reporting defect.

## Ratchet procedure

1. Read the achieved number from the CI job summary (backend) or the Vitest summary (frontend).
2. If the achieved number has exceeded the floor by a comfortable margin across several consecutive runs, raise the floor to just below the achieved value.
3. Commit the floor change on its own, with the achieved number in the commit message.
4. Never lower a floor. If a legitimate change reduces coverage, add tests in the same change.

## Priority order for closing gaps

Ranked by risk rather than by how easy the percentage is to move:

1. **`Hosts/Auth`** — session, logout, revocation and redirect validation. Six mandatory `NIE-AUTHN-*` rules depend on it.
2. **`Hosts/Api` authorization** — `RequireAccessFunctionAttribute`, `RequireOwnershipAttribute`, `OwnedEntityActionFilter<TEntity>`. Deny paths matter more than allow paths.
3. **`Core/Application`** — the largest body of business rules in the backend.
4. **`packages/ui`** — every derived application depends on it.
5. **`Core/Domain`** — persistence-contract enums where a silent renumber reclassifies existing rows.
6. **`Infrastructure/AI`** — currently untested; scope depends on whether the chatbot feature is adopted.

## Deep quality thresholds

| Signal | Setting | Location | Direction |
| --- | --- | --- | --- |
| Mutation score break | `thresholds.break` | `src/backend/stryker-config.json` | Raise as suites mature |
| CRAP maximum | `CRAP_MAX` | `quality-deep.yml` | Lower as hotspots are paid off |
| Cyclomatic maximum | `CYCLOMATIC_MAX` | `quality-deep.yml` | Lower as hotspots are paid off |

CRAP = `complexity² × (1 − coverage)³ + complexity`. It rises sharply when complex code is untested, which is exactly the code that breaks under change. A complexity-10 method scores 10 at full coverage and 110 at zero coverage.

How it is produced, and why it takes two tools:

- CRAP needs **per-method cyclomatic complexity and per-method coverage in the same report**. The Microsoft collector used for the ci.yml coverage gate emits Cobertura without complexity, so `quality-deep.yml` re-runs coverage through **coverlet in OpenCover format**, which carries both. These are not duplicate tools; only one of the two formats can express complexity.
- ReportGenerator gates **cyclomatic complexity** natively and produces the HTML hotspot report, but it does **not** render a CRAP Score column for coverlet-produced OpenCover input. The CRAP number itself is therefore computed by the paired Windows [`build/Get-CrapScore.ps1`](../build/Get-CrapScore.ps1) and Linux [`build/Get-CrapScore.sh`](../build/Get-CrapScore.sh) entry points from the same XML. Both scorers union sequence-point coverage for the same production method across all test-project reports before calculating CRAP and fail when any merged method exceeds `CRAP_MAX`.
- Source-generator output (regex, OpenAPI, protobuf) is excluded by the script's `ExcludeClassPattern`. It carries enormous complexity that nobody wrote and nobody can test, and counting it buries the real hotspots.

Run it locally:

```bash
pnpm crap:backend
```

### Current CRAP hotspots

Measured 2026-08-08 over 2,461 unique methods in all backend OpenCover reports. These are the standing work items; the `CRAP_MAX` gate sits just above the worst of them so it blocks regression today, and should be lowered as each is addressed.

| CRAP | Complexity | Coverage | Method |
| ---: | ---: | ---: | --- |
| 39402 | 198 | 0% | `FileStorageContentTypes.GetContentType` |
| 2352 | 48 | 0% | `VendorService.ApplyQuery` |
| 1980 | 44 | 0% | `MyInfoController.GetTestProfileValue` |
| 1980 | 44 | 0% | `VendorService.GetFilterOptionsAsync` |
| 1640 | 40 | 0% | `CatalogItemService.ApplyQuery` |

The file-content lookup, data-grid query builders, and MyInfo test-profile selection are security-relevant behavior, so the current list is an active risk backlog rather than a cosmetic metric.

## Service-backed tests

`Integration.Tests` needs real PostgreSQL and RabbitMQ. Tests skip with a stated reason when the connection variables are absent, so the suite is green on a laptop with nothing running, and CI sets both so nothing is skipped there.

```bash
export NIE_TEST_POSTGRES_ADMIN_CONNECTION="Host=localhost;Port=5432;Database=postgres;Username=nie_test;Password=nie_test"
export NIE_TEST_RABBITMQ_CONNECTION="amqp://nie_test:nie_test@localhost:5672"
```

A skipped test is not a passing test. Before claiming provider evidence under NIE-TEST-003, confirm the run reports zero skips. `ci.yml` additionally passes `--minimum-expected-tests`, so a collapsed or mis-discovered suite fails instead of reporting a hollow green. The bare test command remains usable without services, but `pnpm coverage:backend` is calibrated to the complete service-backed suite and requires both services to satisfy its floors.

## Test quality rules

From NIE-TEST-004 and NIE-TEST-005, these are what a reviewer should reject:

- A test with no assertion, or whose assertion cannot fail.
- A test that only asserts a property returns what was just assigned to it.
- A test asserting on CSS selectors or DOM structure instead of roles, accessible names, or returned values.
- A test that depends on execution order, wall-clock time, ambient culture, or another test's data.
- An unconditional `sleep`/`Thread.Sleep` used for synchronisation.
- A test that fails (rather than skips) because an external service is absent.
- A test that passes only on retry, left untracked.

Playwright is configured with `retries: 2` on CI. `e2e.yml` reports any test that passed only on retry as a warning and in the job summary, so retries surface flakiness instead of concealing it.

## Adding a new feature

1. Write the failing test first at the lowest sufficient level (NIE-TEST-001). Record a reason in the evidence report if you skip test-first.
2. Add or update tests at every layer the change touches (NIE-TEST-003).
3. Run the gate commands in `docs/CONTRIBUTING.md`.
4. Confirm coverage did not fall and that no new CRAP hotspot appeared in the code you touched.
5. Record commands, results and residual risk in the report format from `.ai/REPORT.md`.

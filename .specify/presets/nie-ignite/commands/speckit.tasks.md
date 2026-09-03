---
description: Apply NIE Ignite quality gates to native task generation.
---

## NIE Ignite task requirements

- Read all applicable feature artifacts directly, including contracts,
  `data-model.md`, `integrations.md`, `ui/screens.md`, and `workflows.md`.
- Use only the provisioned workspace's local NIE template source. Never clone,
  pull, or fetch a second template repository and never invoke a credential
  helper for template discovery.
- Read the resolved Delivery assurance plan before shaping tasks:
  - POC: prefer large coherent slices and consolidate cases into fewer test
    tasks, but do not omit automated regression coverage for any `operationId`
    or screen command.
  - Standard: add focused changed-behavior tests with implementation and one
    broader affected validation checkpoint per user-story/release slice.
  - Enterprise: use strict test-first development inside each self-contained
    implementation task, then emit complete matching-layer regression,
    evidence, and independent-verifier work even when slower.
  - Apply Enterprise depth only to explicitly risk-escalated slices when the
    rest of the feature uses POC or Standard.
- Keep native checklist syntax and stable task identifiers:
  `- [ ] T001 [P] [US1] Description; depends on T000; writes: src/exact/file.ts`.
- Include authorization, audit, migrations, API contracts, UI permissions,
  automated tests, and validation at the depth and cadence required by the
  assurance plan. Functional and security requirements remain in scope at all
  profiles even when ordinary POC test automation is deferred.
- Start with foundation and a backend contract phase. Implement entities,
  migrations, DTOs, services, workflow/state rules, controllers,
  authorization, audit, concurrency, idempotency, integrations, and automated
  API tests for every OpenAPI `operationId`. Only after a referenced backend
  operation is implemented and its standalone backend tests pass may a frontend
  task bind the approved Vue source to it. Express that prerequisite through a
  direct or transitive `depends on T###` chain. Later journey phases remain
  independently previewable and end-to-end.
- Every numbered task is an independently executable green unit. A task may
  create a failing test first as an internal red-green implementation step, but
  it must also implement enough behavior and run the relevant checks green
  before completion. Never emit a standalone task whose deliverable is an
  intentionally failing, skipped, disabled, quarantined, or placeholder test.
  Put standalone `@layer(test)` regression tasks after the matching
  `@layer(backend)` implementation for every referenced `operationId`.
- The approved Vue `sourcePaths` already own every screen's visual UI/UX. Do
  not create, build, redesign, generate, or duplicate visual screen/page
  markup, layouts, or review-only components in `tasks.md`. Generate
  backend/domain/API work and typed route/composable bindings that connect the
  existing approved components to real data and actions. A thin route wrapper
  is allowed only when it passes state to an approved visual SFC; it must not
  contain a second visual implementation. The design entry receives mock view
  models; the routed application passes API-backed view models into the exact
  same visual SFCs. Preserve the approved component structure, NIE tokens,
  accessibility, responsive layout, and stable tour targets.
- Screens generation already applied product branding, approved product
  navigation and routes, canonical StaffLayout/LoginPage composition, preserved
  repository-mandated template operations/administration destinations such as
  Access Control and Audit Logs, and removed only frontend samples. Add an early
  task that verifies this approved baseline without rewriting it. When the
  application profile says `remove`, add separate
  backend work to remove reference-only registrations, permissions, seeds, and
  API surfaces, then verify Procurement, sample MyInfo, and sample AI Chat are
  absent from frontend routes, menus, source modules, and visible labels. Keep
  the template-owned shell, logo, profile menu, responsive mechanics, tokens,
  shared components, security/access-control/audit/operations routes, and
  reviewed visual structure unchanged.
- Perform that absence check inside the cleanup task and through Ignite's
  repository-owned screen verifier. Never schedule or create a persistent
  sample-removal test, fixture, filename, path, regex, label array, or source
  literal under a generated application's `src/` tree that reintroduces the
  retired sample vocabulary merely to prove it is absent. Persistent app tests
  instead assert a positive allowlist of the specification-owned product
  routes, navigation, permissions, approved application tables, and preserved
  template operations. Migration or schema rebaseline tests must enumerate the
  approved application tables positively; they must not name retired sample
  tables as an absence assertion. A later task must not recreate a
  sample-specific test artifact removed by Screens.
- When Plan found an unfilled `.ai/APPLICATION.md`, add an early ordinary
  foundation task to populate it from the approved application profile and
  technical plan before any parallel product wave.
- Organize tasks into independently testable user-story slices after setup and
  backend contract work. Mark `[P]` only where file and dependency ownership
  really allow parallel execution.
- Keep every phase in one contiguous checklist block; a phase name must never
  reappear after another phase begins. Foundation and backend contract phases
  are valid prerequisites and must leave the project buildable with their
  matching tests passing. After them, every journey phase delivers a usable
  end-to-end slice across data, API, approved UI bindings, access, audit,
  validation, and browser coverage. The first journey verifies the approved
  branded shell and, when authentication is in scope, implements the missing
  sign-in and deep-link return behavior without recreating its visual. Treat
  every later phase boundary as a durable preview and
  feedback checkpoint. Every phase must leave the application buildable, runnable, and useful
  at its declared maturity without relying on a later phase to repair work it
  claims complete.
- In every phase that adds or changes a user-visible workflow, make the first
  ordinary task author or update that slice's role/access-aware guided-tour
  steps, stable targets, and affected user/technical documentation before the
  workflow implementation tasks. Keep later implementation and regression work
  synchronized with that contract. Reuse the repository's shared tour UI and
  never describe a control absent from the current effective access profile.
- Ignite automatically appends a protected `Verify and prepare live preview`
  system step to every phase. It runs build/tests, asks the selected CLI to
  repair failures, starts required services, and checks the preview before the
  phase can be reviewed. Do not add a duplicate numbered phase-gate,
  live-preview-readiness, or generic final-verification task.
- Give every task one exact `@layer(foundation|backend|test|frontend|browser|
  documentation)` marker and compact traceability using `@spec(FR-###)`,
  `@workflow(WF-###)`, `@api(operationId)`, `@entity(Entity.field)`, and for UI
  work `@screen(stable-id)`, `@control(screen-id/control-slug)`, and
  `@state(screen-id/state)`. Derive `control-slug` from the exact interaction
  Control label by lowercasing it, replacing each non-alphanumeric run with one
  hyphen, and trimming edge hyphens. Put every marker on the same native
  checkbox line as its `T###` task; indented notes, phase prose, and headings do
  not count.
  Every automated case has one unique `@test(TEST-###)`; use `@flow(...)` and
  `@annotation(...)` only when applicable.
- Project the canonical graph completely. For each `operationId`, the union of
  its backend tasks must include every `x-requirements` value as `@spec`, every
  `x-workflows` value as `@workflow`, and every `x-entities-read` and
  `x-entities-write` `Entity.field` as `@entity`. A later standalone test task
  depends directly or transitively on all backend tasks for that operation; its
  test/browser tasks include every `x-requirements` value. Every user-facing
  operation needs a later frontend task that depends on its passing standalone
  tests and is tagged with `@api`, every `@screen`, and every deterministic
  `@control` whose interaction uses it. Local-only controls still need a
  frontend task with their `@screen` and `@control`. Every screen needs a later
  browser task that depends on all of its frontend bindings and is tagged with
  that `@screen`, every declared `@control`, every non-local operation, and
  `@state` markers for loading, empty, forbidden, error, and success. Each
  test/browser task needs an `@api` or `@screen` target; evidence-only
  review/report tasks use `@layer(documentation)`. Feedback reconciliation may
  add or revise pending tasks but must never uncheck, rename, or rewrite
  completed history.
- Make implementation completeness machine-checkable with `@concern(...)`
  markers on the same native checkbox lines. The union across foundation and
  backend tasks must include `data-model`, `migration`,
  `integration-boundary`, and `background-processing`; a deliberate
  no-integration/no-worker outcome still receives the marker on the
  foundation/backend task that verifies and documents that outcome.
  For every `operationId`, its backend task union must include `dto`,
  `validation`, `domain-service`, `workflow-state`, `controller`,
  `authorization`, `record-scope`, `audit`, `concurrency`, `idempotency`, and
  `openapi-conformance`. Its automated test union must include `unit-test`,
  `integration-test`, and `allow-deny-test`. Every user-facing operation's
  frontend task union must include `typed-api-client`, `state-binding`,
  `approved-sfc-binding`, `ui-states`, `invalidation`, and `access-control`.
  Every screen's browser task union must include `real-api`,
  `persisted-reload`, `failure-states`, `denied-state`, `desktop-mobile`, and
  `navigation`. Do not use unregistered concern names or prose as a substitute;
  task validation rejects a missing concern before implementation begins.
- Browser tasks exercise every declared command plus loading, empty, success,
  validation, conflict, denied/forbidden, retry, and server-failure states.
  Assert real API calls and persisted reload behavior, never mock production
  mutations, and cover desktop plus 390-pixel phone critical journeys.
- Put `[P]` tasks into contiguous parallel waves. Every `[P]` task MUST end in a
  `writes:` clause listing its exact repository-relative write paths. Write sets
  in one wave MUST be disjoint; owning a directory also owns all descendants.
  A non-`[P]` task or a phase boundary ends the wave and is a hard scheduling
  barrier. Group same-file changes into one coherent task instead of creating
  many micro-tasks that cannot run concurrently.
- Treat the numbered checklist as the exact sequential execution order. A task
  must never register, reference, modify, validate, or run an artifact that is
  first created by a later task. Put every project, file, directory, package,
  migration, schema, route, and service creation task before all consumers.
- State every non-obvious prerequisite as `depends on T###` in the consuming
  task. Every referenced prerequisite must exist, have a lower task number, and
  form an acyclic graph. Before returning, audit the complete checklist for
  missing artifacts, forward references, and dependency cycles, then reorder or
  combine tasks until execution from the first unchecked task can proceed
  without relying on future work.
- Never discard existing completed checkboxes. If the plan changed, add or
  revise tasks explicitly and report any completed task whose assumptions are
  now stale.
- Do not start implementation, commit, push, or deploy.

---
description: Apply NIE Ignite quality gates to native task generation.
scripts:
  sh: scripts/bash/setup-tasks.sh --json
  ps: scripts/powershell/setup-tasks.ps1 -Json
  py: scripts/python/setup_tasks.py --json
---


## User Input

```text
$ARGUMENTS
```

You **MUST** consider the user input before proceeding (if not empty).

## Pre-Execution Checks

**Check for extension hooks (before tasks generation)**:
- Check if `.specify/extensions.yml` exists in the project root.
- If it exists, read it and look for entries under the `hooks.before_tasks` key
- If the YAML cannot be parsed or is invalid, skip hook checking silently and continue normally
- Filter out hooks where `enabled` is explicitly `false`. Treat hooks without an `enabled` field as enabled by default.
- For each remaining hook, do **not** attempt to interpret or evaluate hook `condition` expressions:
  - If the hook has no `condition` field, or it is null/empty, treat the hook as executable
  - If the hook defines a non-empty `condition`, skip the hook and leave condition evaluation to the HookExecutor implementation
- For each executable hook, output the following based on its `optional` flag:
  - **Optional hook** (`optional: true`):
    ```
    ## Extension Hooks

    **Optional Pre-Hook**: {extension}
    Command: `/{command}`
    Description: {description}

    Prompt: {prompt}
    To execute: `/{command}`
    ```
  - **Mandatory hook** (`optional: false`):
    ```
    ## Extension Hooks

    **Automatic Pre-Hook**: {extension}
    Executing: `/{command}`
    EXECUTE_COMMAND: {command}

    Wait for the result of the hook command before proceeding to the Outline.
    ```
    After emitting the block above you MUST actually invoke the hook and wait for it to finish before continuing. Run it the same way you would run the command yourself in this agent/session (the invocation may differ from the literal `{command}` id shown above, e.g. a skills-mode agent runs it as `/skill:speckit-...` or `$speckit-...`). Emitting the block alone does not run the hook.
- If no hooks are registered or `.specify/extensions.yml` does not exist, skip silently

## Outline

1. **Setup**: Run `{SCRIPT}` from repo root and parse FEATURE_DIR, TASKS_TEMPLATE, and AVAILABLE_DOCS list. `FEATURE_DIR` and `TASKS_TEMPLATE` must be absolute paths when provided. `AVAILABLE_DOCS` is a list of document names/relative paths available under `FEATURE_DIR` (for example `research.md` or `contracts/`). For single quotes in args like "I'm Groot", use escape syntax: e.g 'I'\''m Groot' (or double-quote if possible: "I'm Groot").

2. **Load design documents**: Read from FEATURE_DIR:
   - **Required**: plan.md (tech stack, libraries, structure), spec.md (user stories with priorities)
   - **Optional**: data-model.md (entities), contracts/ (interface contracts), research.md (decisions), quickstart.md (test scenarios)
   - **IF EXISTS**: Load `/memory/constitution.md` for project principles and governance constraints
   - Note: Not all projects have all documents. Generate tasks based on what's available.

3. **Execute task generation workflow**:
   - Load plan.md and extract tech stack, libraries, project structure
   - Load spec.md and extract user stories with their priorities (P1, P2, P3, etc.)
   - If data-model.md exists: Extract entities and map to user stories
   - If contracts/ exists: Map interface contracts to user stories
   - If research.md exists: Extract decisions for setup tasks
   - Generate tasks organized by user story (see Task Generation Rules below)
   - Generate dependency graph showing user story completion order
   - Create parallel execution examples per user story
   - Validate task completeness (each user story has all needed tasks, independently testable)

4. **Generate tasks.md**: Read the tasks template from TASKS_TEMPLATE (from the JSON output above) and use it as structure. If TASKS_TEMPLATE is empty, fall back to `.specify/templates/tasks-template.md`. Fill with:
   - Correct feature name from plan.md
   - Phase 1: Setup tasks (project initialization)
   - Phase 2: Foundational tasks (blocking prerequisites for all user stories)
   - Phase 3+: One phase per user story (in priority order from spec.md)
   - Each phase includes: story goal, independent test criteria, tests (if requested), implementation tasks
   - Final Phase: Polish & cross-cutting concerns
   - All tasks must follow the strict checklist format (see Task Generation Rules below)
   - Clear file paths for each task
   - Dependencies section showing story completion order
   - Parallel execution examples per story
   - Implementation strategy section (MVP first, incremental delivery)

## Mandatory Post-Execution Hooks

**You MUST complete this section before reporting completion to the user.**

Check if `.specify/extensions.yml` exists in the project root.
- If it does not exist, or no hooks are registered under `hooks.after_tasks`, skip to the Completion Report.
- If it exists, read it and look for entries under the `hooks.after_tasks` key.
- If the YAML cannot be parsed or is invalid, skip hook checking silently and continue to the Completion Report.
- Filter out hooks where `enabled` is explicitly `false`. Treat hooks without an `enabled` field as enabled by default.
- For each remaining hook, do **not** attempt to interpret or evaluate hook `condition` expressions:
  - If the hook has no `condition` field, or it is null/empty, treat the hook as executable
  - If the hook defines a non-empty `condition`, skip the hook and leave condition evaluation to the HookExecutor implementation
- For each executable hook, output the following based on its `optional` flag:
  - **Mandatory hook** (`optional: false`) — **You MUST emit `EXECUTE_COMMAND:` for each mandatory hook**:
    ```
    ## Extension Hooks

    **Automatic Hook**: {extension}
    Executing: `/{command}`
    EXECUTE_COMMAND: {command}
    ```
    After emitting the block above you MUST actually invoke the hook and wait for it to finish before continuing. Run it the same way you would run the command yourself in this agent/session (the invocation may differ from the literal `{command}` id shown above, e.g. a skills-mode agent runs it as `/skill:speckit-...` or `$speckit-...`). Emitting the block alone does not run the hook.
  - **Optional hook** (`optional: true`):
    ```
    ## Extension Hooks

    **Optional Hook**: {extension}
    Command: `/{command}`
    Description: {description}

    Prompt: {prompt}
    To execute: `/{command}`
    ```

## Completion Report

Output path to generated tasks.md and summary:
- Total task count
- Task count per user story
- Parallel opportunities identified
- Independent test criteria for each story
- Suggested MVP scope (typically just User Story 1)
- Format validation: Confirm ALL tasks follow the checklist format (checkbox, ID, labels, file paths)

Context for task generation: {ARGS}

The tasks.md should be immediately executable - each task must be specific enough that an LLM can complete it without additional context.

## Task Generation Rules

**CRITICAL**: Tasks MUST be organized by user story to enable independent implementation and testing.

**Tests are OPTIONAL**: Only generate test tasks if explicitly requested in the feature specification or if user requests TDD approach.

### Checklist Format (REQUIRED)

Every task MUST strictly follow this format:

```text
- [ ] [TaskID] [P?] [Story?] Description with file path
```

**Format Components**:

1. **Checkbox**: ALWAYS start with `- [ ]` (markdown checkbox)
2. **Task ID**: Sequential number (T001, T002, T003...) in execution order
3. **[P] marker**: Include ONLY if task is parallelizable (different files, no dependencies on incomplete tasks)
4. **[Story] label**: REQUIRED for user story phase tasks only
   - Format: [US1], [US2], [US3], etc. (maps to user stories from spec.md)
   - Setup phase: NO story label
   - Foundational phase: NO story label
   - User Story phases: MUST have story label
   - Polish phase: NO story label
5. **Description**: Clear action with exact file path

**Examples**:

- ✅ CORRECT: `- [ ] T001 Create project structure per implementation plan`
- ✅ CORRECT: `- [ ] T005 [P] Implement authentication middleware in src/middleware/auth.py`
- ✅ CORRECT: `- [ ] T012 [P] [US1] Create User model in src/models/user.py`
- ✅ CORRECT: `- [ ] T014 [US1] Implement UserService in src/services/user_service.py`
- ❌ WRONG: `- [ ] Create User model` (missing ID and Story label)
- ❌ WRONG: `T001 [US1] Create model` (missing checkbox)
- ❌ WRONG: `- [ ] [US1] Create User model` (missing Task ID)
- ❌ WRONG: `- [ ] T001 [US1] Create model` (missing file path)

### Task Organization

1. **From User Stories (spec.md)** - PRIMARY ORGANIZATION:
   - Each user story (P1, P2, P3...) gets its own phase
   - Map all related components to their story:
     - Models needed for that story
     - Services needed for that story
     - Interfaces/UI needed for that story
     - If tests requested: Tests specific to that story
   - Mark story dependencies (most stories should be independent)

2. **From Contracts**:
   - Map each interface contract → to the user story it serves
   - If tests requested: Each interface contract → contract test task [P] before implementation in that story's phase

3. **From Data Model**:
   - Map each entity to the user story(ies) that need it
   - If entity serves multiple stories: Put in earliest story or Setup phase
   - Relationships → service layer tasks in appropriate story phase

4. **From Setup/Infrastructure**:
   - Shared infrastructure → Setup phase (Phase 1)
   - Foundational/blocking tasks → Foundational phase (Phase 2)
   - Story-specific setup → within that story's phase

### Phase Structure

- **Phase 1**: Setup (project initialization)
- **Phase 2**: Foundational (blocking prerequisites - MUST complete before user stories)
- **Phase 3+**: User Stories in priority order (P1, P2, P3...)
  - Within each story: Tests (if requested) → Models → Services → Endpoints → Integration
  - Each phase should be a complete, independently testable increment
- **Final Phase**: Polish & Cross-Cutting Concerns

## Done When

- [ ] tasks.md generated with all phases, task IDs, and file paths
- [ ] Extension hooks dispatched or skipped according to the rules in Mandatory Post-Execution Hooks above
- [ ] Completion reported to user with task count, story breakdown, and MVP scope



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

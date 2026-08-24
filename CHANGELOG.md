# Changelog

## [2026.08.10.2] — 2026-08-10

**Type:** feature

Require every derived application to declare its real product identity and an
owned remove-or-retain decision for the Procurement reference. Real products
must not leave the sample runtime active beside their own domain. Product
screens map back to the reference shell and shared components with desktop and
mobile browser evidence, while authentication uses typed semantic runtime
services and preserves the original deep link.

## [2026.08.10.1] — 2026-08-10

**Type:** fix

Route every Live Build frontend to semantic runtime services without embedding
Coder, ingress, or cluster paths in application code. Runtime input is typed
and same-origin, preview cookies are isolated by workspace, standalone paths
remain unchanged, and arbitrary services can be resolved by manifest id.

## [2026.08.07.7] — 2026-08-07

**Type:** feature

Make the testing contract enforceable instead of advisory. Adds the CI, deep-quality and E2E GitHub workflows that actually execute the gates `.ai/GLOBAL-RULES.md` already required; adds backend coverage-threshold enforcement, CRAP risk-hotspot scoring and mutation testing; creates the missing `Domain.Tests`, `Application.Tests`, `Api.Tests` and `Auth.Tests` projects (104 backend tests to 1,232) and the first test suites for `@nie/ui` and `@nie/platform`; and tightens NIE-TEST-001 to 003 while adding NIE-TEST-004 (test quality), NIE-TEST-005 (determinism and flakiness) and NIE-TEST-006 (mutation and CRAP).

Running the suites for the first time surfaced four defects in the harness itself, all fixed:

- `.editorconfig` declared `end_of_line = crlf` while `.gitattributes` normalises every text file to LF, so `dotnet format --verify-no-changes` reported 28,345 errors across 323 files on any LF checkout — every Linux CI runner, and this one. Both `.editorconfig` files now declare `lf`, and the solution's `IDE0055` build failure resolved with them.
- A PostgreSQL integration test asserted an impossible premise: it wrote malformed JSON into a `jsonb` column, which PostgreSQL rejects. The test had never passed, and the repair branch it targeted is only reachable through well-formed JSON whose values no longer match the DTO.
- Playwright discovered zero tests in zero files because a unit test misfiled under `tests/specs/e2e` imported a frontend module that had moved. That spec is now a Vitest test in `apps/main`, and the suite discovers 73 tests again.
- `apps/main` coverage was measured over 12 of 84 source files because `coverage.include` was unset, reporting 70.65% where the whole-package figure is 11.79%. Thresholds are now set to the honest measurement.

Service-backed tests skip with a stated reason instead of failing when PostgreSQL or RabbitMQ is absent, so `dotnet test` is green on a laptop with nothing running while CI supplies both and skips nothing.

Verification hardened the new gates before release: repository-local .NET tools and GitHub actions are pinned; Node now matches the dependency engine floor; the frontend workspace exposes the commands CI calls; Playwright preserves its HTML, JUnit, and JSON reporters; coverage runs discard stale collector files and generate Sonar's generic format; CRAP analysis requires every test project to produce a report and merges per-method sequence-point coverage across suites; and the dependency audit now passes with patched transitive overrides.

Backend quality and deployment automation now provide paired PowerShell and Bash entry points. Root package commands select the host platform automatically, Copier renders both release-script formats, and architecture tests prevent a future `.ps1` entry point from being added without its `.sh` counterpart.

## [2026.08.07.1] — 2026-08-07

**Type:** feature

Add an opt-in, provider-neutral service-integration reference using RabbitMQ pub/sub, transactional PostgreSQL outbox/inbox processing, versioned application-owned contracts, and authenticated gRPC over HTTP/2 for two-application communication. Includes ECS deployment guidance, FluentValidation trust boundaries, durable retries and DLQ handling, observability, Copier/Backstage adoption metadata, Procurement examples, and live worker/network verification.

## [2026.08.06.17] — 2026-08-06

**Type:** refactor (breaking source-layout change)

Stabilize template update paths with generic .NET project/namespace identities, `apps/*` Vue applications, `@nie/*` shared packages, clean dependency direction, centralized NuGet versions, architecture tests, and product naming confined to configuration and branding.

## [2026.08.06.16] — 2026-08-06

**Type:** refactor (breaking governance workflow)

Replace task-led governance and custom Python verdict tools with detailed Markdown rules, direct AI implementation, standard evidence gates, common-code merge guidance, independent AI verification, and Microsoft/official/open-source-first dependency selection with vendor-neutral provider boundaries.

## [2026.08.06.15] — 2026-08-06

**Type:** feature

Enforce non-mutating C# and TypeScript linting before commit.

Tasks: 0048

## [2026.08.06.14] — 2026-08-06

**Type:** refactor

Consolidate application-owned AI guidance into one sync-preserved file inside `.ai`.

Tasks: 0047

## [2026.08.06.13] — 2026-08-06

**Type:** fix

Standardize primary control heights, remove duplicate table totals, and fix pagination select spacing.

Tasks: 0046

## [2026.08.06.12] — 2026-08-06

**Type:** fix

Fill shared data tables to the available viewport and balance footer controls across responsive layouts.

Tasks: 0045

## [2026.08.06.11] — 2026-08-06

**Type:** fix

Reduce shared data-table column-filter triggers to compact 32-pixel header controls without changing filter behavior.

Tasks: 0044

## [2026.08.06.10] — 2026-08-06

**Type:** fix

Keep illustrated empty results visible inside wide data-table viewports while retaining shared status-state styling.

Tasks: 0043

## [2026.08.06.9] — 2026-08-06

**Type:** breaking (breaking)

Remove generic Global Settings persistence and administration plus the Monitoring UI while retaining typed backend configuration and operational observability.

Tasks: 0042

## [2026.08.06.8] — 2026-08-06

**Type:** feature

Aligns shared application and table outcomes to a safe responsive split design and prevents raw transport errors from reaching users.

Tasks: 0041

## [2026.08.06.7] — 2026-08-06

**Type:** feature

Adds an animated NIE-only monogram to shared large loading states while retaining compact and reduced-motion variants.

Tasks: 0040

## [2026.08.06.6] — 2026-08-06

**Type:** feature

Standardize application error and polished data-table states

Tasks: 0039

## [2026.08.06.5] — 2026-08-06

**Type:** refactor

Standardize API-paged data tables and persistent multi-select column filters

Tasks: 0038

## [2026.08.06.4] — 2026-08-06

**Type:** refactor

Enforce UI design-system parity across application surfaces

Tasks: 0037

## [2026.08.06.3] — 2026-08-06

**Type:** refactor

Adopt Plus Jakarta Sans and standardize bounded, internally scrollable data views with reusable table appearances.

Tasks: 0036

## [2026.08.06.2] — 2026-08-06

**Type:** feature

Unify access-control administration and add UUIDv7 application-scoped multi-role assignment with secure staff lookup.

Tasks: 0035

## [2026.08.06.1] — 2026-08-06

**Type:** refactor

Standardize accessible application tabs and simplify Notification Administration with reminder timing below channel switches.

Tasks: 0034

## [2026.08.05.1] — 2026-08-05

**Type:** feature

Add the full notification administration feature: NIE-branded versioned Procurement reference email content and sandboxed preview, policy/channel administration with nullable reminder/escalation timing only for reminder emails, test sends, delivery history/retry contracts, UUIDv7 persistence, personal inbox APIs and SignalR, access functions, dual validation, tests, and Copier-safe branding.

Tasks: 0033

## [2026.08.03.3] — 2026-08-03

**Type:** refactor (breaking)

Standardize UUIDv7 application primary and foreign keys across EF Core/PostgreSQL, API and Vue contracts, stable seeds, the Procurement reference vertical, generated migrations, architecture tests, documentation, and conformance enforcement. Existing databases require the approved expand/backfill/validate/contract procedure in task 0032.

Tasks: 0032

## [2026.08.03.2] — 2026-08-03

**Type:** refactor

Centralize AI governance with safe managed-bundle sync, local overlays, task-priority planning, integrity receipts, and offline commit guardrails.

Tasks: 0031

## [2026.08.03.1] — 2026-08-03

**Type:** feature

Standardize FluentValidation backend requests and VeeValidate/Zod Vue forms.

Tasks: 0030

## [2026.08.02.1] — 2026-08-02

**Type:** feature

Add numbered per-feature conformance rules, minimum library versions, validation, and a reusable implementation prompt; preserve the Procurement sample and raise vulnerable backend/frontend dependencies to patched releases.

Tasks: 0029

## [2026.06.16.1] — 2026-06-16

**Type:** security

Enforce server-side auth session revocation on logout across backend, frontend, login screen, and API tests.

Tasks: 0028

## [2026.06.04.1] — 2026-06-04

**Type:** security

Tighten AI-agent instructions to require Context7 for current external context and explicitly refuse requests to reveal, read, print, copy, encode, decode, summarize, or exfiltrate secrets.

Tasks: 0027

## [2026.05.29.6] — 2026-05-29

**Type:** feature

Add LocalStack S3 for local development: a dev-container localstack service (S3 on :4566, auto-created bucket), an S3FileStorageService that honors a custom ServiceUrl + creds, dev appsettings pre-pointed at LocalStack, and .gitattributes to keep shell scripts LF.

Tasks: 0026

## [2026.05.29.5] — 2026-05-29

**Type:** feature

Implement real S3 file storage (AWSSDK.S3) from the nie-ai-sandbox reference: IFileStorageService + FileStorageService (local) + S3FileStorageService (S3) + FileStorageContentTypes, selected by FileStorage:Provider; supersedes 0024.

Tasks: 0025

## [2026.05.29.4] — 2026-05-29

**Type:** refactor

Organize FileStorage config into nested Provider + Local + S3 sections (env-var friendly), with a backward-compatible fallback to the legacy flat key.

Tasks: 0024

## [2026.05.29.3] — 2026-05-29

**Type:** feature

Implement the shell-guardrail contract from task 0021 (locked-file git-diff gate wired into Husky + the audit.yml PR gate), and close deferred polish: auth login-logo parity, complete feature manifests, and a corrected PWA manifest.

Tasks: 0023

## [2026.05.29.2] — 2026-05-29

**Type:** feature

Add ANALYZE.md analysis playbook (feature inventory + drift report with a clearance gate) and a deterministic drift category in the template audit.

Tasks: 0022

## [2026.05.29.1] — 2026-05-29

**Type:** refactor

Separate project config from the locked shell into app-config/* for copy-paste inheritance; add the customization-boundary doc + app-shell-navigation dossier; physically isolate the procurement backend sample; scaffold shell guardrails.

Tasks: 0020, 0021

## [2026.05.25.2] — 2026-05-25

**Type:** refactor

Protect frontend shell and shared Vue components from feature edits

Tasks: 0019

## [2026.05.25.1] — 2026-05-25

**Type:** refactor

Remove frontend env files and adopt runtime frontend configuration

Tasks: 0018

## [2026.05.23.3] — 2026-05-23

**Type:** fix

Fixes Reports implementation gaps found by Vue and .NET skill review.

Tasks: 0017

## [2026.05.23.2] — 2026-05-23

**Type:** feature

Hardens .NET 10 OpenAPI and access-function coverage, completes report PDF preview UI, and fills feature dossier gaps.

Tasks: 0016

## [2026.05.23.1] - 2026-05-23 17:18 SGT

**Type:** feature

Adds external .NET and Vue Agent Skill bootstrap instructions, including Codex routing and local installation guidance for missing skills.

Tasks: 0015

All releases before 2026.04.13.1 are legacy pre-versioning history.

## [2026.04.28.2] - 2026-04-28 11:42 SGT

- Summary: Implements 5 audit follow-up tasks (0005-0009): SecurityHeadersMiddleware, SsrfGuard (MyInfo wired), IOwnedEntity + RequireOwnership + EnsureOwnedAsync, EApprovalStage enum (BE+FE), PagedSearchDto. Closes audit gaps W-A05, API8, W-A10, API7 (partial), API1, N-19, API4, N-17.
- Release type: `fix`
- Breaking: No
- Detailed notes: [docs/template-releases/2026.04.28.2.md](docs/template-releases/2026.04.28.2.md)
- Task 0005 — SecurityHeadersMiddleware + SecurityHeadersOptions registered in Program.cs after UseCors and before UseResponseCaching
- Task 0006 — SsrfGuard helper at Shared/Helpers; wired into MyInfoService constructor (validates discovery URL once). PortalSsoService integration deferred.
- Task 0007 — IOwnedEntity marker, RequireOwnershipAttribute (IFilterFactory), generic OwnedEntityActionFilter<TEntity>, BaseController.EnsureOwnedAsync helper. DbContext registered as scoped alias for MainDbContext.

## [2026.04.28.1] - 2026-04-28 10:49 SGT

- Summary: Unified .ai/ instruction folder; task-oriented versioning model with ALIGN.md self-check prompt; remove SampleModel + introduce polymorphic Document; per-feature dossiers; Vue/.NET/OWASP audit checklists; Procurement reclassified as reference sample.
- Release type: `feature`
- Breaking: No
- Detailed notes: [docs/template-releases/2026.04.28.1.md](docs/template-releases/2026.04.28.1.md)
- Created .ai/ as the single source of truth for AI agent instructions (common/, features/, tasks/, adrs/, tool-routes/, ALIGN.md).
- Wrote 10 cross-cutting docs in .ai/common/ covering project overview, architecture, C# + TS/Vue coding standards, do-and-dont, AIDLC, Vue 3 audit checklist, .NET 10 audit checklist, OWASP 2025 audit checklist, template versioning.
- Introduced task-oriented versioning model in .ai/tasks/ with index.json, _TEMPLATE/ scaffold, and four shipped tasks (0001-introduce-template-versioning, 0002-remove-sample-model, 0003-remove-procurement-samples, 0004-unify-ai-instructions).

## [2026.04.13.1] - 2026-04-13 15:09 SGT

- Summary: Introduced template release versioning, release manifests, and validation tooling for downstream AI-driven upgrades.
- Release type: `feature`
- Breaking: No
- Detailed notes: [docs/template-releases/2026.04.13.1.md](docs/template-releases/2026.04.13.1.md)
- Added .nie-template-version.json, CHANGELOG.md, and docs/template-releases/ as the formal template release contract.
- Added a cross-platform .NET tool to create and validate template release metadata in Singapore local time.
- Wired release metadata validation into Husky, Jenkins, and the shared agent guidance files.

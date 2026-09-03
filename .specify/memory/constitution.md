# NIE Ignite Engineering Constitution

## Core Principles

### I. Repository Files Are Authoritative

Specifications, plans, contracts, task state, and implementation live in the
project repository. Agents MUST read the relevant files directly and make
targeted edits. Full documents MUST NOT be copied into prompts or reconstructed
from a database projection. Database records and UI models are caches, history,
or execution metadata; they MUST NOT silently overwrite newer workspace files.

### II. Repository Instructions and Template Are the Technical Authority

Agents MUST discover and obey the repository's actual instruction hierarchy:
root instructions first, then nested instructions for the files being changed,
then the repository's documented AI workflow, global/library/feature guidance,
README files, and technical manifests. More specific instructions refine the
broader rules but MUST NOT weaken safety, authorization, audit, or validation
requirements. A referenced instruction file that is absent or contradictory is
a planning gate failure and MUST be reported instead of replaced with an
invented convention.

An application-specific instruction file that still contains explicit template
placeholders is scaffolding, not an architectural decision. Plan MUST record and
resolve it from the approved specification while preserving its documented
schema and all established non-placeholder decisions.

The repository's `.ai/APPLICATION.md` and canonical assurance-profile contract
define whether delivery is POC, Standard, or Enterprise. The active
specification MUST mirror that selection, and Plan MUST resolve any mismatch
before implementation. The selected profile changes test, validation, evidence,
and independent-review depth; it MUST NOT weaken functional, architecture,
authorization, validation, audit, privacy, or secret-handling requirements.

The cloned repository and its pinned NIE template baseline define the current
frameworks, project layout, packages, service topology, runtime configuration,
and validation commands. Plans MUST derive those details by inspecting source
and manifests, record the inspected paths and revision, and reuse established
components before introducing new abstractions. This constitution deliberately
does not duplicate package names, directory layouts, or versions that can drift.

### III. Authorization and Audit Are Non-Negotiable

Navigation visibility and API authorization MUST use the project's access
functions. Role-name checks and controller/action discovery are prohibited.
Business entities requiring traceability MUST use the established timestamp and
audit mechanisms. Security-sensitive operations MUST be explicit, least
privileged, and covered by negative authorization tests.

### IV. Profile-Aware, Incremental Delivery

Every requirement MUST be testable and every task MUST identify concrete file
paths and profile-appropriate validation. Work is organized into coherent,
independently demonstrable user-story slices. POC MUST favor the cheapest
compile/type/smoke proof and MUST NOT generate a separate automated-test task
for every ordinary implementation task. Standard MUST add focused regression
coverage and consolidate broader checks at story checkpoints. Enterprise MUST
use strict test-first evidence, every matching test layer, complete affected
gates, and fresh independent verification for material work. Any repository
risk escalation overrides the faster profile for that slice. An agent MUST stop
and report a failing check that is required by the resolved profile rather than
marking a task complete.

For every user-facing slice, agents MUST define the role/access-aware guided
tour contract and documentation impact before implementation, then update tour,
documentation, source, and regression evidence together. A tour MUST reuse the
repository's shared overlay, use stable semantic targets, remain non-mutating,
and never describe an action unavailable to the current effective access
profile. Tour and documentation work is product implementation, not final
polish that can be postponed until after all phases.

Parallel tasks MUST be emitted as contiguous `[P]` waves with explicit,
repository-relative `writes:` sets. Tasks in one wave MUST have disjoint write
ownership and satisfied earlier dependencies. Ordinary tasks and phase changes
are hard barriers. A sleeping retry MUST NOT consume an execution slot.

### V. Safe Automation

Unattended agents MUST run in a workspace-write sandbox with interactive
approval disabled; unrestricted host access is prohibited. Paths supplied by
users or generated artifacts MUST be confined to the active project and active
feature. Secrets MUST never be written into specifications, logs, commits, or
generated examples. Specifying, planning, refining, and task generation MUST
not require network access.

## Required Feature Artifacts

The active feature directory is recorded in `.specify/feature.json`. A feature
uses native Spec Kit artifacts:

- `spec.md` for overview, user scenarios, requirements, assumptions, and
  measurable success criteria.
- `data-model.md` for the reviewed logical entity, field, relationship,
  constraint, lifecycle, retention, privacy, and audit contract.
- `plan.md`, `research.md`, `quickstart.md`, and `contracts/` for technical
  mappings and implementation design. Planning MUST NOT rewrite the approved
  product specification or screen source.
- `ui/screens.md` for screen states, interaction behavior, accessibility, and
  responsive behavior when UI work is in scope. It also records the guided-tour
  step/target/role contract for each user-visible workflow.
- `integrations.md` for external systems and trust boundaries, exchanged data,
  direction, authorization, failures/retries, and audit expectations. It MUST
  explicitly record when no external integration is required.
- `workflows.md` for important domain or system flows when workflow behavior is
  in scope.
- `tasks.md` for dependency-ordered native checklist tasks.

API contracts SHOULD be machine-readable OpenAPI under `contracts/` rather than
duplicated prose. A missing artifact MAY be omitted only when the plan records
why it is not applicable.

## Development and Release Gates

Before implementation, the specification MUST pass clarification and
requirements checks, the repository and specification assurance values MUST
agree, the plan MUST pass the constitution check, and tasks MUST be consistent
with the final design artifacts and selected assurance depth. Implementation
MUST preserve completed-task history and MUST NOT silently reset task state.

Agents MUST NOT commit, push, merge, publish, deploy, delete a repository, or
rewrite Git history unless that exact action is separately and explicitly
authorized. Generating or editing Spec Kit artifacts never implies release
authorization.

## Governance

This constitution is the fixed baseline for NIE Ignite managed workspaces and
supersedes generated project conventions where they conflict. Amendments require
an approved, versioned NIE preset change and a migration assessment. Workspace
bootstrap MUST report constitution drift and MUST NOT silently overwrite an
authored constitution during a routine status or upgrade operation.

**Version**: 1.2.0 | **Ratified**: 2026-07-23 | **Last Amended**: 2026-08-12

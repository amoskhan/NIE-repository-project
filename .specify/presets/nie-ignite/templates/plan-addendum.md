## NIE Design Artifact Index

<!--
Complete this index after Phase 1. Use N/A only with a concrete reason.
The linked files are feature-local and are read directly by later stages.
-->

| Concern | Artifact | Status / Notes |
|---------|----------|----------------|
| Research and decisions | `research.md` | [COMPLETE / N/A with reason] |
| Data and relationships | `data-model.md` | [COMPLETE / N/A with reason] |
| API contracts | `contracts/` | [COMPLETE / N/A with reason] |
| Integrations and trust boundaries | `integrations.md` | [COMPLETE] |
| Screens and interaction states | `ui/screens.md` | [COMPLETE / N/A with reason] |
| Domain and system flows | `workflows.md` | [COMPLETE / N/A with reason] |
| Verification walkthrough | `quickstart.md` | [COMPLETE] |

**NIE validation commands**: [List the exact repository commands selected after
inspecting the actual solution, package manifests, and test projects.]

## NIE Implementation Design

<!--
This structure is fixed by the NIE preset. Derive the project-specific content
from the repository and approved specification. Do not remove a heading; use
N/A only with a concrete reason.
-->

### Repository instruction manifest

| Instruction / manifest path | Scope | Constraint or decision used |
|-----------------------------|-------|-----------------------------|
| `[exact repository path]` | [repository / backend / frontend / feature] | [specific guidance applied] |

**Inspected source revision**: `[git commit or repository baseline identifier]`

**Technical manifests inspected**: `[solution, project, package, workspace, runtime, and service manifest paths]`

### Delivery assurance plan

**Resolved profile**: `[poc / standard / enterprise]`

**Application profile source**: `[exact .ai/APPLICATION.md field and active application-profile.md path]`

**Risk-escalated slices**: `[N/A with reason, or the exact security/data/integration/infrastructure slices using Enterprise verification]`

**Per-task evidence**: `[checks due after each task under the resolved profile]`

**Story/feature/release gates**: `[consolidated checks and when they run]`

**Deferred evidence or promotion debt**: `[POC/Standard gaps and catch-up trigger, or N/A with reason]`

### Repository baseline

[Existing solution/projects, reusable NIE packages and patterns, and exact
paths inspected before making design decisions.]

### Backend and service boundaries

[ASP.NET Core endpoints/services, DTO and mapping boundaries, dependency
injection, error semantics, and integration points. N/A with reason if there is
no backend impact.]

### Data, migration, and audit design

[Entities, relationships, EF Core configuration/migrations, concurrency,
retention, and audit behavior. N/A with reason if there is no data impact.]

### Access-control design

[Required access functions, API enforcement, navigation visibility, negative
authorization cases, and least-privilege assumptions.]

### Frontend and interaction design

[Vue routes, shared components/composables, state and API services, loading,
empty, error, validation, accessibility, and responsive states. N/A with reason
if there is no UI impact.]

### Contracts and workflows

[OpenAPI artifacts, external contracts, actors, transitions, alternate paths,
failure recovery, and audit points. Link the feature-local artifacts.]

### Verification strategy

[Exact profile-required unit, integration, contract, frontend, and end-to-end
commands selected from the repository. Distinguish per-task checks from
story/feature/release gates and identify any intentionally deferred evidence.]

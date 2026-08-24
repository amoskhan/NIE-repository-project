# Workflow Engine

Canonical NIE rules for the Workflow Engine feature.

Rules version: 2026.08.07.1
Feature key: workflow-engine  
Adoption: **default-on**

## Adoption and navigation

- Menu or entry point: not independently required. Primary > Approvals (conditional)
- Visibility: Use a global Approvals queue only when users act across entity types; otherwise embed timeline/actions in the owning screen.
- If the feature is not adopted, report not-applicable with the product or architecture reason; do not silently omit it.

## Required libraries and minimums

| Library | Package/runtime | Minimum | Ecosystem |
| --- | --- | --- | --- |
| .NET / ASP.NET Core | net | 10.0.0 | runtime |
| Entity Framework Core | Microsoft.EntityFrameworkCore | 10.0.5 | nuget |
| Npgsql EF Core Provider | Npgsql.EntityFrameworkCore.PostgreSQL | 10.0.1 | nuget |
| PostgreSQL | postgres | 17.0.0 | service |
| Mapster | Mapster | 10.0.4 | nuget |
| Vue | vue | 3.5.30 | npm |

Versions are floors, not forced pins for derived applications. Stable upgrades are allowed when compatibility, build, tests, security, and deployment evidence pass. The template itself keeps exact tested pins and lockfiles.

## Rules

| Rule | Severity | Area | Requirement | Required evidence |
| --- | --- | --- | --- | --- |
| NIE-WORKFLOW-001 | error | domain | Define states/transitions as data and execute transitions only through IWorkflowService; controllers and clients never mutate state directly. | architecture-tests |
| NIE-WORKFLOW-002 | error | authorization | Authorize each transition with access functions plus record/department ownership and backend-evaluated available actions. | api-tests |
| NIE-WORKFLOW-003 | error | consistency | Validate source state, transition, required data, concurrency token, and side effects in one transaction. | integration-tests |
| NIE-WORKFLOW-004 | error | audit | Append a workflow state log and audit event for every attempted/successful/failed transition. | tests |
| NIE-WORKFLOW-005 | error | frontend | Render the backend-provided timeline and available transitions; show confirmation, validation, pending, conflict, and failure states. | browser-tests |
| NIE-WORKFLOW-006 | error | verification | Test valid/invalid transitions, role and ownership denial, concurrency, idempotency, audit/log ordering, and UI action visibility. | tests |

## Canonical reference footprint

Use these files as working examples and comparison targets. Procurement is a reference vertical, not a runtime dependency.

- src/backend/Core/Domain/Enums/EWorkflowState.cs
- src/backend/Core/Domain/Models/WorkflowTransition.cs
- src/backend/Core/Domain/Models/WorkflowStateLog.cs
- src/backend/Core/Application/Features/Workflow/IWorkflowService.cs
- src/backend/Core/Application/Features/Workflow/WorkflowService.cs
- src/backend/Hosts/Api/Controllers/WorkflowController.cs
- src/frontend/apps/main/src/types/workflow.ts
- src/frontend/apps/main/src/services/workflowService.ts
- src/frontend/apps/main/src/components/workflow/WorkflowTimeline.vue
- src/frontend/apps/main/src/components/workflow/WorkflowActionBar.vue

## AI implementation and verification

The implementing AI must:

1. Assess every rule above before editing and identify affected backend, frontend, data, security, navigation, and test surfaces.
2. Compare the application implementation with the pinned canonical template reference. Classify each shared file as identical, behind, customized, ahead, conflict, or not applicable.
3. Implement missing behavior directly. Preserve domain behavior and integrate shared updates through extension points rather than overwriting valid customization.
4. Add or update focused unit, architecture, integration, component, API, and browser tests according to the evidence type for each rule.
5. Run the standard repository gates and record exact commands, exit status, and meaningful test counts or artifacts.
6. Report each rule as pass, fail, not-applicable, manual-review, or approved-exception with file/line and test evidence.

A separate AI verifier must review material changes from a fresh context, inspect the diff and evidence, rerun risk-relevant gates, and issue an independent verdict. A claim without traceable evidence is not a pass.

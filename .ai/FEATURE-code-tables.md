# Code Tables

Reference/lookup data (Code entities) with typed ECodeType/ECodeName enums, a CodeService, and frontend code-table option composables.

Rules version: 2026.08.07.1
Feature key: code-tables  
Adoption: **mandatory**

## Adoption and navigation

- Menu or entry point: not independently required. Administration > Reference Data
- Visibility: Add only when administrators can manage reference data; otherwise code tables are embedded selectors.
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
| Zod | zod | 3.25.67 | npm |

Versions are floors, not forced pins for derived applications. Stable upgrades are allowed when compatibility, build, tests, security, and deployment evidence pass. The template itself keeps exact tested pins and lockfiles.

## Rules

| Rule | Severity | Area | Requirement | Required evidence |
| --- | --- | --- | --- | --- |
| NIE-CODE-001 | error | domain | Use ECodeType and ECodeName constants/enums; never scatter raw type/name strings. | analyzers |
| NIE-CODE-002 | error | data | Enforce unique Type+Name, stable semantic names, active filtering, deterministic display order, and deactivation instead of deletion. | database-tests |
| NIE-CODE-003 | error | contracts | Do not persist or expose code-table row IDs as stable business meaning; use the semantic code/name. | contract-tests |
| NIE-CODE-004 | error | frontend | Use shared typed selectors, cache safely, display labels, submit semantic values, and refresh after administration changes. | component-tests |
| NIE-CODE-005 | error | audit | Audit reference-data create/update/activate/deactivate operations. | tests |
| NIE-CODE-006 | error | verification | Test uniqueness, ordering, active filtering, deactivation, seed reconciliation, and frontend validation. | tests |

## Canonical reference footprint

Use these files as working examples and comparison targets. Procurement is a reference vertical, not a runtime dependency.

- src/backend/Core/Domain/Models/Code.cs
- src/backend/Core/Domain/Enums/ECodeType.cs
- src/backend/Core/Domain/Enums/ECodeName.cs
- src/backend/Core/Application/Contracts/CodeDto.cs
- src/backend/Core/Application/Features/Code/ICodeService.cs
- src/backend/Core/Application/Features/Code/CodeService.cs
- src/backend/Hosts/Api/Controllers/CodeController.cs
- src/frontend/apps/main/src/services/codeTableService.ts
- src/frontend/apps/main/src/composables/useCodeTableOptions.ts

## AI implementation and verification

The implementing AI must:

1. Assess every rule above before editing and identify affected backend, frontend, data, security, navigation, and test surfaces.
2. Compare the application implementation with the pinned canonical template reference. Classify each shared file as identical, behind, customized, ahead, conflict, or not applicable.
3. Implement missing behavior directly. Preserve domain behavior and integrate shared updates through extension points rather than overwriting valid customization.
4. Add or update focused unit, architecture, integration, component, API, and browser tests according to the evidence type for each rule.
5. Run the standard repository gates and record exact commands, exit status, and meaningful test counts or artifacts.
6. Report each rule as pass, fail, not-applicable, manual-review, or approved-exception with file/line and test evidence.

A separate AI verifier must review material changes from a fresh context, inspect the diff and evidence, rerun risk-relevant gates, and issue an independent verdict. A claim without traceable evidence is not a pass.

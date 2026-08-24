# Shared Utilities

Cross-cutting helpers: dependency-free `@nie/contracts`, non-visual `@nie/platform`, and the small dependency-free backend `BuildingBlocks` project.

Rules version: 2026.08.07.1
Feature key: shared-utilities  
Adoption: **mandatory**

## Adoption and navigation

- Menu or entry point: not independently required. No dedicated menu is required.
- Visibility: Shared utilities never create menus.
- If the feature is not adopted, report not-applicable with the product or architecture reason; do not silently omit it.

## Required libraries and minimums

| Library | Package/runtime | Minimum | Ecosystem |
| --- | --- | --- | --- |
| Vue | vue | 3.5.30 | npm |
| Vue Router | vue-router | 4.5.1 | npm |
| Vue I18n | vue-i18n | 11.1.3 | npm |
| Zod | zod | 3.25.67 | npm |
| Axios | axios | 1.18.0 | npm |
| js-cookie | js-cookie | 3.0.7 | npm |

Versions are floors, not forced pins for derived applications. Stable upgrades are allowed when compatibility, build, tests, security, and deployment evidence pass. The template itself keeps exact tested pins and lockfiles.

## Rules

| Rule | Severity | Area | Requirement | Required evidence |
| --- | --- | --- | --- | --- |
| NIE-SHARED-001 | error | scope | Keep @nie/contracts dependency-free; keep @nie/platform non-visual, domain-neutral, and usable by both frontend applications; keep backend BuildingBlocks dependency-free and domain-neutral. | architecture-tests |
| NIE-SHARED-002 | error | exports | Expose stable typed package or subpath/barrel exports for contracts, config, composables, utilities, and i18n without deep private imports. | type-check |
| NIE-SHARED-003 | error | configuration | Centralize non-secret runtime frontend constants and validate external runtime input with typed schemas. | tests |
| NIE-SHARED-004 | error | boundaries | Do not add .vue files, global styles, domain entities, product menus, or feature workflows to the shared package. | architecture-tests |
| NIE-SHARED-005 | error | api | Use composables only for reusable stateful/lifecycle behavior; keep pure transformations as utilities. | review |
| NIE-SHARED-006 | error | verification | Type-check shared in isolation and test it through both main and auth consumers. | build |
| NIE-SHARED-007 | error | identity | Retain stable `@nie/contracts`, `@nie/platform`, `@nie/ui`, and backend BuildingBlocks identities. Do not prefix shared packages, folders, assemblies, namespaces, cookies, or runtime globals with the product name; supply product variation through configuration and supported extension points. | architecture-and-template-update-tests |

## Canonical reference footprint

Use these files as working examples and comparison targets. Procurement is a reference vertical, not a runtime dependency.

- src/frontend/packages/platform/package.json
- src/frontend/packages/contracts/package.json
- src/frontend/packages/contracts/README.md
- src/frontend/packages/contracts/src/index.ts
- src/frontend/packages/contracts/src/api.ts
- src/frontend/packages/contracts/src/role.ts
- src/frontend/packages/contracts/src/user.ts
- src/frontend/packages/platform/src/index.ts
- src/frontend/packages/platform/src/composables/index.ts
- src/frontend/packages/platform/src/composables/useApi.ts
- src/frontend/packages/platform/src/composables/useAuth.ts
- src/frontend/apps/main/src/composables/useAuth.ts
- src/frontend/packages/platform/src/utils/index.ts
- src/frontend/packages/platform/src/utils/format.ts
- src/frontend/packages/platform/src/utils/validationProblem.ts
- src/backend/BuildingBlocks/BuildingBlocks/BuildingBlocks.csproj
- src/backend/Tests/Architecture.Tests/LayerDependencyTests.cs

## AI implementation and verification

The implementing AI must:

1. Assess every rule above before editing and identify affected backend, frontend, data, security, navigation, and test surfaces.
2. Compare the application implementation with the pinned canonical template reference. Classify each shared file as identical, behind, customized, ahead, conflict, or not applicable.
3. Implement missing behavior directly. Preserve domain behavior and integrate shared updates through extension points rather than overwriting valid customization.
4. Add or update focused unit, architecture, integration, component, API, and browser tests according to the evidence type for each rule.
5. Run the standard repository gates and record exact commands, exit status, and meaningful test counts or artifacts.
6. Report each rule as pass, fail, not-applicable, manual-review, or approved-exception with file/line and test evidence.

A separate AI verifier must review material changes from a fresh context, inspect the diff and evidence, rerun risk-relevant gates, and issue an independent verdict. A claim without traceable evidence is not a pass.

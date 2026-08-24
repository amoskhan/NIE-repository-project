# App Shell & Navigation

Canonical NIE rules for the App Shell & Navigation feature.

Rules version: 2026.08.10.1
Feature key: app-shell-navigation  
Adoption: **mandatory**

## Adoption and navigation

- Menu or entry point: required at **Shell root**.
- Visibility: Primary, Administration, and Profile groups are filtered by route/access metadata.
- If the feature is not adopted, report not-applicable with the product or architecture reason; do not silently omit it.

## Required libraries and minimums

| Library | Package/runtime | Minimum | Ecosystem |
| --- | --- | --- | --- |
| Node.js | node | 22.12.0 | runtime |
| pnpm | pnpm | 10.33.0 | runtime |
| Vue | vue | 3.5.30 | npm |
| Vue Router | vue-router | 4.5.1 | npm |
| Heroicons for Vue | @heroicons/vue | 2.2.0 | npm |

Versions are floors, not forced pins for derived applications. Stable upgrades are allowed when compatibility, build, tests, security, and deployment evidence pass. The template itself keeps exact tested pins and lockfiles.

## Rules

| Rule | Severity | Area | Requirement | Required evidence |
| --- | --- | --- | --- | --- |
| NIE-SHELL-001 | error | structure | Keep StaffLayout, sidebar mechanics, topbar, router guard, and permission resolver template-owned. | architecture review and diff |
| NIE-SHELL-002 | error | configuration | Put project navigation, routes, access functions, roles, and branding only in app-config. | architecture review and diff |
| NIE-SHELL-003 | error | authorization | Every rendered route component must declare permission or permissions metadata; every menu item must use the same access bundle. | component, router, and access tests |
| NIE-SHELL-004 | error | frontend | Lazy-load route pages, preserve deep links and path prefixes, and render a deliberate not-found/forbidden state. | browser-tests |
| NIE-SHELL-005 | error | accessibility | Support keyboard navigation, focus restoration, responsive collapse, skip navigation, and accessible menu names. | browser-tests |
| NIE-SHELL-006 | error | verification | Test menu visibility and direct URL authorization for representative roles. | browser-tests |
| NIE-SHELL-007 | error | application-states | Render shared borderless responsive split-layout result states for unauthorized, forbidden, not-found, timeout, throttling, server, gateway, and unavailable outcomes; use semantic visual treatments, explicit recovery actions, and safe user-facing copy; unmatched routes and denied navigation must never silently redirect to a successful page. | component-router-and-browser-tests |
| NIE-SHELL-008 | error | product-derivation | A derived application must expose its real product title, routes, navigation, roles, and branding through app-config. It must not display `NIE Template` or active Procurement navigation unless an explicit owned retention decision applies. Preserve the generic source identities required by NIE-STRUCT-ID-001. | app-config review, architecture-tests, and browser-tests |

## Canonical reference footprint

Use these files as working examples and comparison targets. Procurement is a reference vertical, not a runtime dependency.

- src/frontend/apps/main/src/staff/layouts/StaffLayout.vue
- src/frontend/apps/main/src/composables/useSidebar.ts
- src/frontend/apps/main/src/composables/usePermissions.ts
- src/frontend/apps/main/src/composables/navTypes.ts
- src/frontend/apps/main/src/router/index.ts
- src/frontend/apps/main/src/staff/pages/status/ApplicationStatusPage.vue
- src/frontend/apps/main/src/App.vue
- src/frontend/apps/auth/src/App.vue
- src/frontend/apps/auth/src/router/index.ts
- src/frontend/apps/auth/src/components/AuthStatusPage.vue
- src/frontend/apps/main/src/constants/permissions.ts
- src/frontend/apps/main/src/app-config/navigation.ts
- src/frontend/apps/main/src/app-config/routes.ts
- src/frontend/apps/main/src/app-config/accessFunctions.ts
- src/frontend/apps/main/src/app-config/branding.ts

## AI implementation and verification

The implementing AI must:

1. Assess every rule above before editing and identify affected backend, frontend, data, security, navigation, and test surfaces.
2. Compare the application implementation with the pinned canonical template reference. Classify each shared file as identical, behind, customized, ahead, conflict, or not applicable.
3. Implement missing behavior directly. Preserve domain behavior and integrate shared updates through extension points rather than overwriting valid customization.
4. Add or update focused unit, architecture, integration, component, API, and browser tests according to the evidence type for each rule.
5. Run the standard repository gates and record exact commands, exit status, and meaningful test counts or artifacts.
6. Report each rule as pass, fail, not-applicable, manual-review, or approved-exception with file/line and test evidence.

A separate AI verifier must review material changes from a fresh context, inspect the diff and evidence, rerun risk-relevant gates, and issue an independent verdict. A claim without traceable evidence is not a pass.

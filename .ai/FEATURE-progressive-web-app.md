# Progressive Web App

Canonical NIE rules for the Progressive Web App feature.

Rules version: 2026.08.07.1
Feature key: progressive-web-app  
Adoption: **mandatory**

## Adoption and navigation

- Menu or entry point: not independently required. No dedicated menu is required.
- Visibility: Installation/update/offline affordances belong in the shell, not a permanent menu item.
- If the feature is not adopted, report not-applicable with the product or architecture reason; do not silently omit it.

## Required libraries and minimums

| Library | Package/runtime | Minimum | Ecosystem |
| --- | --- | --- | --- |
| Vue | vue | 3.5.30 | npm |
| Vite | vite | 8.0.16 | npm |

Versions are floors, not forced pins for derived applications. Stable upgrades are allowed when compatibility, build, tests, security, and deployment evidence pass. The template itself keeps exact tested pins and lockfiles.

## Rules

| Rule | Severity | Area | Requirement | Required evidence |
| --- | --- | --- | --- | --- |
| NIE-PWA-001 | error | service-worker | Register exactly one owned service worker with versioned caches and a deliberate update/activation flow. | browser-tests |
| NIE-PWA-002 | error | security | Never cache authenticated API responses, tokens, credentials, or personalized HTML in shared caches. | service-worker-tests |
| NIE-PWA-003 | error | offline | Cache only approved static assets and provide a safe offline fallback without pretending writes succeeded. | browser-tests |
| NIE-PWA-004 | error | manifest | Keep manifest name, icons, scope, start URL, theme, and path-prefix behavior consistent with runtime branding/configuration. | manifest-tests |
| NIE-PWA-005 | error | experience | Notify users when an update is ready and offer a controlled reload; do not trap users on stale assets. | browser-tests |
| NIE-PWA-006 | error | verification | Test first install, update, offline navigation, cache cleanup, path prefix, and authenticated-response exclusion. | browser-tests |

## Canonical reference footprint

Use these files as working examples and comparison targets. Procurement is a reference vertical, not a runtime dependency.

- src/frontend/apps/main/public/manifest.json
- src/frontend/apps/main/public/sw.js
- src/frontend/apps/main/vite.config.ts

## AI implementation and verification

The implementing AI must:

1. Assess every rule above before editing and identify affected backend, frontend, data, security, navigation, and test surfaces.
2. Compare the application implementation with the pinned canonical template reference. Classify each shared file as identical, behind, customized, ahead, conflict, or not applicable.
3. Implement missing behavior directly. Preserve domain behavior and integrate shared updates through extension points rather than overwriting valid customization.
4. Add or update focused unit, architecture, integration, component, API, and browser tests according to the evidence type for each rule.
5. Run the standard repository gates and record exact commands, exit status, and meaningful test counts or artifacts.
6. Report each rule as pass, fail, not-applicable, manual-review, or approved-exception with file/line and test evidence.

A separate AI verifier must review material changes from a fresh context, inspect the diff and evidence, rerun risk-relevant gates, and issue an independent verdict. A claim without traceable evidence is not a pass.

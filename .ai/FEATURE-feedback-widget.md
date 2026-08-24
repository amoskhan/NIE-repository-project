# Feedback Widget

A floating thumbs-up/down feedback widget that posts to a feedback endpoint, built on the reusable NieAppFeedbackHub UI component.

Rules version: 2026.08.07.1
Feature key: feedback-widget  
Adoption: **mandatory**

## Adoption and navigation

- Menu or entry point: not independently required. Global authenticated shell widget
- Visibility: Mount once in the main application; never mount on the Auth application.
- If the feature is not adopted, report not-applicable with the product or architecture reason; do not silently omit it.

## Required libraries and minimums

| Library | Package/runtime | Minimum | Ecosystem |
| --- | --- | --- | --- |
| Vue | vue | 3.5.30 | npm |
| Axios | axios | 1.18.0 | npm |
| Sentry for Vue | @sentry/vue | 9.47.1 | npm |

Versions are floors, not forced pins for derived applications. Stable upgrades are allowed when compatibility, build, tests, security, and deployment evidence pass. The template itself keeps exact tested pins and lockfiles.

## Rules

| Rule | Severity | Area | Requirement | Required evidence |
| --- | --- | --- | --- | --- |
| NIE-FEEDBACK-001 | error | frontend | Mount one accessible feedback widget in the authenticated shell and identify context with applicationSlug.routeName/functionId. | browser-tests |
| NIE-FEEDBACK-002 | error | data | Use the standard rating/comment/context contract and validate it on both frontend and backend. | contract-tests |
| NIE-FEEDBACK-003 | error | experience | Provide sending, success, failure, retry, dismissal, and a 24-hour post-submit hide behavior. | component-tests |
| NIE-FEEDBACK-004 | error | privacy | Do not capture page content, secrets, or personal data automatically; include only approved context metadata. | security-review |
| NIE-FEEDBACK-005 | error | configuration | Allow a non-secret runtime disable flag but keep the feature enabled by default for maintained frontend applications. | runtime-config-tests |
| NIE-FEEDBACK-006 | error | verification | Test context ID, validation, retry, dismissal, time-based hide, and keyboard/focus behavior. | tests |

## Canonical reference footprint

Use these files as working examples and comparison targets. Procurement is a reference vertical, not a runtime dependency.

- src/frontend/apps/main/src/components/feedback/FloatingFeedbackButton.vue
- src/frontend/apps/main/src/services/feedbackService.ts
- src/frontend/packages/ui/src/components/composite/app-feedback/NieAppFeedbackHub.vue
- src/frontend/packages/ui/src/components/composite/app-feedback/index.ts

## AI implementation and verification

The implementing AI must:

1. Assess every rule above before editing and identify affected backend, frontend, data, security, navigation, and test surfaces.
2. Compare the application implementation with the pinned canonical template reference. Classify each shared file as identical, behind, customized, ahead, conflict, or not applicable.
3. Implement missing behavior directly. Preserve domain behavior and integrate shared updates through extension points rather than overwriting valid customization.
4. Add or update focused unit, architecture, integration, component, API, and browser tests according to the evidence type for each rule.
5. Run the standard repository gates and record exact commands, exit status, and meaningful test counts or artifacts.
6. Report each rule as pass, fail, not-applicable, manual-review, or approved-exception with file/line and test evidence.

A separate AI verifier must review material changes from a fresh context, inspect the diff and evidence, rerun risk-relevant gates, and issue an independent verdict. A claim without traceable evidence is not a pass.

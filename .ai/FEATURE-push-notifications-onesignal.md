# Push Notifications (OneSignal)

Push notification delivery via OneSignal, with a provider abstraction and user notification preferences.

Rules version: 2026.08.07.1
Feature key: push-notifications-onesignal  
Adoption: **mandatory**

## Adoption and navigation

- Menu or entry point: required at **Administration > Notifications**.
- Visibility: User subscription controls may also appear in Profile; broadcast controls require a dedicated access function.
- If the feature is not adopted, report not-applicable with the product or architecture reason; do not silently omit it.

## Required libraries and minimums

| Library | Package/runtime | Minimum | Ecosystem |
| --- | --- | --- | --- |
| .NET / ASP.NET Core | net | 10.0.0 | runtime |
| OneSignal Web SDK | OneSignalSDK.page.js | 16.0.0 | cdn |
| Vue | vue | 3.5.30 | npm |
| Axios | axios | 1.18.0 | npm |

Versions are floors, not forced pins for derived applications. Stable upgrades are allowed when compatibility, build, tests, security, and deployment evidence pass. The template itself keeps exact tested pins and lockfiles.

## Rules

| Rule | Severity | Area | Requirement | Required evidence |
| --- | --- | --- | --- | --- |
| NIE-PUSH-001 | error | architecture | Use a PushNotificationProvider/IPushNotificationService abstraction; OneSignal is the default implementation, not a domain dependency. | architecture-tests |
| NIE-PUSH-002 | error | frontend | Initialize the OneSignal v16 SDK once from runtime public configuration and expose permission/subscription/error states. | browser-tests |
| NIE-PUSH-003 | error | identity | Map the external user after login and remove the mapping on every logout/session-expiry path. | browser-tests |
| NIE-PUSH-004 | error | reliability | Treat push as best-effort and never make a critical workflow depend on delivery. | review |
| NIE-PUSH-005 | error | security | Keep provider secrets backend-only, minimize notification personal data, and require confirmation/audit for broadcasts. | security-tests |
| NIE-PUSH-006 | error | verification | Test unsupported/denied permission, opt-in/out, identity map/unmap, disabled config, provider failure, and broadcast authorization. | tests |

## Canonical reference footprint

Use these files as working examples and comparison targets. Procurement is a reference vertical, not a runtime dependency.

- src/backend/Core/Application/Features/PushNotification/IPushNotificationService.cs
- src/backend/Infrastructure/Persistence/Providers/PushNotification/OneSignalPushNotificationService.cs
- src/backend/Infrastructure/Persistence/Options/OneSignalSettings.cs
- src/frontend/apps/main/src/services/oneSignalService.ts

## AI implementation and verification

The implementing AI must:

1. Assess every rule above before editing and identify affected backend, frontend, data, security, navigation, and test surfaces.
2. Compare the application implementation with the pinned canonical template reference. Classify each shared file as identical, behind, customized, ahead, conflict, or not applicable.
3. Implement missing behavior directly. Preserve domain behavior and integrate shared updates through extension points rather than overwriting valid customization.
4. Add or update focused unit, architecture, integration, component, API, and browser tests according to the evidence type for each rule.
5. Run the standard repository gates and record exact commands, exit status, and meaningful test counts or artifacts.
6. Report each rule as pass, fail, not-applicable, manual-review, or approved-exception with file/line and test evidence.

A separate AI verifier must review material changes from a fresh context, inspect the diff and evidence, rerun risk-relevant gates, and issue an independent verdict. A claim without traceable evidence is not a pass.

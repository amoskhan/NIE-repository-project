# Singpass MyInfo

Canonical NIE rules for the Singpass MyInfo feature.

Rules version: 2026.08.07.1
Feature key: singpass-myinfo  
Adoption: **opt-in**

## Adoption and navigation

- Menu or entry point: required at **Profile > MyInfo**.
- Visibility: Use Primary > MyInfo only when it is a core daily workflow; always guard route and API.
- If the feature is not adopted, report not-applicable with the product or architecture reason; do not silently omit it.

## Required libraries and minimums

| Library | Package/runtime | Minimum | Ecosystem |
| --- | --- | --- | --- |
| .NET / ASP.NET Core | net | 10.0.0 | runtime |
| jose-jwt | jose-jwt | 5.2.0 | nuget |
| Microsoft IdentityModel Tokens | Microsoft.IdentityModel.Tokens | 8.17.0 | nuget |
| Microsoft Redis distributed cache | Microsoft.Extensions.Caching.StackExchangeRedis | 10.0.5 | nuget |
| Valkey | valkey | 8.0.0 | service |
| Vue | vue | 3.5.30 | npm |
| Axios | axios | 1.18.0 | npm |

Versions are floors, not forced pins for derived applications. Stable upgrades are allowed when compatibility, build, tests, security, and deployment evidence pass. The template itself keeps exact tested pins and lockfiles.

## Rules

| Rule | Severity | Area | Requirement | Required evidence |
| --- | --- | --- | --- | --- |
| NIE-MYINFO-001 | error | protocol | Keep PAR, DPoP, JWE/JWS, token exchange, and person-data calls server-side behind IMyInfoService. | integration-tests |
| NIE-MYINFO-002 | error | state | Use a random single-use state with nonce, verifier, private key, and ten-minute expiry in server-side distributed cache; remove it before completing callback processing. | api-tests |
| NIE-MYINFO-003 | error | security | Use SSRF allowlists, validate issuer/audience/signatures/times, minimize scopes, and keep keys/provider credentials outside source. | security-tests |
| NIE-MYINFO-004 | error | privacy | Do not persist person data without explicit product/legal approval and a retention purpose; never log tokens, keys, or returned personal payloads. | privacy-review |
| NIE-MYINFO-005 | error | frontend | Show configured/unconfigured, redirecting, callback, expired/reused state, success, and safe failure states without exposing protocol material. | browser-tests |
| NIE-MYINFO-006 | error | verification | Test state replay/expiry, provider errors, invalid tokens, SSRF denial, access denial, data minimization, and disabled configuration. | tests |

## Canonical reference footprint

Use these files as working examples and comparison targets. Procurement is a reference vertical, not a runtime dependency.

- src/backend/Core/Application/Features/MyInfo/IMyInfoService.cs
- src/backend/Infrastructure/Persistence/Providers/MyInfo/MyInfoService.cs
- src/backend/Hosts/Api/Controllers/MyInfoController.cs
- src/frontend/apps/main/src/services/myInfoService.ts
- src/frontend/apps/main/src/staff/pages/staff/MyInfoPage.vue
- src/frontend/apps/main/src/staff/pages/staff/MyInfoCallback.vue

## AI implementation and verification

The implementing AI must:

1. Assess every rule above before editing and identify affected backend, frontend, data, security, navigation, and test surfaces.
2. Compare the application implementation with the pinned canonical template reference. Classify each shared file as identical, behind, customized, ahead, conflict, or not applicable.
3. Implement missing behavior directly. Preserve domain behavior and integrate shared updates through extension points rather than overwriting valid customization.
4. Add or update focused unit, architecture, integration, component, API, and browser tests according to the evidence type for each rule.
5. Run the standard repository gates and record exact commands, exit status, and meaningful test counts or artifacts.
6. Report each rule as pass, fail, not-applicable, manual-review, or approved-exception with file/line and test evidence.

A separate AI verifier must review material changes from a fresh context, inspect the diff and evidence, rerun risk-relevant gates, and issue an independent verdict. A claim without traceable evidence is not a pass.

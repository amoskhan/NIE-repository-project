# Authentication

Auth API (login, session, refresh, Portal SSO) backed by Valkey sessions, and the Main API SessionValidationMiddleware that gates every request.

Rules version: 2026.08.10.1
Feature key: authentication  
Adoption: **mandatory**

## Adoption and navigation

- Menu or entry point: not independently required. Profile > Logout
- Visibility: Login is a separate Auth application; logout is always available to authenticated users.
- If the feature is not adopted, report not-applicable with the product or architecture reason; do not silently omit it.

## Required libraries and minimums

| Library | Package/runtime | Minimum | Ecosystem |
| --- | --- | --- | --- |
| .NET / ASP.NET Core | net | 10.0.0 | runtime |
| Microsoft.AspNetCore.OpenApi | Microsoft.AspNetCore.OpenApi | 10.0.10 | nuget |
| Microsoft.OpenApi | Microsoft.OpenApi | 2.7.5 | nuget |
| ASP.NET Core OpenID Connect | Microsoft.AspNetCore.Authentication.OpenIdConnect | 10.0.5 | nuget |
| ASP.NET Core WS-Federation | Microsoft.AspNetCore.Authentication.WsFederation | 10.0.5 | nuget |
| Microsoft Redis distributed cache | Microsoft.Extensions.Caching.StackExchangeRedis | 10.0.5 | nuget |
| Valkey | valkey | 8.0.0 | service |
| jose-jwt | jose-jwt | 5.2.0 | nuget |
| Vue | vue | 3.5.30 | npm |
| Vue Router | vue-router | 4.5.1 | npm |
| Axios | axios | 1.18.0 | npm |
| js-cookie | js-cookie | 3.0.7 | npm |

Versions are floors, not forced pins for derived applications. Stable upgrades are allowed when compatibility, build, tests, security, and deployment evidence pass. The template itself keeps exact tested pins and lockfiles.

## Rules

| Rule | Severity | Area | Requirement | Required evidence |
| --- | --- | --- | --- | --- |
| NIE-AUTHN-001 | error | architecture | Keep login/protocol handling in the Auth API/application and validate the resulting server-side session in the Main API. | architecture-tests |
| NIE-AUTHN-002 | error | session | Store opaque revocable sessions in Valkey with expiry; do not make browser storage or a client JWT the source of roles/access functions. | tests |
| NIE-AUTHN-003 | error | logout | Every logout path revokes the server-side session before clearing local browser state; old tokens must be rejected. | api-tests |
| NIE-AUTHN-004 | error | security | Mark anonymous endpoints explicitly, restrict test-session endpoints to Development, use secure cookie settings, and validate redirect targets. | security-tests |
| NIE-AUTHN-005 | error | audit | Audit login success/failure, logout, refresh, expiry, denial, and session revocation without logging tokens or credentials. | tests |
| NIE-AUTHN-006 | error | frontend | Centralize auth state in the auth service/composable, handle expiry consistently, and prevent protected content flashing before session validation. | browser-tests |
| NIE-AUTHN-007 | error | runtime-routing | Resolve Auth/Main frontends and APIs from the typed semantic runtime service contract after strict same-origin validation, with standalone fallbacks only. An unauthenticated Main-app entry must navigate to the Auth frontend and return to the original validated Main deep link after login; it must never expose a raw API `Unauthorized` response or construct an infrastructure path. | configuration-unit, router, and browser-tests |
| NIE-AUTHN-008 | error | cookie-isolation | Scope browser session cookies with the runtime-provided workspace name and common application path when present; remove cookies with the same attributes and prevent one preview workspace from reading or overwriting another workspace's session. | security-unit-and-browser-tests |

## Canonical reference footprint

Use these files as working examples and comparison targets. Procurement is a reference vertical, not a runtime dependency.

- src/backend/Hosts/Auth/Program.cs
- src/backend/Hosts/Auth/Controllers/AuthController.cs
- src/backend/Hosts/Auth/Services/AuthSessionService.cs
- src/backend/Hosts/Auth/Services/PortalSsoService.cs
- src/backend/Hosts/Auth/appsettings.json
- src/backend/Hosts/Auth/Models/AuthSessionDto.cs
- src/backend/Hosts/Auth/Models/LoginRequest.cs
- src/backend/Hosts/Auth/Models/LoginResponse.cs
- src/backend/Hosts/Auth/Models/IssuedLoginResponse.cs
- src/backend/Hosts/Auth/Models/RefreshResponse.cs
- src/backend/Hosts/Auth/Models/CreateTestSessionRequest.cs
- src/backend/Hosts/Auth/Models/CreateTestSessionResponse.cs

## AI implementation and verification

The implementing AI must:

1. Assess every rule above before editing and identify affected backend, frontend, data, security, navigation, and test surfaces.
2. Compare the application implementation with the pinned canonical template reference. Classify each shared file as identical, behind, customized, ahead, conflict, or not applicable.
3. Implement missing behavior directly. Preserve domain behavior and integrate shared updates through extension points rather than overwriting valid customization.
4. Add or update focused unit, architecture, integration, component, API, and browser tests according to the evidence type for each rule.
5. Run the standard repository gates and record exact commands, exit status, and meaningful test counts or artifacts.
6. Report each rule as pass, fail, not-applicable, manual-review, or approved-exception with file/line and test evidence.

A separate AI verifier must review material changes from a fresh context, inspect the diff and evidence, rerun risk-relevant gates, and issue an independent verdict. A claim without traceable evidence is not a pass.

# Authorization (Access Functions)

Access-function authorization with a unified three-tab administration surface, multi-role and application-scoped assignments, staff lookup, UUIDv7 persistence, and backend/frontend enforcement.

Rules version: 2026.08.07.1
Feature key: authorization-access-functions  
Adoption: **mandatory**

## Adoption and navigation

- Menu or entry point: required at **Administration > Access Control**.
- Visibility: A single access-control item is visible only with the shared access-control permission bundle; Users, Roles, and Access Functions are tabs inside it.
- If the feature is not adopted, report not-applicable with the product or architecture reason; do not silently omit it.

## Required libraries and minimums

| Library | Package/runtime | Minimum | Ecosystem |
| --- | --- | --- | --- |
| .NET / ASP.NET Core | net | 10.0.0 | runtime |
| Entity Framework Core | Microsoft.EntityFrameworkCore | 10.0.5 | nuget |
| Npgsql EF Core Provider | Npgsql.EntityFrameworkCore.PostgreSQL | 10.0.1 | nuget |
| PostgreSQL | postgres | 17.0.0 | service |
| Microsoft Redis distributed cache | Microsoft.Extensions.Caching.StackExchangeRedis | 10.0.5 | nuget |
| Valkey | valkey | 8.0.0 | service |
| FluentValidation | FluentValidation | 12.1.1 | nuget |
| FluentValidation dependency injection extensions | FluentValidation.DependencyInjectionExtensions | 12.1.1 | nuget |
| Vue | vue | 3.5.30 | npm |
| Vue Router | vue-router | 4.5.1 | npm |
| Axios | axios | 1.18.0 | npm |
| VeeValidate | vee-validate | 4.15.1 | npm |
| VeeValidate Zod integration | @vee-validate/zod | 4.15.1 | npm |
| Zod | zod | 3.25.67 | npm |

Versions are floors, not forced pins for derived applications. Stable upgrades are allowed when compatibility, build, tests, security, and deployment evidence pass. The template itself keeps exact tested pins and lockfiles.

## Rules

| Rule | Severity | Area | Requirement | Required evidence |
| --- | --- | --- | --- | --- |
| NIE-AUTHZ-001 | error | backend | Every controller action must carry a method-level RequireAccessFunction with one or more catalog constants, or an explicit approved public-endpoint marker. | architecture and API authorization tests |
| NIE-AUTHZ-002 | error | frontend | Every screen route must declare permission metadata and every matching menu item must use the same permission/access bundle. | route, component, and browser access tests |
| NIE-AUTHZ-003 | error | catalog | Keep backend constants, seed definitions, role grants, frontend constants, route guards, and menu guards synchronized. | contract-tests |
| NIE-AUTHZ-004 | error | roles | Every application must define at least two stable system roles including an administrator and at least one business/non-administrator role. | seed and role-contract tests |
| NIE-AUTHZ-005 | error | roles | Grant System Administrator every active access function; never infer API authorization from frontend role labels or menu visibility. | seed-tests |
| NIE-AUTHZ-006 | error | security | Enforce record/department ownership (BOLA) after function authorization whenever access depends on the target record. | api-tests |
| NIE-AUTHZ-007 | error | cache | Invalidate affected users' cached access functions after role/access changes and keep the cache short-lived. | integration-tests |
| NIE-AUTHZ-008 | error | audit | Audit role creation/update/deletion, assignments, access-function changes, and denied access. | tests |
| NIE-AUTHZ-009 | error | safety | A parameterless RequireAccessFunction is invalid and must fail closed; class-level attributes never replace action classification. | unit-tests |
| NIE-AUTHZ-010 | error | verification | Test each representative role through menu visibility, direct route navigation, API allow/deny, cache invalidation, and ownership boundaries. | tests |
| NIE-AUTHZ-011 | error | navigation | Expose one Administration > Access Control sidebar item with Users, Roles, and Access Functions tabs; do not retain separate Users/Roles or Access Functions menu destinations, but preserve old URLs as authorized redirects when applications may have bookmarks. | component-and-browser-tests |
| NIE-AUTHZ-012 | error | roles-ui | Render roles as an accessible left-side vertical selector; edit role name and description in a validated modal; group access functions by module and show a multi-select-safe round control, an API or Screen chip, and an accessible information disclosure for every function. | component-and-browser-tests |
| NIE-AUTHZ-013 | error | users-ui | Show canonical staff identity and available contact, department, designation, global-role, application-access, and effective-function details; support assigning multiple roles in one validated operation without creating duplicate assignments. | component-and-api-tests |
| NIE-AUTHZ-014 | error | application-scope | Represent applications and application-role assignments with UUIDv7 keys and a unique application/user/role boundary; application-scoped roles never become global authorization and every application endpoint must enforce both its access function and explicit application or record boundary. | architecture-migration-and-api-tests |
| NIE-AUTHZ-015 | error | validation | Validate access-control request DTOs with FluentValidation and access-control forms with VeeValidate plus Zod; bound and de-duplicate role/application batches and return the shared RFC 7807 validation contract. | validation-and-component-tests |
| NIE-AUTHZ-016 | error | directory-security | Resolve staff only through a server-side directory service using IHttpClientFactory, validated options, SSRF destination allowlisting, cancellation, and bounded timeouts; persist only approved profile fields and never expose provider credentials to the browser. | security-and-integration-tests |
| NIE-AUTHZ-017 | error | audit | Audit directory lookup outcomes, global and application role grants/removals, role metadata changes, and role access-function changes without logging secrets or unnecessary personal data. | tests |
| NIE-AUTHZ-018 | error | permissions-ui | Hide or disable role and assignment mutation controls unless the current user holds the exact matching manage access function; frontend visibility is usability only and never replaces backend authorization. | component-and-api-tests |

## Canonical reference footprint

Use these files as working examples and comparison targets. Procurement is a reference vertical, not a runtime dependency.

- src/backend/Core/Domain/Models/AccessFunction.cs
- src/backend/Core/Domain/Models/Role.cs
- src/backend/Core/Domain/Models/RoleAccessFunction.cs
- src/backend/Core/Domain/Models/UserRole.cs
- src/backend/Core/Domain/Models/Application.cs
- src/backend/Core/Domain/Models/ApplicationAccess.cs
- src/backend/Core/Domain/Models/UserContactProfile.cs
- src/backend/Core/Domain/Enums/EAccessFunctionType.cs
- src/backend/Core/Domain/Enums/ERole.cs
- src/backend/Core/Application/Security/AccessFunctionCatalog.cs
- src/backend/Hosts/Api/Authorization/RequireAccessFunctionAttribute.cs
- src/backend/Hosts/Api/Middleware/UserRolesMiddleware.cs
- src/backend/Hosts/Api/Controllers/AccessControlController.cs
- src/backend/Core/Application/Features/AccessFunction/IAccessFunctionService.cs
- src/backend/Core/Application/Features/AccessFunction/AccessFunctionService.cs
- src/backend/Core/Application/Features/ApplicationAccess/ApplicationAccessService.cs
- src/backend/Infrastructure/Persistence/Providers/StaffDirectory/StaffDirectoryService.cs
- src/backend/Core/Application/Security/SystemApplicationIds.cs
- src/frontend/apps/main/src/staff/pages/admin/Users.vue
- src/frontend/apps/main/src/components/admin/access-control
- src/frontend/apps/main/src/services/roleService.ts
- src/frontend/apps/main/src/app-config/accessFunctions.ts
- src/frontend/apps/main/src/app-config/navigation.ts
- src/frontend/apps/main/src/app-config/routes.ts

## AI implementation and verification

The implementing AI must:

1. Assess every rule above before editing and identify affected backend, frontend, data, security, navigation, and test surfaces.
2. Compare the application implementation with the pinned canonical template reference. Classify each shared file as identical, behind, customized, ahead, conflict, or not applicable.
3. Implement missing behavior directly. Preserve domain behavior and integrate shared updates through extension points rather than overwriting valid customization.
4. Add or update focused unit, architecture, integration, component, API, and browser tests according to the evidence type for each rule.
5. Run the standard repository gates and record exact commands, exit status, and meaningful test counts or artifacts.
6. Report each rule as pass, fail, not-applicable, manual-review, or approved-exception with file/line and test evidence.

A separate AI verifier must review material changes from a fresh context, inspect the diff and evidence, rerun risk-relevant gates, and issue an independent verdict. A claim without traceable evidence is not a pass.

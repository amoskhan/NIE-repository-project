# Input Validation

Mandatory request and form validation: FluentValidation with an asynchronous RFC 7807 backend pipeline, plus VeeValidate and Zod for typed Vue forms.

Rules version: 2026.08.07.1
Feature key: input-validation  
Adoption: **mandatory**

## Adoption and navigation

- Menu or entry point: not independently required. No dedicated menu is required.
- Visibility: Validation is embedded in every input surface and never creates a standalone menu.
- If the feature is not adopted, report not-applicable with the product or architecture reason; do not silently omit it.

## Required libraries and minimums

| Library | Package/runtime | Minimum | Ecosystem |
| --- | --- | --- | --- |
| .NET / ASP.NET Core | net | 10.0.0 | runtime |
| FluentValidation | FluentValidation | 12.1.1 | nuget |
| FluentValidation dependency injection extensions | FluentValidation.DependencyInjectionExtensions | 12.1.1 | nuget |
| xUnit.net v3 with Microsoft Testing Platform v2 | xunit.v3.mtp-v2 | 3.2.2 | nuget |
| Vue | vue | 3.5.30 | npm |
| VeeValidate | vee-validate | 4.15.1 | npm |
| VeeValidate Zod integration | @vee-validate/zod | 4.15.1 | npm |
| Zod | zod | 3.25.67 | npm |

Versions are floors, not forced pins for derived applications. Stable upgrades are allowed when compatibility, build, tests, security, and deployment evidence pass. The template itself keeps exact tested pins and lockfiles.

## Rules

| Rule | Severity | Area | Requirement | Required evidence |
| --- | --- | --- | --- | --- |
| NIE-VALID-001 | error | backend | Create a FluentValidation AbstractValidator for every externally supplied complex request/command DTO and register validators by assembly through the shared validation library. | architecture-tests |
| NIE-VALID-002 | error | backend | Invoke validators asynchronously before controller execution, always support ValidateAsync/cancellation, and do not use the legacy synchronous FluentValidation ASP.NET auto-validation pipeline. | api-tests |
| NIE-VALID-003 | error | contract | Return HTTP 400 application/problem+json using ValidationProblemDetails with camelCase field paths, string-array errors, instance, and traceId for both binding and FluentValidation failures. | contract-tests |
| NIE-VALID-004 | error | architecture | Keep the reusable filter/problem factory in the Validation library, feature request validators beside their API/feature, and stateful business invariants in application/domain services; do not put Procurement or other domain rules in platform code. | architecture-tests |
| NIE-VALID-005 | error | frontend | Define each interactive form with a Zod schema and let VeeValidate own values, touched/dirty/submitting state, field errors, and submit orchestration through the supported Zod integration. | component-tests |
| NIE-VALID-006 | error | frontend | Infer form types from Zod where practical, bind errors to the shared input/select error contract, focus or navigate to the first invalid field/step, and map backend field errors by the same camelCase paths. | component-and-browser-tests |
| NIE-VALID-007 | error | authority | Treat frontend validation as immediate user feedback only; the backend is authoritative and must validate every request even when the frontend already accepted it. | api-tests |
| NIE-VALID-008 | error | accessibility | Associate each error with its control using aria-invalid and aria-describedby, expose dynamic errors accessibly, preserve keyboard use, and never rely on color alone. | accessibility-tests |
| NIE-VALID-009 | error | security | Bound lengths, collection sizes, numeric ranges, and allowed values at trust boundaries; never include passwords, tokens, secrets, or raw sensitive values in validation messages or logs. | security-tests |
| NIE-VALID-010 | error | verification | Test valid, missing, malformed, boundary, nested collection, cross-field, binding-failure, and backend/frontend parity scenarios; dependency and architecture checks must fail if FluentValidation, VeeValidate, or Zod is removed or replaced. | tests and dependency/architecture review |

## Canonical reference footprint

Use these files as working examples and comparison targets. Procurement is a reference vertical, not a runtime dependency.

- src/backend/BuildingBlocks/Validation/Validation.csproj
- src/backend/BuildingBlocks/Validation/FluentValidationActionFilter.cs
- src/backend/BuildingBlocks/Validation/ValidationProblemFactory.cs
- src/backend/BuildingBlocks/Validation/ValidationServiceCollectionExtensions.cs
- src/backend/Hosts/Api/Validation
- src/backend/Hosts/Auth/Validation
- src/backend/Tests/Validation.Tests
- src/frontend/apps/auth/src/components/LoginPage.vue
- src/frontend/packages/platform/src/utils/validationProblem.ts
- src/frontend/apps/main/src/staff/pages/staff/NewPurchaseRequest.vue
- src/frontend/apps/main/src/staff/pages/staff/VendorManagement.vue
- src/frontend/apps/main/src/staff/pages/staff/CatalogItems.vue

## AI implementation and verification

The implementing AI must:

1. Assess every rule above before editing and identify affected backend, frontend, data, security, navigation, and test surfaces.
2. Compare the application implementation with the pinned canonical template reference. Classify each shared file as identical, behind, customized, ahead, conflict, or not applicable.
3. Implement missing behavior directly. Preserve domain behavior and integrate shared updates through extension points rather than overwriting valid customization.
4. Add or update focused unit, architecture, integration, component, API, and browser tests according to the evidence type for each rule.
5. Run the standard repository gates and record exact commands, exit status, and meaningful test counts or artifacts.
6. Report each rule as pass, fail, not-applicable, manual-review, or approved-exception with file/line and test evidence.

A separate AI verifier must review material changes from a fresh context, inspect the diff and evidence, rerun risk-relevant gates, and issue an independent verdict. A claim without traceable evidence is not a pass.

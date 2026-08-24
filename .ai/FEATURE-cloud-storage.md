# Cloud Storage (Local + S3)

Canonical NIE rules for the Cloud Storage (Local + S3) feature.

Rules version: 2026.08.07.1
Feature key: cloud-storage  
Adoption: **default-on**

## Adoption and navigation

- Menu or entry point: not independently required. Administration > Monitoring (health only)
- Visibility: No storage-browser menu unless the product explicitly requires one.
- If the feature is not adopted, report not-applicable with the product or architecture reason; do not silently omit it.

## Required libraries and minimums

| Library | Package/runtime | Minimum | Ecosystem |
| --- | --- | --- | --- |
| .NET / ASP.NET Core | net | 10.0.0 | runtime |
| AWS SDK for S3 | AWSSDK.S3 | 4.0.17 | nuget |
| LocalStack | localstack | 4.11.1 | service |

Versions are floors, not forced pins for derived applications. Stable upgrades are allowed when compatibility, build, tests, security, and deployment evidence pass. The template itself keeps exact tested pins and lockfiles.

## Rules

| Rule | Severity | Area | Requirement | Required evidence |
| --- | --- | --- | --- | --- |
| NIE-STORAGE-001 | error | architecture | Use IFileStorageService with Local and S3 providers selected by validated options and DI. | architecture-tests |
| NIE-STORAGE-002 | error | data | Store file metadata in PostgreSQL and bytes in the selected provider; persist normalized relative object keys, never local absolute paths. | integration-tests |
| NIE-STORAGE-003 | error | security | Use the AWS default credential chain outside local emulation; never embed storage credentials in source or log signed URLs. | security-review |
| NIE-STORAGE-004 | error | performance | Stream large content, bound uploads/downloads, sanitize names/keys, and validate content independently of extensions. | tests |
| NIE-STORAGE-005 | error | operations | Use LocalStack for S3 development/integration tests and expose provider health without exposing bucket contents. | integration-tests |
| NIE-STORAGE-006 | error | verification | Run the same provider contract tests against Local and S3-compatible storage. | contract-tests |

## Canonical reference footprint

Use these files as working examples and comparison targets. Procurement is a reference vertical, not a runtime dependency.

- src/backend/Core/Application/Features/FileStorage/IFileStorageService.cs
- src/backend/Infrastructure/Persistence/Providers/FileStorage/FileStorageService.cs
- src/backend/Infrastructure/Persistence/Providers/FileStorage/S3FileStorageService.cs
- src/backend/Infrastructure/Persistence/Providers/FileStorage/FileStorageContentTypes.cs

## AI implementation and verification

The implementing AI must:

1. Assess every rule above before editing and identify affected backend, frontend, data, security, navigation, and test surfaces.
2. Compare the application implementation with the pinned canonical template reference. Classify each shared file as identical, behind, customized, ahead, conflict, or not applicable.
3. Implement missing behavior directly. Preserve domain behavior and integrate shared updates through extension points rather than overwriting valid customization.
4. Add or update focused unit, architecture, integration, component, API, and browser tests according to the evidence type for each rule.
5. Run the standard repository gates and record exact commands, exit status, and meaningful test counts or artifacts.
6. Report each rule as pass, fail, not-applicable, manual-review, or approved-exception with file/line and test evidence.

A separate AI verifier must review material changes from a fresh context, inspect the diff and evidence, rerun risk-relevant gates, and issue an independent verdict. A claim without traceable evidence is not a pass.

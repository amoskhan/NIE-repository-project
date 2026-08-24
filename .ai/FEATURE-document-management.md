# Document Management

Polymorphic document storage and download via the Document entity, DocumentService, and a pluggable file-storage provider.

Rules version: 2026.08.07.1
Feature key: document-management  
Adoption: **mandatory**

## Adoption and navigation

- Menu or entry point: not independently required. Embedded in owning feature
- Visibility: Add a Document Library only when cross-entity document discovery is an explicit product requirement.
- If the feature is not adopted, report not-applicable with the product or architecture reason; do not silently omit it.

## Required libraries and minimums

| Library | Package/runtime | Minimum | Ecosystem |
| --- | --- | --- | --- |
| .NET / ASP.NET Core | net | 10.0.0 | runtime |
| Entity Framework Core | Microsoft.EntityFrameworkCore | 10.0.5 | nuget |
| Npgsql EF Core Provider | Npgsql.EntityFrameworkCore.PostgreSQL | 10.0.1 | nuget |
| PostgreSQL | postgres | 17.0.0 | service |
| Mapster | Mapster | 10.0.4 | nuget |
| AWS SDK for S3 | AWSSDK.S3 | 4.0.17 | nuget |
| Vue | vue | 3.5.30 | npm |
| Axios | axios | 1.18.0 | npm |

Versions are floors, not forced pins for derived applications. Stable upgrades are allowed when compatibility, build, tests, security, and deployment evidence pass. The template itself keeps exact tested pins and lockfiles.

## Rules

| Rule | Severity | Area | Requirement | Required evidence |
| --- | --- | --- | --- | --- |
| NIE-DOC-001 | error | data | Choose one ownership model per use case: a typed linking entity for relational constraints or a documented polymorphic owner contract; do not mix silently. | architecture-review |
| NIE-DOC-002 | error | security | Authorize upload/download/delete against both the document access function and owning record; prevent path traversal and BOLA. | api-tests |
| NIE-DOC-003 | error | validation | Validate size, allowed type, signature/content, file name, and malware scanning policy before persistence. | tests |
| NIE-DOC-004 | error | storage | Persist a provider key, original user filename, content type, size, checksum, owner, uploader, and timestamps; stream file bytes through IFileStorageService. | integration-tests |
| NIE-DOC-005 | error | frontend | Show validation, progress, cancel/retry, failure, and confirmed deletion states using the shared file-upload patterns. | browser-tests |
| NIE-DOC-006 | error | audit | Audit upload, download where policy requires it, replacement, and deletion without logging file contents. | tests |

## Canonical reference footprint

Use these files as working examples and comparison targets. Procurement is a reference vertical, not a runtime dependency.

- src/backend/Core/Domain/Models/Document.cs
- src/backend/Core/Application/Features/Document/IDocumentService.cs
- src/backend/Core/Application/Features/Document/DocumentService.cs
- src/backend/Core/Application/Features/FileStorage/IFileStorageService.cs
- src/backend/Infrastructure/Persistence/Providers/FileStorage/FileStorageService.cs
- src/backend/Hosts/Api/Controllers/DocumentController.cs
- src/frontend/packages/ui/src/components/composite/file-upload/index.ts

## AI implementation and verification

The implementing AI must:

1. Assess every rule above before editing and identify affected backend, frontend, data, security, navigation, and test surfaces.
2. Compare the application implementation with the pinned canonical template reference. Classify each shared file as identical, behind, customized, ahead, conflict, or not applicable.
3. Implement missing behavior directly. Preserve domain behavior and integrate shared updates through extension points rather than overwriting valid customization.
4. Add or update focused unit, architecture, integration, component, API, and browser tests according to the evidence type for each rule.
5. Run the standard repository gates and record exact commands, exit status, and meaningful test counts or artifacts.
6. Report each rule as pass, fail, not-applicable, manual-review, or approved-exception with file/line and test evidence.

A separate AI verifier must review material changes from a fresh context, inspect the diff and evidence, rerun risk-relevant gates, and issue an independent verdict. A claim without traceable evidence is not a pass.

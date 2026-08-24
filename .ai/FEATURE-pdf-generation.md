# PDF Generation

Canonical NIE rules for the PDF Generation feature.

Rules version: 2026.08.07.1
Feature key: pdf-generation  
Adoption: **opt-in**

## Adoption and navigation

- Menu or entry point: required at **Primary > Reports**.
- Visibility: Only when adopted and the user has report screen/API access.
- If the feature is not adopted, report not-applicable with the product or architecture reason; do not silently omit it.

## Required libraries and minimums

| Library | Package/runtime | Minimum | Ecosystem |
| --- | --- | --- | --- |
| .NET / ASP.NET Core | net | 10.0.0 | runtime |
| Microsoft.Playwright | Microsoft.Playwright | 1.58.0 | nuget |
| Vue | vue | 3.5.30 | npm |
| Axios | axios | 1.18.0 | npm |

Versions are floors, not forced pins for derived applications. Stable upgrades are allowed when compatibility, build, tests, security, and deployment evidence pass. The template itself keeps exact tested pins and lockfiles.

## Rules

| Rule | Severity | Area | Requirement | Required evidence |
| --- | --- | --- | --- | --- |
| NIE-REPORT-001 | error | architecture | Generate PDFs through IPdfGenerationService backed by Microsoft.Playwright; controllers must not drive browsers directly. | architecture-tests |
| NIE-REPORT-002 | error | security | Authorize each report, validate typed filters, use trusted server-side HTML, and encode all untrusted values. | security-tests |
| NIE-REPORT-003 | error | frontend | Use Reports categories/cards, filters, inline preview, print, and download with explicit loading/error/empty states. | browser-tests |
| NIE-REPORT-004 | error | operations | Provision the compatible Playwright browser/runtime in development and deployment images and enforce timeout/resource limits. | container-tests |
| NIE-REPORT-005 | error | audit | Audit report preview/export with report type, safe filter summary, actor, outcome, and correlation ID. | tests |
| NIE-REPORT-006 | error | verification | Test filter validation, authorization, HTML encoding, browser absence, timeout, PDF headers/content, preview, and download. | tests |

## Canonical reference footprint

Use these files as working examples and comparison targets. Procurement is a reference vertical, not a runtime dependency.

- src/backend/Core/Application/Features/PdfGeneration/IPdfGenerationService.cs
- src/backend/Infrastructure/Persistence/Providers/PdfGeneration/PlaywrightPdfGenerationService.cs
- src/backend/Hosts/Api/Controllers/ReportController.cs
- src/backend/Core/Application/Contracts/Report/ReportRequestDto.cs
- src/frontend/apps/main/src/services/reportService.ts

## AI implementation and verification

The implementing AI must:

1. Assess every rule above before editing and identify affected backend, frontend, data, security, navigation, and test surfaces.
2. Compare the application implementation with the pinned canonical template reference. Classify each shared file as identical, behind, customized, ahead, conflict, or not applicable.
3. Implement missing behavior directly. Preserve domain behavior and integrate shared updates through extension points rather than overwriting valid customization.
4. Add or update focused unit, architecture, integration, component, API, and browser tests according to the evidence type for each rule.
5. Run the standard repository gates and record exact commands, exit status, and meaningful test counts or artifacts.
6. Report each rule as pass, fail, not-applicable, manual-review, or approved-exception with file/line and test evidence.

A separate AI verifier must review material changes from a fresh context, inspect the diff and evidence, rerun risk-relevant gates, and issue an independent verdict. A claim without traceable evidence is not a pass.

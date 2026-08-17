# PDF Generation - File Map

## Owned files

| Path                                                                                      | Layer            | Purpose                                                              |
| ----------------------------------------------------------------------------------------- | ---------------- | -------------------------------------------------------------------- |
| `src/backend/Libraries/Services/Services/PdfGeneration/IPdfGenerationService.cs`          | Contract         | Report type discovery, HTML generation, and PDF conversion boundary. |
| `src/backend/Libraries/Services/Services/PdfGeneration/PlaywrightPdfGenerationService.cs` | Service          | Data-backed report HTML plus Playwright HTML-to-PDF conversion.      |
| `src/backend/API/Controllers/ReportController.cs`                                         | API              | Report type, HTML preview, inline PDF, and download endpoints.       |
| `src/backend/API/Reports.http`                                                            | API docs         | Manual report endpoint calls.                                        |
| `src/frontend/main/src/pages/reports/ReportsIndex.vue`                                    | Route            | Report list grouped by category.                                     |
| `src/frontend/main/src/pages/reports/ReportDetail.vue`                                    | Route            | Filters, live PDF preview, print, refresh, and download actions.     |
| `src/frontend/main/src/services/reportService.ts`                                         | Frontend service | Typed report API client.                                             |

## Touched files

| Path                                                             | Why                                        |
| ---------------------------------------------------------------- | ------------------------------------------ |
| `src/backend/API/Program.cs`                                     | Registers `IPdfGenerationService`.         |
| `src/backend/Libraries/Shared/Security/AccessFunctionCatalog.cs` | Grants report screen/API access functions. |
| `src/frontend/main/src/router/index.ts`                          | Adds protected report routes.              |
| `src/frontend/main/src/composables/usePermissions.ts`            | Adds the Reports sidebar item.             |
| `src/frontend/main/src/constants/permissions.ts`                 | Defines report permission constants.       |

## Runtime dependencies

The PDF conversion path expects Playwright CLI to be available. Configure `Reports:PlaywrightExecutablePath` when the default `npx`/`npx.cmd playwright` path is not valid for the deployment image.

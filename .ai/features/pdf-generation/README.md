# PDF Generation & Reports

> **Status:** `optional`

## Overview

Generates PDF reports from trusted server-side HTML using Playwright headless Chromium. Includes a Reports sidebar menu with grouped report cards, filters, inline PDF preview, print, and download actions.

## Key Files

| Layer      | Path                                                       | Purpose                                                            |
| ---------- | ---------------------------------------------------------- | ------------------------------------------------------------------ |
| Service    | `Services/PdfGeneration/IPdfGenerationService.cs`          | Interface: GeneratePdfFromHtml, GetReportTypes, GenerateReportHtml |
| Service    | `Services/PdfGeneration/PlaywrightPdfGenerationService.cs` | Data-backed report HTML and Playwright PDF conversion              |
| Controller | `API/Controllers/ReportController.cs`                      | GET types, POST HTML preview, POST inline PDF, POST download       |
| FE Page    | `pages/reports/ReportsIndex.vue`                           | Report type cards grouped by category                              |
| FE Page    | `pages/reports/ReportDetail.vue`                           | Filters, PDF preview, refresh, print, and download                 |
| FE Service | `services/reportService.ts`                                | API client                                                         |
| Config     | `build/Dockerfile.api`                                     | Playwright Chromium dependencies                                   |

## Report Types

- **Procurement:** PO Summary, Vendor Analysis, Spending by Requester, Approval Timeline
- **Audit:** Audit Trail, User Activity

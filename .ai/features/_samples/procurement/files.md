# Procurement — File Map

> This is a **reference sample** — read it, copy the patterns, then remove it (see
> [`remove.md`](./remove.md)). Its seed data is fictional.
>
> The self-contained sample files live under `**/Samples/Procurement/` (namespaces
> unchanged). Wiring that has to sit inside a shared file is wrapped in
> `// === SAMPLE: procurement ... ===` fences so it can be found and removed
> mechanically. Frontend project data lives in `src/frontend/main/src/app-config/*`.

## Owned files (delete-with-feature)

### Backend entities and DTOs

| Path                                                                               | Purpose                                                                                                                                   |
| ---------------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------- |
| `src/backend/Libraries/Domain/Models/Samples/Procurement/Vendor.cs`                | Vendor entity (master data)                                                                                                               |
| `src/backend/Libraries/Domain/Models/Samples/Procurement/CatalogItem.cs`           | Catalog item per vendor                                                                                                                   |
| `src/backend/Libraries/Domain/Models/Samples/Procurement/PurchaseOrder.cs`         | PO header with status workflow                                                                                                            |
| `src/backend/Libraries/Domain/Models/Samples/Procurement/PurchaseOrderLine.cs`     | PO line items (qty + unit price)                                                                                                          |
| `src/backend/Libraries/Domain/Models/Samples/Procurement/PurchaseOrderApproval.cs` | Approval chain rows                                                                                                                       |
| `src/backend/Libraries/Domain/Models/Samples/Procurement/PurchaseOrderDocument.cs` | Document attachment with hard FK to PO                                                                                                    |
| `src/backend/Libraries/Shared/Dto/Samples/Procurement/VendorDto.cs`                | Vendor DTO                                                                                                                                |
| `src/backend/Libraries/Shared/Dto/Samples/Procurement/CatalogItemDto.cs`           | Catalog item DTO                                                                                                                          |
| `src/backend/Libraries/Shared/Dto/Samples/Procurement/PurchaseOrderDto.cs`         | PO DTO with nested Lines/Approvals/Documents                                                                                              |
| `src/backend/Libraries/Shared/Enum/Samples/Procurement/EPurchaseOrderStatus.cs`    | Status enum (Draft, Submitted, PendingManagerApproval, PendingFinanceApproval, PendingProcurementApproval, Approved, Rejected, Cancelled) |

### Backend services and controllers

| Path                                                                                                                 | Purpose                                                           |
| -------------------------------------------------------------------------------------------------------------------- | ----------------------------------------------------------------- |
| `src/backend/Libraries/Services/Services/Samples/Procurement/Vendor/IVendorService.cs`                               | service interface                                                 |
| `src/backend/Libraries/Services/Services/Samples/Procurement/Vendor/VendorService.cs`                                | service impl                                                      |
| `src/backend/Libraries/Services/Services/Samples/Procurement/CatalogItem/ICatalogItemService.cs`                     | service interface                                                 |
| `src/backend/Libraries/Services/Services/Samples/Procurement/CatalogItem/CatalogItemService.cs`                      | service impl                                                      |
| `src/backend/Libraries/Services/Services/Samples/Procurement/PurchaseOrder/IPurchaseOrderService.cs`                 | service interface                                                 |
| `src/backend/Libraries/Services/Services/Samples/Procurement/PurchaseOrder/PurchaseOrderService.cs`                  | service impl                                                      |
| `src/backend/Libraries/Services/Services/Samples/Procurement/PurchaseOrderDocument/IPurchaseOrderDocumentService.cs` | document service interface                                        |
| `src/backend/Libraries/Services/Services/Samples/Procurement/PurchaseOrderDocument/PurchaseOrderDocumentService.cs`  | document service impl                                             |
| `src/backend/API/Controllers/Samples/Procurement/VendorController.cs`                                                | CRUD endpoints                                                    |
| `src/backend/API/Controllers/Samples/Procurement/CatalogItemController.cs`                                           | CRUD endpoints                                                    |
| `src/backend/API/Controllers/Samples/Procurement/PurchaseOrderController.cs`                                         | CRUD + Submit + ProcessApproval + UploadDocument + DeleteDocument |

### Frontend

| Path                                                               | Purpose                      |
| ------------------------------------------------------------------ | ---------------------------- |
| `src/frontend/main/src/staff/pages/staff/ProcurementDashboard.vue` | Main dashboard with KPIs     |
| `src/frontend/main/src/staff/pages/staff/VendorManagement.vue`     | Vendor list/CRUD             |
| `src/frontend/main/src/staff/pages/staff/CatalogItems.vue`         | Catalog list/CRUD            |
| `src/frontend/main/src/staff/pages/staff/NewPurchaseRequest.vue`   | Create PO                    |
| `src/frontend/main/src/staff/pages/staff/OrderHistory.vue`         | PO list                      |
| `src/frontend/main/src/staff/pages/staff/PurchaseOrderDetail.vue`  | PO detail + approval actions |
| `src/frontend/main/src/staff/pages/staff/ApprovalQueue.vue`        | Pending approvals            |
| `src/frontend/main/src/services/vendorService.ts`                  | Vendor API client            |
| `src/frontend/main/src/services/catalogItemService.ts`             | Catalog API client           |
| `src/frontend/main/src/services/purchaseOrderService.ts`           | PO API client                |

## Touched files (line-level edits required when removing the feature)

| Path                                                             | What it contains                                                                | Why must be touched                                                                                              |
| ---------------------------------------------------------------- | ------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------- |
| `src/backend/Libraries/Data/Data/MainDbContext.cs`               | Fenced procurement DbSets + relationship configs + Code seed rows               | Delete the three `// === SAMPLE: procurement === ` blocks (and the trailing comma on the last template Code row) |
| `src/backend/API/Program.cs`                                     | Fenced procurement DI block + usings                                            | Delete the fenced block + procurement usings                                                                     |
| `src/backend/API/Mapping/MappingProfile.cs`                      | Fenced procurement Mapster configs                                              | Delete the fenced block                                                                                          |
| `src/backend/API/Extensions/DatabaseSeeder.cs`                   | Fenced seed calls + Code rows + demo-seed methods                               | Delete the fenced blocks + method group                                                                          |
| `src/backend/Libraries/Shared/Security/AccessFunctionCatalog.cs` | Fenced const + seed blocks + role-bundle codes                                  | Delete the fenced blocks + procurement codes in Manager/User/Viewer bundles                                      |
| `src/backend/Libraries/Shared/Enum/ECodeType.cs`                 | VENDOR_CATEGORY, CATALOG_CATEGORY, UNIT_OF_MEASURE, DELIVERY_LOCATION, CURRENCY | Remove unused values                                                                                             |
| `src/backend/Libraries/Shared/Enum/ECodeName.cs`                 | procurement Name values                                                         | Remove                                                                                                           |
| `src/frontend/main/src/app-config/routes.ts`                     | procurement `PROJECT_ROUTES`                                                    | Remove procurement routes; repoint the dashboard route                                                           |
| `src/frontend/main/src/app-config/navigation.ts`                 | procurement `PRIMARY_NAV_ITEMS`                                                 | Remove procurement nav items                                                                                     |
| `src/frontend/main/src/app-config/accessFunctions.ts`            | Procurement* codes + Vendor/Catalog UI permissions + role maps + role labels    | Remove procurement entries                                                                                       |
| `src/frontend/main/src/app-config/branding.ts`                   | `FEEDBACK_FUNCTION_PREFIX = "procurement"`                                      | Set to your project namespace                                                                                    |
| `tests/specs/fixtures/test-config.ts`                            | vendor + purchaseOrder ApiEndpoints + procurement Routes                        | Remove                                                                                                           |

## Migrations

| Migration                                                      | What it does                                                                                                                                                                          |
| -------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `<timestamp>_InitialCreate.cs`                                 | Creates the procurement schema (Vendors, CatalogItems, PurchaseOrders, PurchaseOrderLines, PurchaseOrderApprovals, PurchaseOrderDocuments) along with the rest of the template schema |
| (created on removal) `<timestamp>_RemoveProcurementSamples.cs` | Drops all procurement tables and seeded rows                                                                                                                                          |

## External dependencies

None — procurement uses only what already ships in the template (EF Core, Mapster, and the FileStorage service).

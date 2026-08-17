# Document Management — File Map

## Owned files

### Backend

| Path                                                                         | Layer      | Purpose                                                                                                                                                                                 |
| ---------------------------------------------------------------------------- | ---------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `src/backend/Libraries/Domain/Models/Document.cs`                            | Entity     | `FilePath`, `FileSize`, `UserFileName`, optional polymorphic `OwnerType` (e.g. `"PurchaseOrder"`) and `OwnerId`. Inherits `TimestampedEntity` so the EF Core hook auto-audits every CUD |
| `src/backend/Libraries/Services/Services/Document/IDocumentService.cs`       | Interface  | Inherits `IBaseService<Document>` — generic CRUD                                                                                                                                        |
| `src/backend/Libraries/Services/Services/Document/DocumentService.cs`        | Service    | Thin wrapper around `BaseService<Document>` (no custom logic — projects extend if they need filtering helpers)                                                                          |
| `src/backend/Libraries/Services/Services/FileStorage/IFileStorageService.cs` | Interface  | `SaveFileAsync(IFormFile, fileName)`, `GetFileAsync(filePath)`, `DeleteFileAsync(filePath)`, `GetFilePathAsync(fileName)`                                                               |
| `src/backend/Libraries/Services/Services/FileStorage/FileStorageService.cs`  | Service    | Local-disk implementation: writes to `{FileStorage:BasePath}/yyyy-MM/{guid}.{ext}`, returns the relative path; `GetContentType` maps 30+ file extensions to MIME types                  |
| `src/backend/API/Controllers/DocumentController.cs`                          | Controller | `UploadFile` (multipart), `DownloadFile?path=`, `DeleteFile?id=`. `[RequireAccessFunction]` gates: `DocumentManage` for upload/delete, `DocumentDownload` for download                  |

### Frontend

| Path                                                                     | Layer     | Purpose                                                                                   |
| ------------------------------------------------------------------------ | --------- | ----------------------------------------------------------------------------------------- |
| `src/frontend/packages/ui/src/components/composite/file-upload/`         | Component | `AppFileUpload.vue` — drag-and-drop or click-to-browse upload primitive used across pages |
| `src/frontend/packages/ui/src/components/composite/file-upload/index.ts` | Barrel    | Re-export from `@apptemplate/ui`                                                          |

## Touched files

| Path                                                                                                     | What it contains                                                                                                                                                                                 | Why must be touched                                                                                                        |
| -------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ | -------------------------------------------------------------------------------------------------------------------------- |
| `src/backend/Libraries/Data/Data/MainDbContext.cs`                                                       | DbSet `Documents` + Fluent API                                                                                                                                                                   | Required for the table and any unique indexes (e.g. on `OwnerType + OwnerId` if you want to enforce unique-per-owner)      |
| `src/backend/API/Program.cs`                                                                             | `builder.Services.AddScoped<IDocumentService, DocumentService>()`, `builder.Services.AddScoped<IFileStorageService, FileStorageService>()`, plus the upload-directory ensure block (lines 48-50) | DI + boot-time directory creation                                                                                          |
| `src/backend/API/appsettings.json`                                                                       | `FileStorage:BasePath`                                                                                                                                                                           | Local upload root; in production this should be a mounted volume, NOT the working directory                                |
| `src/backend/API/Mapping/MappingProfile.cs`                                                              | Mapster `Document → DocumentDto` (if a DTO is used)                                                                                                                                              | Required for projection if the controller returns a DTO                                                                    |
| `src/backend/Libraries/Shared/Security/AccessFunctionCatalog.cs`                                         | `Api.DocumentManage`, `Api.DocumentDownload` codes and seed defs                                                                                                                                 | Gates the controller actions                                                                                               |
| `src/backend/Libraries/Services/Services/AuditLog/IAuditLogger.cs` + `AuditLogger.cs`                    | `LogFileUploadAsync`, `LogFileDownloadAsync`, `LogFileDeleteAsync`                                                                                                                               | The `DocumentController` calls these for explicit `EAuditCategory.FileOperation` entries on top of the automatic CUD audit |
| `.gitignore`                                                                                             | `src/backend/API/uploads/`                                                                                                                                                                       | Required so dev uploads are NOT committed                                                                                  |
| Procurement-specific: `src/backend/Libraries/Domain/Models/Samples/Procurement/PurchaseOrderDocument.cs` | Hard-FK linking entity to `PurchaseOrder` and `Document`                                                                                                                                         | Reference sample showing the alternative (linking-entity) pattern                                                          |

## Migrations

| Migration                                       | What it does                                                                                                                                                |
| ----------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------- |
| First migration containing `Documents`          | Creates the `Documents` table with `Id`, `FilePath`, `FileSize`, `UserFileName`, `OwnerType`, `OwnerId`, `CreatedOn`, `CreatedBy`, `UpdatedOn`, `UpdatedBy` |
| Optional `<timestamp>_AddDocumentOwnerIndex.cs` | Adds an index on `(OwnerType, OwnerId)` for fast "all documents for X" queries                                                                              |

## External dependencies

None for the local-disk path — pure `System.IO.File` and `System.IO.Path`. The S3 / Azure Blob alternatives (see `customize.md`) require an SDK package per provider.

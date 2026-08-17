# Document Management

> **Status:** `core`
> **Removable in derived repos:** **no** — file uploads are a baseline expectation
> **Required by:** any feature that uploads / serves files (procurement uses its OWN linking entity, but consumes `FileStorageService`)

The Document Management feature provides two layers:

1. **Generic blob entity** — `Document` is a polymorphic file row with `OwnerType` (string) + `OwnerId` (int) for loose attachments. Use this when you want a quick "attach a file to anything" pattern without a per-feature linking table.
2. **File-storage service** — `FileStorageService` writes to disk under `FileStorage:BasePath` in a `yyyy-MM/<guid>.ext` layout, returns the relative path, knows MIME types for download, and provides delete. Storage is on the local filesystem by default; the service is small and easy to swap for S3/blob/etc.

`Document` inherits `TimestampedEntity` so every upload / metadata change automatically writes an audit row. `IAuditLogger.LogFileUploadAsync` / `LogFileDownloadAsync` / `LogFileDeleteAsync` are also called from the controller for explicit category=FileOperation entries.

The procurement reference sample uses a DIFFERENT pattern — `PurchaseOrderDocument` is a hard-FK linking entity from PO → Document. Both patterns are valid template citizens:

- **Polymorphic** (`Document.OwnerType + OwnerId`): fast to wire, no migration per consumer. Trade-off: no FK integrity, harder to query "all docs for X" with EF includes.
- **Linking entity** (e.g. `PurchaseOrderDocument`): full FK integrity, cascade deletes, EF nav properties. Trade-off: one new entity per consumer.

Pick polymorphic for one-off attachments (profile photo, generic uploads). Pick linking entity for first-class document collections (PO files, reports, etc.).

## Quick links

- [`files.md`](./files.md) — every file owned and touched by this feature
- [`do-dont.md`](./do-dont.md) — feature-specific rules
- [`customize.md`](./customize.md) — switching to S3, raising file size, attaching docs to a new owner type
- [`verify.md`](./verify.md) — upload / download / delete smoke

## Architectural shape

```mermaid
flowchart LR
  Client["Vue page<br/>upload form"] -->|POST /api/Document/UploadFile<br/>multipart/form-data| Ctrl["DocumentController"]
  Ctrl --> Svc[DocumentService]
  Ctrl --> Storage[FileStorageService]
  Storage -->|writes| Disk[(FileStorage:BasePath<br/>yyyy-MM/{guid}.ext)]
  Svc --> Db[(Documents table)]
  Db -->|TimestampedEntity hook| Audit[(AuditLog: Create)]
  Ctrl -->|LogFileUploadAsync| Audit
  Client -->|GET /api/Document/DownloadFile?path=| Ctrl
  Ctrl --> Storage
  Storage -->|read+content-type| Disk
  Ctrl -->|FileResult| Client
```

## Key entry points

| Layer               | Path                                                                                                   | Purpose                                                                                                                                                                    |
| ------------------- | ------------------------------------------------------------------------------------------------------ | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Entity              | `src/backend/Libraries/Domain/Models/Document.cs`                                                      | `FilePath`, `FileSize`, `UserFileName`, optional `OwnerType + OwnerId` (polymorphic). Inherits `TimestampedEntity` for automatic audit                                     |
| Document service    | `src/backend/Libraries/Services/Services/Document/IDocumentService.cs` + `DocumentService.cs`          | Thin `BaseService<Document>` wrapper for CRUD                                                                                                                              |
| Storage service     | `src/backend/Libraries/Services/Services/FileStorage/IFileStorageService.cs` + `FileStorageService.cs` | `SaveFileAsync(IFormFile, fileName)`, `GetFileAsync(filePath)`, `DeleteFileAsync(filePath)`, `GetFilePathAsync(fileName)` (pre-allocate path), `GetContentType(extension)` |
| Controller          | `src/backend/API/Controllers/DocumentController.cs`                                                    | `UploadFile` (gated by `Api.DocumentManage`), `DownloadFile` (gated by `Api.DocumentDownload`), `DeleteFile` (gated by `Api.DocumentManage`)                               |
| Config              | `src/backend/API/appsettings.json` `FileStorage:BasePath`                                              | Local directory root for uploads (defaults to `src/backend/API/uploads/` in dev)                                                                                           |
| FE upload primitive | `src/frontend/packages/ui/src/components/composite/file-upload/`                                       | `AppFileUpload` component used by pages that need uploads                                                                                                                  |

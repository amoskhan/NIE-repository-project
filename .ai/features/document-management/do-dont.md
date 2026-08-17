# Document Management — Do and Don't

## DO ✅

1. **DO** decide between **polymorphic** (`Document.OwnerType + OwnerId`) and **linking-entity** (`PurchaseOrderDocument`-style) BEFORE you write the consumer feature. Polymorphic is faster to wire; linking-entity is safer for first-class collections. Both are valid template citizens.
2. **DO** route every upload through `IFileStorageService.SaveFileAsync`. The service decides where the file lives, the relative path format, and the directory creation. Bypassing it (e.g. calling `File.WriteAllBytes` directly) breaks the assumption that all paths are `yyyy-MM/{guid}.ext`.
3. **DO** persist the relative path returned by `SaveFileAsync` into `Document.FilePath`. Never persist the absolute path — it changes when `FileStorage:BasePath` changes (across environments).
4. **DO** persist `Document.UserFileName` separately from `Document.FilePath`. The path is opaque (`yyyy-MM/{guid}.ext`); the user file name is what we display and what `Content-Disposition: attachment; filename=` returns on download.
5. **DO** validate file size + extension on the controller BEFORE calling `SaveFileAsync`. The service does no validation — that's by design (the service is reusable for trusted callers).
6. **DO** use `[RequireAccessFunction(AccessFunctionCodes.Api.DocumentDownload)]` on download endpoints and `Api.DocumentManage` on upload/delete endpoints. The codes are already in the catalog.
7. **DO** call `IAuditLogger.LogFileUploadAsync(fileName, fileSize, entityName, entityId)` after a successful upload, `LogFileDownloadAsync` after download, `LogFileDeleteAsync` after delete. These produce `EAuditCategory.FileOperation` rows with file-specific context that the auto-audit hook does not capture.
8. **DO** use `LogFileUploadAsync`'s `entityName` + `entityId` arguments to record the OWNER context — not the Document's own row. Auditors care about "what was attached to PO #1234", not "Document #999 was created".
9. **DO** keep `FileStorage:BasePath` outside the source tree in production (e.g. `/var/lib/myapp/uploads`). The dev default `src/backend/API/uploads/` works for local but writes inside the source — guarded by `.gitignore`.
10. **DO** delete the file on disk BEFORE deleting the `Document` row, so a failure midway leaves orphans rather than dangling rows. The `DocumentController.DeleteFile` action follows this order.

## DON'T ❌

1. **DON'T** mix the two patterns for the same entity. If `PurchaseOrder` uses `PurchaseOrderDocument`, do NOT also write `Document.OwnerType = "PurchaseOrder"` rows. Pick one and stick with it for that consumer.
2. **DON'T** put downloadable URLs in `Document.FilePath`. The path is local-relative; URLs are constructed by `DocumentController.DownloadFile?path=...` so the client never accesses files directly.
3. **DON'T** disable file extension validation. Even with no virus scanning, blocking executable extensions (`.exe`, `.bat`, `.ps1`, `.sh`, `.js`, `.html`) is a baseline defense. Add the validation on `DocumentController.UploadFile`.
4. **DON'T** trust the original file name as a filesystem path. The service generates a `Guid`-based unique name precisely to avoid path traversal (`../../etc/passwd`) and collisions.
5. **DON'T** stream responses through the controller without setting `Content-Type` correctly. `FileStorageService.GetContentType(extension)` does this; route through it.
6. **DON'T** commit the `uploads/` directory. Confirm `.gitignore` covers it before any PR.
7. **DON'T** keep the file on disk after deleting the row. Orphan files accumulate and leak storage. The controller's delete order (file → row) is by design.
8. **DON'T** allow files larger than `Kestrel:Limits:MaxRequestBodySize` (default 30 MB in ASP.NET Core). Raise it deliberately in `Program.cs` if you need larger uploads — and then bump the rate-limiter's per-route quota too.
9. **DON'T** log the full file content. Log the file name, size, owner reference, and that's it.
10. **DON'T** use `Document.OwnerType` as a string the way you'd use a code Type — it's a free-form string in this entity. If you want validation, add a CHECK constraint or an `EOwnerType` enum and enforce its `.ToString()` value at the controller.

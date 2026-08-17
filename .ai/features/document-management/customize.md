# Document Management — Customize

## 1. Attach a generic Document to a new owner type (polymorphic pattern)

Use this pattern for one-off attachments (profile photo, certification doc, etc.).

1. On upload, set the `OwnerType` to a stable string (recommended: introduce a small enum `EOwnerType` and use `EOwnerType.Profile.ToString()`):
   ```csharp
   var doc = new Document
   {
       FilePath = await _storage.SaveFileAsync(file, file.FileName),
       FileSize = file.Length,
       UserFileName = file.FileName,
       OwnerType = "Profile",
       OwnerId = userId, // int FK; cast or hash if userId is a string
   };
   _context.Documents.Add(doc);
   await _context.SaveChangesAsync();
   ```
2. To list "all docs for owner X":
   ```csharp
   var docs = await _context.Documents
       .Where(d => d.OwnerType == "Profile" && d.OwnerId == userId)
       .OrderByDescending(d => d.CreatedOn)
       .ToListAsync();
   ```
3. No migration needed — `OwnerType` and `OwnerId` are nullable on the entity.
4. Add an index for performance on hot owner lookups:
   ```sql
   CREATE INDEX "IX_Documents_OwnerType_OwnerId" ON "Documents" ("OwnerType", "OwnerId");
   ```

## 2. Attach Documents via a hard-FK linking entity (PurchaseOrderDocument pattern)

Use this for first-class document collections that need cascade delete and EF nav properties.

1. Create a new entity `src/backend/Libraries/Domain/Models/<Owner>Document.cs`:
   ```csharp
   public class ReportDocument : TimestampedEntity
   {
       public int ReportId { get; set; }
       public Report Report { get; set; } = default!;

       public int DocumentId { get; set; }
       public Document Document { get; set; } = default!;
   }
   ```
2. Add the DbSet to `MainDbContext` and a Fluent API config (FK pair, cascade delete from Report).
3. Add a migration: `dotnet ef migrations add AddReportDocuments`.
4. In your owner controller, on upload save BOTH `Document` and the linking entity in one `SaveChangesAsync` call.
5. To list docs for an owner:
   ```csharp
   var docs = await _context.Reports
       .Where(r => r.Id == reportId)
       .SelectMany(r => r.ReportDocuments.Select(rd => rd.Document))
       .ToListAsync();
   ```
6. Cascade delete is automatic when the parent (Report) is deleted, but the file on disk remains — wire `IFileStorageService.DeleteFileAsync` into your delete handler.

## 3. Move from local disk to S3

1. Create `src/backend/Libraries/Services/Services/FileStorage/S3FileStorageService.cs` implementing `IFileStorageService`. Use `AWSSDK.S3` (`PutObjectAsync`, `GetObjectAsync`, `DeleteObjectAsync`). Keep the same return contract: `SaveFileAsync` returns the relative `yyyy-MM/{guid}.ext` key.
2. Replace `builder.Services.AddScoped<IFileStorageService, FileStorageService>();` in `Program.cs:81` with the new implementation behind a config flag:
   ```csharp
   if (builder.Configuration["FileStorage:Provider"] == "S3")
       builder.Services.AddScoped<IFileStorageService, S3FileStorageService>();
   else
       builder.Services.AddScoped<IFileStorageService, FileStorageService>();
   ```
3. Add `FileStorage:S3:BucketName`, `FileStorage:S3:Region`, and credentials (preferably via instance profile, NOT in appsettings).
4. The `Document.FilePath` value remains the relative key — no migration needed for existing rows.
5. For download, `S3FileStorageService.GetFileAsync` returns the byte[] + content-type just like the disk version.

## 4. Increase max upload size

The default ASP.NET Core request body limit is 30 MB. To allow 100 MB:

1. Edit `src/backend/API/Program.cs` near the WebApplication build:
   ```csharp
   builder.Services.Configure<KestrelServerOptions>(options =>
   {
       options.Limits.MaxRequestBodySize = 100 * 1024 * 1024;
   });
   builder.Services.Configure<FormOptions>(options =>
   {
       options.MultipartBodyLengthLimit = 100 * 1024 * 1024;
   });
   ```
2. If running behind IIS or NGINX, also raise their respective body limits.
3. Bump rate-limiter quota for the upload endpoint (see `rate-limiting/customize.md`) — large uploads should not consume the same per-IP budget as quick API calls.

## 4. Restrict upload extensions

In `DocumentController.UploadFile`, before `SaveFileAsync`:

```csharp
private static readonly string[] _allowedExtensions =
    [".pdf", ".docx", ".xlsx", ".png", ".jpg", ".jpeg"];

var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
if (!_allowedExtensions.Contains(ext))
    return BadRequest(ApiResponse.Failure($"Extension '{ext}' is not allowed."));
```

Keep the list short and conservative. Add `.csv` / `.txt` only if your business case justifies CSV injection risk handling.

## 5. Add antivirus scanning

1. Add `ClamAV.Client` (or your scanner SDK) as a dep.
2. Inject `IClamClient` into `DocumentController.UploadFile`.
3. Scan `file.OpenReadStream()` BEFORE calling `SaveFileAsync`. On hit, return 422 and audit-log via `IAuditLogger.LogAsync(EAuditAction.SystemEvent, EAuditCategory.System, "AntivirusHit", ...)`.

## 6. Pre-allocate a file path (large multi-part upload)

`FileStorageService.GetFilePathAsync` returns a relative path WITHOUT writing the file — useful for tus / resumable uploads or when uploading from a separate worker:

```csharp
var relativePath = await _storage.GetFilePathAsync(originalName);
// Pass `relativePath` to the worker; the worker writes to that key.
// Then create the Document row referencing the same relativePath.
```

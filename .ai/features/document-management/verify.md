# Document Management — Verify

## Backend

```bash
dotnet build src/backend/AppTemplate.sln
dotnet run --project src/backend/API
```

On boot, confirm the upload directory exists:

```bash
ls src/backend/API/uploads
# Expect: directory exists (created by Program.cs:48-50 if missing)
```

## API smoke — upload / download / delete

```bash
SESSION=$(curl -s -X POST http://localhost:5001/api/Auth/CreateTestSession \
  -H "Content-Type: application/json" \
  -d '{"UserId":"alice"}' | jq -r .sessionToken)

# Assign Administrator (DocumentManage + DocumentDownload)
curl -s -X POST http://localhost:5002/api/AccessControl/AssignRole \
  -H "Content-Type: application/json" \
  -H "X-Session-Id: $SESSION" \
  -d '{"userId":"alice","roleId":1}'

# Create a small test file
echo "Hello from audit smoke" > /tmp/smoke.txt

# Upload
RESP=$(curl -s -X POST http://localhost:5002/api/Document/UploadFile \
  -H "X-Session-Id: $SESSION" \
  -F "file=@/tmp/smoke.txt")
echo "$RESP" | jq

PATH=$(echo "$RESP" | jq -r .filePath)
echo "Stored at: $PATH"

# Confirm the file exists on disk
ls "src/backend/API/uploads/$PATH"
# Expect: a file like 2026-04/<guid>.txt

# Confirm the Document row was created (with audit row)
psql "$DATABASE_URL" -c \
  "SELECT \"FilePath\", \"FileSize\", \"UserFileName\" FROM \"Documents\" ORDER BY \"Id\" DESC LIMIT 1;"

psql "$DATABASE_URL" -c \
  "SELECT \"Action\", \"Category\", \"EntityName\" FROM \"AuditLogs\"
   WHERE \"EntityName\" = 'Document' ORDER BY \"Timestamp\" DESC LIMIT 1;"
# Expect: Action = 1 (Create), Category = 0 (Data) — automatic from TimestampedEntity hook

psql "$DATABASE_URL" -c \
  "SELECT \"Action\", \"Category\", \"NewValues\" FROM \"AuditLogs\"
   WHERE \"Action\" = 30 ORDER BY \"Timestamp\" DESC LIMIT 1;"
# Expect: Action = 30 (FileUpload), Category = 3 (FileOperation) — explicit from controller

# Download
curl -s -o /tmp/downloaded.txt -w "%{http_code}\n" \
  "http://localhost:5002/api/Document/DownloadFile?path=$PATH" \
  -H "X-Session-Id: $SESSION"
# Expect: 200

diff /tmp/smoke.txt /tmp/downloaded.txt
# Expect: no diff
```

## Negative tests

```bash
# Empty file rejected
curl -s -o /dev/null -w "%{http_code}\n" -X POST \
  "http://localhost:5002/api/Document/UploadFile" \
  -H "X-Session-Id: $SESSION" \
  -F "file=@/dev/null"
# Expect: 400 (FileStorageService throws ArgumentException for empty)

# Path traversal blocked (the service uses Guid.NewGuid for the unique name; the requested user file name never reaches disk)
curl -s -X POST http://localhost:5002/api/Document/UploadFile \
  -H "X-Session-Id: $SESSION" \
  -F "file=@/tmp/smoke.txt;filename=../../etc/passwd"
# Expect: 200 (the file is saved under uploads/yyyy-MM/<guid>.passwd or similar — confirm
# nothing was written outside the uploads directory)
ls -la src/backend/API/uploads/../  # confirm no stray file landed in the parent
```

## Deletion

```bash
# Delete the Document row + the file on disk
DOC_ID=$(psql "$DATABASE_URL" -tAc "SELECT \"Id\" FROM \"Documents\" ORDER BY \"Id\" DESC LIMIT 1;")

curl -s -o /dev/null -w "%{http_code}\n" -X DELETE \
  "http://localhost:5002/api/Document/DeleteFile?id=$DOC_ID" \
  -H "X-Session-Id: $SESSION"
# Expect: 200

# Confirm row is gone
psql "$DATABASE_URL" -c "SELECT count(*) FROM \"Documents\" WHERE \"Id\" = $DOC_ID;"
# Expect: 0

# Confirm file is gone from disk
ls "src/backend/API/uploads/$PATH"
# Expect: ls error (no such file)

# Confirm FileDelete audit row
psql "$DATABASE_URL" -c \
  "SELECT \"Action\", \"OldValues\" FROM \"AuditLogs\"
   WHERE \"Action\" = 32 ORDER BY \"Timestamp\" DESC LIMIT 1;"
# Expect: Action = 32 (FileDelete)
```

## Permissions

```bash
# Viewer cannot upload
SESSION_VIEWER=$(...) # session of a user with Viewer role only
curl -s -o /dev/null -w "%{http_code}\n" -X POST \
  "http://localhost:5002/api/Document/UploadFile" \
  -H "X-Session-Id: $SESSION_VIEWER" \
  -F "file=@/tmp/smoke.txt"
# Expect: 403

# Confirm AccessDenied audit row
psql "$DATABASE_URL" -c \
  "SELECT \"AdditionalData\" FROM \"AuditLogs\"
   WHERE \"Action\" = 28 AND \"AdditionalData\"::text LIKE '%document.manage%'
   ORDER BY \"Timestamp\" DESC LIMIT 1;"
```

## Frontend (manual click-path)

1. Login as Administrator. Open `Procurement → New Purchase Request` (which uses the linking-entity pattern).
2. Click "Attach Document", pick a file. Confirm the file appears in the list with a remove button.
3. Submit the PO. Confirm the file is downloadable from the PO detail page.
4. Delete the document from the detail page. Confirm the row is gone and the file is no longer downloadable.
5. The `AppFileUpload` component (`@apptemplate/ui`) handles drag-and-drop — drag a file onto the dropzone and verify it uploads.

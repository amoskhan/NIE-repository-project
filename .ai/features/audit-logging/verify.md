# Audit Logging — Verify

## Backend

```bash
dotnet build src/backend/AppTemplate.sln
dotnet run --project src/backend/API
```

The first run applies migrations (creating `AuditLogs` if absent). On subsequent runs the table should already exist and contain rows.

## Automatic audit smoke

```bash
SESSION=$(curl -s -X POST http://localhost:5001/api/Auth/CreateTestSession \
  -H "Content-Type: application/json" \
  -d '{"UserId":"alice"}' | jq -r .sessionToken)

# Make a change to a TimestampedEntity (Vendor inherits from TimestampedEntity)
curl -s -X POST http://localhost:5002/api/Vendor/Save \
  -H "Content-Type: application/json" \
  -H "X-Session-Id: $SESSION" \
  -d '{"name":"Audit Smoke Vendor","categoryName":"IT Services"}'

# Confirm an audit entry was written
psql "$DATABASE_URL" -c \
  "SELECT \"Action\", \"Category\", \"EntityName\", \"NewValues\"
   FROM \"AuditLogs\"
   WHERE \"EntityName\" = 'Vendor'
   ORDER BY \"Timestamp\" DESC LIMIT 1;"

# Expect a row with Action = 1 (Create), Category = 0 (Data), and JSON in NewValues
```

## Manual audit smoke

```bash
# Trigger an authorized denial (requires a non-admin user)
SESSION=$(curl -s -X POST http://localhost:5001/api/Auth/CreateTestSession \
  -H "Content-Type: application/json" \
  -d '{"UserId":"viewer-only"}' | jq -r .sessionToken)

# Hit a guarded endpoint without the grant
curl -s -o /dev/null -w "%{http_code}\n" \
  http://localhost:5002/api/AccessControl/GetOverview \
  -H "X-Session-Id: $SESSION"
# Expect 403

# Confirm the AccessDenied audit entry
psql "$DATABASE_URL" -c \
  "SELECT \"Action\", \"Category\", \"Outcome\", \"AdditionalData\"
   FROM \"AuditLogs\"
   WHERE \"Action\" = 28 AND \"UserId\" = 'viewer-only'
   ORDER BY \"Timestamp\" DESC LIMIT 1;"
# Expect Action = 28 (AccessDenied), Category = 2 (AccessControl), Outcome = 'Denied'
```

## Read API smoke

```bash
SESSION=$(... # Administrator user)

curl -s "http://localhost:5002/api/AuditLog/GetAuditLogs?pageNumber=1&pageSize=20&category=Data" \
  -H "X-Session-Id: $SESSION" | jq '.totalCount, .items | length'
# Expect: a number followed by 20 (or fewer)

curl -s "http://localhost:5002/api/AuditLog/GetAuditLogs?action=Login" \
  -H "X-Session-Id: $SESSION" | jq '.items[0]'
# Expect a row with action = "Login"
```

## TickerQ purge job

```bash
# Open the dashboard
open http://localhost:5002/tickerq/dashboard
# Find the AuditLogPurge function. Expect cron 0 0 2 * * * (daily 2 AM SGT).

# Trigger it manually from the dashboard.
# Watch the API logs for:
#   "Starting audit log purge job. Retention period: 6 months"
#   "Audit log purge completed. Total records deleted: <N>. Cutoff date: <YYYY-MM-DD>"

# Confirm the JobExecuted self-audit row
psql "$DATABASE_URL" -c \
  "SELECT \"EntityId\", \"DurationMs\", \"Outcome\", \"AdditionalData\"
   FROM \"AuditLogs\"
   WHERE \"Action\" = 52 AND \"EntityId\" = 'AuditLogPurge'
   ORDER BY \"Timestamp\" DESC LIMIT 1;"
# Expect Outcome = 'Success', AdditionalData like 'Deleted N records older than YYYY-MM-DD'
```

## Frontend (manual click-path)

1. Login as an Administrator user. Open `/audit`.
2. Confirm the page renders with the most recent rows first.
3. Filter by Category = `AccessControl` — only role / permission events should remain.
4. Click a row — the detail panel shows `OldValues`, `NewValues`, `ChangedProperties`, `AdditionalData` as collapsible JSON.
5. Filter by Action = `Login`. Confirm any logins from the smoke test appear.
6. Filter by Date Range = today. Confirm rows are bounded.
7. Confirm the page is hidden from the sidebar for a Viewer user (gated by `screen.audit.view`).

## Negative tests

```bash
# Viewer cannot read audit logs
SESSION=$(... # viewer)
curl -s -o /dev/null -w "%{http_code}\n" \
  http://localhost:5002/api/AuditLog/GetAuditLogs \
  -H "X-Session-Id: $SESSION"
# Expect 403

# The denial itself produces an AccessDenied row in AuditLogs (chain of evidence)
```

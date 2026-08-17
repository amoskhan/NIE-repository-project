# Authorization (Access Functions) — Verify

## Backend

```bash
dotnet build src/backend/AppTemplate.sln
dotnet run --project src/backend/API -- seed
dotnet run --project src/backend/API
```

After seed, the `AccessFunctions` table should contain one row per `AccessFunctionSeedDefinition` and `Roles` should contain four rows (Administrator, Operations Manager, Operations User, Read Only Viewer).

## API smoke

```bash
SESSION=$(curl -s -X POST http://localhost:5001/api/Auth/CreateTestSession \
  -H "Content-Type: application/json" \
  -d '{"UserId":"alice"}' | jq -r .sessionToken)

# Without any role assignment, GetCurrentAccessProfile returns empty arrays
curl -s http://localhost:5002/api/AccessControl/GetCurrentAccessProfile \
  -H "X-Session-Id: $SESSION" | jq

# Hit a guarded endpoint without the grant
curl -s -o /dev/null -w "%{http_code}\n" \
  http://localhost:5002/api/AuditLog/GetAuditLogs \
  -H "X-Session-Id: $SESSION"
# Expect: 403

# Assign Administrator
USER_ID=$(curl -s http://localhost:5002/api/AccessControl/GetCurrentAccessProfile \
  -H "X-Session-Id: $SESSION" | jq -r .userId)

curl -s -X POST http://localhost:5002/api/AccessControl/AssignRole \
  -H "X-Session-Id: $SESSION" \
  -H "Content-Type: application/json" \
  -d "{\"userId\":\"$USER_ID\",\"roleId\":1}"

# Re-fetch the profile — codes list should now include screen + api codes
curl -s http://localhost:5002/api/AccessControl/GetCurrentAccessProfile \
  -H "X-Session-Id: $SESSION" | jq .accessFunctionCodes

# The same guarded endpoint should now return 200
curl -s -o /dev/null -w "%{http_code}\n" \
  http://localhost:5002/api/AuditLog/GetAuditLogs \
  -H "X-Session-Id: $SESSION"
# Expect: 200
```

## Audit on denial

After a 403 response from a `[RequireAccessFunction]`-guarded endpoint, the `AuditLogs` table should contain a row with:

- `Action = AccessDenied` (28)
- `Category = AccessControl` (2)
- `Severity = Warning` (1)
- `Outcome = "Denied"`
- `AdditionalData` JSON containing `accessFunctionCode` and `requestPath`

```sql
SELECT "Action", "Category", "Outcome", "AdditionalData", "UserId", "RequestUrl"
FROM "AuditLogs"
WHERE "Action" = 28
ORDER BY "Timestamp" DESC
LIMIT 5;
```

## Frontend (manual click-path)

1. Login as a Viewer-role user.
2. Confirm the sidebar does NOT show "Audit" or "Access Control" (those are gated by `screen.audit.view` and `screen.access-control.view`).
3. Try to navigate to `/access-control` directly — the router guard should redirect to a `403` placeholder.
4. Login as an Administrator user.
5. Open `/access-control` — the page renders.
6. Open the Roles tab → pick "Operations User" → toggle off `api.procurement.order.manage` → save.
7. Logout / login as that user. Try to call `POST /api/PurchaseOrder/Save` — expect 403.
8. Confirm the audit log has a `PermissionUpdated` row from the role edit and an `AccessDenied` row from the rejected save.

## Cache invalidation

```bash
# Watch the Valkey cache key for the user
redis-cli -p 6379 GET "user_access_functions_alice"
# Should see a JSON array of codes after first call

# Toggle a role grant in the admin UI, then immediately re-check
redis-cli -p 6379 GET "user_access_functions_alice"
# Should be empty (invalidated by AccessControlController.UpdateRoleAccessFunctions)
```

## Catalog consistency

```bash
# Make sure no controller method references a code that no longer exists in the catalog
grep -r "AccessFunctionCodes\.Api\." src/backend/API/Controllers/
# Every match should resolve to a const declared in AccessFunctionCatalog.cs
```

## FE permission constants in sync

```bash
# All Screen / Api codes referenced in the FE constants file should match
grep -E "screen\.|api\." src/frontend/main/src/constants/permissions.ts
# Compare with grep output from AccessFunctionCatalog.cs — same codes, same casing
```

# Code Tables — Verify

## Backend

```bash
dotnet build src/backend/AppTemplate.sln
dotnet run --project src/backend/API -- seed
```

After seed, every `ECodeType` value should be represented in the `Codes` table:

```sql
SELECT "Type", count(*) FROM "Codes" GROUP BY "Type" ORDER BY "Type";
-- Expect rows for TITLE, USER_TYPE, VENDOR_CATEGORY, CATALOG_CATEGORY,
-- UNIT_OF_MEASURE, DELIVERY_LOCATION, CURRENCY (+ any project additions)
```

## API smoke

```bash
SESSION=$(curl -s -X POST http://localhost:5001/api/Auth/CreateTestSession \
  -H "Content-Type: application/json" \
  -d '{"UserId":"alice"}' | jq -r .sessionToken)

# Without the CodeRead access function — denied
curl -s -o /dev/null -w "%{http_code}\n" \
  "http://localhost:5002/api/Code/GetAllByCodeType?codeType=VENDOR_CATEGORY" \
  -H "X-Session-Id: $SESSION"
# Expect: 403 (because the dev session has no role yet)

# Assign Administrator (which carries CodeRead) and retry
curl -s -X POST http://localhost:5002/api/AccessControl/AssignRole \
  -H "Content-Type: application/json" \
  -H "X-Session-Id: $SESSION" \
  -d '{"userId":"alice","roleId":1}'

curl -s "http://localhost:5002/api/Code/GetAllByCodeType?codeType=VENDOR_CATEGORY" \
  -H "X-Session-Id: $SESSION" | jq
# Expect: array of { id, displayName, description, displayOrder, isActive }
# All entries IsActive == true, ordered by displayOrder ascending
```

## No string literals (BE)

```bash
# Find any hardcoded code Type strings in the BE outside the enum and seeder
grep -rn "VENDOR_CATEGORY\|CATALOG_CATEGORY\|UNIT_OF_MEASURE\|DELIVERY_LOCATION" \
  src/backend/ \
  --include="*.cs" \
  --exclude="ECodeType.cs" \
  --exclude="ECodeName.cs" \
  --exclude="DatabaseSeeder.cs"
# Expect: no matches (every reference should go through ECodeType.X.ToString())
```

## No string literals (FE)

```bash
# Find any hardcoded code Type strings in the FE outside CodeTableType
grep -rn '"VENDOR_CATEGORY"\|"CATALOG_CATEGORY"\|"UNIT_OF_MEASURE"' \
  src/frontend/main/src/ \
  --exclude="codeTableService.ts"
# Expect: no matches (every reference should go through CodeTableType.VendorCategory etc.)
```

## Cache behavior (FE)

In a browser DevTools console, on a logged-in page:

```js
// First call — hits the network
const a = await import("@/services/codeTableService").then((m) =>
  m.default.getByType("VENDOR_CATEGORY"),
);

// Second call — served from in-memory Map, no network call (check Network tab)
const b = await import("@/services/codeTableService").then((m) =>
  m.default.getByType("VENDOR_CATEGORY"),
);

// Force refresh — should fire a network request
const c = await import("@/services/codeTableService").then((m) =>
  m.default.getByType("VENDOR_CATEGORY", true),
);
```

## Composable behavior

```ts
// Inside a Vue component:
const { codeTableOptions, loadingCodeTables, loadCodeTableOptions } =
  useCodeTableOptions([CodeTableType.VendorCategory, CodeTableType.Currency]);

await loadCodeTableOptions();
// Expect: codeTableOptions.value.VENDOR_CATEGORY is an array
//         codeTableOptions.value.CURRENCY is an array
//         loadingCodeTables.value transitioned true → false
```

Both lookups load in parallel — open the Network tab and confirm two simultaneous requests fired.

## DB invariant

```sql
-- Type+Name pair must be unique
SELECT "Type", "Name", count(*) FROM "Codes" GROUP BY "Type", "Name" HAVING count(*) > 1;
-- Expect: 0 rows
```

## Negative tests

```bash
# Unknown type returns empty array (NOT 404)
curl -s "http://localhost:5002/api/Code/GetAllByCodeType?codeType=DOES_NOT_EXIST" \
  -H "X-Session-Id: $SESSION" | jq
# Expect: []

# Inactive rows are filtered
psql "$DATABASE_URL" -c \
  "UPDATE \"Codes\" SET \"IsActive\" = false WHERE \"Name\" = 'IT_SERVICES';"

curl -s "http://localhost:5002/api/Code/GetAllByCodeType?codeType=VENDOR_CATEGORY" \
  -H "X-Session-Id: $SESSION" | jq '.[].displayName'
# Expect: list does NOT include the row whose Name was IT_SERVICES

# Restore
psql "$DATABASE_URL" -c \
  "UPDATE \"Codes\" SET \"IsActive\" = true WHERE \"Name\" = 'IT_SERVICES';"
```

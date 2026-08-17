# Code Tables — Customize

## 1. Add a new code Type (e.g. `DEPARTMENT_CODE`)

1. Edit `src/backend/Libraries/Shared/Enum/ECodeType.cs` — append `DEPARTMENT_CODE,` (NOTE: append, do NOT reorder; the string-form is what's stored in the DB):
   ```csharp
   public enum ECodeType
   {
       TITLE,
       USER_TYPE,
       VENDOR_CATEGORY,
       CATALOG_CATEGORY,
       UNIT_OF_MEASURE,
       DELIVERY_LOCATION,
       CURRENCY,
       DEPARTMENT_CODE,   // ← new
   }
   ```
2. Edit `src/backend/Libraries/Shared/Enum/ECodeName.cs` — add a new comment block and append the names:
   ```csharp
   //Type - ECodeType.DEPARTMENT_CODE
   FINANCE_DEPT,
   HR_DEPT,
   IT_DEPT,
   ```
3. Edit `src/backend/API/Extensions/DatabaseSeeder.cs` `SeedCodesAsync` — add seed entries, e.g.:
   ```csharp
   new Code { Type = ECodeType.DEPARTMENT_CODE.ToString(), Name = ECodeName.FINANCE_DEPT.ToString(), DisplayName = "Finance", DisplayOrder = 10, IsActive = true },
   new Code { Type = ECodeType.DEPARTMENT_CODE.ToString(), Name = ECodeName.HR_DEPT.ToString(),      DisplayName = "Human Resources", DisplayOrder = 20, IsActive = true },
   new Code { Type = ECodeType.DEPARTMENT_CODE.ToString(), Name = ECodeName.IT_DEPT.ToString(),      DisplayName = "Information Technology", DisplayOrder = 30, IsActive = true },
   ```
4. Edit `src/frontend/main/src/services/codeTableService.ts` — add the FE mirror:
   ```ts
   export const CodeTableType = {
     // ... existing
     DepartmentCode: "DEPARTMENT_CODE",
   } as const;
   ```
5. Run `dotnet run --project src/backend/API -- seed`. Confirm rows appear:
   ```sql
   SELECT * FROM "Codes" WHERE "Type" = 'DEPARTMENT_CODE' ORDER BY "DisplayOrder";
   ```
6. Use it on a page:
   ```ts
   import { useCodeTableOptions } from "@/composables/useCodeTableOptions";
   import { CodeTableType } from "@/services/codeTableService";

   const { codeTableOptions, loadCodeTableOptions } = useCodeTableOptions([
     CodeTableType.DepartmentCode,
   ]);
   onMounted(() => loadCodeTableOptions());

   // template: <select v-for="opt in codeTableOptions[CodeTableType.DepartmentCode]">...
   ```

## 2. Add a new value to an existing Type

1. Add the symbol to `ECodeName.cs` under the right `//Type - ECodeType.X` block.
2. Add the seed row to `DatabaseSeeder.SeedCodesAsync`. The seeder is idempotent — running it again just upserts.
3. Restart the API (or run the seeder).
4. Force-refresh the FE cache:
   ```ts
   await codeTableService.getByType(CodeTableType.X, /* forceRefresh */ true);
   ```
   Or simply reload the browser tab; the in-memory `codeTableCache` Map clears on page reload.

## 3. Retire a code value safely

DO NOT delete the row. Set it inactive:

```sql
UPDATE "Codes" SET "IsActive" = false
WHERE "Type" = 'VENDOR_CATEGORY' AND "Name" = 'OUTDATED_CATEGORY';
```

The seeder respects the existing IsActive when reconciling (or, if your seeder always overwrites, change the deactivation strategy: keep the seed row but with `IsActive = false`). The query `GetAllByCodeTypeAsync` filters inactive rows; existing entities that already reference the `Name` retain their reference.

## 4. Rename a DisplayName

```sql
UPDATE "Codes" SET "DisplayName" = 'New Label'
WHERE "Type" = 'CURRENCY' AND "Name" = 'SGD';
```

Or update the seeder so the next `dotnet run -- seed` reconciles it.

Also clear the FE cache: every browser tab caches the result in-memory, so users see the old label until they refresh OR you call `getByType(type, true)` from the admin save flow.

## 5. Reorder dropdown options

Update `DisplayOrder` in `Codes`. The `CodeService` orders by it; the FE further sorts in `fetchByType`. No code change needed.

## 6. Render a code value's DisplayName from a stored Name

Lookups go FE-side: load the type once, then use the cached map.

```ts
const codes = await codeTableService.getByType(CodeTableType.VendorCategory);
const display =
  codes.find((c) => c.value === entity.categoryName)?.label ??
  entity.categoryName;
```

For server-side rendering in a service (e.g. PDF generation), inject `ICodeService` and call `GetAllByCodeTypeAsync` once at the top of the operation, then build a dictionary in C# memory.

## 7. Bulk import code rows from a spreadsheet

1. Convert the spreadsheet to a CSV with columns `Type, Name, DisplayName, DisplayOrder`.
2. Add a one-shot migration that calls `migrationBuilder.Sql("COPY \"Codes\" FROM ...")` (Postgres) or `SqlBulkCopy` (SQL Server).
3. After the import, also append the `Name` values to `ECodeName.cs` so future code can reference them by enum. This is mandatory — the "no string literals" rule still applies.

# Code Tables — Do and Don't

## DO ✅

1. **DO** reference `ECodeType.X.ToString()` and `ECodeName.Y.ToString()` everywhere a Code `Type` or `Name` string is needed. This is the **only** allowed source. The two enum files explicitly state this in a header comment.
2. **DO** add new code values by extending the enums first (BE), then mirroring `CodeTableType` (FE). Both enums are append-only — never reorder existing values, since the `.ToString()` is keyed off the name not the int.
3. **DO** group new `ECodeName` values under a `//Type - ECodeType.YYY` comment block in `ECodeName.cs`. The grouping is the human-readable map — without it future maintainers can't tell which Names belong to which Type.
4. **DO** seed every new value via `DatabaseSeeder.SeedCodesAsync`. The seeder uses upsert semantics, so re-running `dotnet run -- seed` reconciles new rows without breaking existing ones.
5. **DO** call `codeTableService.getByType(CodeTableType.X)` from FE pages. The FE-side cache deduplicates concurrent calls — you can call it from N child components and it will fire one HTTP request.
6. **DO** use `useCodeTableOptions([CodeTableType.A, CodeTableType.B, ...])` when a page needs multiple lookups. It loads them in parallel with `Promise.allSettled` and surfaces a single reactive `loading` flag.
7. **DO** filter inactive rows OUT on the BE side (`CodeService.GetAllByCodeTypeAsync` already does `Where(c => c.IsActive)`) AND defensively on the FE (`fetchByType` filters again). Rows that are inactive should never appear in dropdowns.
8. **DO** use `DisplayName` for user-facing rendering and `Name` (or the enum) for code paths. The mapping `Name → DisplayName` is the entire purpose of the table.
9. **DO** invalidate the FE cache (`codeTableService.getByType(type, true)`) after admin edits to a code-table type — otherwise the user sees stale dropdowns until reload.
10. **DO** keep `Code` extending `BaseEntity` (NOT `TimestampedEntity`). Code rows are reference data; full audit on every read/write would flood the audit log.

## DON'T ❌

1. **DON'T** ever hardcode a code Type string like `"VENDOR_CATEGORY"` in a controller, service, page, or composable. Reference `ECodeType.VENDOR_CATEGORY.ToString()`. If you find a violation in old code, fix it; if you can't find an enum value, add one — never paper over with a string literal.
2. **DON'T** ever hardcode a code Name string like `"IT_SERVICES"`. Same rule. If you need to compare a `Code.Name` value, do `code.Name == ECodeName.IT_SERVICES.ToString()`.
3. **DON'T** add a new lookup table for "vendor types v2" when the answer is "add a new ECodeType + seed rows". Code is the canonical lookup mechanism — proliferating tables defeats the design.
4. **DON'T** delete a row from `Codes` to "retire" a value. Set `IsActive = false`. Hard-deleting breaks historical references in entities that store the `Name` value (e.g. `Vendor.CategoryName`).
5. **DON'T** add custom columns to `Code`. The five-field shape is what makes one lookup mechanism serve every lookup; special-purpose fields (e.g. "currency_decimals") belong in a domain table, not on Code.
6. **DON'T** mix Types in one query. Each `GetAllByCodeType` call returns ONE type. The FE cache is keyed by type for the same reason.
7. **DON'T** rely on row IDs (`Code.Id`) in business logic — IDs are unstable across environments. Match on `(Type, Name)` instead.
8. **DON'T** seed display-name strings in source code outside `DatabaseSeeder.SeedCodesAsync` and the actual rendering call site. The Display column is the localization point; duplicating it weakens that contract.
9. **DON'T** put PII or environment-specific values in Codes. Codes are reference data, identical across dev / staging / prod.
10. **DON'T** call `codeTableService.getByType(...)` inside a tight loop or watcher. The cache makes repeat calls cheap, but a watcher firing on every keystroke still wastes a Map lookup. Hoist it into `onMounted` or a computed.

# Authorization (Access Functions) — Customize

## 1. Add a new screen-level access function (e.g. `screen.reports.export`)

1. Edit `src/backend/Libraries/Shared/Security/AccessFunctionCatalog.cs`:
   - Add the constant inside `AccessFunctionCodes.Screen`:
     ```csharp
     public const string ReportsExport = "screen.reports.export";
     ```
   - Add a `new AccessFunctionSeedDefinition(...)` row inside `AccessFunctionCatalog.AccessFunctions` with `Type = EAccessFunctionType.Screen`, `Module = "Reporting"`, `Route = "/reports/export"`, `HttpMethod = null`, a unique `DisplayOrder` (next free integer in that module).
2. Decide which roles get it. Add the constant to `RoleSeedDefinition.AccessFunctionCodes` for each role that should have it inside `AccessFunctionCatalog.Roles` (typically Administrator + the relevant operations role).
3. Run `dotnet run --project src/backend/API -- seed`. The `DatabaseSeeder.SeedAccessFunctionsAsync` upsert reconciles the new row.
4. Edit `src/frontend/main/src/composables/usePermissions.ts` — add the new code to the `PRIMARY_NAV_ITEMS` (or admin nav) entry that should be gated by it.
5. Edit `src/frontend/main/src/router/index.ts` — add `meta: { requiredAccessFunction: 'screen.reports.export' }` to the route. The router guard in the same file consumes this meta.
6. Edit `src/frontend/main/src/constants/permissions.ts` — add the FE constant alias matching the new code so other FE files can import it.

## 2. Add a new API access function (e.g. `api.reports.read`)

1. Edit `AccessFunctionCatalog.cs` — add to `AccessFunctionCodes.Api` and to the `AccessFunctions` seed list with `Type = EAccessFunctionType.Api`, `Route = "/api/Reports"`, `HttpMethod = "GET"`.
2. Add to the role bundles in `AccessFunctionCatalog.Roles` that should have it.
3. In the controller method:
   ```csharp
   [HttpGet]
   [RequireAccessFunction(AccessFunctionCodes.Api.ReportsRead)]
   public async Task<IActionResult> GetReports() { ... }
   ```
4. Re-run `dotnet run -- seed` (or restart the API — the seeder runs automatically on first launch in many environments; check `Program.cs:200-206`).

## 3. Add a brand-new Role (e.g. `FINANCE_REVIEWER`)

1. Edit `src/backend/Libraries/Shared/Enum/ERole.cs` — add `FinanceReviewer = 5` (use the next free numeric ID — these IDs are stable PKs in the seed data).
2. Edit `AccessFunctionCatalog.Roles` — append a new `RoleSeedDefinition` with that ID, a UPPER_SNAKE `Code`, a display `Name`, the `DisplayOrder`, and the bundle of `AccessFunctionCodes.*` it gets.
3. Run `dotnet run -- seed`.
4. Edit `src/frontend/main/src/composables/usePermissions.ts` — add the role code to `userRoleLabel` mapping (UI label only — does NOT affect auth).
5. Optional: add a default localized label in `src/frontend/packages/shared/src/i18n/`.

## 4. Bulk-replace the access functions granted to a role at runtime

This is the supported live-edit path (the seeder handles boot-time defaults).

1. Open the Access Control admin page (`/access-control`).
2. Open the role detail panel.
3. Toggle access functions on/off — under the hood the page calls `PUT /api/AccessControl/UpdateRoleAccessFunctions` (`AccessControlController.UpdateRoleAccessFunctions`), which:
   - Replaces all `RoleAccessFunction` rows for that role
   - Calls `IAuditLogger.LogRoleAccessChangedAsync`
   - Invalidates the Valkey cache for every user holding that role (see `AccessControlController` for the invalidation call after the update)

## 5. Make the cache TTL longer / shorter for `user_access_functions_{userId}`

1. Open `src/backend/Libraries/Services/Services/AccessFunction/AccessFunctionService.cs`.
2. Locate the `DistributedCacheEntryOptions` near `_cache.SetStringAsync(cacheKey, ...)` in `GetUserAccessFunctionCodesAsync` (~line 80).
3. Adjust `SetAbsoluteExpiration(TimeSpan.From...)` to your new value. Default is short (a few minutes) so that role changes propagate quickly.
4. Be aware that a longer TTL means slower propagation of role/access changes — combine with explicit invalidation in `AccessControlController` if you go above 5 minutes.

## 6. Decide what happens when an endpoint has NO `[RequireAccessFunction]`

By design, an unguarded endpoint is implicitly open to any authenticated session (the `SessionValidationMiddleware` still runs). If you want to deny anonymous + unguarded access globally, add a global authorization filter in `src/backend/API/Program.cs` near `builder.Services.AddControllers()`:

```csharp
builder.Services.AddControllers(options =>
{
    options.Filters.Add(new RequireAccessFunctionAttribute()); // empty = always deny
});
```

But the project's convention is the opposite: explicit opt-in per endpoint. Document it in `agents/NNN-implicit-open.md` if you flip this.

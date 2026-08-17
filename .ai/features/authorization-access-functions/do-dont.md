# Authorization (Access Functions) — Do and Don't

## DO ✅

1. **DO** declare every protected endpoint with `[RequireAccessFunction(AccessFunctionCodes.Api.XxxYyy)]` on the controller method (or class). The attribute is the only authorization gate the project respects.
2. **DO** add new codes to `AccessFunctionCodes.Screen` or `AccessFunctionCodes.Api` in `AccessFunctionCatalog.cs` BEFORE writing the controller method that uses it. The compiler will then catch typos.
3. **DO** add a matching `AccessFunctionSeedDefinition` row in `AccessFunctionCatalog.AccessFunctions` for every new code, including its `Module`, `Type`, `ResourceName`, `Route`, `HttpMethod`, `Description`, and `DisplayOrder`. The admin UI groups by `Module` and orders by `DisplayOrder`.
4. **DO** prefer multiple narrow codes over one broad code — `api.audit-log.read` and `api.audit-log.purge` are better than `api.audit-log.full`. It costs nothing and lets ops grant least-privilege.
5. **DO** use `[RequireAccessFunction(code1, code2)]` (variadic) when an endpoint should allow EITHER of two grants — the attribute is OR-semantic.
6. **DO** name codes in `dot.case` with the `screen.` or `api.` prefix and a domain segment: `api.<module>.<resource>.<verb>` (`api.procurement.order.approve`). Stick to lowercase; the attribute compares with `StringComparer.OrdinalIgnoreCase` but consistent casing keeps grep clean.
7. **DO** drive the FE sidebar / admin nav from `Screen.*` codes via `usePermissions.ts`. A page should only render in the menu if `hasAccessFunction(AccessFunctionCodes.Screen.XxxView)`.
8. **DO** assign new access functions to seeded roles in `AccessFunctionCatalog.Roles` so dev / test / staging come up with sensible defaults. `RoleSeedDefinition.AccessFunctionCodes` is the seeded grant list.
9. **DO** rely on the Valkey cache `user_access_functions_{userId}` populated by `AccessFunctionService.GetUserAccessFunctionCodesAsync` — invalidate it ONLY in the role-update controllers (after `UpdateRoleAccessFunctions`, `AssignRole`, `RemoveAssignment`).
10. **DO** audit denials — `RequireAccessFunctionAttribute.OnAuthorizationAsync` already calls `IAuditLogger.LogAccessDeniedAsync`. Don't replicate that in your service; trust the attribute.

## DON'T ❌

1. **DON'T** add a `RolePermission` table or any kind of permission entity. The model is **Role ↔ RoleAccessFunction ↔ AccessFunction** — and that's it. Adding a new join entity recreates the very design we replaced.
2. **DON'T** infer authorization from controller name + action name (e.g. "if action contains 'Delete' require admin"). The original template had this and it was removed deliberately. Every endpoint MUST opt into authorization explicitly via `[RequireAccessFunction]`.
3. **DON'T** use ASP.NET `[Authorize(Roles = "...")]` or `[Authorize(Policy = "...")]`. Roles are a UI grouping concept here; authorization is keyed off access function codes.
4. **DON'T** hardcode an access function code as a string literal anywhere outside `AccessFunctionCatalog.cs`. Always reference the constant: `AccessFunctionCodes.Api.AuditRead`.
5. **DON'T** read roles from the session payload — they aren't there, by design (see `authentication/do-dont.md` rule 2). Roles flow through `UserRole` rows in PostgreSQL; the FE asks `/api/AccessControl/GetCurrentAccessProfile` after session boot.
6. **DON'T** check `currentUser.value.roles.includes("Admin")` in a Vue component for gating UI. Use `hasAccessFunction(AccessFunctionCodes.Screen.AccessControlView)` — role labels are display-only.
7. **DON'T** call `IAccessFunctionService.HasAccessAsync(...)` from inside a controller method to "double-check" — the attribute already did the work and audited the failure path. Re-checking duplicates the audit row.
8. **DON'T** seed access functions in random places — they belong in `AccessFunctionCatalog`, picked up by `DatabaseSeeder.SeedAccessFunctionsAsync`. `DbContext.OnModelCreating(...).HasData(...)` is acceptable for the very first migration but the seeder pattern is preferred for additions.
9. **DON'T** mutate `RoleAccessFunctions` directly via raw SQL — go through `RoleService.UpdateRoleAccessFunctionsAsync` so the audit trail (`AuditLogger.LogRoleAccessChangedAsync`) is written.
10. **DON'T** forget to invalidate `user_access_functions_{userId}` after `AssignRole` / `RemoveAssignment` / `UpdateRoleAccessFunctions`. Failing to do so means the user's grants stay stale until their session next refreshes — a confusing bug for admins.

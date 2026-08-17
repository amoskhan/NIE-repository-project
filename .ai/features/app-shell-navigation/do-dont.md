# App Shell & Navigation — Do and Don't

The shell is **template-owned**; its data is **project-owned**. The whole point is
copy-paste inheritance: a derived repo pastes a newer shell file over its own and keeps
its `app-config/*`. That only holds if the rules below are followed.

## DO ✅

1. **DO** add or remove sidebar items in `src/frontend/main/src/app-config/navigation.ts` (`PRIMARY_NAV_ITEMS` / `ADMIN_NAV_ITEMS`).
2. **DO** add or remove routes in `src/frontend/main/src/app-config/routes.ts` — `PROJECT_ROUTES` for always-present pages, `OPTIONAL_ROUTES` for pages that a derived repo might delete.
3. **DO** add/rename access-function codes, UI permissions, role→permission maps, and role display labels in `src/frontend/main/src/app-config/accessFunctions.ts` — and keep them in lock-step with the backend `AccessFunctionCatalog.cs`.
4. **DO** rebrand by editing `theme/appTheme.ts` (`brandLabel`, the product name) and `app-config/branding.ts` (`BRAND_LOGO`, `FEEDBACK_FUNCTION_PREFIX`).
5. **DO** gate a menu item with `permission` (one code) or `permissions` (any-of) so it only shows for authorized users — the shell hides unauthorized items automatically.
6. **DO** keep `OPTIONAL_ROUTES.pagePath` a `../`-relative string literal (e.g. `"../pages/reports/ReportsIndex.vue"`). The router's `import.meta.glob` matches literal keys; a non-literal or `@/`-aliased value silently fails to resolve.
7. **DO** copy a newer shell file from the template verbatim to inherit an upstream improvement — your `app-config/*` is untouched because the shell holds no project data.

## DON'T ❌

1. **DON'T** add a menu item, route, access code, or brand string by editing `StaffLayout.vue`, `useSidebar.ts`, `usePermissions.ts`, `router/index.ts`, or `constants/permissions.ts`. These are LOCKED — edits here are overwritten on the next template sync.
2. **DON'T** hardcode the product name or logo in `StaffLayout.vue`. The brand label comes from `theme/appTheme.ts` via `useTheme().brandLabel`; the logo from `app-config/branding.ts`.
3. **DON'T** move `import.meta.glob` out of `router/index.ts`. Its keys are build-time literals resolved relative to `src/router/`; relocating it changes the base and breaks optional-page resolution.
4. **DON'T** make `resolvePermissions` read project data directly. It takes the maps as a parameter; callers inject `LEGACY_ROLE_PERMISSIONS` + `ACCESS_FUNCTION_PERMISSION_MAP` from `app-config/accessFunctions.ts`.
5. **DON'T** rename a route `name` without updating every `navigation.ts` item that targets it (`route` / `activeRoutes`) — the sidebar active-state matches on route name.
6. **DON'T** change `meta.permission` (scalar) to `meta.permissions` (array) or vice-versa without matching the guard's expectation — both are honored, but the key must match the intent.
7. **DON'T** put feature/business logic in the shell. Pages live in `staff/pages/**`; the shell only frames them.

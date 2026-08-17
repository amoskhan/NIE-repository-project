# App Shell & Navigation — File Map

## Owned files — LOCKED (template-owned shell; inherit by copy-paste)

| Path                                                  | Layer       | Purpose                                                                                                                                                                                                                    |
| ----------------------------------------------------- | ----------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `src/frontend/main/src/staff/layouts/StaffLayout.vue` | Shell       | Sidebar + topbar + notifications + profile menu. Renders `navItems`/`adminNavItems`; reads brand label from the theme and logo/feedback-prefix from `app-config/branding.ts`. Holds **no** menu/route/code/brand literals. |
| `src/frontend/main/src/composables/useSidebar.ts`     | Shell state | Responsive expand/collapse, breakpoints, mobile drawer state.                                                                                                                                                              |
| `src/frontend/main/src/composables/usePermissions.ts` | Shell logic | `navItems`/`adminNavItems` (filtered from `app-config/navigation.ts`), `hasPermission`, `userRoleLabel`. Imports role-label data from `app-config/accessFunctions.ts`.                                                     |
| `src/frontend/main/src/composables/navTypes.ts`       | Types       | The `NavItem` interface (re-exported from `usePermissions.ts` for back-compat).                                                                                                                                            |
| `src/frontend/main/src/router/index.ts`               | Router      | Mounts `StaffLayout`, applies the `meta.permission(s)` guard, resolves `OPTIONAL_ROUTES` via `import.meta.glob`, sets `document.title` from the theme brand. Routes come from `app-config/routes.ts`.                      |
| `src/frontend/main/src/constants/permissions.ts`      | Resolver    | `resolvePermissions(user, maps)` + `NestedValues`/`PermissionUser`/`PermissionResolutionMaps` types. No project codes.                                                                                                     |

## Owned files — PROJECT-OWNED (the only place to edit for nav/brand)

| Path                                                  | Layer  | Purpose                                                                                                                                                                            |
| ----------------------------------------------------- | ------ | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `src/frontend/main/src/app-config/navigation.ts`      | Config | `PRIMARY_NAV_ITEMS`, `ADMIN_NAV_ITEMS` — the sidebar menu.                                                                                                                         |
| `src/frontend/main/src/app-config/routes.ts`          | Config | `PROJECT_ROUTES` (always-present routes) + `OPTIONAL_ROUTES` (pages that may be removed in a derived repo).                                                                        |
| `src/frontend/main/src/app-config/accessFunctions.ts` | Config | `AccessFunctionCode`, `UiPermission`, permission bundles, `LEGACY_ROLE_PERMISSIONS`, `ACCESS_FUNCTION_PERMISSION_MAP`, role labels. Mirror of the backend `AccessFunctionCatalog`. |
| `src/frontend/main/src/app-config/branding.ts`        | Config | `BRAND_LOGO` (logo asset), `FEEDBACK_FUNCTION_PREFIX`.                                                                                                                             |
| `src/frontend/main/src/theme/appTheme.ts`             | Config | `brandLabel` (product name shown in the shell + document title).                                                                                                                   |

## Touched files

| Path                                                                  | What it contains                                                 | Why                                                                        |
| --------------------------------------------------------------------- | ---------------------------------------------------------------- | -------------------------------------------------------------------------- |
| `src/frontend/main/src/main.ts`                                       | `initTheme(mainThemeConfig)` then mounts the router              | Theme runtime (incl. `brandLabel`) is initialized before the shell renders |
| `src/frontend/main/src/composables/useTheme.ts`                       | Re-exposes `brandLabel` from the `@apptemplate/ui` theme runtime | Lets `StaffLayout.vue` show the brand reactively                           |
| `src/frontend/packages/ui` (`AppLaunchpadProfileMenu`, theme runtime) | Profile-menu component + `brandLabel` ref                        | The shell composes these; owned by the `ui-component-library` feature      |

## Migrations

None — frontend only.

## External dependencies

| Package               | Purpose                                                 |
| --------------------- | ------------------------------------------------------- |
| `vue-router`          | Routing + navigation guard                              |
| `js-cookie`           | Reads the session/user cookie in the guard              |
| `@apptemplate/ui`     | `AppLaunchpadProfileMenu`, theme runtime (`brandLabel`) |
| `@apptemplate/shared` | `FRONTEND_CONSTANTS` (cookie names)                     |

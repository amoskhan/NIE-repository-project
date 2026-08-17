# App Shell & Navigation — Customize

Every customization below happens in `src/frontend/main/src/app-config/*` (or
`theme/appTheme.ts` for the brand label). **Never edit the locked shell files**
(`StaffLayout.vue`, `useSidebar.ts`, `usePermissions.ts`, `router/index.ts`,
`constants/permissions.ts`) — copy newer versions over yours to inherit shell
improvements. See `.ai/common/11-customization-boundary.md`.

## 1. Add a sidebar menu item

1. Add the route first (see recipe 2) so the item has somewhere to go.
2. Edit `app-config/navigation.ts`. Append to `PRIMARY_NAV_ITEMS` (main nav) or
   `ADMIN_NAV_ITEMS` (the "Administration" group):
   ```ts
   {
     name: "Invoices",
     icon: "receipt_long",          // Material Symbols name
     route: "invoices",             // must equal a route `name` in routes.ts
     activeRoutes: ["invoices", "invoice-detail"], // optional: keep item active on detail pages
     permissions: [...REPORT_PERMISSIONS],          // optional gate (any-of); omit to always show
   },
   ```
3. To gate by a single code use `permission: AccessFunctionCode.Api.SomeCode` instead of
   `permissions`. Items the user can't access are hidden automatically.

## 2. Add a route

Edit `app-config/routes.ts`.

- **Always-present page** → add a `RouteRecordRaw` to `PROJECT_ROUTES`:

  ```ts
  {
    path: "invoices",
    name: "invoices",
    component: () => import("@/staff/pages/staff/Invoices.vue"),
    meta: { title: "Invoices", permissions: [...REPORT_PERMISSIONS] },
  },
  ```

  `meta.title` sets the topbar/document title; `meta.permission` (scalar) or
  `meta.permissions` (any-of array) gates the route — unauthorized users are redirected
  to `dashboard`.

- **Optionally-present page** (one a derived repo might delete) → add an
  `OptionalRouteDescriptor` to `OPTIONAL_ROUTES`:
  ```ts
  {
    path: "invoices",
    name: "invoices",
    pagePath: "../staff/pages/staff/Invoices.vue", // "../"-relative LITERAL (matched by import.meta.glob)
    title: "Invoices",
    meta: { permissions: [...REPORT_PERMISSIONS] },
  },
  ```
  The router only registers it if the `.vue` file still exists, so removing the page
  won't break the build.

## 3. Add or rename an access-function code / role label

Edit `app-config/accessFunctions.ts`.

1. Add the code under `AccessFunctionCode.Screen` or `.Api` (keep it in lock-step with
   the backend `src/backend/Libraries/Shared/Security/AccessFunctionCatalog.cs`).
2. If it should imply a UI permission, extend `ACCESS_FUNCTION_PERMISSION_MAP`.
3. For the legacy role-based fallback, extend `LEGACY_ROLE_PERMISSIONS`.
4. To change how a role is labelled in the profile menu, edit `ROLE_LABELS` (and, if
   needed, `ADMIN_ROLE_LABEL` / `AUDITOR_ROLE_LABEL` / `DEFAULT_ROLE_LABEL`).
5. Reference the new code in `navigation.ts` / `routes.ts` gates as needed.

## 4. Rebrand (product name, logo, feedback namespace)

1. **Product name** (sidebar text + `document.title`) → `theme/appTheme.ts`, set
   `brandLabel: "Your Project"`. It flows through `useTheme().brandLabel`.
2. **Logo** → drop your asset in `src/assets/` and point `BRAND_LOGO` in
   `app-config/branding.ts` at it (swap the `import`).
3. **Feedback namespace** → set `FEEDBACK_FUNCTION_PREFIX` in `app-config/branding.ts`
   to your module key (used as `${prefix}.<route-name>` for the feedback widget).
4. **Auth screen brand** → `src/frontend/auth/src/theme/appTheme.ts` (`brandLabel`).

## 5. Verify

Run the checks in [`verify.md`](./verify.md): type-check, build, and a grep proving the
shell files contain no nav/brand/code literals.

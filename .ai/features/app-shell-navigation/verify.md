# App Shell & Navigation — Verify

## Build-time

From `src/frontend`:

1. **Type-check** (catches broken imports, the injected `resolvePermissions(user, maps)`
   signature, and the moved `as const` unions):
   ```bash
   pnpm --filter main type-check
   ```
2. **Build** (proves `import.meta.glob` still resolves the `OPTIONAL_ROUTES` page literals
   and that route chunks still split):
   ```bash
   pnpm --filter main build:production
   ```
   In the chunk list you should see the optional pages emitted when present
   (`ChatView-*.js`, `ReportsIndex`/`ReportDetail-*.js`).

## The shell is data-free (the core invariant)

These greps MUST return nothing — project data must not live in the locked shell:

```bash
# No nav arrays inside the composable:
grep -n "PRIMARY_NAV_ITEMS *= *\[" src/frontend/main/src/composables/usePermissions.ts

# No hardcoded brand or feedback namespace inside the layout:
grep -nE "App Template|procurement\.|app-logo\.svg" src/frontend/main/src/staff/layouts/StaffLayout.vue

# No access-code constants inside the resolver:
grep -n "AccessFunctionCode" src/frontend/main/src/constants/permissions.ts
```

The project data MUST exist in `app-config/`:

```bash
test -f src/frontend/main/src/app-config/navigation.ts
test -f src/frontend/main/src/app-config/routes.ts
test -f src/frontend/main/src/app-config/accessFunctions.ts
test -f src/frontend/main/src/app-config/branding.ts
```

## Runtime (services running — see `tests/README.md`)

1. Sign in and load `#/`, `#/vendors`, and an admin route (`#/users`). The sidebar
   renders, the active item highlights, and the brand label + logo show.
2. Visit a permission-gated route without the permission → redirected to `dashboard`.
3. Visit an `OPTIONAL_ROUTES` page (`#/reports`, `#/chat`) → loads when the page exists,
   and is skipped without error when the project has deleted it.
4. The page title (`document.title`) reads `<Page> | <brandLabel>`.

## Expected UI states

- **No session** → the guard redirects to the auth login URL before any shell renders.
- **Authorized** → sidebar shows only the items whose `permission(s)` the user holds; the
  "Administration" group appears only when `adminNavItems` is non-empty.
- **Mobile (<768px)** → sidebar collapses to the drawer; swipe-right opens it.

## Derived-repo smoke (the inheritance guarantee)

Add a menu item by editing **only** `app-config/navigation.ts` and confirm it appears
without touching any locked shell file. Then overwrite a locked shell file with the
template's newer copy and confirm the menu item survives (it lives in `app-config/`).

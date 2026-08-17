# UI Component Library — Verify

## Workspace install

```bash
pnpm install
# Expect: no peer-dep warnings about @apptemplate/ui
```

## Type check the package

```bash
pnpm --filter @apptemplate/ui type-check
# Expect: no errors
```

## Build the package

```bash
pnpm --filter @apptemplate/ui build
# Expect:
#   - vite build succeeds
#   - vue-tsc emits *.d.ts files into dist/
```

## Type check both consumer apps

```bash
pnpm --filter main type-check
pnpm --filter auth type-check
# Expect: no missing-symbol errors against @apptemplate/ui
```

## Confirm the barrel exports every component

```bash
# Count App* components vs barrel exports
find src/frontend/packages/ui/src/components -name "App*.vue" | wc -l
# Expect: 27 (9 primitives + 13 composites + 5 theme = 27)

grep -c "^export" src/frontend/packages/ui/src/index.ts
# Expect: at least matching count (some exports are wildcard / utility lines)
```

## Visual smoke (main app)

1. Start the stack: `🚀 All Services (Hot Reload)`.
2. Login at `http://localhost:8001`.
3. Open the admin dashboard at `http://localhost:8002`.
4. Visit each page that exercises a key composite:
   - Audit Log (`/audit`) — `AppDataTable`, `AppPagination`, `AppFilterBar`
   - Access Control (`/access-control`) — `AppDataTable`, `AppModal`, `AppConfirmDialog`
   - PO Detail — `AppFileUploadField`, `AppStatePanel`
   - Profile menu (top-right corner) — `AppLaunchpadProfileMenu`
5. Confirm all components render without console errors.

## Tailwind content glob

```bash
# Confirm the consumer app's Tailwind config includes the UI lib source
grep -A 5 "content:" src/frontend/main/tailwind.config.js
# Expect: glob covering "node_modules/@apptemplate/ui/src/**/*.{vue,ts}"
# (or a workspace-relative path if pnpm uses linked workspace packages)
```

## Style import

```bash
# Confirm the global stylesheet is loaded once per app
grep -rn "@apptemplate/ui/styles" src/frontend/main/src src/frontend/auth/src
# Expect: exactly one match per app, in main.ts
```

## Component prop contract

Pick a component (e.g. `AppButton`) and confirm props compile:

```vue
<script setup lang="ts">
import { AppButton } from "@apptemplate/ui";
</script>

<template>
  <AppButton variant="primary" size="md" :loading="false" @click="onClick"
    >Click</AppButton
  >
</template>
```

`pnpm type-check` should pass.

## Theme switching

1. The `useTheme` composable in `src/frontend/main/src/composables/useTheme.ts` toggles theme tokens.
2. Switch theme via the profile menu's theme switcher.
3. Confirm CSS variables in `:root` change (DevTools → Elements → :root).
4. Confirm primitives like `AppButton` reflect the new accent color.

## No business logic in lib

```bash
# Confirm no API calls or BE-specific imports inside the lib
grep -rn "from.*services/\|api\.\(get\|post\|put\|delete\)" \
  src/frontend/packages/ui/src
# Expect: no matches
```

## Source-only export check

```bash
# Confirm the package can be consumed without a dist/ build
rm -rf src/frontend/packages/ui/dist
pnpm --filter main type-check
# Expect: no errors. The consumer compiles the lib's source via TS.
```

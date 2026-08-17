# Shared Utilities — Do and Don't

## DO ✅

1. **DO** put a TypeScript type in `@apptemplate/shared/types` if it's used by BOTH apps (main + auth). If it's app-specific, keep it inside the app (`src/frontend/main/src/types`).
2. **DO** mirror BE DTO shapes here when the FE consumes them. `Role`, `User`, `ApiResponse<T>` are the canonical patterns.
3. **DO** use `useApi` from `@apptemplate/shared/composables` as the base for new FE service modules. It centralizes axios config, base URL, and 401 redirect behavior.
4. **DO** keep i18n keys in JSON files under `src/i18n/locales/`. Never inline string literals — `t('common.save')` is the contract.
5. **DO** add a new `SupportedLocale` member when introducing a new translation, AND drop the matching `locales/<code>.json` file at the same time.
6. **DO** keep the package source-only (the package.json declares no `dist` build; `tsc --noEmit` is the script). Consumers compile through TS.
7. **DO** put cross-cutting composables here (`useApi`, `useAuth`, `useFormValidation`). App-specific composables (e.g. `usePermissions`, `useSignalR`) belong in the app's `src/composables/` folder.
8. **DO** export each subpath separately (`./composables`, `./utils`, `./types`, `./i18n`) so consumers can tree-shake.
9. **DO** use the `@apptemplate/shared` shared `useAuth` only as a baseline. The main app's `useAuth.ts` (in `src/frontend/main/src/composables/`) is the project-aware one (roles, permissions, OneSignal cleanup) — do NOT push project-specific behavior down into the shared composable.
10. **DO** keep zod schemas alongside the form they validate (in the page component) and use `useFormValidation` to wire them. The composable expects a zod schema as input.

## DON'T ❌

1. **DON'T** put .vue components here. The split is intentional: visible components live in `@apptemplate/ui`. Use the empty `components/` stub only when adding cross-app **logic**-only components (rare).
2. **DON'T** add Tailwind classes or CSS imports to this package. It's pure TS / composables / data. Styles belong in `@apptemplate/ui` or in consumer apps.
3. **DON'T** put `@/`-aliased imports inside the package. Use relative paths only — apps have their own `@/` aliases that don't apply here.
4. **DON'T** put project-specific types here (e.g. `PurchaseOrder`). Keep them in the app or in a project-specific shared package fork.
5. **DON'T** import `axios` directly in pages. Wrap it via `useApi` so 401 handling, base URL, and retry semantics are consistent.
6. **DON'T** mutate the i18n instance globally (e.g. `i18n.global.locale.value = "fr"`). Use `setLocale("fr")` so persisted preferences and lazy locale loading stay correct.
7. **DON'T** export internal helpers from the barrel. If a function is used only inside `useFormValidation`, scope it to that file.
8. **DON'T** add npm packages here without checking the consumer apps already have them. The shared package's deps are part of the consumer dep graph; bloated deps slow every install.
9. **DON'T** rename a type that the BE also publishes (e.g. `User`). Renaming creates a divergence between FE and BE DTOs that audit-logging dashboards rely on.
10. **DON'T** publish to npm. The package is `private: true` — internal consumption only.

# Shared Utilities — File Map

## Owned files

### Package metadata

| Path                                        | Layer    | Purpose                                                                                     |
| ------------------------------------------- | -------- | ------------------------------------------------------------------------------------------- |
| `src/frontend/packages/shared/package.json` | Manifest | `private: true`, `name: "@apptemplate/shared"`, source-only export, five subpath exports    |
| `src/frontend/packages/shared/src/index.ts` | Barrel   | Re-exports all subpaths and the i18n named exports (`i18n`, `setLocale`, `SupportedLocale`) |

### Composables

| Path                                                                | Layer      | Purpose                                                                                                                                                                                                                 |
| ------------------------------------------------------------------- | ---------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `src/frontend/packages/shared/src/composables/index.ts`             | Barrel     | Re-exports the three composables                                                                                                                                                                                        |
| `src/frontend/packages/shared/src/composables/useApi.ts`            | Composable | Axios instance helper / wrapper used as the base for FE service modules                                                                                                                                                 |
| `src/frontend/packages/shared/src/composables/useAuth.ts`           | Composable | Auth primitive (cookie-driven gate). NOTE: the main app has its own `useAuth.ts` in `src/frontend/main/src/composables/useAuth.ts` that is RICHER (roles, permissions). This one is the shared base used by the auth FE |
| `src/frontend/packages/shared/src/composables/useFormValidation.ts` | Composable | Zod-backed form validation primitive                                                                                                                                                                                    |

### Utilities

| Path                                               | Layer   | Purpose                                                                                          |
| -------------------------------------------------- | ------- | ------------------------------------------------------------------------------------------------ |
| `src/frontend/packages/shared/src/utils/index.ts`  | Barrel  | Re-exports utility functions                                                                     |
| `src/frontend/packages/shared/src/utils/format.ts` | Utility | Currency, date, number formatters (locale-aware via `Intl.NumberFormat` / `Intl.DateTimeFormat`) |

### Types

| Path                                              | Layer  | Purpose                                                 |
| ------------------------------------------------- | ------ | ------------------------------------------------------- |
| `src/frontend/packages/shared/src/types/index.ts` | Barrel | Re-exports all type modules                             |
| `src/frontend/packages/shared/src/types/api.ts`   | Type   | `ApiResponse<T>`, paged result, error shape             |
| `src/frontend/packages/shared/src/types/role.ts`  | Type   | `Role`, `RoleAccessFunction` shapes (mirror of BE DTOs) |
| `src/frontend/packages/shared/src/types/user.ts`  | Type   | `User`, `UserRole` shapes                               |

### i18n

| Path                                             | Layer       | Purpose                                                                                            |
| ------------------------------------------------ | ----------- | -------------------------------------------------------------------------------------------------- |
| `src/frontend/packages/shared/src/i18n/index.ts` | i18n        | `createI18n` instance, `setLocale(locale)`, `SupportedLocale` union type, eager-imports `locales/` |
| `src/frontend/packages/shared/src/i18n/locales/` | Locale JSON | Translation bundles per locale                                                                     |

### Components stub

| Path                                                   | Layer  | Purpose                                                             |
| ------------------------------------------------------ | ------ | ------------------------------------------------------------------- |
| `src/frontend/packages/shared/src/components/index.ts` | Barrel | Empty barrel reserved for future .vue components shared across apps |

## Touched files

| Path                                           | What it contains                                                                     | Why must be touched                                |
| ---------------------------------------------- | ------------------------------------------------------------------------------------ | -------------------------------------------------- |
| `src/frontend/main/package.json`               | `"@apptemplate/shared": "workspace:*"`                                               | Required for the main app to import                |
| `src/frontend/auth/package.json`               | `"@apptemplate/shared": "workspace:*"`                                               | Required for the auth app to import                |
| `src/frontend/main/src/main.ts`                | `import { i18n } from "@apptemplate/shared"; app.use(i18n);`                         | Wires i18n into the main app                       |
| `src/frontend/auth/src/main.ts`                | Same wiring                                                                          | Wires i18n into the auth app                       |
| `src/frontend/pnpm-workspace.yaml`             | Includes `packages/shared`                                                           | Required for `workspace:*` resolution              |
| `src/frontend/main/tsconfig.json` (and auth's) | `paths` resolution may include `@apptemplate/shared` (or rely on workspace symlinks) | Required so TypeScript finds the source-only types |

## Migrations

None — pure FE.

## External dependencies

| Package          | Purpose                                    |
| ---------------- | ------------------------------------------ |
| `vue` (peer-ish) | Vue 3 reactive primitives                  |
| `vue-i18n`       | `createI18n`, t(), locale management       |
| `vue-router`     | Router types used by some composables      |
| `axios`          | HTTP client behind `useApi`                |
| `js-cookie`      | Cookie reading inside `useAuth`            |
| `zod`            | Schema validation for `useFormValidation`  |
| `@heroicons/vue` | Icons used in any future shared components |

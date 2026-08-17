# UI Component Library — File Map

## Owned files

### Package metadata

| Path                                          | Layer     | Purpose                                                                                        |
| --------------------------------------------- | --------- | ---------------------------------------------------------------------------------------------- |
| `src/frontend/packages/ui/package.json`       | Manifest  | Private workspace package, source-only export, scripts (`dev`, `build`, `type-check`, `clean`) |
| `src/frontend/packages/ui/tsconfig.json`      | TS config | Source typings for consumers                                                                   |
| `src/frontend/packages/ui/vite.config.ts`     | Bundler   | Vite library mode for the optional `dist/` artifact (consumers default to source)              |
| `src/frontend/packages/ui/tailwind.config.js` | Tailwind  | Color tokens, font stack, spacing scales — the design DNA                                      |
| `src/frontend/packages/ui/postcss.config.js`  | PostCSS   | Tailwind + autoprefixer pipeline                                                               |
| `src/frontend/packages/ui/README.md`          | Docs      | Per-package readme                                                                             |

### Barrel + supporting

| Path                                              | Layer       | Purpose                                                                                                                               |
| ------------------------------------------------- | ----------- | ------------------------------------------------------------------------------------------------------------------------------------- |
| `src/frontend/packages/ui/src/index.ts`           | Barrel      | Re-exports every component + composable + utility. Adding a new component means adding an `export * from "./components/.../Xxx"` here |
| `src/frontend/packages/ui/src/lib/utils.ts`       | Utility     | `cn`, `formatDate`, `formatDateTime`, `truncate`, `capitalize`, `generateId`, `sleep`, `debounce`                                     |
| `src/frontend/packages/ui/src/styles/globals.css` | Styles      | Tailwind layer + theme tokens (CSS variables)                                                                                         |
| `src/frontend/packages/ui/src/theme/`             | Tokens      | Theme presets (palettes), theme-switching composable                                                                                  |
| `src/frontend/packages/ui/src/composables/`       | Composables | Shared hooks; re-exported from the barrel                                                                                             |

### Primitive components (`./components/ui/`)

| Path                                 | Component   | Purpose                                                                  |
| ------------------------------------ | ----------- | ------------------------------------------------------------------------ |
| `components/ui/alert/AppAlert.vue`   | `AppAlert`  | Inline alert with `variant: "info" \| "success" \| "warning" \| "error"` |
| `components/ui/badge/AppBadge.vue`   | `AppBadge`  | Status pill with size + variant                                          |
| `components/ui/button/AppButton.vue` | `AppButton` | Button with `variant`, `size`, `loading`, `disabled`, `icon` slots       |
| `components/ui/card/AppCard.vue`     | `AppCard`   | Section/content card                                                     |
| `components/ui/input/AppInput.vue`   | `AppInput`  | Form input wrapper with label, hint, error states                        |
| `components/ui/modal/AppModal.vue`   | `AppModal`  | Overlay + dialog with named slots for header / body / footer             |
| `components/ui/select/AppSelect.vue` | `AppSelect` | Single/multi-select; integrates with code-table options                  |
| `components/ui/switch/AppSwitch.vue` | `AppSwitch` | Boolean toggle                                                           |
| `components/ui/table/AppTable.vue`   | `AppTable`  | Plain table primitive (composite `AppDataTable` wraps this)              |

### Composite components (`./components/composite/`)

| Path                                                                    | Component                 | Purpose                                                        |
| ----------------------------------------------------------------------- | ------------------------- | -------------------------------------------------------------- |
| `components/composite/app-feedback/AppFeedbackHub.vue`                  | `AppFeedbackHub`          | The thumbs-up/down + textarea visual used by `feedback-widget` |
| `components/composite/confirm/AppConfirmDialog.vue`                     | `AppConfirmDialog`        | Yes/No confirmation modal                                      |
| `components/composite/data-table/AppDataTable.vue`                      | `AppDataTable`            | Sortable, paginated, filterable data grid                      |
| `components/composite/data-table/AppColumnFilterMenu.vue`               | `AppColumnFilterMenu`     | The per-column filter popover used by `AppDataTable`           |
| `components/composite/file-upload/AppFileUploadField.vue`               | `AppFileUploadField`      | Drag-drop + browse upload field                                |
| `components/composite/filter-bar/AppFilterBar.vue`                      | `AppFilterBar`            | Multi-filter chip bar (used on dashboards / list pages)        |
| `components/composite/list-controls/AppListControls.vue`                | `AppListControls`         | Search box + view-toggle + count strip for list pages          |
| `components/composite/loading/AppLoadingOverlay.vue`                    | `AppLoadingOverlay`       | Page-level spinner with optional message                       |
| `components/composite/page-header/AppPageHeader.vue`                    | `AppPageHeader`           | Title + breadcrumb + actions slot                              |
| `components/composite/pagination/AppPagination.vue`                     | `AppPagination`           | Page-number + page-size controls                               |
| `components/composite/profile-menu/AppLaunchpadProfileMenu.vue`         | `AppLaunchpadProfileMenu` | The user avatar dropdown used in `StaffLayout.vue`             |
| `components/composite/smart-filter-dropdown/AppSmartFilterDropdown.vue` | `AppSmartFilterDropdown`  | Type-ahead dropdown with checkbox multi-select                 |
| `components/composite/state-panel/AppStatePanel.vue`                    | `AppStatePanel`           | Empty / error / not-found / loading states with illustrations  |
| `components/composite/toast/AppToastContainer.vue`                      | `AppToastContainer`       | Toast stack consumer used by `useToast`                        |

### Theme components (`./components/theme/`)

| Path                                         | Component               | Purpose                 |
| -------------------------------------------- | ----------------------- | ----------------------- |
| `components/theme/AppThemeAuthPanel.vue`     | `AppThemeAuthPanel`     | Demo auth-panel surface |
| `components/theme/AppThemeReportCard.vue`    | `AppThemeReportCard`    | Demo report card        |
| `components/theme/AppThemeShell.vue`         | `AppThemeShell`         | Demo shell layout       |
| `components/theme/AppThemeStatCard.vue`      | `AppThemeStatCard`      | Demo KPI card           |
| `components/theme/AppThemeWizardStepper.vue` | `AppThemeWizardStepper` | Demo wizard stepper     |

## Touched files

| Path                                                | What it contains                                                                    | Why must be touched                              |
| --------------------------------------------------- | ----------------------------------------------------------------------------------- | ------------------------------------------------ |
| `src/frontend/main/package.json`                    | `"@apptemplate/ui": "workspace:*"` dep                                              | Required for the main app to import the package  |
| `src/frontend/auth/package.json`                    | `"@apptemplate/ui": "workspace:*"` dep                                              | Required for the auth app to import the package  |
| `src/frontend/main/src/main.ts`                     | `import "@apptemplate/ui/styles"`                                                   | Loads the global Tailwind layer + tokens         |
| `src/frontend/auth/src/main.ts`                     | `import "@apptemplate/ui/styles"`                                                   | Same for auth                                    |
| `src/frontend/main/tailwind.config.js` (and auth's) | `content` glob extended to include `node_modules/@apptemplate/ui/src/**/*.{vue,ts}` | Required for Tailwind JIT to scan UI lib classes |
| `src/frontend/main/vite.config.ts` (and auth's)     | Vite `resolve.dedupe` for `vue` to avoid double-bundling                            | The UI lib has `vue` as a peer dep               |
| `src/frontend/pnpm-workspace.yaml`                  | Includes `packages/ui` in the workspace                                             | Required for `workspace:*` resolution            |

## Migrations

None — no DB layer.

## External dependencies

| Package                   | Purpose                                                                 |
| ------------------------- | ----------------------------------------------------------------------- |
| `vue` (peer)              | Component framework                                                     |
| `@heroicons/vue`          | Icon set used by primitives + composites                                |
| `@vueuse/core`            | Reactive utilities (e.g. `useElementVisibility`, `onClickOutside`)      |
| `clsx` + `tailwind-merge` | Behind `cn()` utility                                                   |
| `tailwindcss` (peer)      | Atomic styling — but each app is responsible for its own Tailwind build |

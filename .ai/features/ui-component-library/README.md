# UI Component Library — `@apptemplate/ui`

> **Status:** `core`
> **Removable in derived repos:** **no** — every Vue page consumes at least one component from this package
> **Required by:** every page in `src/frontend/main` and `src/frontend/auth`

`@apptemplate/ui` is the project's internal Vue 3 component library. It is a private workspace package (`private: true`, no npm publish) that ships:

- **9 primitive components** (`./components/ui/*`) — `AppAlert`, `AppBadge`, `AppButton`, `AppCard`, `AppInput`, `AppModal`, `AppSelect`, `AppSwitch`, `AppTable`. These are headless / minimally-styled building blocks with prop-driven variants.
- **13 composite components** (`./components/composite/*`) — `AppFeedbackHub`, `AppConfirmDialog`, `AppDataTable` (+ `AppColumnFilterMenu`), `AppFileUploadField`, `AppFilterBar`, `AppListControls`, `AppLoadingOverlay`, `AppPageHeader`, `AppPagination`, `AppLaunchpadProfileMenu`, `AppSmartFilterDropdown`, `AppStatePanel`, `AppToastContainer`. These are higher-level patterns combining primitives + business logic.
- **5 theme demo components** (`./components/theme/*`) — `AppThemeAuthPanel`, `AppThemeReportCard`, `AppThemeShell`, `AppThemeStatCard`, `AppThemeWizardStepper`. Theme-tokens reference / demo surfaces.
- **Composables** — `./composables` (re-exported)
- **Theme tokens** — `./theme` (re-exported)
- **Utilities** — `cn`, `formatDate`, `formatDateTime`, `truncate`, `capitalize`, `generateId`, `sleep`, `debounce` from `./lib/utils`
- **Global stylesheet** — `./styles/globals.css` (consumed via `import "@apptemplate/ui/styles"`)

The package is consumed via path-based pnpm workspace resolution (`workspace:*`); main and auth FE both import from `@apptemplate/ui`. Source-only distribution (`main / module / types` all point at `./src/index.ts`) — Vite consumers compile through TS during build, so there's no per-publish artifact step.

## Quick links

- [`files.md`](./files.md) — every file owned and touched by this feature
- [`do-dont.md`](./do-dont.md) — feature-specific rules
- [`customize.md`](./customize.md) — adding a component, updating tokens, theme overrides
- [`verify.md`](./verify.md) — package builds, typings work, components render

## Architectural shape

```mermaid
flowchart LR
  Page["Vue page<br/>main FE / auth FE"] -->|import| Idx["@apptemplate/ui<br/>(src/index.ts barrel)"]
  Idx --> Prim["./components/ui<br/>9 primitives"]
  Idx --> Comp["./components/composite<br/>13 composites"]
  Idx --> Theme["./components/theme<br/>5 theme demos"]
  Idx --> Util["./lib/utils<br/>cn, formatDate, debounce, ..."]
  Idx --> Comps2["./composables"]
  Idx --> Tokens["./theme"]
  Page -->|import "@apptemplate/ui/styles"| Css["src/styles/globals.css"]
  Css --> Tw[Tailwind 3 config]
```

## Key entry points

| Layer                | Path                                                                                                                                                                                                                    | Purpose                                                                                                                                                                                                        |
| -------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Package              | `src/frontend/packages/ui/package.json`                                                                                                                                                                                 | `private: true`, `name: "@apptemplate/ui"`, source-only export (`main / module / types → ./src/index.ts`), `./styles` subpath, `dev: vite build --watch`, `build: vite build && vue-tsc --emitDeclarationOnly` |
| Barrel               | `src/frontend/packages/ui/src/index.ts`                                                                                                                                                                                 | The single export surface; everything is re-exported through this file                                                                                                                                         |
| Primitive components | `src/frontend/packages/ui/src/components/ui/{alert,badge,button,card,input,modal,select,switch,table}/App*.vue`                                                                                                         | One folder per primitive with a single `.vue` file plus an `index.ts` re-export                                                                                                                                |
| Composite components | `src/frontend/packages/ui/src/components/composite/{app-feedback,confirm,data-table,file-upload,filter-bar,list-controls,loading,page-header,pagination,profile-menu,smart-filter-dropdown,state-panel,toast}/App*.vue` | Higher-level patterns; `data-table` ships two files (`AppDataTable.vue` + `AppColumnFilterMenu.vue`)                                                                                                           |
| Theme demo           | `src/frontend/packages/ui/src/components/theme/App*.vue`                                                                                                                                                                | Reference surfaces for theme tokens — used in marketing-style theme previews                                                                                                                                   |
| Theme tokens         | `src/frontend/packages/ui/src/theme/`                                                                                                                                                                                   | Color palettes / tokens + composables to switch themes                                                                                                                                                         |
| Composables          | `src/frontend/packages/ui/src/composables/`                                                                                                                                                                             | Reusable hooks shipped alongside components                                                                                                                                                                    |
| Utilities            | `src/frontend/packages/ui/src/lib/utils.ts`                                                                                                                                                                             | `cn` (clsx + tailwind-merge), `formatDate`, `formatDateTime`, `truncate`, `capitalize`, `generateId`, `sleep`, `debounce`                                                                                      |
| Stylesheet           | `src/frontend/packages/ui/src/styles/globals.css`                                                                                                                                                                       | Tailwind base + component layer + theme tokens                                                                                                                                                                 |
| Tailwind config      | `src/frontend/packages/ui/tailwind.config.js`                                                                                                                                                                           | Color tokens, font, spacing — the design source of truth                                                                                                                                                       |

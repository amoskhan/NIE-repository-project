# @apptemplate/ui

Shared visual design system and reusable Vue components for App Template.

## Put Code Here When

- The component or composable is intended for reuse across `main`, `auth`, or future frontend apps
- The logic is UI-focused and not tied to a single business domain
- A design-system surface should be standardized instead of duplicated

## Structure

- `src/components/ui/` for low-level primitives
- `src/components/composite/` for reusable higher-level UI patterns
- `src/composables/` for UI-oriented shared logic
- `src/theme/` for theme runtime, presets, and tokens
- `src/index.ts` for package exports

## Reuse-First Components

- `AppDataTable` for list pages with search, filtering, pagination, and mobile dock behavior
- `AppListControls` for list toolbar behavior
- `AppSmartFilterDropdown` for shared filter popovers and sheets
- `AppPageHeader` for page titles and metadata
- `AppModal`, `AppPagination`, `AppStatePanel`, and `AppFileUpload` before adding app-specific variants

## Authoring Rules

- Use the `App` prefix for exported components
- Export every new public surface from the nearest `index.ts` and from `src/index.ts`
- Keep business-domain API calls out of this package
- Prefer props and slots over app-specific assumptions

## Validation

- Run `pnpm run build:ui` from `src/frontend`
- If a change affects a consuming screen, validate it through the relevant app with Playwright or manual browser checks

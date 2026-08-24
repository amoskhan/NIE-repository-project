# @nie/ui

Shared visual design system and reusable Vue components for NIE Template.

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

- `NieDataTable` for list pages with search, filtering, pagination, and mobile dock behavior
- `NieListControls` for list toolbar behavior
- `NieSmartFilterDropdown` for shared filter popovers and sheets
- `NiePageHeader` for page titles and metadata
- `NieModal`, `NiePagination`, `NieStatePanel`, and `NieFileUpload` before adding app-specific variants

## Authoring Rules

- Use the `Nie` prefix for exported components
- Export every new public surface from the nearest `index.ts` and from `src/index.ts`
- Keep business-domain API calls out of this package
- Prefer props and slots over app-specific assumptions

## Validation

- Run `pnpm run build:ui` from `src/frontend`
- If a change affects a consuming screen, validate it through the relevant app with Playwright or manual browser checks

# @apptemplate/shared

Shared non-visual utilities used across frontend apps.

## Put Code Here When

- The code is app-agnostic and reusable across `main`, `auth`, or future apps
- It represents shared types, utilities, or generic composables
- The logic should not depend on a specific page, route, or business feature

## Structure

- `src/types/` for shared TypeScript models and contracts
- `src/utils/` for reusable pure helpers
- `src/composables/` for framework-aware but app-agnostic shared logic
- `src/i18n/` for translation helpers and shared locale setup
- `src/index.ts` and secondary indexes for exports

## Keep Out

- App-specific API clients
- Page-level state management tied to a single route
- UI-only components that belong in `@apptemplate/ui`
- Business workflows that only one app uses

## Authoring Rules

- Keep exported APIs stable and typed
- Export new shared surfaces from the relevant index files immediately
- Prefer pure utilities where possible so agents can reuse them safely

## Validation

- Run `pnpm run build:shared` from `src/frontend`
- Re-run the consuming app build or targeted tests when shared types change

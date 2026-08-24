# UI Component Library

The @nie/ui private workspace package: primitive + composite Vue components, theme runtime, and shared utilities.

Rules version: 2026.08.10.1
Feature key: ui-component-library  
Adoption: **mandatory**

## Adoption and navigation

- Menu or entry point: not independently required. No dedicated menu is required.
- Visibility: The UI library supplies components; applications own navigation.
- If the feature is not adopted, report not-applicable with the product or architecture reason; do not silently omit it.

## Required libraries and minimums

| Library | Package/runtime | Minimum | Ecosystem |
| --- | --- | --- | --- |
| Node.js | node | 22.12.0 | runtime |
| pnpm | pnpm | 10.33.0 | runtime |
| Vue | vue | 3.5.30 | npm |
| Vite | vite | 8.0.16 | npm |
| TypeScript | typescript | 6.0.2 | npm |
| vue-tsc | vue-tsc | 3.2.6 | npm |
| ESLint | eslint | 10.8.0 | npm |
| typescript-eslint | typescript-eslint | 8.65.0 | npm |
| eslint-plugin-vue | eslint-plugin-vue | 10.10.0 | npm |
| Tailwind CSS | tailwindcss | 4.2.2 | npm |
| PostCSS | postcss | 8.5.18 | npm |
| VueUse | @vueuse/core | 14.2.1 | npm |
| Heroicons for Vue | @heroicons/vue | 2.2.0 | npm |
| Class Variance Authority | class-variance-authority | 0.7.1 | npm |
| clsx | clsx | 2.1.1 | npm |
| tailwind-merge | tailwind-merge | 3.5.0 | npm |

Versions are floors, not forced pins for derived applications. Stable upgrades are allowed when compatibility, build, tests, security, and deployment evidence pass. The template itself keeps exact tested pins and lockfiles.

## Rules

| Rule | Severity | Area | Requirement | Required evidence |
| --- | --- | --- | --- | --- |
| NIE-UI-001 | error | scope | Keep @nie/ui visual, reusable, accessible, domain-neutral, and independent of application APIs/routes/business entities. | architecture-tests |
| NIE-UI-002 | error | components | Use Nie-prefixed PascalCase SFCs, typed props/emits/slots, Composition API script setup lang=ts, and a single public barrel. | lint-and-type-check |
| NIE-UI-003 | error | design | Use semantic theme tokens and Tailwind CSS 4 utilities; do not hardcode product colors or duplicate variants in consumers. | visual-review |
| NIE-UI-004 | error | data-flow | Keep state minimal, derive with computed, use props/events by default, and expose imperative APIs only when necessary and typed. | review |
| NIE-UI-005 | error | accessibility | Provide keyboard, focus, labels, semantics, contrast, reduced-motion, and screen-reader behavior for every interactive component. | component-and-browser-tests |
| NIE-UI-006 | error | verification | Build/type-check the package and render-test components in both main and auth applications across supported themes. | build-and-tests |
| NIE-UI-007 | error | tabs | Use the typed @nie/ui NieTabs component for application tablists. Preserve ARIA tab semantics, roving tab stops, visible focus, Arrow Left/Right plus Home/End navigation, route or lazy-loading behavior owned by the page, and the shared responsive active/icon/count treatment; do not duplicate page-specific tab markup or visual styles. | component-browser-and-grep-tests |
| NIE-UI-008 | error | typography | Use Plus Jakarta Sans with the released 300 through 800 weights for Main, Auth, shared UI runtime tokens, and Tailwind fallbacks; retain Inter and system-ui fallbacks and do not mix product-wide Lexend or Nunito declarations. | component-and-grep-tests |
| NIE-UI-009 | error | data-tables | Use the shared NieDataTable for searchable/paged application lists; its desktop control must have a bounded configurable viewport, one labelled keyboard-focusable two-axis scroll region, sticky headers, reachable wide columns/actions, and mobile card behavior. Do not keep page-owned duplicate data-table components. | component-browser-and-grep-tests |
| NIE-UI-010 | error | scrolling | Bound long desktop list, catalog, editor, preview, and matrix controls to the remaining viewport and give the content an internal labelled scroll region with overscroll containment; allow natural document flow on narrow mobile layouts. | browser-tests |
| NIE-UI-011 | error | table-appearance | Offer elevated, minimal, and striped NieDataTable appearances through a typed shared prop and semantic tokens; consumers select a supported appearance instead of forking table markup or hardcoding a new visual treatment. | component-and-visual-tests |
| NIE-UI-012 | error | tokens | Keep the canonical typography, weight, spacing, control-height, radius, status-colour, and elevation scales in @nie/ui shared styles. Application and shared SFC styling must consume semantic tokens and must not introduce literal product colours, private radius scales, private spacing scales, or nonstandard font weights. | component-and-grep-tests |
| NIE-UI-013 | error | primitives | Use shared NieInput, NieSelect, NieTextarea, NieButton, NieSwitch, NieTabs, NieCard, NieModal, and NieDataTable geometry for equivalent controls. Specialised controls must use the same semantic control height, radius, typography, error, focus, disabled, and labelled-field semantics. Primary one-line search/filter fields and adjacent toolbar actions use the medium control height; compact header filters, pagination, and popover controls use only their documented small/compact variants. | component-browser-and-grep-tests |
| NIE-UI-014 | error | responsive-layout | At supported desktop, tablet, and 390-pixel phone widths, keep page titles readable without ellipsis, prevent document-level horizontal overflow, preserve natural mobile flow, and provide at least a 24-by-24-pixel effective pointer target or an accessibility-standard target exception. | browser-tests |
| NIE-UI-015 | error | theme-runtime | Apply, announce, and persist theme mode, preset, density, scenario, layout, preference, radius, and motion through the shared runtime; validate stored values against the typed theme contract before use. | component-and-browser-tests |
| NIE-UI-016 | error | consumer-overrides | Do not override shared primitive geometry with application-wide element selectors or duplicate product palettes, typography, table, input, button, radius, or dark-mode foundations in consumer stylesheets. | component-and-grep-tests |
| NIE-UI-017 | error | remote-data-tables | For live lists, use NieDataTable serverSide with a separate totalItems value, typed query-change and filter-options-request events, server-returned row pages only, and stale-response protection in the shared application composable. | component-contract-and-api-tests |
| NIE-UI-018 | error | column-filters | Use a compact 32-pixel-high column-filter trigger with at least a 32-pixel minimum width, an accessible column label, and a visible selected-count state. Column filter panels must remain open during table scrolling and multi-selection, preserve selected values while option pages change, page/search values through the API, reposition without covering the sticky header, and close on outside interaction or Escape. | component-and-browser-tests |
| NIE-UI-019 | error | table-states | Keep the data-table header and bounded remaining-viewport scroll region visible for empty server results. Center one shared illustrated NieResultState in the remaining space, reuse the application split/orbit visual language, and anchor it to the visible horizontal scroll viewport so wide columns cannot move the illustration off-screen. Expose only 10, 20, 50, and 100 rows per page by default; page-size changes reset to page one and request the API. Display the total result count once in the top toolbar. Keep the footer to one compact row containing an accessible dropdown-only page-size selector and exactly first, previous, current-page, next, and last pagination controls; do not render page-number lists, range text, or a second total. Reserve explicit right padding for the non-interactive page-size chevron. At phone width, position the compact footer above the fixed search/filter toolbar without overlap and without reducing the list's usable viewport height. | component-contract-and-browser-tests |
| NIE-UI-020 | error | categorical-data | Render short categorical table values through declarative NieDataTable chips or NieBadge slots using shared semantic tones and optional dots; do not create page-local badge palettes. | component-and-grep-tests |
| NIE-UI-021 | error | loading | Use the shared NieLoaderSymbol precision-orbit variant for compact controls and its same-tone N/I/E brand variant for large page, table, and overlay loading states. Keep the lion outside the loading symbol, use one outer orbit, remove jumping and rotation under reduced-motion preferences, and do not introduce screen-specific spinner implementations. | component-browser-and-grep-tests |
| NIE-UI-022 | error | safe-result-states | Render initial loading, empty, and failure outcomes inside NieDataTable through the shared NieResultState contract. Pass a supported HTTP status explicitly where available, permit only conservative recognition of supported status codes as a compatibility fallback, and never expose raw Axios, transport, exception, stack, endpoint, or backend error text to users. | component-contract-and-security-tests |
| NIE-UI-023 | error | table-preferences | Use the single shared data-table preferences dialog, weekly saved-filter reminder, and provider-neutral store contract for column order/visibility, ordered sorts, saved filters, rows per page, density, appearance, repair, reset, and reminder acknowledgement. Its sort and filter editors must reflow from their own container width without clipping or overlap, and every modal action must remain fully inside the dynamic viewport. Its Default filters builder must list every eligible column before values are loaded, retrieve searched and paged values through the existing server facet contract, and preserve primitive multi-select values across columns and pages. Do not add consumer-owned preference popups, client-authoritative reminder timestamps, or browser-only persistence. | component-contract-and-browser-tests |
| NIE-UI-024 | error | table-density | Expose compact, comfortable, and spacious density through typed shared props and semantic spacing tokens. On narrow phones, use the remaining dynamic viewport height, contained internal scrolling, compact cards, safe-area-aware controls, and no document-level horizontal overflow. | component-parity-and-browser-tests |
| NIE-UI-025 | error | reference-parity | For every derived product screen, record which canonical shell, shared components, interaction states, spacing, typography, form/table patterns, and responsive behavior it reuses. Verify representative product screens at desktop and 390-pixel phone widths. Visual similarity without shared-component/code-structure evidence is not sufficient, and Procurement content must not leak into the product. | reference-pattern-map, component-review, and browser-tests |

## Canonical reference footprint

Use these files as working examples and comparison targets. Procurement is a reference vertical, not a runtime dependency.

- src/frontend/packages/ui/package.json
- src/frontend/packages/ui/tsconfig.json
- src/frontend/packages/ui/vite.config.ts
- src/frontend/packages/ui/tailwind.config.js
- src/frontend/packages/ui/postcss.config.js
- src/frontend/packages/ui/README.md
- src/frontend/packages/ui/src/index.ts
- src/frontend/packages/ui/src/lib/utils.ts
- src/frontend/packages/ui/src/components/ui/tabs/NieTabs.vue
- src/frontend/packages/ui/src/components/ui/tabs/types.ts
- src/frontend/packages/ui/src/components/ui/tabs/index.ts
- src/frontend/packages/ui/src/components/ui/textarea/NieTextarea.vue
- src/frontend/packages/ui/src/components/ui/textarea/index.ts
- src/frontend/packages/ui/src/components/composite/data-table/NieDataTable.vue
- src/frontend/packages/ui/src/components/composite/data-table/NieColumnFilterMenu.vue
- src/frontend/packages/ui/src/components/composite/data-table/types.ts
- src/frontend/packages/ui/src/components/composite/pagination/NiePagination.vue
- src/frontend/packages/ui/src/components/composite/loading/NieLoaderSymbol.vue
- src/frontend/packages/ui/src/components/composite/loading/NieLoadingOverlay.vue
- src/frontend/packages/ui/src/components/composite/loading/index.ts
- src/frontend/packages/ui/src/components/composite/result-state/NieResultState.vue
- src/frontend/packages/ui/src/components/composite/result-state/index.ts
- src/frontend/packages/ui/src/components/ui/badge/NieBadge.vue
- src/frontend/packages/ui/src/styles/globals.css
- src/frontend/packages/ui/src/theme/presets.ts

## AI implementation and verification

The implementing AI must:

1. Assess every rule above before editing and identify affected backend, frontend, data, security, navigation, and test surfaces.
2. Compare the application implementation with the pinned canonical template reference. Classify each shared file as identical, behind, customized, ahead, conflict, or not applicable.
3. Implement missing behavior directly. Preserve domain behavior and integrate shared updates through extension points rather than overwriting valid customization.
4. Add or update focused unit, architecture, integration, component, API, and browser tests according to the evidence type for each rule.
5. Run the standard repository gates and record exact commands, exit status, and meaningful test counts or artifacts.
6. Report each rule as pass, fail, not-applicable, manual-review, or approved-exception with file/line and test evidence.

A separate AI verifier must review material changes from a fresh context, inspect the diff and evidence, rerun risk-relevant gates, and issue an independent verdict. A claim without traceable evidence is not a pass.

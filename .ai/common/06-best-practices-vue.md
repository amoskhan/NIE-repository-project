# 06 — Vue 3 Best Practices Audit Checklist

Source of truth: <https://vuejs.org/style-guide/rules-strongly-recommended.html> + <https://learnvue.co/articles/vue-best-practices>.

This checklist applies to every Vue/TypeScript change. Each rule is `pass` / `fail` / `n/a`. Track current state for the template in the `Status` column; update when you fix.

| #    | Rule                                                                                | Status (template baseline 2026.07.31.1)                                              | How to verify                                                                                    | Where to fix                                                               |
| ---- | ----------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------ | ------------------------------------------------------------------------------------------------ | -------------------------------------------------------------------------- |
| V-01 | Multi-word component names (`UserCard`, not `Card`)                                 | pass (`App*` prefix on shared components)                                            | grep for `<Card>`, `<Panel>` siblings — anything 1-word in `src/frontend/**/*.vue` template tags | Rename component file + import + usages                                    |
| V-02 | `<script setup lang="ts">` everywhere                                               | pass                                                                                 | grep `<script>` without `setup` in SFCs                                                          | Convert to setup syntax                                                    |
| V-03 | Detailed prop definitions (`type`, `required`, `default`, `validator`)              | partial — many use bare `defineProps<{}>()` without runtime defaults                 | grep `defineProps<` and inspect; require `withDefaults(defineProps<...>(), {...})`               | Add `withDefaults` and validators                                          |
| V-04 | Always key `v-for`                                                                  | pass (most cases)                                                                    | grep `v-for` lines without `:key=` on same element                                               | Add `:key`                                                                 |
| V-05 | Never `v-if` + `v-for` on same element                                              | needs scan                                                                           | grep lines containing both                                                                       | Wrap with `<template v-for>` and inner `v-if`                              |
| V-06 | Self-close components without children                                              | partial                                                                              | scan for empty `<AppIcon>...</AppIcon>` pairs                                                    | Convert to `<AppIcon ... />`                                               |
| V-07 | Order of tags `<script>` → `<template>` → `<style>`                                 | pass (template convention)                                                           | head of every SFC                                                                                | Reorder if needed                                                          |
| V-08 | No `any` in TypeScript                                                              | pass per rule, **needs scan**                                                        | `grep -rn ': any\b\|<any>' src/frontend/**/*.{ts,vue}`                                           | Replace with proper types                                                  |
| V-09 | Component file names PascalCase                                                     | pass                                                                                 | `find src/frontend -name "*.vue"`                                                                | Rename non-PascalCase files                                                |
| V-10 | Props in templates use kebab-case                                                   | pass                                                                                 | template inspection                                                                              | Convert camelCase prop usage in templates                                  |
| V-11 | Status / state / category fields use mirrored enums (template rule, not a Vue rule) | **fail in places** — `feedback.${routeName}` style strings, ad-hoc badge color logic | grep status comparisons against string literals                                                  | Mirror BE enum to `src/frontend/main/src/types/<feature>.ts` and reference |
| V-12 | API access via service modules, not direct axios in components                      | pass                                                                                 | grep `axios\.\|fetch(` inside `*.vue`                                                            | Move to a service in `src/frontend/main/src/services/`                     |
| V-13 | Loading + error state in every async page                                           | needs scan                                                                           | inspect each page for `isLoading` + `error` handling                                             | Add states                                                                 |
| V-14 | Composition API consistently (no Options API)                                       | pass                                                                                 | grep `export default {` in SFCs                                                                  | Convert                                                                    |
| V-15 | Lazy-loaded routes                                                                  | pass (router uses dynamic `import()`)                                                | inspect `router/index.ts`                                                                        | Convert remaining static imports                                           |
| V-16 | Accessibility — semantic landmarks, alt text, focus management                      | partial — `StaffLayout` has skip link, modals not all keyboard-trapped               | manual audit + axe-core run                                                                      | Add aria-* + focus traps                                                   |

## How to run a full audit

```bash
# Pseudo-script — wire into CI later
pnpm --filter main type-check
pnpm --filter main lint
grep -rn ": any\b" src/frontend/main/src/ src/frontend/auth/src/ src/frontend/packages/
grep -rn "axios\." src/frontend/main/src/**/*.vue
grep -rEn "v-for=\".*\"\s+v-if=" src/frontend/**/*.vue
```

## Fixing strategy

When you fix a row:

1. Update `Status` to `pass`.
2. Add a one-line note in `CHANGELOG.md` if the fix shipped in a template release.
3. If a class of violations is too large to fix in one PR, open a follow-up task in `.ai/tasks/` (e.g. `0010-vue-no-any-cleanup`).

## Open follow-up tasks (template baseline 2026.07.31.1)

- `[ ] V-03 strict prop validators across UI library composites`
- `[ ] V-08 zero-`any` policy enforcement (currently a few legacy spots)`
- `[ ] V-11 mirror EPurchaseOrderStatus to FE enum and replace string usages`
- `[ ] V-13 every async page has explicit error state`
- `[ ] V-16 accessibility pass on modals/popovers`

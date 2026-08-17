# UI Component Library — Customize

## 1. Add a new primitive (e.g. `AppAvatar`)

1. Create the folder + component:
   ```
   src/frontend/packages/ui/src/components/ui/avatar/
     AppAvatar.vue
     index.ts
   ```
2. `index.ts`:
   ```ts
   export { default as AppAvatar } from "./AppAvatar.vue";
   ```
3. Edit `src/frontend/packages/ui/src/index.ts` — append:
   ```ts
   export * from "./components/ui/avatar";
   ```
4. Use it in a consumer:
   ```vue
   <script setup lang="ts">
   import { AppAvatar } from "@apptemplate/ui";
   </script>
   <template>
     <AppAvatar :src="user.photoUrl" :name="user.fullName" />
   </template>
   ```
5. Confirm the consumer app's Tailwind picks up classes used in the new component (the `content` glob in `tailwind.config.js` covers it).

## 2. Add a new composite that wraps primitives

1. Create `src/frontend/packages/ui/src/components/composite/<name>/App<Name>.vue`. Inside, import the primitive(s) you need:
   ```vue
   <script setup lang="ts">
   import { AppButton, AppInput } from "..";
   // or import directly from primitive folders if you prefer
   </script>
   ```
2. Add the folder `index.ts` re-export.
3. Add `export * from "./components/composite/<name>";` to `src/index.ts`.

## 3. Update theme tokens (palette / spacing)

1. Edit `tailwind.config.js` — extend `theme.extend.colors`:
   ```js
   theme: {
     extend: {
       colors: {
         primary: { 50: "#...", 500: "#...", 900: "#..." },
         surface: { ... }
       }
     }
   }
   ```
2. Edit `src/styles/globals.css` — define CSS variables that map to those tokens:
   ```css
   :root {
     --color-primary: theme("colors.primary.500");
     --color-surface: theme("colors.surface.50");
   }
   .dark {
     --color-surface: theme("colors.surface.900");
   }
   ```
3. Components that consume tokens via CSS vars will automatically pick up the change.
4. Bump consumer apps' theme cache (e.g. clear browser localStorage if a theme composable persists user choice).

## 4. Override a component's appearance per project

The lib is intentionally minimal-styled. To override:

1. In the consumer page, pass class overrides via the `class` attribute on the component:
   ```vue
   <AppButton class="!bg-emerald-600 !hover:bg-emerald-700">Special</AppButton>
   ```
2. The `cn()` utility merges classes deterministically; the consumer's class wins on conflict (`tailwind-merge` semantics).
3. If a project needs deeper style overrides on dozens of pages, define a wrapper component in the project (e.g. `src/frontend/main/src/components/ProjectButton.vue` that wraps `AppButton`).

## 5. Bump a peer dependency (e.g. Vue 3.5)

1. Update `package.json` `peerDependencies` in `@apptemplate/ui`.
2. Update both consumer apps' `package.json` to the matching version.
3. Run `pnpm install` at the root. pnpm validates peer deps; mismatches surface as warnings.
4. Run `pnpm --filter @apptemplate/ui type-check` — if the new Vue version introduces breaking type changes, surface fixes here first.

## 6. Run the lib in watch mode while developing the consumer

In one terminal:

```bash
pnpm --filter @apptemplate/ui dev
```

This runs `vite build --watch`. The consumer's Vite picks up the source files directly (the package is source-only), so most changes hot-reload without the watch — but watch is useful when you also build the optional `dist/` artifact.

## 7. Build the optional `dist/` for external consumers

```bash
pnpm --filter @apptemplate/ui build
```

This produces `dist/` with `vite build` output + `vue-tsc --emitDeclarationOnly` types. The package is still source-only by default; consumers that want compiled output can change their import resolution to use `dist`.

## 8. Add a new utility to `lib/utils.ts`

1. Append the helper:
   ```ts
   export function pluralize(count: number, singular: string, plural?: string) { ... }
   ```
2. Edit `src/index.ts` — extend the named export list:
   ```ts
   export {
     cn,
     formatDate,
     formatDateTime,
     truncate,
     capitalize,
     generateId,
     sleep,
     debounce,
     pluralize,
   } from "./lib/utils";
   ```
3. Use it in a consumer: `import { pluralize } from "@apptemplate/ui";`.

## 9. Remove a component (deprecation flow)

1. Mark the component with a `// @deprecated` comment in the .vue file's `<script setup>` block.
2. Add a re-export shim in the new location if you renamed it.
3. Open an issue tracking the removal in 2 minor versions.
4. After grace period, remove from `src/index.ts` and delete the folder.

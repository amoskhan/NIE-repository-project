# Shared Utilities — Customize

## 1. Add a new shared TypeScript type

1. If it's a new domain area, create a new file:
   ```ts
   // src/frontend/packages/shared/src/types/notification.ts
   export interface NotificationItem {
     id: number;
     title: string;
     body: string;
     readAt?: string | null;
   }
   ```
2. Edit `src/frontend/packages/shared/src/types/index.ts`:
   ```ts
   export * from "./notification";
   ```
3. Use it in either app:
   ```ts
   import type { NotificationItem } from "@apptemplate/shared";
   ```

## 2. Add a new shared utility function

1. Append to `src/frontend/packages/shared/src/utils/format.ts` (or create a sibling file):
   ```ts
   export function formatBytes(bytes: number, fractionDigits = 1): string {
     if (bytes < 1024) return `${bytes} B`;
     const units = ["KB", "MB", "GB", "TB"];
     let value = bytes / 1024;
     let unitIndex = 0;
     while (value >= 1024 && unitIndex < units.length - 1) {
       value /= 1024;
       unitIndex++;
     }
     return `${value.toFixed(fractionDigits)} ${units[unitIndex]}`;
   }
   ```
2. Re-exported automatically via `utils/index.ts` (`export *`).

## 3. Add a new locale (e.g. Bahasa Melayu)

1. Create `src/frontend/packages/shared/src/i18n/locales/ms.json` with your translation tree.
2. Edit `src/frontend/packages/shared/src/i18n/index.ts`:
   - Import the new JSON: `import ms from "./locales/ms.json";`
   - Add to the `messages` object: `messages: { en, ms }`
   - Add to the `SupportedLocale` union: `export type SupportedLocale = "en" | "ms";`
3. Wire a UI control in the FE that calls `setLocale("ms")` when the user picks Malay.
4. Existing pages using `t(...)` automatically pick up the new translation tree.

## 4. Add a new composable

1. Create `src/frontend/packages/shared/src/composables/useFeatureFlag.ts`:
   ```ts
   import { ref, type Ref } from "vue";

   const flags = ref<Record<string, boolean>>({});

   export function useFeatureFlag(name: string): Ref<boolean> {
     return computed(() => !!flags.value[name]);
   }

   export function setFeatureFlag(name: string, enabled: boolean) {
     flags.value[name] = enabled;
   }
   ```
2. Edit `src/frontend/packages/shared/src/composables/index.ts`:
   ```ts
   export * from "./useFeatureFlag";
   ```
3. Use in either app:
   ```ts
   import { useFeatureFlag, setFeatureFlag } from "@apptemplate/shared";
   const enabled = useFeatureFlag("new-dashboard");
   ```

## 5. Wire `useApi` for a new FE service

The existing pattern in `src/frontend/main/src/services/api.ts` uses a project-local axios instance — but `useApi` from shared is the canonical primitive for new packages. For a brand-new microfrontend or auth FE feature:

```ts
// src/frontend/auth/src/services/myService.ts
import { useApi } from "@apptemplate/shared";

const api = useApi();

export const myService = {
  async getThing() {
    return (await api.get("/api/Thing")).data;
  },
};
```

For the main FE's existing services that use the local `api.ts`, leave them as-is — duplicating the wrapper migration is high-risk for low gain.

## 6. Tighten zod schemas with a shared base

If multiple forms share fields (e.g. email, NRIC), define them once:

```ts
// src/frontend/packages/shared/src/utils/schemas.ts (new file)
import { z } from "zod";

export const emailSchema = z.string().email("Enter a valid email address");
export const nricSchema = z
  .string()
  .regex(/^[STFG]\d{7}[A-Z]$/, "Invalid NRIC/FIN");
```

Then add `export * from "./schemas";` to `utils/index.ts`. Forms compose these:

```ts
import { emailSchema, nricSchema } from "@apptemplate/shared";
const formSchema = z.object({ email: emailSchema, nric: nricSchema });
```

## 7. Subset the bundle for the auth FE

The auth FE has a minimal surface — it doesn't need the full type set or every composable. Because each subpath is exported separately, the auth FE can import only what it needs:

```ts
// src/frontend/auth/src/main.ts
import { i18n } from "@apptemplate/shared/i18n";
// NOT: import { i18n, useFormValidation, formatBytes, type Role } from "@apptemplate/shared";
```

Tree-shaking handles the rest, but explicit subpath imports document intent.

## 8. Drop the `components/` stub if you never need it

The `components/index.ts` is currently empty. To remove:

1. Delete `src/frontend/packages/shared/src/components/`.
2. Edit `src/frontend/packages/shared/src/index.ts` — remove `export * from "./components";`.
3. Edit `package.json` — remove the `./components` subpath export.

Or leave it — the empty barrel costs nothing and keeps the door open.

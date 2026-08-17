# 03 — TypeScript / Vue 3 Coding Standards

Applies to anything under `src/frontend/`.

## Naming

- Interfaces / Types: **PascalCase** (`UserProfile`, `ButtonVariant`)
- Functions / variables: **camelCase**
- Constants: **SCREAMING_SNAKE_CASE** (or camelCase for local consts)
- Component file names: **PascalCase.vue**
- Components in templates: **PascalCase** (`<AppButton />`)
- Props in templates: **kebab-case** (`<AppButton variant-mode="primary" />`)

## File layout (Vue SFC)

Order MUST be: `<script setup lang="ts">` → `<template>` → `<style>` (optional, scoped).

```vue
<script setup lang="ts">
import { ref, onMounted } from "vue";
import { useRouter } from "vue-router";
import { useToast, AppButton, AppCard } from "@apptemplate/ui";
import entityService, { type Entity } from "@/services/entityService";
import { EEntityStatus } from "@/types/entity"; // status ENUM, not string

const isLoading = ref(true);
const items = ref<Entity[]>([]);

const fetchData = async () => {
  isLoading.value = true;
  try {
    items.value = await entityService.getAll();
  } finally {
    isLoading.value = false;
  }
};
onMounted(fetchData);
</script>

<template>
  <AppCard title="Entities">
    <p v-if="isLoading">Loading…</p>
    <ul v-else>
      <li v-for="item in items" :key="item.id">
        {{ item.name }} —
        <span :class="badgeClass(item.status)">{{
          statusLabel(item.status)
        }}</span>
      </li>
    </ul>
  </AppCard>
</template>
```

## Component rules (from Vue 3 Style Guide — Strongly Recommended)

- Always use `<script setup lang="ts">`.
- Multi-word component names — never one word (`<AppButton>` not `<Button>`).
- Detailed prop definitions: every prop has `type`, optional `required`, optional `default`, optional `validator`.
- Always key `v-for`.
- Never combine `v-if` and `v-for` on the same element.
- Self-close components without children (`<AppIcon name="close" />`).
- Order of element/attribute groups: definition (`is`, `v-for`), conditionals, render modifiers, then events, then content.

## Service pattern

```typescript
import api from "./api";
import type { EEntityStatus } from "@/types/entity";

export interface Entity {
  id?: number;
  name: string;
  description?: string | null;
  status: EEntityStatus; // enum mirror, never string
  createdOn?: string | null;
}

const entityService = {
  async getAll(): Promise<Entity[]> {
    return (await api.get<Entity[]>("/api/Entity/GetAll")).data;
  },
  async getById(id: number): Promise<Entity> {
    return (await api.get<Entity>(`/api/Entity/Get/${id}`)).data;
  },
  async save(entity: Entity): Promise<Entity> {
    const endpoint = entity.id ? "/api/Entity/Edit" : "/api/Entity/Save";
    return (await api.post<Entity>(endpoint, entity)).data;
  },
  async delete(id: number): Promise<void> {
    await api.post(`/api/Entity/Delete/${id}`);
  },
};
export default entityService;
```

## Frontend runtime configuration

- Do not use per-environment frontend `.env` files for deployed API roots,
  auth redirects, cookie names, Sentry DSN, OneSignal App ID, or feature flags.
- Shared runtime constants live in
  `src/frontend/packages/shared/src/config/constants.ts`.
- API URLs must derive from the app base path via `FRONTEND_CONSTANTS.backend`
  / `FRONTEND_CONSTANTS.api`.
- Optional public integration values must come from
  `window.__APP_TEMPLATE_CONFIG__` or the matching runtime `<meta>` tags read by
  `getMetaContent()` in that same `constants.ts`, so the one frontend build
  artifact can be promoted across environments.
- `import.meta.env` is allowed only for Vite-provided build mode checks such as
  `MODE`, not for `VITE_*` application configuration.

## Template-owned frontend surfaces

Treat the staff shell, sidebar, topbar, and shared Vue components as
template-owned infrastructure. Feature work must not edit these files to fit a
single screen:

- `src/frontend/main/src/staff/layouts/StaffLayout.vue`
- `src/frontend/main/src/composables/useSidebar.ts`
- `src/frontend/main/src/components/common/**`
- `src/frontend/packages/ui/src/components/**`
- `src/frontend/packages/ui/src/theme/**`

For page-specific needs, create or edit feature-owned components under the
feature folder, route page, service, composable, or permission/navigation data
source. If the app shell or shared component library genuinely needs a change,
it must be handled as an explicit template task with verification across the
main/auth apps and responsive layouts.

## Enum mirroring (mandatory)

Every backend enum (`Shared.Enum.E*`) MUST have a TypeScript mirror. Place the mirror in:

- `src/frontend/main/src/types/<feature>.ts` (app-specific), or
- `src/frontend/packages/shared/src/types/` (cross-app).

```typescript
// Mirror of Shared.Enum.EPurchaseOrderStatus
export enum EPurchaseOrderStatus {
  Draft = "Draft",
  Submitted = "Submitted",
  PendingManagerApproval = "PendingManagerApproval",
  PendingFinanceApproval = "PendingFinanceApproval",
  PendingProcurementApproval = "PendingProcurementApproval",
  Approved = "Approved",
  Rejected = "Rejected",
  Cancelled = "Cancelled",
}
```

The string values must match the C# `enum.ToString()` output. Status / state / type / category fields in component code reference these enums — never raw string literals.

## Toast and error handling

```typescript
const toast = useToast();
toast.success("Saved");
toast.error("Failed to save");
```

Never let a fetch failure go silent. Surface either an inline error or a toast.

## Don't

- Don't use `any`.
- Don't call `axios` or `fetch` directly inside a component — go through a service.
- Don't hardcode status / state / category strings — use the mirrored enum.
- Don't ship a component that lacks loading and error states.
- Don't add frontend `.env*` files or `import.meta.env.VITE_*` application
  configuration. Use shared runtime constants instead.
- Don't modify the staff sidebar/topbar shell or common/shared Vue components
  for feature work. Add feature-owned components and data instead.

<script setup lang="ts">
import { computed, ref } from "vue";
import {
  NieButton,
  NieDataTable,
  type NieDataTableFilterOptionPage,
  type NieDataTableFilterOptionsRequest,
  type NieDataTableQuery,
} from "@nie/ui";
import type { UserAccessSummary } from "@/types";

const props = withDefaults(
  defineProps<{
    users: UserAccessSummary[];
    totalItems: number;
    loading?: boolean;
    error?: string | null;
    filterOptionPages?: Record<string, NieDataTableFilterOptionPage>;
    saving?: boolean;
    canManage?: boolean;
  }>(),
  { canManage: true },
);

const emit = defineEmits<{
  assign: [user: UserAccessSummary | null];
  "remove-global": [assignmentId: string];
  "remove-application": [assignmentId: string];
  "query-change": [query: NieDataTableQuery];
  "filter-options-request": [request: NieDataTableFilterOptionsRequest];
  retry: [];
}>();

const search = ref("");
const rows = computed(() =>
  props.users.map((user) => ({
    ...user,
    displayLabel: user.displayName?.trim() || user.userId,
    departmentLabel:
      user.departmentDescription?.trim() ||
      user.department?.trim() ||
      "Not available",
    roleNames: user.assignments.map((assignment) => assignment.roleName),
    applicationNames: user.applicationAccesses.map(
      (assignment) => assignment.applicationName,
    ),
  })),
);

const columns = [
  { key: "displayLabel", label: "Staff member" },
  { key: "departmentLabel", label: "Department" },
  { key: "roleNames", label: "Global roles" },
  { key: "applicationNames", label: "Application access" },
];

function initials(value: string): string {
  const words = value.trim().split(/\s+/).filter(Boolean);
  return words.length <= 1
    ? (words[0]?.slice(0, 2) || "?").toUpperCase()
    : `${words[0][0]}${words.at(-1)?.[0] ?? ""}`.toUpperCase();
}

</script>

<template>
  <NieDataTable
    preference-key="administration.access-control-users"
    :definition-version="1"
    v-model:search="search"
    :columns="columns"
    :data="rows"
    server-side
    :total-items="totalItems"
    :filter-option-pages="filterOptionPages"
    row-key="userId"
    :loading="loading"
    :error="error"
    search-placeholder="Search users, roles, or applications"
    empty-state-title="No users with access"
    empty-state-message="Assign a global role or application access to get started."
    hide-create
    hide-edit
    hide-delete
    @query-change="emit('query-change', $event)"
    @filter-options-request="emit('filter-options-request', $event)"
    @retry="emit('retry')"
  >
    <template #toolbar-actions>
      <NieButton v-if="canManage" @click="emit('assign', null)">
        <span class="material-symbols-outlined text-card-title">person_add</span>
        Assign access
      </NieButton>
    </template>

    <template #cell-displayLabel="{ row }">
      <div class="flex min-w-[17rem] items-start gap-3 py-1">
        <span
          class="inline-flex size-10 shrink-0 items-center justify-center rounded-xl bg-primary-600 text-sm font-bold text-on-brand shadow-[var(--theme-shadow-soft)]"
        >
          {{ initials(row.displayLabel) }}
        </span>
        <div class="min-w-0">
          <p class="font-semibold text-secondary-900 dark:text-white">
            {{ row.displayLabel }}
          </p>
          <p class="text-xs text-secondary-500">{{ row.userId }}</p>
          <a
            v-if="row.email"
            :href="`mailto:${row.email}`"
            class="mt-1 block max-w-64 truncate text-xs text-primary-700 hover:underline dark:text-primary-300"
          >
            {{ row.email }}
          </a>
          <p class="mt-1 text-caption text-secondary-400">
            {{ row.accessFunctionCodes.length }} effective access functions
          </p>
        </div>
      </div>
    </template>

    <template #cell-departmentLabel="{ row }">
      <div class="max-w-60 whitespace-normal">
        <p class="font-medium">{{ row.departmentLabel }}</p>
        <p
          v-if="row.designation || row.title"
          class="mt-1 text-xs text-secondary-500"
        >
          {{ row.designation || row.title }}
        </p>
      </div>
    </template>

    <template #cell-roleNames="{ row }">
      <div class="flex max-w-72 flex-wrap gap-1.5 whitespace-normal">
        <span
          v-for="assignment in row.assignments"
          :key="assignment.id"
          class="inline-flex items-center gap-1 rounded-full bg-primary-50 py-1 pl-2.5 pr-1 text-xs font-semibold text-primary-700 dark:bg-primary-950/40 dark:text-primary-300"
        >
          {{ assignment.roleName }}
          <button
            v-if="canManage"
            type="button"
            class="inline-flex size-6 items-center justify-center rounded-full hover:bg-primary-100 disabled:opacity-40"
            :disabled="saving"
            :aria-label="`Remove ${assignment.roleName} from ${row.displayLabel}`"
            @click.stop="emit('remove-global', assignment.id)"
          >
            <span class="material-symbols-outlined text-body">close</span>
          </button>
        </span>
        <span
          v-if="row.assignments.length === 0"
          class="text-xs text-secondary-400"
        >
          No global roles
        </span>
      </div>
    </template>

    <template #cell-applicationNames="{ row }">
      <div class="flex max-w-96 flex-wrap gap-1.5 whitespace-normal">
        <span
          v-for="assignment in row.applicationAccesses"
          :key="assignment.id"
          class="inline-flex items-center gap-1 rounded-full bg-secondary-100 py-1 pl-2.5 pr-1 text-xs text-secondary-700 dark:bg-secondary-800 dark:text-secondary-200"
        >
          <strong>{{ assignment.applicationName }}</strong>
          <span class="text-secondary-500">· {{ assignment.roleName }}</span>
          <button
            v-if="canManage"
            type="button"
            class="inline-flex size-6 items-center justify-center rounded-full hover:bg-secondary-200 disabled:opacity-40 dark:hover:bg-secondary-700"
            :disabled="saving"
            :aria-label="`Remove ${assignment.roleName} from ${assignment.applicationName}`"
            @click.stop="emit('remove-application', assignment.id)"
          >
            <span class="material-symbols-outlined text-body">close</span>
          </button>
        </span>
        <span
          v-if="row.applicationAccesses.length === 0"
          class="text-xs text-secondary-400"
        >
          Global roles only
        </span>
      </div>
    </template>

    <template #extra-actions="{ row }">
      <button
        v-if="canManage"
        type="button"
        class="inline-flex min-h-11 items-center gap-1 rounded-xl px-3 py-2 text-xs font-bold text-primary-700 hover:bg-primary-50 dark:text-primary-300 dark:hover:bg-primary-950/40"
        @click.stop="emit('assign', row)"
      >
        <span class="material-symbols-outlined text-body-lg">add</span>
        Add access
      </button>
    </template>
  </NieDataTable>
</template>

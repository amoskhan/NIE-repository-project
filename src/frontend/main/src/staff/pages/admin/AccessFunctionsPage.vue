<script setup lang="ts">
import { computed, onMounted, ref } from "vue";
import { AppDataTable } from "@apptemplate/ui";
import roleService from "@/services/roleService";
import { useToast } from "@/composables/useToast";
import { buildFilterOptions } from "@/utils/listFilterOptions";
import type { AccessFunction } from "@/types";

interface AccessFunctionRow extends AccessFunction {
  typeLabel: string;
  sourceLabel: string;
  targetLabel: string;
}

const toast = useToast();
const loading = ref(true);
const search = ref("");
const selectedFilters = ref<Record<string, Array<string | number | boolean>>>(
  {},
);
const accessFunctions = ref<AccessFunction[]>([]);

const rows = computed<AccessFunctionRow[]>(() =>
  accessFunctions.value.map((accessFunction) => ({
    ...accessFunction,
    typeLabel: formatType(accessFunction.type),
    sourceLabel: accessFunction.isSystemFunction ? "Code-defined" : "Database",
    targetLabel: formatTarget(accessFunction),
  })),
);

const columns = [
  { key: "name", label: "Access Function" },
  { key: "module", label: "Module" },
  { key: "typeLabel", label: "Type" },
  { key: "targetLabel", label: "Target" },
  { key: "sourceLabel", label: "Source" },
];

const filterGroups = computed(() => [
  {
    key: "module",
    label: "Module",
    options: buildFilterOptions(rows.value, (row) => row.module),
  },
  {
    key: "typeLabel",
    label: "Type",
    options: buildFilterOptions(rows.value, (row) => row.typeLabel),
  },
  {
    key: "sourceLabel",
    label: "Source",
    options: buildFilterOptions(rows.value, (row) => row.sourceLabel),
  },
]);

const screenCount = computed(
  () => rows.value.filter((row) => row.typeLabel === "Screen").length,
);

const apiCount = computed(
  () => rows.value.filter((row) => row.typeLabel === "API").length,
);

onMounted(async () => {
  await loadAccessFunctions();
});

async function loadAccessFunctions() {
  loading.value = true;

  try {
    accessFunctions.value = await roleService.getAllAccessFunctions();
  } catch {
    toast.error("Failed to load access functions");
  } finally {
    loading.value = false;
  }
}

function formatType(type: AccessFunction["type"]): string {
  if (type === 1 || type === "Screen") {
    return "Screen";
  }

  if (type === 2 || type === "Api") {
    return "API";
  }

  return String(type ?? "Unknown");
}

function formatTarget(accessFunction: AccessFunction): string {
  const routeOrResource =
    accessFunction.route || accessFunction.resourceName || "-";
  const method = accessFunction.httpMethod?.trim();

  return method ? `${method} ${routeOrResource}` : routeOrResource;
}

function searchAccessor(row: AccessFunctionRow) {
  return [
    row.name,
    row.code,
    row.module,
    row.typeLabel,
    row.sourceLabel,
    row.targetLabel,
    row.description,
    row.resourceName,
    row.route,
    row.httpMethod,
  ];
}

function typeBadgeClass(typeLabel: string): string {
  return typeLabel === "API"
    ? "bg-emerald-100 text-emerald-700"
    : "bg-sky-100 text-sky-700";
}

function sourceBadgeClass(sourceLabel: string): string {
  return sourceLabel === "Code-defined"
    ? "bg-violet-100 text-violet-700"
    : "bg-slate-100 text-slate-600";
}
</script>

<template>
  <div class="space-y-5 flex flex-col flex-1 min-h-0">
    <section
      class="rounded-[1.75rem] border border-slate-200 bg-white/95 p-6 shadow-soft backdrop-blur"
    >
      <div
        class="flex flex-col gap-4 lg:flex-row lg:items-start lg:justify-between"
      >
        <div class="max-w-3xl">
          <p
            class="text-xs font-bold uppercase tracking-[0.18em] text-slate-400"
          >
            Code-managed Catalog
          </p>
          <h2 class="mt-2 text-2xl font-bold text-slate-900">
            Access Functions
          </h2>
          <p class="mt-2 text-sm leading-6 text-slate-500">
            This catalogue is intentionally read-only in the UI. Add or change
            access functions in backend code through
            <span class="font-mono text-[12px] text-slate-600"
              >AccessFunctionCodes</span
            >
            and
            <span class="font-mono text-[12px] text-slate-600"
              >AccessFunctionCatalog</span
            >
            so route checks, API authorization, and seeded roles stay tightly
            aligned.
          </p>
        </div>

        <div class="grid gap-3 sm:grid-cols-3">
          <div
            class="rounded-2xl border border-slate-100 bg-slate-50 px-4 py-3"
          >
            <p
              class="text-xs font-bold uppercase tracking-[0.18em] text-slate-400"
            >
              Total
            </p>
            <p class="mt-2 text-2xl font-extrabold text-slate-900">
              {{ rows.length }}
            </p>
          </div>
          <div
            class="rounded-2xl border border-slate-100 bg-slate-50 px-4 py-3"
          >
            <p
              class="text-xs font-bold uppercase tracking-[0.18em] text-slate-400"
            >
              Screen
            </p>
            <p class="mt-2 text-2xl font-extrabold text-slate-900">
              {{ screenCount }}
            </p>
          </div>
          <div
            class="rounded-2xl border border-slate-100 bg-slate-50 px-4 py-3"
          >
            <p
              class="text-xs font-bold uppercase tracking-[0.18em] text-slate-400"
            >
              API
            </p>
            <p class="mt-2 text-2xl font-extrabold text-slate-900">
              {{ apiCount }}
            </p>
          </div>
        </div>
      </div>
    </section>

    <AppDataTable
      class="flex-1 min-h-0"
      v-model:search="search"
      v-model:selected-filters="selectedFilters"
      :columns="columns"
      :data="rows"
      :loading="loading"
      :filter-groups="filterGroups"
      row-key="id"
      search-placeholder="Search access functions"
      hide-create
      hide-edit
      hide-delete
      hide-actions
      :search-accessor="searchAccessor"
      @retry="loadAccessFunctions"
    >
      <template #cell-name="{ row }">
        <div>
          <p class="text-sm font-bold text-slate-800">{{ row.name }}</p>
          <p class="mt-1 text-[11px] font-mono text-slate-500">
            {{ row.code }}
          </p>
          <p
            v-if="row.description"
            class="mt-2 text-xs leading-5 text-slate-500"
          >
            {{ row.description }}
          </p>
        </div>
      </template>

      <template #cell-module="{ value }">
        <span
          class="rounded-full bg-slate-100 px-2.5 py-1 text-xs font-bold text-slate-600"
        >
          {{ value }}
        </span>
      </template>

      <template #cell-typeLabel="{ value }">
        <span
          class="rounded-full px-2.5 py-1 text-xs font-bold"
          :class="typeBadgeClass(String(value ?? ''))"
        >
          {{ value }}
        </span>
      </template>

      <template #cell-targetLabel="{ row }">
        <div>
          <p class="text-sm font-medium text-slate-700">
            {{ row.targetLabel }}
          </p>
          <p class="mt-1 text-[11px] text-slate-400">{{ row.resourceName }}</p>
        </div>
      </template>

      <template #cell-sourceLabel="{ value }">
        <span
          class="rounded-full px-2.5 py-1 text-xs font-bold"
          :class="sourceBadgeClass(String(value ?? ''))"
        >
          {{ value }}
        </span>
      </template>
    </AppDataTable>
  </div>
</template>

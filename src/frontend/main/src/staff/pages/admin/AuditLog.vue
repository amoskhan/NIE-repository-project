<script setup lang="ts">
import { computed, onMounted, ref } from "vue";
import { AppButton, AppDataTable, AppModal, useToast } from "@apptemplate/ui";
import auditLogService from "@/services/auditLogService";
import type { AuditLogEntry } from "@/types/audit";
import { buildFilterOptions } from "@/utils/listFilterOptions";

const toast = useToast();

const loading = ref(false);
const rows = ref<AuditLogEntry[]>([]);
const search = ref("");
const selectedFilters = ref<Record<string, Array<string | number | boolean>>>(
  {},
);
const selectedEntry = ref<AuditLogEntry | null>(null);

const columns = [
  { key: "timestamp", label: "Timestamp", type: "date" as const },
  { key: "systemName", label: "System" },
  { key: "entityName", label: "Entity" },
  { key: "entityId", label: "Entity ID" },
  { key: "action", label: "Action" },
  { key: "userName", label: "User" },
];

const filterGroups = computed(() => [
  {
    key: "entityName",
    label: "Entity",
    options: buildFilterOptions(rows.value, (row) => row.entityName),
  },
  {
    key: "action",
    label: "Action",
    options: buildFilterOptions(rows.value, (row) => row.action),
  },
]);

async function loadLogs() {
  loading.value = true;

  try {
    const result = await auditLogService.search({ page: 1, pageSize: 500 });
    rows.value = result.items;
  } catch {
    toast.error("Failed to load audit log");
    rows.value = [];
  } finally {
    loading.value = false;
  }
}

function openDetails(entry: AuditLogEntry) {
  selectedEntry.value = entry;
}

function handleDetailsModalChange(value: boolean) {
  if (!value) {
    selectedEntry.value = null;
  }
}

function formatTimestamp(timestamp: string | null | undefined) {
  if (!timestamp) {
    return "-";
  }

  return new Date(timestamp).toLocaleString("en-SG", {
    day: "2-digit",
    month: "short",
    year: "numeric",
    hour: "2-digit",
    minute: "2-digit",
    second: "2-digit",
  });
}

function formatActor(log: AuditLogEntry) {
  return log.userName || log.userId || "System";
}

function formatJson(value?: string | null) {
  if (!value) {
    return "No data recorded";
  }

  try {
    return JSON.stringify(JSON.parse(value), null, 2);
  } catch {
    return value;
  }
}

function exportCsv(entries: AuditLogEntry[]) {
  const rowsToExport = [
    [
      "Timestamp",
      "System",
      "Entity",
      "Entity ID",
      "Action",
      "User",
      "Description",
    ],
    ...entries.map((log) => [
      log.timestamp,
      log.systemName,
      log.entityName,
      log.entityId ?? "",
      log.action,
      formatActor(log),
      log.actionDescription ?? "",
    ]),
  ];

  const csv = rowsToExport
    .map((row) =>
      row.map((value) => `"${String(value).split('"').join('""')}"`).join(","),
    )
    .join("\n");

  const blob = new Blob([csv], { type: "text/csv" });
  const url = URL.createObjectURL(blob);
  const link = document.createElement("a");
  link.href = url;
  link.download = "audit-log.csv";
  link.click();
  URL.revokeObjectURL(url);
}

function actionClass(action: string): string {
  switch (action) {
    case "Create":
      return "bg-emerald-100 text-emerald-700";
    case "Update":
      return "bg-sky-100 text-sky-700";
    case "Delete":
      return "bg-rose-100 text-rose-700";
    default:
      return "bg-slate-100 text-slate-700";
  }
}

onMounted(() => {
  void loadLogs();
});
</script>

<template>
  <div class="space-y-4 flex flex-col flex-1 min-h-0">
    <AppDataTable
      class="flex-1 min-h-0"
      v-model:search="search"
      v-model:selected-filters="selectedFilters"
      :columns="columns"
      :data="rows"
      row-key="id"
      :loading="loading"
      :filter-groups="filterGroups"
      search-placeholder="Search all audit activity"
      hide-create
      hide-actions
      row-clickable
      @retry="loadLogs"
      @row-click="openDetails"
    >
      <template #toolbar-actions="{ filteredData }">
        <AppButton variant="outline" size="sm" @click="exportCsv(filteredData)">
          <span class="material-symbols-outlined text-[18px]">download</span>
          <span>Export CSV</span>
        </AppButton>
      </template>

      <template #cell-timestamp="{ value }">
        {{ formatTimestamp(value) }}
      </template>

      <template #cell-entityId="{ value }">
        {{ value || "-" }}
      </template>

      <template #cell-action="{ value }">
        <span
          class="rounded-full px-2 py-1 text-xs font-semibold"
          :class="actionClass(String(value ?? ''))"
        >
          {{ value }}
        </span>
      </template>

      <template #cell-userName="{ row }">
        {{ formatActor(row) }}
      </template>
    </AppDataTable>

    <AppModal
      :model-value="!!selectedEntry"
      title="Audit Entry"
      size="xl"
      @update:model-value="handleDetailsModalChange"
    >
      <div v-if="selectedEntry" class="space-y-5">
        <div class="grid gap-4 md:grid-cols-2">
          <div
            class="rounded-xl border border-secondary-200 p-4 dark:border-secondary-700"
          >
            <p
              class="text-xs font-semibold uppercase tracking-[0.18em] text-secondary-500 dark:text-secondary-400"
            >
              Timestamp
            </p>
            <p
              class="mt-2 text-sm font-medium text-secondary-900 dark:text-secondary-100"
            >
              {{ formatTimestamp(selectedEntry.timestamp) }}
            </p>
          </div>

          <div
            class="rounded-xl border border-secondary-200 p-4 dark:border-secondary-700"
          >
            <p
              class="text-xs font-semibold uppercase tracking-[0.18em] text-secondary-500 dark:text-secondary-400"
            >
              Action
            </p>
            <p
              class="mt-2 text-sm font-medium text-secondary-900 dark:text-secondary-100"
            >
              {{ selectedEntry.action }}
            </p>
          </div>

          <div
            class="rounded-xl border border-secondary-200 p-4 dark:border-secondary-700"
          >
            <p
              class="text-xs font-semibold uppercase tracking-[0.18em] text-secondary-500 dark:text-secondary-400"
            >
              Entity
            </p>
            <p
              class="mt-2 text-sm font-medium text-secondary-900 dark:text-secondary-100"
            >
              {{ selectedEntry.entityName }}
              <span v-if="selectedEntry.entityId"
                >#{{ selectedEntry.entityId }}</span
              >
            </p>
          </div>

          <div
            class="rounded-xl border border-secondary-200 p-4 dark:border-secondary-700"
          >
            <p
              class="text-xs font-semibold uppercase tracking-[0.18em] text-secondary-500 dark:text-secondary-400"
            >
              User
            </p>
            <p
              class="mt-2 text-sm font-medium text-secondary-900 dark:text-secondary-100"
            >
              {{ formatActor(selectedEntry) }}
            </p>
          </div>
        </div>

        <section class="space-y-3">
          <h3
            class="text-sm font-semibold text-secondary-900 dark:text-secondary-100"
          >
            Action Description
          </h3>
          <div
            class="rounded-xl border border-secondary-200 bg-secondary-50 p-4 text-sm text-secondary-700 dark:border-secondary-700 dark:bg-secondary-900 dark:text-secondary-200"
          >
            {{ selectedEntry.actionDescription || "No description recorded" }}
          </div>
        </section>

        <section class="space-y-3">
          <h3
            class="text-sm font-semibold text-secondary-900 dark:text-secondary-100"
          >
            Changed Properties
          </h3>
          <pre
            class="overflow-x-auto rounded-xl border border-secondary-200 bg-secondary-50 p-4 text-xs text-secondary-700 dark:border-secondary-700 dark:bg-secondary-900 dark:text-secondary-200"
            >{{ formatJson(selectedEntry.changedProperties) }}</pre>
        </section>

        <section class="grid gap-4 lg:grid-cols-2">
          <div class="space-y-3">
            <h3
              class="text-sm font-semibold text-secondary-900 dark:text-secondary-100"
            >
              Old Values
            </h3>
            <pre
              class="overflow-x-auto rounded-xl border border-secondary-200 bg-secondary-50 p-4 text-xs text-secondary-700 dark:border-secondary-700 dark:bg-secondary-900 dark:text-secondary-200"
              >{{ formatJson(selectedEntry.oldValues) }}</pre>
          </div>

          <div class="space-y-3">
            <h3
              class="text-sm font-semibold text-secondary-900 dark:text-secondary-100"
            >
              New Values
            </h3>
            <pre
              class="overflow-x-auto rounded-xl border border-secondary-200 bg-secondary-50 p-4 text-xs text-secondary-700 dark:border-secondary-700 dark:bg-secondary-900 dark:text-secondary-200"
              >{{ formatJson(selectedEntry.newValues) }}</pre>
          </div>
        </section>

        <section class="space-y-3">
          <h3
            class="text-sm font-semibold text-secondary-900 dark:text-secondary-100"
          >
            Additional Data
          </h3>
          <pre
            class="overflow-x-auto rounded-xl border border-secondary-200 bg-secondary-50 p-4 text-xs text-secondary-700 dark:border-secondary-700 dark:bg-secondary-900 dark:text-secondary-200"
            >{{ formatJson(selectedEntry.additionalData) }}</pre>
        </section>
      </div>
    </AppModal>
  </div>
</template>

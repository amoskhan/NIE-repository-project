<script setup lang="ts">
import { ref } from "vue";
import { NieButton, NieDataTable, NieModal } from "@nie/ui";
import auditLogService from "@/services/audit/auditLogService";
import type { AuditLogEntry } from "@/types/audit";
import { useServerDataTable } from "@/composables/data-tables/useServerDataTable";

const auditTable = useServerDataTable<AuditLogEntry>({
  search: auditLogService.search,
  getFilterOptions: auditLogService.getFilterOptions,
});
const {
  rows,
  totalItems,
  loading,
  error,
  filterOptionPages,
  load: loadLogs,
  loadFilterOptions,
  reload: reloadLogs,
} = auditTable;
const search = ref("");
const selectedFilters = ref<Record<string, Array<string | number | boolean>>>(
  {},
);
const selectedEntry = ref<AuditLogEntry | null>(null);

const columns = [
  { key: "timestamp", label: "Timestamp", type: "date" as const },
  { key: "categoryName", label: "Category" },
  { key: "entityName", label: "Entity" },
  { key: "entityId", label: "Entity ID" },
  {
    key: "actionName",
    label: "Action",
    chip: {
      toneMap: {
        Create: "success",
        Update: "info",
        Delete: "danger",
      },
      dot: true,
    },
  },
  { key: "userName", label: "User" },
];

function openDetails(entry: AuditLogEntry) {
  selectedEntry.value = entry;
}

function auditRowLabel(entry: AuditLogEntry): string {
  return `Open ${entry.actionName} audit entry for ${entry.entityName}`;
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
      log.categoryName,
      log.entityName,
      log.entityId ?? "",
      log.actionName,
      formatActor(log),
      log.outcome ?? "",
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

</script>

<template>
  <div class="space-y-4 flex flex-col flex-1 min-h-0">
    <NieDataTable
      preference-key="administration.audit-logs"
      :definition-version="1"
      class="flex-1 min-h-0"
      v-model:search="search"
      v-model:selected-filters="selectedFilters"
      :columns="columns"
      :data="rows"
      server-side
      :total-items="totalItems"
      :filter-option-pages="filterOptionPages"
      row-key="id"
      :loading="loading"
      :error="error"
      search-placeholder="Search all audit activity"
      hide-create
      hide-actions
      row-clickable
      :row-aria-label="auditRowLabel"
      @query-change="loadLogs"
      @filter-options-request="loadFilterOptions"
      @retry="reloadLogs"
      @row-click="openDetails"
    >
      <template #toolbar-actions="{ filteredData }">
        <NieButton variant="outline" @click="exportCsv(filteredData)">
          <span class="material-symbols-outlined text-card-title">download</span>
          <span>Export CSV</span>
        </NieButton>
      </template>

      <template #cell-timestamp="{ value }">
        {{ formatTimestamp(value) }}
      </template>

      <template #cell-entityId="{ value }">
        {{ value || "-" }}
      </template>

      <template #cell-userName="{ row }">
        {{ formatActor(row) }}
      </template>
    </NieDataTable>

    <NieModal
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
              class="text-xs font-semibold uppercase tracking-wide text-secondary-500 dark:text-secondary-400"
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
              class="text-xs font-semibold uppercase tracking-wide text-secondary-500 dark:text-secondary-400"
            >
              Action
            </p>
            <p
              class="mt-2 text-sm font-medium text-secondary-900 dark:text-secondary-100"
            >
              {{ selectedEntry.actionName }}
            </p>
          </div>

          <div
            class="rounded-xl border border-secondary-200 p-4 dark:border-secondary-700"
          >
            <p
              class="text-xs font-semibold uppercase tracking-wide text-secondary-500 dark:text-secondary-400"
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
              class="text-xs font-semibold uppercase tracking-wide text-secondary-500 dark:text-secondary-400"
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
            {{ selectedEntry.outcome || "No outcome recorded" }}
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
            >{{ formatJson(selectedEntry.changedProperties) }}</pre
          >
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
              >{{ formatJson(selectedEntry.oldValues) }}</pre
            >
          </div>

          <div class="space-y-3">
            <h3
              class="text-sm font-semibold text-secondary-900 dark:text-secondary-100"
            >
              New Values
            </h3>
            <pre
              class="overflow-x-auto rounded-xl border border-secondary-200 bg-secondary-50 p-4 text-xs text-secondary-700 dark:border-secondary-700 dark:bg-secondary-900 dark:text-secondary-200"
              >{{ formatJson(selectedEntry.newValues) }}</pre
            >
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
            >{{ formatJson(selectedEntry.additionalData) }}</pre
          >
        </section>
      </div>
    </NieModal>
  </div>
</template>

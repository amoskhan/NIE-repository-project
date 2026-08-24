<script setup lang="ts">
import { computed } from "vue";
import { cn } from "../../../lib/utils";
import { ChevronUpIcon, ChevronDownIcon } from "@heroicons/vue/24/outline";
import NieLoaderSymbol from "../../composite/loading/NieLoaderSymbol.vue";

export interface Column<T = unknown> {
  key: string;
  label: string;
  sortable?: boolean;
  width?: string;
  class?: string;
  render?: (row: T, index: number) => unknown;
}

interface Props {
  columns: Column[];
  data: unknown[];
  loading?: boolean;
  sortBy?: string;
  sortOrder?: "asc" | "desc";
  emptyMessage?: string;
  hoverable?: boolean;
  striped?: boolean;
  rowClickable?: boolean;
  rowAriaLabel?: (row: unknown, index: number) => string;
  class?: string;
}

const props = withDefaults(defineProps<Props>(), {
  loading: false,
  sortOrder: "asc",
  emptyMessage: "No data available",
  hoverable: true,
  striped: false,
  rowClickable: false,
});

const emit = defineEmits<{
  sort: [key: string, order: "asc" | "desc"];
  "row-click": [row: unknown, index: number];
}>();

const tableClasses = computed(() =>
  cn(
    "min-w-full divide-y divide-secondary-200 dark:divide-secondary-700",
    props.class
  )
);

const handleSort = (column: Column) => {
  if (!column.sortable) return;
  const newOrder = props.sortBy === column.key && props.sortOrder === "asc" ? "desc" : "asc";
  emit("sort", column.key, newOrder);
};

const getSortDirection = (column: Column): "ascending" | "descending" | "none" => {
  if (props.sortBy !== column.key) return "none";
  return props.sortOrder === "asc" ? "ascending" : "descending";
};

const handleRowActivation = (row: unknown, index: number, event?: Event) => {
  if (!props.rowClickable) return;

  const eventTarget = event?.target;
  const closestInteractive =
    eventTarget instanceof Element
      ? eventTarget.closest(
          "button, a[href], input, select, textarea, [role='button'], [role='link']",
        )
      : null;
  if (closestInteractive && closestInteractive !== event?.currentTarget) {
    return;
  }

  emit("row-click", row, index);
};

const getRowAriaLabel = (row: unknown, index: number): string => {
  const customLabel = props.rowAriaLabel?.(row, index)?.trim();
  return customLabel || `Open row ${index + 1}`;
};

const getCellValue = (row: unknown, column: Column, index: number): unknown => {
  if (column.render) {
    return column.render(row as never, index);
  }
  return (row as Record<string, unknown>)[column.key];
};
</script>

<template>
  <div class="overflow-x-auto rounded-lg border border-secondary-200 dark:border-secondary-700">
    <table :class="tableClasses">
      <thead class="bg-secondary-50 dark:bg-secondary-800">
        <tr>
          <th
            v-for="column in columns"
            :key="column.key"
            :style="{ width: column.width }"
            :class="[
              'px-6 py-3 text-left text-xs font-medium uppercase tracking-wide text-secondary-500 dark:text-secondary-400',
              column.class,
            ]"
            :aria-sort="column.sortable ? getSortDirection(column) : undefined"
          >
            <button
              v-if="column.sortable"
              type="button"
              class="flex min-h-11 w-full items-center gap-1 text-left hover:text-secondary-800 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary-500 dark:hover:text-secondary-100"
              :aria-label="`Sort by ${column.label}`"
              @click="handleSort(column)"
            >
              <span>{{ column.label }}</span>
              <span class="flex flex-col">
                <ChevronUpIcon
                  class="h-3 w-3"
                  :class="sortBy === column.key && sortOrder === 'asc' ? 'text-primary-600' : 'text-secondary-300'"
                />
                <ChevronDownIcon
                  class="h-3 w-3 -mt-1"
                  :class="sortBy === column.key && sortOrder === 'desc' ? 'text-primary-600' : 'text-secondary-300'"
                />
              </span>
            </button>
            <span v-else>{{ column.label }}</span>
          </th>
        </tr>
      </thead>
      <tbody class="divide-y divide-secondary-200 bg-white dark:divide-secondary-700 dark:bg-secondary-900">
        <!-- Loading state -->
        <tr v-if="loading">
          <td :colspan="columns.length" class="px-6 py-12 text-center">
            <div class="flex items-center justify-center gap-2">
              <NieLoaderSymbol size="sm" label="Loading table data" />
              <span class="text-secondary-500 dark:text-secondary-400">Loading...</span>
            </div>
          </td>
        </tr>

        <!-- Empty state -->
        <tr v-else-if="!data.length">
          <td :colspan="columns.length" class="px-6 py-12 text-center text-secondary-500 dark:text-secondary-400">
            {{ emptyMessage }}
          </td>
        </tr>

        <!-- Data rows -->
        <tr
          v-else
          v-for="(row, index) in data"
          :key="index"
          :data-table-interactive-row="rowClickable ? '' : undefined"
          role="row"
          :tabindex="rowClickable ? 0 : undefined"
          :aria-label="rowClickable ? getRowAriaLabel(row, index) : undefined"
          :class="[
            hoverable ? 'hover:bg-secondary-50 dark:hover:bg-secondary-800' : '',
            rowClickable ? 'cursor-pointer focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-inset focus-visible:ring-primary-500' : '',
            striped && index % 2 === 1 ? 'bg-secondary-50 dark:bg-secondary-800/50' : '',
          ]"
          @click="handleRowActivation(row, index, $event)"
          @keydown.enter.self="handleRowActivation(row, index)"
          @keydown.space.self.prevent="handleRowActivation(row, index)"
        >
          <td
            v-for="column in columns"
            :key="column.key"
            class="whitespace-nowrap px-6 py-4 text-sm text-secondary-900 dark:text-secondary-100"
          >
            <slot :name="`cell-${column.key}`" :row="row" :value="getCellValue(row, column, index)" :index="index">
              {{ getCellValue(row, column, index) }}
            </slot>
          </td>
        </tr>
      </tbody>
    </table>
  </div>
</template>

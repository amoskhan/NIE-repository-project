<template>
  <div
    class="data-table-container bg-white dark:bg-gray-800 rounded-2xl shadow transition-colors duration-300"
  >
    <!-- Header with Actions -->
    <div class="px-4 md:px-6 py-4 border-b dark:border-gray-700 text-left">
      <div class="flex items-center justify-between gap-3">
        <div class="hidden min-w-0 flex-1">
          <h2
            class="text-xl font-bold text-gray-800 dark:text-white truncate font-heading"
          >
            {{ title }}
          </h2>
        </div>

        <div class="flex items-center gap-2 flex-nowrap flex-shrink-0">
          <div
            v-if="isSearchable"
            class="relative w-48 sm:w-64 md:w-72 flex-shrink-0"
          >
            <input
              v-model="searchQuery"
              type="text"
              placeholder="Search records..."
              class="w-full pl-10 pr-4 py-2 border border-gray-200 dark:border-gray-600 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 bg-white dark:bg-gray-700 text-gray-900 dark:text-white placeholder-gray-500 dark:placeholder-gray-400 text-sm"
              @input="handleSearch"
            />
            <MagnifyingGlassIcon
              class="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-gray-400 dark:text-gray-500"
            />
          </div>

          <!-- Create Button -->
          <button
            v-if="!hideCreate"
            class="inline-flex items-center justify-center gap-1.5 px-3.5 py-2 bg-gradient-to-r from-blue-600 to-indigo-600 text-white rounded-lg shadow-sm hover:shadow-lg transition-all text-sm font-semibold whitespace-nowrap flex-shrink-0"
            @click="$emit('create')"
          >
            <PlusIcon class="h-4 w-4" />
            <span>New</span>
          </button>
        </div>
      </div>

      <p
        v-if="description"
        class="hidden md:block text-sm text-gray-800 dark:text-gray-400 mt-1 font-medium"
        style="font-family: var(--font-body)"
      >
        {{ description }}
      </p>
    </div>

    <div class="data-table-body px-4 md:px-6 py-6">
      <!-- Loading State -->
      <div v-if="loading" class="p-12 flex items-center justify-center">
        <div class="text-center">
          <div
            class="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600 mx-auto"
          ></div>
          <p class="mt-4 text-gray-600 dark:text-gray-300">Loading...</p>
        </div>
      </div>

      <!-- Error State -->
      <div v-else-if="error" class="p-12 text-center">
        <div class="text-red-600 mb-4">
          <ExclamationTriangleIcon class="h-12 w-12 mx-auto" />
        </div>
        <p class="text-gray-700 dark:text-gray-300 font-medium">{{ error }}</p>
        <button
          class="mt-4 px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700"
          @click="$emit('retry')"
        >
          Retry
        </button>
      </div>

      <!-- Empty State -->
      <div v-else-if="!data || data.length === 0" class="p-12 text-center">
        <div class="text-gray-400 dark:text-gray-500 mb-4">
          <InboxIcon class="h-12 w-12 mx-auto" />
        </div>
        <p class="text-gray-600 dark:text-gray-300 font-medium">
          No records found
        </p>
        <p class="text-sm text-gray-500 dark:text-gray-400 mt-1">
          Create your first record to get started
        </p>
      </div>

      <!-- Data Views -->
      <div v-else>
        <div
          v-if="filteredData.length === 0"
          class="p-10 text-center text-gray-500 dark:text-gray-400"
        >
          <p class="text-base font-semibold">No matching records</p>
          <p class="text-sm mt-1">Try refining your search terms.</p>
        </div>

        <div v-else>
          <!-- Data Table - Desktop -->
          <div class="hidden md:block overflow-x-auto relative pb-2">
            <table class="min-w-max w-full">
              <thead
                class="bg-gray-50 dark:bg-gray-700 border-b dark:border-gray-600"
              >
                <tr>
                  <th
                    v-for="column in columns"
                    :key="column.key"
                    class="px-6 py-3 text-left text-xs font-medium text-gray-800 dark:text-gray-300 uppercase tracking-wider select-none cursor-pointer"
                    style="font-family: var(--font-body)"
                    @click="toggleSort(column)"
                  >
                    <div class="flex items-center gap-1">
                      <span>{{ column.label }}</span>
                      <ChevronUpIcon
                        v-if="sortKey === column.key && sortDirection === 'asc'"
                        class="h-4 w-4 text-gray-600 dark:text-gray-300"
                      />
                      <ChevronDownIcon
                        v-else-if="
                          sortKey === column.key && sortDirection === 'desc'
                        "
                        class="h-4 w-4 text-gray-600 dark:text-gray-300"
                      />
                    </div>
                  </th>
                  <th
                    v-if="!hideActions"
                    class="px-6 py-3 text-right text-xs font-medium text-gray-800 dark:text-gray-300 uppercase tracking-wider sticky right-0 bg-gray-50 dark:bg-gray-700 border-l border-gray-200 dark:border-gray-600"
                    style="font-family: var(--font-body)"
                  >
                    Actions
                  </th>
                </tr>
              </thead>
              <tbody
                class="bg-white dark:bg-gray-800 divide-y divide-gray-200 dark:divide-gray-700"
              >
                <tr
                  v-for="row in paginatedData"
                  :key="row[rowKey]"
                  :class="[
                    'hover:bg-gray-50 dark:hover:bg-gray-700 transition-colors',
                    rowClickable ? 'cursor-pointer' : '',
                  ]"
                  @click="rowClickable ? $emit('row-click', row) : null"
                >
                  <td
                    v-for="column in columns"
                    :key="column.key"
                    class="px-6 py-4 whitespace-nowrap text-sm text-gray-900 dark:text-gray-100"
                  >
                    <slot
                      :name="`cell-${column.key}`"
                      :row="row"
                      :value="row[column.key]"
                    >
                      {{ formatCellValue(row[column.key], column) }}
                    </slot>
                  </td>
                  <td
                    v-if="!hideActions"
                    class="px-6 py-4 whitespace-nowrap text-right text-sm font-medium sticky right-0 bg-white dark:bg-gray-800 border-l border-gray-200 dark:border-gray-700"
                  >
                    <div class="flex items-center justify-end gap-2">
                      <button
                        v-if="!hideEdit"
                        class="text-blue-600 hover:text-blue-800 dark:text-blue-400 dark:hover:text-blue-300"
                        title="Edit"
                        @click="$emit('edit', row)"
                      >
                        <PencilIcon class="h-5 w-5" />
                      </button>
                      <button
                        v-if="!hideDelete"
                        :disabled="!canDeleteRow(row)"
                        :title="deleteDisabledTitleForRow(row) ?? 'Delete'"
                        :class="[
                          'dark:text-red-400',
                          canDeleteRow(row)
                            ? 'text-red-600 hover:text-red-800 dark:hover:text-red-300'
                            : 'text-gray-300 dark:text-gray-600 cursor-not-allowed',
                        ]"
                        @click="$emit('delete', row)"
                      >
                        <TrashIcon class="h-5 w-5" />
                      </button>
                      <slot name="extra-actions" :row="row"></slot>
                    </div>
                  </td>
                </tr>
              </tbody>
            </table>
          </div>

          <!-- Mobile Card View -->
          <div class="md:hidden space-y-2.5">
            <div
              v-for="row in paginatedData"
              :key="row[rowKey]"
              :class="[
                'rounded-lg border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-900 shadow-sm p-2.5',
                rowClickable ? 'cursor-pointer' : '',
              ]"
              @click="rowClickable ? $emit('row-click', row) : null"
            >
              <div class="flex flex-col gap-1.5">
                <div
                  v-for="column in columns"
                  :key="column.key"
                  class="flex items-start justify-between gap-2 min-h-[24px]"
                >
                  <span
                    class="text-[10px] font-semibold uppercase tracking-wide text-gray-500 dark:text-gray-400 leading-tight flex-shrink-0"
                  >
                    {{ column.label }}
                  </span>
                  <span
                    class="text-[13px] text-gray-900 dark:text-gray-100 text-right leading-tight"
                  >
                    <slot
                      :name="`cell-${column.key}`"
                      :row="row"
                      :value="row[column.key]"
                    >
                      {{ formatCellValue(row[column.key], column) }}
                    </slot>
                  </span>
                </div>
              </div>
              <div
                v-if="!hideActions"
                class="flex items-center justify-end gap-2 pt-2 mt-2 border-t border-gray-200 dark:border-gray-700"
              >
                <button
                  v-if="!hideEdit"
                  class="text-blue-600 hover:text-blue-800 dark:text-blue-400 dark:hover:text-blue-300 p-1"
                  title="Edit"
                  @click="$emit('edit', row)"
                >
                  <PencilIcon class="h-4 w-4" />
                </button>
                <button
                  v-if="!hideDelete"
                  :disabled="!canDeleteRow(row)"
                  :title="deleteDisabledTitleForRow(row) ?? 'Delete'"
                  :class="[
                    'p-1 dark:text-red-400',
                    canDeleteRow(row)
                      ? 'text-red-600 hover:text-red-800 dark:hover:text-red-300'
                      : 'text-gray-300 dark:text-gray-600 cursor-not-allowed',
                  ]"
                  @click="$emit('delete', row)"
                >
                  <TrashIcon class="h-4 w-4" />
                </button>
                <slot name="extra-actions" :row="row"></slot>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Pagination -->
    <div
      v-if="data && data.length > 0"
      class="px-4 md:px-6 py-3 md:py-4 border-t dark:border-gray-700 flex flex-col items-stretch gap-3 md:flex-row md:justify-end md:items-center"
    >
      <div
        class="text-xs md:text-sm text-gray-700 dark:text-gray-300 md:text-right"
      >
        Showing {{ startIndex + 1 }} to {{ endIndex }} of
        {{ filteredData.length }} results
      </div>
      <div class="flex items-center justify-end gap-2 text-sm">
        <button
          :disabled="currentPage === 1 || totalPages === 0"
          :class="[
            'px-3 py-1.5 rounded-lg transition-colors text-xs md:text-sm font-medium',
            currentPage === 1 || totalPages === 0
              ? 'bg-gray-100 text-gray-400 dark:bg-gray-800 dark:text-gray-600 cursor-not-allowed'
              : 'bg-gray-200 text-gray-700 hover:bg-gray-300 dark:bg-gray-700 dark:text-gray-200 dark:hover:bg-gray-600',
          ]"
          @click="currentPage = 1"
        >
          First
        </button>
        <button
          :disabled="currentPage === 1 || totalPages === 0"
          :class="[
            'px-3 py-1.5 rounded-lg transition-colors text-xs md:text-sm font-medium',
            currentPage === 1 || totalPages === 0
              ? 'bg-gray-100 text-gray-400 dark:bg-gray-800 dark:text-gray-600 cursor-not-allowed'
              : 'bg-gray-200 text-gray-700 hover:bg-gray-300 dark:bg-gray-700 dark:text-gray-200 dark:hover:bg-gray-600',
          ]"
          @click="currentPage--"
        >
          Prev
        </button>
        <span
          class="px-3 py-1 rounded-lg bg-gray-100 dark:bg-gray-700 text-xs md:text-sm font-semibold text-gray-700 dark:text-gray-200"
        >
          {{ currentPage }}/{{ totalPages }}
        </span>
        <button
          :disabled="currentPage === totalPages || totalPages === 0"
          :class="[
            'px-3 py-1.5 rounded-lg transition-colors text-xs md:text-sm font-medium',
            currentPage === totalPages || totalPages === 0
              ? 'bg-gray-100 text-gray-400 dark:bg-gray-800 dark:text-gray-600 cursor-not-allowed'
              : 'bg-gray-200 text-gray-700 hover:bg-gray-300 dark:bg-gray-700 dark:text-gray-200 dark:hover:bg-gray-600',
          ]"
          @click="currentPage++"
        >
          Next
        </button>
        <button
          :disabled="currentPage === totalPages || totalPages === 0"
          :class="[
            'px-3 py-1.5 rounded-lg transition-colors text-xs md:text-sm font-medium',
            currentPage === totalPages || totalPages === 0
              ? 'bg-gray-100 text-gray-400 dark:bg-gray-800 dark:text-gray-600 cursor-not-allowed'
              : 'bg-gray-200 text-gray-700 hover:bg-gray-300 dark:bg-gray-700 dark:text-gray-200 dark:hover:bg-gray-600',
          ]"
          @click="currentPage = totalPages"
        >
          Last
        </button>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch } from "vue";
import {
  MagnifyingGlassIcon,
  PlusIcon,
  PencilIcon,
  TrashIcon,
  ExclamationTriangleIcon,
  InboxIcon,
} from "@heroicons/vue/24/outline";
import { ChevronDownIcon, ChevronUpIcon } from "@heroicons/vue/20/solid";

// Props
interface Column {
  key: string;
  label: string;
  type?: "text" | "number" | "boolean" | "date" | "custom";
  format?: (value: any) => string;
  decimals?: number;
}

type SortDirection = "asc" | "desc";

const props = withDefaults(
  defineProps<{
    title: string;
    description?: string;
    columns: Column[];
    data: any[] | null;
    rowKey: string;
    loading?: boolean;
    error?: string | null;
    searchable?: boolean;
    hideCreate?: boolean;
    hideEdit?: boolean;
    hideDelete?: boolean;
    hideActions?: boolean;
    pageSize?: number;
    page?: number;
    search?: string;
    /** Emit "row-click" on row tap; sets cursor: pointer too. */
    rowClickable?: boolean;
    canDelete?: (row: any) => boolean;
    deleteDisabledTitle?: (row: any) => string;
  }>(),
  {
    searchable: true,
    pageSize: 10,
    rowClickable: false,
    canDelete: () => true,
  },
);

// Set defaults
const isSearchable = computed(() => props.searchable);
const pageSizeValue = computed(() => props.pageSize);
const emit = defineEmits<{
  create: [];
  edit: [row: any];
  delete: [row: any];
  "row-click": [row: any];
  search: [query: string];
  retry: [];
  "update:page": [page: number];
  "update:search": [query: string];
}>();

const canDeleteRow = (row: any): boolean => {
  try {
    return props.canDelete?.(row) ?? true;
  } catch {
    return true;
  }
};

const deleteDisabledTitleForRow = (row: any): string | undefined => {
  if (canDeleteRow(row)) return undefined;
  try {
    return props.deleteDisabledTitle?.(row);
  } catch {
    return undefined;
  }
};

// State
const searchQuery = ref(props.search ?? "");
const currentPage = ref(props.page ?? 1);

const sortKey = ref<string | null>(null);
const sortDirection = ref<SortDirection | null>(null);

watch(
  () => props.search,
  (value) => {
    const next = value ?? "";
    if (next !== searchQuery.value) {
      searchQuery.value = next;
    }
  },
);

watch(
  () => props.page,
  (value) => {
    const next = typeof value === "number" && value > 0 ? value : 1;
    if (next !== currentPage.value) {
      currentPage.value = next;
    }
  },
);

// Computed
const filteredData = computed(() => {
  if (!props.data) return [];
  if (!isSearchable.value || !searchQuery.value.trim()) {
    return props.data;
  }

  const query = searchQuery.value.trim().toLowerCase();
  return props.data.filter((row) =>
    props.columns.some((column) => {
      const value = row[column.key];
      if (value === null || value === undefined) {
        return false;
      }
      return String(value).toLowerCase().includes(query);
    }),
  );
});

const compareValues = (
  aValue: any,
  bValue: any,
  column: Column,
  direction: SortDirection,
): number => {
  const dir = direction === "asc" ? 1 : -1;

  const aIsNil = aValue === null || aValue === undefined || aValue === "";
  const bIsNil = bValue === null || bValue === undefined || bValue === "";
  if (aIsNil && bIsNil) return 0;
  if (aIsNil) return 1 * dir;
  if (bIsNil) return -1 * dir;

  switch (column.type) {
    case "number": {
      const aNum = typeof aValue === "number" ? aValue : Number(aValue);
      const bNum = typeof bValue === "number" ? bValue : Number(bValue);
      const aOk = Number.isFinite(aNum);
      const bOk = Number.isFinite(bNum);
      if (aOk && bOk) return (aNum - bNum) * dir;
      if (aOk) return -1 * dir;
      if (bOk) return 1 * dir;
      return String(aValue).localeCompare(String(bValue)) * dir;
    }
    case "boolean": {
      const aBool = aValue ? 1 : 0;
      const bBool = bValue ? 1 : 0;
      return (aBool - bBool) * dir;
    }
    case "date": {
      const aTime = new Date(aValue).getTime();
      const bTime = new Date(bValue).getTime();
      const aOk = Number.isFinite(aTime);
      const bOk = Number.isFinite(bTime);
      if (aOk && bOk) return (aTime - bTime) * dir;
      if (aOk) return -1 * dir;
      if (bOk) return 1 * dir;
      return String(aValue).localeCompare(String(bValue)) * dir;
    }
    default:
      return (
        String(aValue).localeCompare(String(bValue), undefined, {
          numeric: true,
          sensitivity: "base",
        }) * dir
      );
  }
};

const sortedData = computed(() => {
  if (!filteredData.value.length) return [];
  if (!sortKey.value || !sortDirection.value) return filteredData.value;

  const column = props.columns.find((c) => c.key === sortKey.value);
  if (!column) return filteredData.value;

  const withIndex = filteredData.value.map((row, index) => ({ row, index }));
  withIndex.sort((a, b) => {
    const primary = compareValues(
      a.row[sortKey.value as string],
      b.row[sortKey.value as string],
      column,
      sortDirection.value as SortDirection,
    );
    if (primary !== 0) return primary;
    return a.index - b.index;
  });
  return withIndex.map((x) => x.row);
});

const totalPages = computed(() => {
  if (!sortedData.value.length) return 0;
  return Math.ceil(sortedData.value.length / pageSizeValue.value);
});

const startIndex = computed(() => {
  if (!filteredData.value.length) return 0;
  return (currentPage.value - 1) * pageSizeValue.value;
});

const endIndex = computed(() => {
  if (!sortedData.value.length) return 0;
  return Math.min(
    startIndex.value + pageSizeValue.value,
    sortedData.value.length,
  );
});

const paginatedData = computed(() => {
  if (!sortedData.value.length) return [];
  return sortedData.value.slice(startIndex.value, endIndex.value);
});

watch([searchQuery, () => pageSizeValue.value], () => {
  currentPage.value = 1;
  emit("update:page", 1);
});

watch(totalPages, (value) => {
  if (value === 0) {
    currentPage.value = 1;
    emit("update:page", 1);
    return;
  }
  if (currentPage.value > value) {
    currentPage.value = value;
    emit("update:page", value);
  }
});

const toggleSort = (column: Column) => {
  if (sortKey.value !== column.key) {
    sortKey.value = column.key;
    sortDirection.value = "asc";
    return;
  }

  if (sortDirection.value === "asc") {
    sortDirection.value = "desc";
    return;
  }

  // Return to the default "no sorting" state
  sortKey.value = null;
  sortDirection.value = null;
};

// Methods
let searchTimeout: ReturnType<typeof setTimeout> | null = null;
const handleSearch = () => {
  if (searchTimeout) {
    clearTimeout(searchTimeout);
  }
  const normalizedQuery = searchQuery.value.trim();
  emit("update:search", normalizedQuery);
  searchTimeout = setTimeout(() => {
    emit("search", normalizedQuery);
  }, 300);
};

watch(currentPage, (value) => {
  const next = typeof value === "number" && value > 0 ? value : 1;
  if (next !== value) {
    currentPage.value = next;
    return;
  }
  emit("update:page", next);
});

const formatCellValue = (value: any, column: Column): string => {
  if (value === null || value === undefined) return "-";

  if (column.format) {
    return column.format(value);
  }

  switch (column.type) {
    case "boolean":
      return value ? "Yes" : "No";
    case "date":
      return new Date(value).toLocaleDateString();
    case "number":
      if (typeof value === "number") {
        if (Number.isInteger(value)) {
          return value.toLocaleString();
        }

        const decimals = column.decimals ?? 2;
        return value.toLocaleString(undefined, {
          minimumFractionDigits: decimals,
          maximumFractionDigits: decimals,
        });
      }
      return String(value);
    default:
      return String(value);
  }
};
</script>

<style scoped>
.data-table-container {
  display: flex;
  flex-direction: column;
  height: 100%;
  max-height: calc(100vh - 120px);
}

.data-table-body {
  flex: 1 1 auto;
  overflow-y: auto;
  overflow-x: hidden;
  -webkit-overflow-scrolling: touch;
  scrollbar-width: thin;
  scrollbar-color: rgba(156, 163, 175, 0.5) transparent;
}

.data-table-body::-webkit-scrollbar {
  width: 6px;
}

.data-table-body::-webkit-scrollbar-track {
  background: transparent;
}

.data-table-body::-webkit-scrollbar-thumb {
  background: rgba(156, 163, 175, 0.5);
  border-radius: 3px;
}

.data-table-body::-webkit-scrollbar-thumb:hover {
  background: rgba(156, 163, 175, 0.7);
}

@media (max-width: 767px) {
  .data-table-container {
    max-height: calc(100vh - 80px);
  }
}

@media (min-width: 768px) {
  .data-table-body {
    min-height: 400px;
  }
}
</style>

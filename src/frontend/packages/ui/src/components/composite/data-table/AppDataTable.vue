<template>
  <div
    class="data-table-container overflow-visible rounded-2xl border border-secondary-200 bg-white shadow transition-colors duration-300 dark:border-secondary-700 dark:bg-secondary-900"
  >
    <AppListControls
      v-if="showToolbar"
      :search-term="searchQuery"
      :search-placeholder="searchPlaceholder"
      :selected-filters="selectedFilterState"
      :filter-groups="resolvedFilterGroups"
      :filter-dropdown-visibility="toolbarFilterDropdownVisibility"
      :mobile-show-back-button="mobileShowBackButton"
      :mobile-back-aria-label="mobileBackAriaLabel"
      :summary="summaryText"
      :show-search="isSearchable"
      @update:search-term="handleSearchInput"
      @update:selected-filters="handleFiltersUpdate"
      @back="$emit('back')"
    >
      <template #actions>
        <slot name="toolbar-actions" :filtered-data="filteredData"></slot>

        <AppButton v-if="!hideCreate" size="sm" @click="$emit('create')">
          <PlusIcon class="h-4 w-4" />
          <span>{{ createLabel }}</span>
        </AppButton>
      </template>
    </AppListControls>

    <div
      class="data-table-body px-4 pt-6 md:px-6"
      :style="mobileToolbarInsetStyle"
    >
      <div v-if="loading" class="flex items-center justify-center p-12">
        <div class="text-center">
          <AppLoaderSymbol
            size="lg"
            label="Loading table data"
            class="mx-auto"
          />
          <p class="mt-4 text-secondary-600 dark:text-secondary-300">
            Loading...
          </p>
        </div>
      </div>

      <div v-else-if="error" class="p-12 text-center">
        <div class="mb-4 text-red-600">
          <ExclamationTriangleIcon class="mx-auto h-12 w-12" />
        </div>
        <p class="font-medium text-secondary-700 dark:text-secondary-300">
          {{ error }}
        </p>
        <AppButton class="mt-4" @click="$emit('retry')">Retry</AppButton>
      </div>

      <div v-else-if="!data || data.length === 0" class="p-12 text-center">
        <div class="mb-4 text-secondary-400 dark:text-secondary-500">
          <InboxIcon class="mx-auto h-12 w-12" />
        </div>
        <p class="font-medium text-secondary-600 dark:text-secondary-300">
          {{ emptyStateTitle }}
        </p>
        <p class="mt-1 text-sm text-secondary-500 dark:text-secondary-400">
          {{ emptyStateMessage }}
        </p>
      </div>

      <div v-else>
        <div
          v-if="filteredData.length === 0"
          class="p-10 text-center text-secondary-500 dark:text-secondary-400"
        >
          <p class="text-base font-semibold">No matching records</p>
          <p class="mt-1 text-sm">Try clearing some filters or search terms.</p>
        </div>

        <div v-else>
          <div
            class="relative hidden overflow-x-auto overflow-y-visible pb-2 md:block"
          >
            <table class="min-w-max w-full">
              <thead
                class="border-b bg-secondary-50 dark:border-secondary-700 dark:bg-secondary-800"
              >
                <tr>
                  <th
                    v-for="(column, columnIndex) in columns"
                    :key="column.key"
                    class="relative select-none overflow-visible px-6 py-4 text-left align-middle text-[13px] font-semibold uppercase tracking-[0.14em] text-secondary-700 dark:text-secondary-300"
                  >
                    <div class="flex items-center justify-between gap-3">
                      <button
                        type="button"
                        class="group flex min-w-0 flex-1 items-center gap-2 text-left"
                        @click="toggleSort(column)"
                      >
                        <span class="truncate">{{ column.label }}</span>
                        <ChevronUpIcon
                          v-if="
                            sortKey === column.key && sortDirection === 'asc'
                          "
                          class="h-4 w-4 text-secondary-600 dark:text-secondary-300"
                        />
                        <ChevronDownIcon
                          v-else-if="
                            sortKey === column.key && sortDirection === 'desc'
                          "
                          class="h-4 w-4 text-secondary-600 dark:text-secondary-300"
                        />
                      </button>

                      <AppColumnFilterMenu
                        v-if="getColumnFilterGroup(column.key)"
                        :column-label="column.label"
                        :model-value="selectedFilterState[column.key] ?? []"
                        :options="
                          getColumnFilterGroup(column.key)?.options ?? []
                        "
                        :align="
                          columnIndex < columns.length / 2 ? 'left' : 'right'
                        "
                        @update:model-value="
                          handleColumnFilterSelection(column.key, $event)
                        "
                        @search-all="
                          emit('column-filter-search', column.key, $event)
                        "
                      />
                    </div>
                  </th>
                  <th
                    v-if="!hideActions"
                    class="sticky right-0 border-l border-secondary-200 bg-secondary-50 px-6 py-3 text-right text-xs font-medium uppercase tracking-wider text-secondary-700 dark:border-secondary-700 dark:bg-secondary-800 dark:text-secondary-300"
                  >
                    Actions
                  </th>
                </tr>
              </thead>
              <tbody
                class="divide-y divide-secondary-200 bg-white dark:divide-secondary-700 dark:bg-secondary-900"
              >
                <tr
                  v-for="row in paginatedData"
                  :key="row[rowKey]"
                  :class="[
                    rowClickable
                      ? 'cursor-pointer transition-colors hover:bg-secondary-50 dark:hover:bg-secondary-800/70'
                      : 'transition-colors hover:bg-secondary-50 dark:hover:bg-secondary-800/50',
                  ]"
                  @click="handleRowClick(row)"
                >
                  <td
                    v-for="column in columns"
                    :key="column.key"
                    class="px-6 py-4 whitespace-nowrap text-sm text-secondary-900 dark:text-secondary-100"
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
                    class="sticky right-0 border-l border-secondary-200 bg-white px-6 py-4 text-right text-sm font-medium dark:border-secondary-700 dark:bg-secondary-900"
                  >
                    <div class="flex items-center justify-end gap-2">
                      <button
                        v-if="!hideEdit"
                        class="text-primary-600 transition hover:text-primary-800 dark:text-primary-400 dark:hover:text-primary-300"
                        title="Edit"
                        @click.stop="$emit('edit', row)"
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
                            ? 'text-red-600 transition hover:text-red-800 dark:hover:text-red-300'
                            : 'cursor-not-allowed text-secondary-300 dark:text-secondary-600',
                        ]"
                        @click.stop="$emit('delete', row)"
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

          <div class="space-y-2.5 md:hidden">
            <div
              v-for="row in paginatedData"
              :key="row[rowKey]"
              :class="[
                'min-w-0 rounded-lg border border-secondary-200 bg-white p-2.5 shadow-sm dark:border-secondary-700 dark:bg-secondary-950',
                rowClickable ? 'cursor-pointer' : '',
              ]"
              @click="handleRowClick(row)"
            >
              <div class="flex flex-col gap-1.5">
                <div
                  v-for="column in columns"
                  :key="column.key"
                  class="grid min-h-[24px] gap-1 sm:grid-cols-[minmax(5rem,auto)_minmax(0,1fr)] sm:gap-2"
                >
                  <span
                    class="min-w-0 text-[10px] font-semibold uppercase tracking-wide text-secondary-500 dark:text-secondary-400"
                  >
                    {{ column.label }}
                  </span>
                  <span
                    class="min-w-0 break-words text-left text-[13px] leading-tight text-secondary-900 dark:text-secondary-100 sm:text-right"
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
                class="mt-2 flex items-center justify-end gap-2 border-t border-secondary-200 pt-2 dark:border-secondary-700"
              >
                <button
                  v-if="!hideEdit"
                  class="inline-flex h-10 w-10 items-center justify-center text-primary-600 transition hover:text-primary-800 dark:text-primary-400 dark:hover:text-primary-300"
                  title="Edit"
                  @click.stop="$emit('edit', row)"
                >
                  <PencilIcon class="h-5 w-5" />
                </button>
                <button
                  v-if="!hideDelete"
                  :disabled="!canDeleteRow(row)"
                  :title="deleteDisabledTitleForRow(row) ?? 'Delete'"
                  :class="[
                    'inline-flex h-10 w-10 items-center justify-center dark:text-red-400',
                    canDeleteRow(row)
                      ? 'text-red-600 transition hover:text-red-800 dark:hover:text-red-300'
                      : 'cursor-not-allowed text-secondary-300 dark:text-secondary-600',
                  ]"
                  @click.stop="$emit('delete', row)"
                >
                  <TrashIcon class="h-5 w-5" />
                </button>
                <slot name="extra-actions" :row="row"></slot>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>

    <div
      v-if="filteredData.length > 0"
      class="border-t border-secondary-200 px-4 py-4 dark:border-secondary-700 md:px-6"
      :style="mobileToolbarInsetStyle"
    >
      <AppPagination
        :current-page="currentPage"
        :total-pages="Math.max(totalPages, 1)"
        :total-items="filteredData.length"
        :items-per-page="pageSizeValue"
        @update:current-page="handlePageChange"
      />
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, onUnmounted, ref, watch } from "vue";
import {
  ExclamationTriangleIcon,
  InboxIcon,
  PencilIcon,
  PlusIcon,
  TrashIcon,
} from "@heroicons/vue/24/outline";
import { ChevronDownIcon, ChevronUpIcon } from "@heroicons/vue/20/solid";
import { AppButton } from "../../ui/button";
import { AppPagination } from "../pagination";
import { AppListControls } from "../list-controls";
import AppLoaderSymbol from "../loading/AppLoaderSymbol.vue";
import AppColumnFilterMenu from "./AppColumnFilterMenu.vue";

interface Column {
  key: string;
  label: string;
  type?: "text" | "number" | "boolean" | "date" | "custom";
  format?: (value: any) => string;
  decimals?: number;
  filter?: boolean | ColumnFilterConfig;
}

type SortDirection = "asc" | "desc";
type FilterValue = string | number | boolean;

interface FilterOption {
  label: string;
  value: FilterValue;
  count?: number;
}

interface FilterGroup {
  key: string;
  label: string;
  options: FilterOption[];
  getValue?: (row: any) => unknown;
}

interface ColumnFilterConfig {
  enabled?: boolean;
  label?: string;
  options?: FilterOption[];
  getValue?: (row: any) => unknown;
  getLabel?: (value: FilterValue) => string;
}

interface ResolvedFilterGroup extends FilterGroup {
  source: "column" | "group";
}

const props = withDefaults(
  defineProps<{
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
    searchPlaceholder?: string;
    createLabel?: string;
    filterGroups?: FilterGroup[];
    selectedFilters?: Record<string, FilterValue[]>;
    searchAccessor?: (row: any) => unknown[];
    canDelete?: (row: any) => boolean;
    deleteDisabledTitle?: (row: any) => string;
    rowClickable?: boolean;
    emptyStateTitle?: string;
    emptyStateMessage?: string;
    showToolbar?: boolean;
    mobileShowBackButton?: boolean;
    mobileBackAriaLabel?: string;
  }>(),
  {
    searchable: true,
    pageSize: 10,
    searchPlaceholder: "Search all records",
    createLabel: "New",
    filterGroups: () => [],
    selectedFilters: () => ({}),
    canDelete: () => true,
    rowClickable: false,
    emptyStateTitle: "No records found",
    emptyStateMessage: "Create your first record to get started",
    showToolbar: true,
    mobileShowBackButton: false,
    mobileBackAriaLabel: "Go back",
  },
);

const emit = defineEmits<{
  create: [];
  edit: [row: any];
  delete: [row: any];
  search: [query: string];
  retry: [];
  "update:page": [page: number];
  "update:search": [query: string];
  "update:selectedFilters": [value: Record<string, FilterValue[]>];
  "row-click": [row: any];
  back: [];
  "column-filter-search": [columnKey: string, query: string];
}>();

function cloneFilters(
  value: Record<string, FilterValue[]> | undefined,
): Record<string, FilterValue[]> {
  if (!value) {
    return {};
  }

  return Object.fromEntries(
    Object.entries(value).map(([key, items]) => [key, [...items]]),
  );
}

const isSearchable = computed(() => props.searchable);
const pageSizeValue = computed(() => props.pageSize);
const searchQuery = ref(props.search ?? "");
const currentPage = ref(props.page ?? 1);
const isMobileViewport = ref(false);
const selectedFilterState = ref<Record<string, FilterValue[]>>(
  cloneFilters(props.selectedFilters),
);

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

watch(
  () => props.selectedFilters,
  (value) => {
    selectedFilterState.value = cloneFilters(value);
  },
  { deep: true },
);

function normalizeValue(value: unknown): string {
  if (value === null || value === undefined) {
    return "";
  }

  if (typeof value === "boolean") {
    return value ? "true" : "false";
  }

  return String(value).trim().toLowerCase();
}

function toFilterValues(value: unknown): FilterValue[] {
  const values = Array.isArray(value) ? value : [value];

  return values.flatMap((item) => {
    if (item === null || item === undefined || item === "") {
      return [];
    }

    if (
      typeof item === "boolean" ||
      typeof item === "number" ||
      typeof item === "string"
    ) {
      return [item];
    }

    if (item instanceof Date) {
      return [item.toISOString()];
    }

    return [String(item)];
  });
}

function getColumnFilterConfig(column: Column): ColumnFilterConfig {
  if (column.filter === false) {
    return { enabled: false };
  }

  if (!column.filter || column.filter === true) {
    return { enabled: true };
  }

  return {
    enabled: true,
    ...column.filter,
  };
}

function sortFilterOptions(options: FilterOption[]): FilterOption[] {
  return options.slice().sort((left, right) =>
    left.label.localeCompare(right.label, undefined, {
      numeric: true,
      sensitivity: "base",
    }),
  );
}

function formatFilterOptionLabel(
  value: FilterValue,
  column?: Column,
  getLabel?: (value: FilterValue) => string,
): string {
  if (getLabel) {
    return getLabel(value);
  }

  if (column) {
    return formatCellValue(value, column);
  }

  return String(value);
}

function buildDerivedFilterOptions(
  rows: any[],
  column: Column | undefined,
  getValue: (row: any) => unknown,
  getLabel?: (value: FilterValue) => string,
): FilterOption[] {
  const counts = new Map<string, FilterOption>();

  rows.forEach((row) => {
    toFilterValues(getValue(row)).forEach((value) => {
      const key = normalizeValue(value);
      const existing = counts.get(key);

      if (existing) {
        existing.count = (existing.count ?? 0) + 1;
        return;
      }

      counts.set(key, {
        label: formatFilterOptionLabel(value, column, getLabel),
        value,
        count: 1,
      });
    });
  });

  return sortFilterOptions([...counts.values()]);
}

function resolveFilterOptions(
  baseOptions: FilterOption[],
  rows: any[],
  column: Column | undefined,
  getValue: (row: any) => unknown,
  getLabel?: (value: FilterValue) => string,
): FilterOption[] {
  const derivedOptions = buildDerivedFilterOptions(
    rows,
    column,
    getValue,
    getLabel,
  );

  if (!baseOptions.length) {
    return derivedOptions;
  }

  const derivedByKey = new Map(
    derivedOptions.map((option) => [normalizeValue(option.value), option]),
  );

  const mergedOptions = baseOptions
    .filter((option) => option.value !== null && option.value !== undefined)
    .map((option) => {
      const key = normalizeValue(option.value);
      const derivedOption = derivedByKey.get(key);
      derivedByKey.delete(key);

      return {
        label: option.label,
        value: option.value,
        count: derivedOption?.count ?? option.count ?? 0,
      } satisfies FilterOption;
    });

  return [...mergedOptions, ...sortFilterOptions([...derivedByKey.values()])];
}

const resolvedFilterGroups = computed<ResolvedFilterGroup[]>(() => {
  const rows = props.data ?? [];
  const manualGroups = new Map(
    props.filterGroups.map((group) => [group.key, group]),
  );
  const resolvedGroups: ResolvedFilterGroup[] = [];
  const usedGroupKeys = new Set<string>();

  props.columns.forEach((column) => {
    const filterConfig = getColumnFilterConfig(column);
    if (filterConfig.enabled === false) {
      return;
    }

    const manualGroup = manualGroups.get(column.key);
    const selectedValues = selectedFilterState.value[column.key] ?? [];

    if (rows.length === 0 && selectedValues.length === 0) {
      if (manualGroup) {
        usedGroupKeys.add(manualGroup.key);
      }
      return;
    }

    const getValue =
      filterConfig.getValue ??
      manualGroup?.getValue ??
      ((row: any) => row[column.key]);

    const options = resolveFilterOptions(
      filterConfig.options?.length
        ? filterConfig.options
        : (manualGroup?.options ?? []),
      rows,
      column,
      getValue,
      filterConfig.getLabel,
    );

    if (!options.length) {
      if (manualGroup) {
        usedGroupKeys.add(manualGroup.key);
      }
      return;
    }

    resolvedGroups.push({
      key: column.key,
      label: filterConfig.label ?? manualGroup?.label ?? column.label,
      options,
      getValue,
      source: "column",
    });

    if (manualGroup) {
      usedGroupKeys.add(manualGroup.key);
    }
  });

  props.filterGroups.forEach((group) => {
    if (usedGroupKeys.has(group.key)) {
      return;
    }

    const selectedValues = selectedFilterState.value[group.key] ?? [];
    if (rows.length === 0 && selectedValues.length === 0) {
      return;
    }

    const matchingColumn = props.columns.find(
      (column) => column.key === group.key,
    );
    const getValue = group.getValue ?? ((row: any) => row[group.key]);
    const options = resolveFilterOptions(
      group.options,
      rows,
      matchingColumn,
      getValue,
      undefined,
    );

    if (!options.length) {
      return;
    }

    resolvedGroups.push({
      ...group,
      options,
      getValue,
      source: "group",
    });
  });

  return resolvedGroups;
});

const columnFilterGroups = computed(() => {
  const groups = new Map<string, ResolvedFilterGroup>();

  resolvedFilterGroups.value.forEach((group) => {
    if (props.columns.some((column) => column.key === group.key)) {
      groups.set(group.key, group);
    }
  });

  return groups;
});

const hasColumnFilters = computed(() => columnFilterGroups.value.size > 0);

const hasMobileToolbar = computed(
  () =>
    isMobileViewport.value &&
    props.showToolbar &&
    (props.searchable ||
      resolvedFilterGroups.value.length > 0 ||
      props.mobileShowBackButton),
);

const mobileToolbarInsetStyle = computed(() =>
  hasMobileToolbar.value
    ? {
        paddingBottom: "calc(env(safe-area-inset-bottom, 0px) + 6rem)",
      }
    : undefined,
);

const toolbarFilterDropdownVisibility = computed<
  "always" | "mobile-only" | "hidden"
>(() => (hasColumnFilters.value ? "mobile-only" : "always"));

function syncViewport() {
  isMobileViewport.value = window.innerWidth < 768;
}

function getSearchValues(row: any): unknown[] {
  if (props.searchAccessor) {
    return props.searchAccessor(row);
  }

  return props.columns.map((column) => row[column.key]);
}

onMounted(() => {
  syncViewport();
  window.addEventListener("resize", syncViewport);
});

onUnmounted(() => {
  window.removeEventListener("resize", syncViewport);
});

function matchesSearch(row: any): boolean {
  if (!isSearchable.value || !searchQuery.value.trim()) {
    return true;
  }

  const query = searchQuery.value.trim().toLowerCase();
  return getSearchValues(row).some((value) => {
    if (value === null || value === undefined) {
      return false;
    }

    if (Array.isArray(value)) {
      return value.some((item) => normalizeValue(item).includes(query));
    }

    return normalizeValue(value).includes(query);
  });
}

function matchesSelectedFilters(row: any): boolean {
  return resolvedFilterGroups.value.every((group) => {
    const selectedValues = selectedFilterState.value[group.key] ?? [];

    if (!selectedValues.length) {
      return true;
    }

    const rawValue = group.getValue ? group.getValue(row) : row[group.key];
    const normalizedRowValues = toFilterValues(rawValue).map((value) =>
      normalizeValue(value),
    );

    return selectedValues.some((selectedValue) =>
      normalizedRowValues.includes(normalizeValue(selectedValue)),
    );
  });
}

const filteredData = computed(() => {
  if (!props.data) {
    return [];
  }

  return props.data.filter(
    (row) => matchesSearch(row) && matchesSelectedFilters(row),
  );
});

const summaryText = computed(() => {
  if (!props.data?.length) {
    return "";
  }

  if (filteredData.value.length === props.data.length) {
    return `${filteredData.value.length} records`;
  }

  return `${filteredData.value.length} of ${props.data.length} records`;
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

  const column = props.columns.find((item) => item.key === sortKey.value);
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
  return withIndex.map((entry) => entry.row);
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

watch(
  [searchQuery, selectedFilterState, () => pageSizeValue.value],
  () => {
    currentPage.value = 1;
    emit("update:page", 1);
  },
  { deep: true },
);

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

function toggleSort(column: Column) {
  if (sortKey.value !== column.key) {
    sortKey.value = column.key;
    sortDirection.value = "asc";
    return;
  }

  if (sortDirection.value === "asc") {
    sortDirection.value = "desc";
    return;
  }

  sortKey.value = null;
  sortDirection.value = null;
}

let searchTimeout: ReturnType<typeof setTimeout> | null = null;

function handleSearchInput(value: string) {
  searchQuery.value = value;
  const normalizedQuery = value.trim();
  emit("update:search", normalizedQuery);

  if (searchTimeout) {
    clearTimeout(searchTimeout);
  }

  searchTimeout = setTimeout(() => {
    emit("search", normalizedQuery);
  }, 250);
}

function handleFiltersUpdate(value: Record<string, FilterValue[]>) {
  selectedFilterState.value = cloneFilters(value);
  emit("update:selectedFilters", cloneFilters(value));
}

function getColumnFilterGroup(
  columnKey: string,
): ResolvedFilterGroup | undefined {
  return columnFilterGroups.value.get(columnKey);
}

function handleColumnFilterSelection(columnKey: string, values: FilterValue[]) {
  const nextFilters = cloneFilters(selectedFilterState.value);

  if (values.length > 0) {
    nextFilters[columnKey] = [...values];
  } else {
    delete nextFilters[columnKey];
  }

  handleFiltersUpdate(nextFilters);
}

function handlePageChange(page: number) {
  currentPage.value = page;
  emit("update:page", page);
}

watch(currentPage, (value) => {
  const next = typeof value === "number" && value > 0 ? value : 1;
  if (next !== value) {
    currentPage.value = next;
    return;
  }
  emit("update:page", next);
});

function canDeleteRow(row: any): boolean {
  try {
    return props.canDelete?.(row) ?? true;
  } catch {
    return true;
  }
}

function deleteDisabledTitleForRow(row: any): string | undefined {
  if (canDeleteRow(row)) return undefined;
  try {
    return props.deleteDisabledTitle?.(row);
  } catch {
    return undefined;
  }
}

function handleRowClick(row: any) {
  if (!props.rowClickable) {
    return;
  }

  emit("row-click", row);
}

function formatCellValue(value: any, column: Column): string {
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
}
</script>

<style scoped>
.data-table-container {
  display: flex;
  flex-direction: column;
  height: 100%;
  min-height: 0;
}

.data-table-body {
  flex: 1 1 auto;
  min-height: 0;
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

@media (min-width: 768px) {
  .data-table-body {
    min-height: 400px;
  }
}
</style>

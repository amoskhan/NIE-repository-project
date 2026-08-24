<template>
  <div
    class="data-table-container relative overflow-hidden rounded-2xl border border-secondary-200 bg-white transition-colors duration-300 dark:border-secondary-700 dark:bg-secondary-900"
    :class="[
      `data-table--${effectiveAppearance}`,
      `data-table--density-${effectiveDensity}`,
    ]"
    :style="tableContainerStyle"
  >
    <NieListControls
      v-if="showToolbar"
      :search-term="searchQuery"
      :search-placeholder="searchPlaceholder"
      :selected-filters="selectedFilterState"
      :filter-groups="resolvedFilterGroups"
      :filter-dropdown-visibility="toolbarFilterDropdownVisibility"
      :mobile-show-back-button="mobileShowBackButton"
      :mobile-back-aria-label="mobileBackAriaLabel"
      :show-search="isSearchable"
      @update:search-term="handleSearchInput"
      @update:selected-filters="handleFiltersUpdate"
      @back="$emit('back')"
    >
      <template #actions>
        <span
          data-table-total-results
          class="whitespace-nowrap text-sm font-semibold text-secondary-500 dark:text-secondary-400"
          role="status"
          aria-live="polite"
        >
          {{ totalResultsLabel }}
        </span>

        <slot name="toolbar-actions" :filtered-data="filteredData"></slot>

        <button
          v-if="preferenceKey"
          type="button"
          data-table-preferences-action
          class="inline-flex min-h-11 items-center gap-2 rounded-[var(--theme-radius-control)] border border-secondary-300 bg-white px-3 text-sm font-semibold text-secondary-700 transition hover:bg-secondary-50 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary-500 dark:border-secondary-600 dark:bg-secondary-900 dark:text-secondary-200 dark:hover:bg-secondary-800"
          aria-label="Configure table preferences"
          @click="openPreferences"
        >
          <AdjustmentsHorizontalIcon class="size-5" />
          <span class="hidden lg:inline">View</span>
        </button>

        <NieButton
          v-if="!hideCreate"
          data-table-create-action
          @click="$emit('create')"
        >
          <PlusIcon class="h-4 w-4" />
          <span>{{ createLabel }}</span>
        </NieButton>
      </template>
    </NieListControls>

    <div
      v-if="resolvedPreferenceState.repairRequired || preferenceLoadError"
      data-table-preference-warning
      class="flex items-center gap-3 border-b border-warning-200 bg-warning-50 px-4 py-2.5 text-sm text-warning-900 dark:border-warning-800 dark:bg-warning-950/40 dark:text-warning-100 md:px-6"
      role="status"
    >
      <ExclamationTriangleIcon class="size-5 shrink-0" />
      <p class="min-w-0 flex-1">
        {{
          preferenceLoadError
            ? "Your saved table view could not be loaded. Screen defaults are active."
            : "Your saved table view needs attention. Safe defaults are active."
        }}
      </p>
      <button
        type="button"
        class="min-h-10 shrink-0 rounded-lg px-3 font-semibold text-warning-900 underline decoration-warning-500 underline-offset-4 hover:bg-warning-100 dark:text-warning-100 dark:hover:bg-warning-900/50"
        @click="openPreferences"
      >
        {{ resolvedPreferenceState.repairRequired ? "Review and repair" : "Review preferences" }}
      </button>
    </div>

    <div
      class="data-table-body px-4 md:pl-6 md:pr-0"
      :style="mobileBodyInsetStyle"
      role="region"
      tabindex="0"
      aria-label="Scrollable data table"
      :aria-busy="loading"
    >
      <NieResultState
        v-if="loading && (!data || data.length === 0)"
        class="flex-1"
        compact
        variant="loading"
        title="Loading records"
        description="Please wait while the latest records are loaded."
      />

      <NieResultState
        v-else-if="error"
        class="flex-1"
        compact
        variant="error"
        :status-code="resolvedErrorStatus"
        :title="resolvedErrorTitle"
        :description="resolvedErrorDescription"
      >
        <template #actions>
          <NieButton @click="$emit('retry')">Retry</NieButton>
        </template>
      </NieResultState>

      <div v-else class="flex min-h-full w-full min-w-0 flex-1 flex-col">
        <p v-if="loading" class="sr-only" role="status">
          Updating table data...
        </p>
        <div class="flex min-h-full w-full min-w-0 flex-1 flex-col">
          <div class="relative hidden pb-2 md:block">
            <table class="min-w-max w-full">
              <thead class="border-b dark:border-secondary-700">
                <tr>
                  <th
                    v-for="(column, columnIndex) in displayedColumns"
                    :key="column.key"
                    class="sticky top-0 z-20 select-none overflow-visible bg-secondary-50 px-5 py-1 text-left align-middle text-label font-semibold uppercase tracking-wide text-secondary-700 dark:bg-secondary-800 dark:text-secondary-300"
                  >
                    <div class="flex items-center justify-between gap-3">
                      <button
                        type="button"
                        class="data-table-sort-button group flex min-h-11 min-w-0 flex-1 items-center gap-2 text-left"
                        @click="toggleSort(column, $event)"
                      >
                        <span class="truncate">{{ column.label }}</span>
                        <ChevronUpIcon
                          v-if="
                            sortDirectionFor(column.key) === 'asc'
                          "
                          class="h-4 w-4 text-secondary-600 dark:text-secondary-300"
                        />
                        <ChevronDownIcon
                          v-else-if="
                            sortDirectionFor(column.key) === 'desc'
                          "
                          class="h-4 w-4 text-secondary-600 dark:text-secondary-300"
                        />
                        <span
                          v-if="sortPriority(column.key) > 0"
                          data-sort-priority
                          class="inline-flex size-5 shrink-0 items-center justify-center rounded-full bg-primary-100 text-[0.6875rem] font-bold text-primary-700 dark:bg-primary-950 dark:text-primary-300"
                        >
                          {{ sortPriority(column.key) }}
                        </span>
                      </button>

                      <NieColumnFilterMenu
                        v-if="getColumnFilterGroup(column.key)"
                        :column-label="column.label"
                        :model-value="selectedFilterState[column.key] ?? []"
                        :options="
                          getColumnFilterGroup(column.key)?.options ?? []
                        "
                        :remote="serverSide"
                        :loading="getColumnFilterOptionPage(column.key)?.loading"
                        :error="getColumnFilterOptionPage(column.key)?.error"
                        :page="getColumnFilterOptionPage(column.key)?.page"
                        :page-size="getColumnFilterOptionPage(column.key)?.pageSize"
                        :total-count="getColumnFilterOptionPage(column.key)?.totalCount"
                        :total-pages="getColumnFilterOptionPage(column.key)?.totalPages"
                        :align="
                          columnIndex < displayedColumns.length / 2 ? 'left' : 'right'
                        "
                        @update:model-value="
                          handleColumnFilterSelection(column.key, $event)
                        "
                        @request-options="
                          handleFilterOptionsRequest(column.key, $event)
                        "
                      />
                    </div>
                  </th>
                  <th
                    v-if="!hideActions"
                    class="sticky right-0 top-0 z-40 border-l border-secondary-200 bg-secondary-50 px-5 py-1 text-right text-xs font-medium uppercase tracking-wide text-secondary-700 dark:border-secondary-700 dark:bg-secondary-800 dark:text-secondary-300"
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
                  :key="getRowKey(row)"
                  :data-table-interactive-row="rowClickable ? '' : undefined"
                  role="row"
                  :tabindex="rowClickable ? 0 : undefined"
                  :aria-label="rowClickable ? getRowAriaLabel(row) : undefined"
                  :class="[
                    rowClickable
                      ? 'cursor-pointer transition-colors hover:bg-secondary-50 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-inset focus-visible:ring-primary-500 dark:hover:bg-secondary-800/70'
                      : 'transition-colors hover:bg-secondary-50 dark:hover:bg-secondary-800/50',
                  ]"
                  @click="handleRowClick(row, $event)"
                  @keydown.enter.self="handleRowClick(row)"
                  @keydown.space.self.prevent="handleRowClick(row)"
                >
                  <td
                    v-for="column in displayedColumns"
                    :key="column.key"
                    class="px-5 py-4 whitespace-nowrap text-sm text-secondary-900 dark:text-secondary-100"
                  >
                    <slot
                      :name="`cell-${column.key}`"
                      :row="row"
                      :value="getRowValue(row, column.key)"
                    >
                      <NieBadge
                        v-if="column.chip"
                        :variant="chipTone(getRowValue(row, column.key), column)"
                        :dot="chipDot(column)"
                        size="sm"
                        rounded
                        :data-table-chip="column.key"
                        data-testid="nie-data-table-chip"
                      >
                        {{ chipLabel(getRowValue(row, column.key), row, column) }}
                      </NieBadge>
                      <template v-else>
                        {{ formatCellValue(getRowValue(row, column.key), column) }}
                      </template>
                    </slot>
                  </td>
                  <td
                    v-if="!hideActions"
                    class="data-table-sticky-actions sticky right-0 z-10 border-l border-secondary-200 bg-white px-5 py-1 text-right text-sm font-medium dark:border-secondary-700 dark:bg-secondary-900"
                  >
                    <div class="flex items-center justify-end gap-2">
                      <button
                        v-if="!hideEdit"
                        class="data-table-edit-action inline-flex size-11 items-center justify-center rounded-[var(--theme-radius-control)] text-primary-600 transition hover:bg-primary-50 hover:text-primary-800 dark:text-primary-400 dark:hover:bg-primary-950/40 dark:hover:text-primary-300"
                        title="Edit"
                        aria-label="Edit record"
                        @click.stop="$emit('edit', row)"
                      >
                        <PencilIcon class="h-5 w-5" />
                      </button>
                      <button
                        v-if="!hideDelete"
                        :disabled="!canDeleteRow(row)"
                        :title="deleteDisabledTitleForRow(row) ?? 'Delete'"
                        :class="[
                          'data-table-delete-action inline-flex size-11 items-center justify-center rounded-[var(--theme-radius-control)] dark:text-danger-400',
                          canDeleteRow(row)
                            ? 'text-danger-600 transition hover:text-danger-800 dark:hover:text-danger-300'
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

          <div
            v-if="paginatedData.length === 0"
            data-table-result-frame
            class="sticky left-0 flex min-h-[20rem] w-full min-w-0 flex-1 items-stretch"
          >
            <NieResultState
              class="data-table-empty-state flex-1"
              compact
              variant="empty"
              :title="resolvedEmptyStateTitle"
              :description="resolvedEmptyStateMessage"
            />
          </div>

          <div v-else class="space-y-2.5 md:hidden">
            <div
              v-for="row in paginatedData"
              :key="getRowKey(row)"
              :data-table-interactive-row="rowClickable ? '' : undefined"
              :role="rowClickable ? 'button' : undefined"
              :tabindex="rowClickable ? 0 : undefined"
              :aria-label="rowClickable ? getRowAriaLabel(row) : undefined"
              :class="[
                'min-w-0 rounded-lg border border-secondary-200 bg-white p-2.5 shadow-[var(--theme-shadow-soft)] dark:border-secondary-700 dark:bg-secondary-950',
                rowClickable
                  ? 'cursor-pointer focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary-500'
                  : '',
              ]"
              @click="handleRowClick(row, $event)"
              @keydown.enter.self="handleRowClick(row)"
              @keydown.space.self.prevent="handleRowClick(row)"
            >
              <div class="flex flex-col gap-1.5">
                <div
                  v-for="column in displayedColumns"
                  :key="column.key"
                  class="grid min-h-[24px] grid-cols-[minmax(6.25rem,0.75fr)_minmax(0,1.25fr)] items-start gap-2"
                >
                  <span
                    class="min-w-0 text-caption font-semibold uppercase tracking-wide text-secondary-500 dark:text-secondary-400"
                  >
                    {{ column.label }}
                  </span>
                  <span
                    class="min-w-0 break-words text-right text-label leading-tight text-secondary-900 dark:text-secondary-100"
                  >
                    <slot
                      :name="`cell-${column.key}`"
                      :row="row"
                      :value="getRowValue(row, column.key)"
                    >
                      <NieBadge
                        v-if="column.chip"
                        :variant="chipTone(getRowValue(row, column.key), column)"
                        :dot="chipDot(column)"
                        size="sm"
                        rounded
                        :data-table-chip="column.key"
                        data-testid="nie-data-table-chip"
                      >
                        {{ chipLabel(getRowValue(row, column.key), row, column) }}
                      </NieBadge>
                      <template v-else>
                        {{ formatCellValue(getRowValue(row, column.key), column) }}
                      </template>
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
                  class="data-table-edit-action inline-flex size-11 items-center justify-center rounded-[var(--theme-radius-control)] text-primary-600 transition hover:bg-primary-50 hover:text-primary-800 dark:text-primary-400 dark:hover:bg-primary-950/40 dark:hover:text-primary-300"
                  title="Edit"
                  aria-label="Edit record"
                  @click.stop="$emit('edit', row)"
                >
                  <PencilIcon class="h-5 w-5" />
                </button>
                <button
                  v-if="!hideDelete"
                  :disabled="!canDeleteRow(row)"
                  :title="deleteDisabledTitleForRow(row) ?? 'Delete'"
                  :class="[
                    'data-table-delete-action inline-flex size-11 items-center justify-center rounded-[var(--theme-radius-control)] dark:text-danger-400',
                    canDeleteRow(row)
                      ? 'text-danger-600 transition hover:text-danger-800 dark:hover:text-danger-300'
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
      v-if="serverSide || totalItemCount > 0"
      data-table-pagination-footer
      class="data-table-pagination-footer border-t border-secondary-200 bg-white px-3 py-2 dark:border-secondary-700 dark:bg-secondary-900 md:px-6"
      :class="{
        'data-table-pagination-footer--with-mobile-toolbar': hasMobileToolbar,
      }"
    >
      <NiePagination
        :current-page="currentPage"
        :total-pages="Math.max(totalPages, 1)"
        :items-per-page="pageSizeValue"
        :page-size-options="pageSizeOptions"
        @update:current-page="handlePageChange"
        @update:items-per-page="handlePageSizeChange"
      />
    </div>

    <NieDataTablePreferencesModal
      v-model="preferencesOpen"
      :columns="columns"
      :filter-groups="preferenceFilterGroups"
      :filter-option-pages="filterOptionPages"
      :remote-filters="serverSide"
      :settings="currentPreferenceSettings"
      :preference-state="resolvedPreferenceState"
      :saving="preferenceSaving"
      :save-error="preferenceSaveError"
      :load-error="preferenceLoadError"
      :save-conflict="preferenceSaveConflict"
      :can-reload="Boolean(preferenceStore?.refresh)"
      :refresh-version="preferenceRefreshVersion"
      :dismissible="!filterReminderConflictPending"
      @save="savePreferences"
      @reset="resetPreferences"
      @filter-options-request="handlePreferenceFilterOptionsRequest"
      @reload="reloadPreferences"
    />

    <NieDataTableFilterReminderModal
      :model-value="filterReminderOpen"
      :filters="filterReminderSummaries"
      :saving="filterReminderSaving"
      :error="filterReminderError"
      @keep="keepSavedFilters"
      @remove="removeSavedFilters"
    />
  </div>
</template>

<script setup lang="ts" generic="TRow extends object">
import {
  computed,
  inject,
  onMounted,
  onUnmounted,
  ref,
  shallowRef,
  watch,
} from "vue";
import {
  AdjustmentsHorizontalIcon,
  ExclamationTriangleIcon,
  PencilIcon,
  PlusIcon,
  TrashIcon,
} from "@heroicons/vue/24/outline";
import { ChevronDownIcon, ChevronUpIcon } from "@heroicons/vue/20/solid";
import { NieBadge } from "../../ui/badge";
import { NieButton } from "../../ui/button";
import { NiePagination } from "../pagination";
import { NieListControls } from "../list-controls";
import NieResultState, {
  type NieResultStatus,
} from "../result-state/NieResultState.vue";
import NieColumnFilterMenu from "./NieColumnFilterMenu.vue";
import NieDataTableFilterReminderModal from "./NieDataTableFilterReminderModal.vue";
import NieDataTablePreferencesModal from "./NieDataTablePreferencesModal.vue";
import {
  NieDataTablePreferenceConflictError,
  isDataTableFilterReminderDue,
  nieDataTablePreferenceStoreKey,
} from "./preferences";
import type {
  NieDataTableAppearance,
  NieDataTableColumn as Column,
  NieDataTableChipConfig,
  NieDataTableChipTone,
  NieDataTableColumnFilterConfig as ColumnFilterConfig,
  NieDataTableFilterGroup as FilterGroup,
  NieDataTableFilterOption as FilterOption,
  NieDataTableFilterOptionPage,
  NieDataTableFilterOptionsRequest,
  NieDataTableFilterValue as FilterValue,
  NieDataTableDensity,
  NieDataTablePreferenceRecord,
  NieDataTablePreferenceFilterOptionsRequest,
  NieDataTablePreferenceSettings,
  NieDataTablePreferenceState,
  NieDataTableQuery,
  NieDataTableSort,
  NieDataTableSortDirection as SortDirection,
} from "./types";

interface ResolvedFilterGroup extends FilterGroup {
  source: "column" | "group";
}

const props = withDefaults(
  defineProps<{
    columns: Column[];
    data: TRow[] | null;
    rowKey: string;
    loading?: boolean;
    error?: string | null;
    errorStatus?: NieResultStatus;
    searchable?: boolean;
    hideCreate?: boolean;
    hideEdit?: boolean;
    hideDelete?: boolean;
    hideActions?: boolean;
    pageSize?: number;
    pageSizeOptions?: number[];
    page?: number;
    search?: string;
    searchPlaceholder?: string;
    createLabel?: string;
    filterGroups?: FilterGroup[];
    selectedFilters?: Record<string, FilterValue[]>;
    searchAccessor?: (row: TRow) => unknown[];
    canDelete?: (row: TRow) => boolean;
    deleteDisabledTitle?: (row: TRow) => string;
    rowClickable?: boolean;
    rowAriaLabel?: (row: TRow) => string;
    emptyStateTitle?: string;
    emptyStateMessage?: string;
    showToolbar?: boolean;
    mobileShowBackButton?: boolean;
    mobileBackAriaLabel?: string;
    maxHeight?: string;
    appearance?: NieDataTableAppearance;
    density?: NieDataTableDensity;
    serverSide?: boolean;
    totalItems?: number;
    filterOptionPages?: Record<string, NieDataTableFilterOptionPage>;
    preferenceKey?: string;
    definitionVersion?: number;
    preferenceState?: NieDataTablePreferenceState;
  }>(),
  {
    searchable: true,
    pageSize: 20,
    pageSizeOptions: () => [10, 20, 50, 100],
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
    maxHeight: "calc(100dvh - 8rem)",
    appearance: "elevated",
    density: "comfortable",
    serverSide: false,
    totalItems: 0,
    filterOptionPages: () => ({}),
    definitionVersion: 1,
    preferenceState: () => ({ repairRequired: false, reasons: [] }),
  },
);

const emit = defineEmits<{
  create: [];
  edit: [row: TRow];
  delete: [row: TRow];
  search: [query: string];
  retry: [];
  "update:page": [page: number];
  "update:pageSize": [pageSize: number];
  "update:search": [query: string];
  "update:selectedFilters": [value: Record<string, FilterValue[]>];
  "row-click": [row: TRow];
  back: [];
  "column-filter-search": [columnKey: string, query: string];
  "query-change": [query: NieDataTableQuery];
  "filter-options-request": [request: NieDataTableFilterOptionsRequest];
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

function getRowValue(row: unknown, key: string): unknown {
  return typeof row === "object" && row !== null
    ? (row as Record<string, unknown>)[key]
    : undefined;
}

function getRowKey(row: TRow): string | number {
  const value = getRowValue(row, props.rowKey);
  return typeof value === "string" || typeof value === "number"
    ? value
    : String(value ?? "");
}

const isSearchable = computed(() => props.searchable);
const serverSide = computed(() => props.serverSide);
const pageSizeValue = ref(props.pageSize);
const searchQuery = ref(props.search ?? "");
const currentPage = ref(props.page ?? 1);
const isMobileViewport = ref(false);
const selectedFilterState = ref<Record<string, FilterValue[]>>(
  cloneFilters(props.selectedFilters),
);
const preferenceStore = inject(nieDataTablePreferenceStoreKey, null);
const preferencesOpen = ref(false);
const preferenceSaving = ref(false);
const preferenceSaveError = ref<string | null>(null);
const preferenceLoadError = ref<string | null>(null);
const preferenceSaveConflict = ref(false);
const preferenceRefreshVersion = ref(0);
const preferenceRevision = ref<number | undefined>();
const filterReminderOpen = shallowRef(false);
const filterReminderSaving = shallowRef(false);
const filterReminderError = shallowRef<string | null>(null);
const filterReminderAcknowledgedAtUtc = shallowRef<string | null>(null);
const filterReminderFilters = shallowRef<Record<string, FilterValue[]>>({});
const filterReminderConflictPending = shallowRef(false);
const internalPreferenceState = ref<NieDataTablePreferenceState>({
  repairRequired: false,
  reasons: [],
});
const effectiveDensity = ref<NieDataTableDensity>(props.density);
const effectiveAppearance = ref<NieDataTableAppearance>(props.appearance);
const columnOrder = ref(props.columns.map((column) => column.key));
const hiddenColumns = ref<string[]>([]);
const sorts = ref<NieDataTableSort[]>([]);
const resolvedPreferenceState = computed<NieDataTablePreferenceState>(() => ({
  repairRequired:
    props.preferenceState.repairRequired ||
    internalPreferenceState.value.repairRequired,
  reasons: [
    ...new Set([
      ...props.preferenceState.reasons,
      ...internalPreferenceState.value.reasons,
    ]),
  ],
}));

const filterReminderSummaries = computed(() => {
  const labels = new Map<string, string>();
  props.columns.forEach((column) => labels.set(column.key, column.label));
  props.filterGroups.forEach((group) => labels.set(group.key, group.label));

  return Object.entries(filterReminderFilters.value)
    .filter(([, values]) => values.length > 0)
    .map(([key, values]) => ({
      key,
      label: labels.get(key) ?? key,
      count: values.length,
    }));
});

function openPreferences(): void {
  if (!preferenceSaveConflict.value) preferenceSaveError.value = null;
  preferencesOpen.value = true;
}
const displayedColumns = computed(() => {
  const byKey = new Map(props.columns.map((column) => [column.key, column]));
  const hidden = new Set(hiddenColumns.value);
  const ordered = columnOrder.value
    .map((key) => byKey.get(key))
    .filter((column): column is Column => Boolean(column))
    .filter((column) => !hidden.has(column.key));
  props.columns.forEach((column) => {
    if (!hidden.has(column.key) && !ordered.some((item) => item.key === column.key)) {
      ordered.push(column);
    }
  });
  return ordered;
});
const hasActiveQuery = computed(
  () =>
    searchQuery.value.trim().length > 0 ||
    Object.values(selectedFilterState.value).some((values) => values.length > 0),
);
const resolvedEmptyStateTitle = computed(() =>
  hasActiveQuery.value ? "No matching records" : props.emptyStateTitle,
);
const resolvedEmptyStateMessage = computed(() =>
  hasActiveQuery.value
    ? "Try changing or clearing the active search and filters."
    : props.emptyStateMessage,
);
const supportedResultStatuses = new Set<number>([
  401, 403, 404, 408, 429, 500, 502, 503,
]);
const resolvedErrorStatus = computed<NieResultStatus | undefined>(() => {
  if (props.errorStatus) {
    return props.errorStatus;
  }

  const match = props.error?.match(/\b(401|403|404|408|429|500|502|503)\b/);
  if (!match) {
    return undefined;
  }

  const statusCode = Number(match[1]);
  return supportedResultStatuses.has(statusCode)
    ? (statusCode as NieResultStatus)
    : undefined;
});
const resolvedErrorTitle = computed(() =>
  resolvedErrorStatus.value ? "" : "Unable to load records",
);
const resolvedErrorDescription = computed(() =>
  resolvedErrorStatus.value
    ? ""
    : "We could not load this table right now. Try again in a moment.",
);
const tableContainerStyle = computed<Record<string, string>>(() => ({
  "--nie-data-table-max-height": props.maxHeight,
  "--nie-data-table-height": props.maxHeight,
}));

const sortKey = computed(() => sorts.value[0]?.key ?? null);
const sortDirection = computed(() => sorts.value[0]?.direction ?? null);

function defaultPreferenceSettings(): NieDataTablePreferenceSettings {
  return {
    pageSize: props.pageSize,
    sorts: [],
    filters: cloneFilters(props.selectedFilters),
    filterReminderAcknowledgedAtUtc: null,
    columnOrder: props.columns.map((column) => column.key),
    hiddenColumns: [],
    density: props.density,
    appearance: props.appearance,
  };
}

const currentPreferenceSettings = computed<NieDataTablePreferenceSettings>(() => ({
  pageSize: pageSizeValue.value,
  sorts: sorts.value.map((sort) => ({ ...sort })),
  filters: cloneFilters(selectedFilterState.value),
  filterReminderAcknowledgedAtUtc: filterReminderAcknowledgedAtUtc.value,
  columnOrder: [...columnOrder.value],
  hiddenColumns: [...hiddenColumns.value],
  density: effectiveDensity.value,
  appearance: effectiveAppearance.value,
}));

function applyPreferenceSettings(settings: NieDataTablePreferenceSettings): void {
  pageSizeValue.value = settings.pageSize;
  currentPage.value = 1;
  sorts.value = settings.sorts.map((sort) => ({ ...sort }));
  selectedFilterState.value = cloneFilters(settings.filters);
  filterReminderAcknowledgedAtUtc.value =
    settings.filterReminderAcknowledgedAtUtc ?? null;
  columnOrder.value = [...settings.columnOrder];
  hiddenColumns.value = [...settings.hiddenColumns];
  effectiveDensity.value = settings.density;
  effectiveAppearance.value = settings.appearance;
  emit("update:pageSize", settings.pageSize);
  emit("update:page", 1);
  emit("update:selectedFilters", cloneFilters(settings.filters));
}

function normalizePreference(
  record: NieDataTablePreferenceRecord,
): { settings: NieDataTablePreferenceSettings; reasons: string[] } {
  const defaults = defaultPreferenceSettings();
  const reasons = [...(record.repairReasons ?? [])];
  const columnMap = new Map(props.columns.map((column) => [column.key, column]));
  const knownKeys = new Set(columnMap.keys());
  const storedOrder = Array.isArray(record.settings.columnOrder)
    ? record.settings.columnOrder
    : [];
  const uniqueOrder = [...new Set(storedOrder.filter((key) => knownKeys.has(key)))];
  const knownStoredOrder = storedOrder.filter((key) => knownKeys.has(key));
  const unknownColumns = storedOrder.filter((key) => !knownKeys.has(key));
  const missingColumns = defaults.columnOrder.filter((key) => !uniqueOrder.includes(key));
  if (record.definitionVersion !== props.definitionVersion) {
    reasons.push(
      `This screen definition changed from version ${record.definitionVersion} to ${props.definitionVersion}.`,
    );
  }
  if (unknownColumns.length) {
    reasons.push("One or more saved columns are no longer available.");
  }
  if (uniqueOrder.length !== knownStoredOrder.length) {
    reasons.push("A duplicate saved column was removed from the display order.");
  }
  if (missingColumns.length) {
    reasons.push("New columns were added to this screen and appended safely.");
  }

  const seenSorts = new Set<string>();
  const safeSorts = (record.settings.sorts ?? []).filter((sort) => {
    const column = columnMap.get(sort.key);
    const valid =
      Boolean(column) &&
      column?.sortable !== false &&
      !seenSorts.has(sort.key) &&
      (sort.direction === "asc" || sort.direction === "desc");
    seenSorts.add(sort.key);
    return valid;
  }).slice(0, 5);
  if (safeSorts.length !== (record.settings.sorts ?? []).length) {
    reasons.push("An invalid or duplicate saved sort was removed.");
  }

  const storedFilters = record.settings.filters ?? {};
  const safeFilters: Record<string, FilterValue[]> = {};
  let removedFilter = false;
  let removedFilterValue = false;
  Object.entries(storedFilters).forEach(([key, rawValues]) => {
    const column = columnMap.get(key);
    const supportedStandaloneFilter = props.filterGroups.some(
      (group) => group.key === key,
    );
    const supported = column
      ? column.persistFilter !== false
      : supportedStandaloneFilter;
    if (!supported || !Array.isArray(rawValues)) {
      removedFilter = true;
      return;
    }

    const seenValues = new Set<string>();
    const values = (rawValues as unknown[]).filter(
      (value): value is FilterValue =>
        typeof value === "string" ||
        typeof value === "boolean" ||
        (typeof value === "number" && Number.isFinite(value)),
    );
    const uniqueValues = values.filter((value) => {
      const identity = `${typeof value}:${String(value)}`;
      if (seenValues.has(identity)) return false;
      seenValues.add(identity);
      return true;
    });
    if (uniqueValues.length !== rawValues.length) removedFilterValue = true;
    if (uniqueValues.length) safeFilters[key] = uniqueValues;
    else if (rawValues.length) removedFilter = true;
  });
  if (removedFilter || Object.keys(safeFilters).length !== Object.keys(storedFilters).length) {
    reasons.push("A saved filter no longer matches this table and was removed.");
  }
  if (removedFilterValue) {
    reasons.push("An invalid or duplicate saved filter value was removed.");
  }

  const hidden = [...new Set((record.settings.hiddenColumns ?? []).filter((key) => {
    const column = columnMap.get(key);
    return knownKeys.has(key) && column?.hideable !== false;
  }))];
  if (hidden.length !== (record.settings.hiddenColumns ?? []).length) {
    reasons.push("An invalid hidden-column setting was removed.");
  }

  const pageSize = props.pageSizeOptions.includes(record.settings.pageSize)
    ? record.settings.pageSize
    : defaults.pageSize;
  if (pageSize !== record.settings.pageSize) reasons.push("The saved page size is not supported.");

  const densities: NieDataTableDensity[] = ["compact", "comfortable", "spacious"];
  const appearances: NieDataTableAppearance[] = ["elevated", "minimal", "striped"];
  const density = densities.includes(record.settings.density)
    ? record.settings.density
    : defaults.density;
  const appearance = appearances.includes(record.settings.appearance)
    ? record.settings.appearance
    : defaults.appearance;
  if (density !== record.settings.density) reasons.push("The saved density was repaired.");
  if (appearance !== record.settings.appearance) reasons.push("The saved table style was repaired.");

  const rawFilterReminderAcknowledgement = (
    record.settings as NieDataTablePreferenceSettings & {
      filterReminderAcknowledgedAtUtc?: unknown;
    }
  ).filterReminderAcknowledgedAtUtc;
  let filterReminderAcknowledgedAtUtc: string | null = null;
  if (rawFilterReminderAcknowledgement !== null && rawFilterReminderAcknowledgement !== undefined) {
    const parsedAcknowledgement =
      typeof rawFilterReminderAcknowledgement === "string"
        ? Date.parse(rawFilterReminderAcknowledgement)
        : Number.NaN;
    const maximumClockSkewMilliseconds = 5 * 60 * 1_000;
    if (
      Number.isFinite(parsedAcknowledgement) &&
      parsedAcknowledgement <= Date.now() + maximumClockSkewMilliseconds
    ) {
      filterReminderAcknowledgedAtUtc = new Date(parsedAcknowledgement).toISOString();
    } else {
      reasons.push("An invalid saved-filter reminder date was removed.");
    }
  }

  return {
    settings: {
      pageSize,
      sorts: safeSorts,
      filters: cloneFilters(safeFilters),
      filterReminderAcknowledgedAtUtc,
      columnOrder: [...uniqueOrder, ...missingColumns],
      hiddenColumns: hidden,
      density,
      appearance,
    },
    reasons: [...new Set(reasons)],
  };
}

function showFilterReminderWhenDue(
  settings: NieDataTablePreferenceSettings,
): void {
  if (!isDataTableFilterReminderDue(settings)) return;

  filterReminderFilters.value = cloneFilters(settings.filters);
  filterReminderError.value = null;
  filterReminderOpen.value = true;
}

async function loadPreferences(): Promise<void> {
  if (!props.preferenceKey || !preferenceStore) return;
  preferenceLoadError.value = null;
  try {
    const record = await preferenceStore.get(props.preferenceKey);
    if (!record) return;
    preferenceRevision.value = record.revision;
    const normalized = normalizePreference(record);
    applyPreferenceSettings(normalized.settings);
    const repairReasons = [
      ...new Set([...(record.repairReasons ?? []), ...normalized.reasons]),
    ];
    internalPreferenceState.value = {
      repairRequired: Boolean(record.repairRequired) || repairReasons.length > 0,
      reasons: repairReasons,
    };
    showFilterReminderWhenDue(normalized.settings);
  } catch {
    internalPreferenceState.value = { repairRequired: false, reasons: [] };
    preferenceLoadError.value =
      "Your saved view could not be loaded. Screen defaults are active.";
  }
}

async function savePreferences(settings: NieDataTablePreferenceSettings): Promise<void> {
  if (preferenceSaveConflict.value) return;
  applyPreferenceSettings(settings);
  preferenceSaving.value = true;
  preferenceSaveError.value = null;
  try {
    if (props.preferenceKey && preferenceStore) {
      const saved = await preferenceStore.save(
        props.preferenceKey,
        props.definitionVersion,
        settings,
        preferenceRevision.value,
      );
      preferenceRevision.value = saved.revision;
      const normalizedSaved = normalizePreference(saved);
      applyPreferenceSettings(normalizedSaved.settings);
    }
    internalPreferenceState.value = { repairRequired: false, reasons: [] };
    preferenceLoadError.value = null;
    preferenceSaveConflict.value = false;
    preferencesOpen.value = false;
    if (props.serverSide) emitQuery();
  } catch (error) {
    preferenceSaveConflict.value =
      error instanceof NieDataTablePreferenceConflictError;
    preferenceSaveError.value = preferenceSaveConflict.value
      ? "This saved view changed in another session. Reload the latest view before saving again."
      : "We couldn't save your table preferences. Review your choices and try again.";
  } finally {
    preferenceSaving.value = false;
  }
}

async function resetPreferences(): Promise<void> {
  preferenceSaving.value = true;
  preferenceSaveError.value = null;
  try {
    if (props.preferenceKey && preferenceStore) {
      await preferenceStore.remove(props.preferenceKey);
    }
    preferenceRevision.value = undefined;
    internalPreferenceState.value = { repairRequired: false, reasons: [] };
    preferenceLoadError.value = null;
    preferenceSaveConflict.value = false;
    filterReminderOpen.value = false;
    filterReminderError.value = null;
    filterReminderFilters.value = {};
    filterReminderConflictPending.value = false;
    applyPreferenceSettings(defaultPreferenceSettings());
    preferencesOpen.value = false;
    if (props.serverSide) emitQuery();
  } finally {
    preferenceSaving.value = false;
  }
}

async function persistFilterReminderDecision(removeFilters: boolean): Promise<void> {
  if (!props.preferenceKey || !preferenceStore) return;

  filterReminderSaving.value = true;
  filterReminderError.value = null;
  const nextSettings: NieDataTablePreferenceSettings = {
    ...currentPreferenceSettings.value,
    sorts: currentPreferenceSettings.value.sorts.map((sort) => ({ ...sort })),
    filters: removeFilters
      ? {}
      : cloneFilters(currentPreferenceSettings.value.filters),
    columnOrder: [...currentPreferenceSettings.value.columnOrder],
    hiddenColumns: [...currentPreferenceSettings.value.hiddenColumns],
  };

  try {
    const saved = await preferenceStore.save(
      props.preferenceKey,
      props.definitionVersion,
      nextSettings,
      preferenceRevision.value,
    );
    preferenceRevision.value = saved.revision;
    const normalized = normalizePreference(saved);
    applyPreferenceSettings(normalized.settings);
    const repairReasons = [
      ...new Set([...(saved.repairReasons ?? []), ...normalized.reasons]),
    ];
    internalPreferenceState.value = {
      repairRequired: Boolean(saved.repairRequired) || repairReasons.length > 0,
      reasons: repairReasons,
    };
    filterReminderOpen.value = false;
    filterReminderFilters.value = {};
    filterReminderConflictPending.value = false;
    if (removeFilters && props.serverSide) emitQuery();
  } catch (error) {
    if (error instanceof NieDataTablePreferenceConflictError) {
      filterReminderOpen.value = false;
      filterReminderConflictPending.value = true;
      preferenceSaveConflict.value = true;
      preferenceSaveError.value =
        "This saved view changed in another session. Reload the latest view before saving again.";
      preferencesOpen.value = true;
    } else {
      filterReminderError.value =
        "We couldn't update your saved filters. Try again.";
    }
  } finally {
    filterReminderSaving.value = false;
  }
}

function keepSavedFilters(): Promise<void> {
  return persistFilterReminderDecision(false);
}

function removeSavedFilters(): Promise<void> {
  return persistFilterReminderDecision(true);
}

async function reloadPreferences(): Promise<void> {
  if (!props.preferenceKey || !preferenceStore?.refresh) return;
  preferenceSaving.value = true;
  const mustReevaluateFilterReminder = filterReminderConflictPending.value;
  let authoritativeSettings: NieDataTablePreferenceSettings | null = null;
  try {
    const record = await preferenceStore.refresh(props.preferenceKey);
    if (record) {
      preferenceRevision.value = record.revision;
      const normalized = normalizePreference(record);
      authoritativeSettings = normalized.settings;
      applyPreferenceSettings(normalized.settings);
      const repairReasons = [
        ...new Set([...(record.repairReasons ?? []), ...normalized.reasons]),
      ];
      internalPreferenceState.value = {
        repairRequired: Boolean(record.repairRequired) || repairReasons.length > 0,
        reasons: repairReasons,
      };
    } else {
      preferenceRevision.value = undefined;
      authoritativeSettings = defaultPreferenceSettings();
      applyPreferenceSettings(authoritativeSettings);
      internalPreferenceState.value = { repairRequired: false, reasons: [] };
    }
    preferenceSaveConflict.value = false;
    preferenceSaveError.value = null;
    preferenceLoadError.value = null;
    preferenceRefreshVersion.value += 1;
    if (mustReevaluateFilterReminder) {
      filterReminderConflictPending.value = false;
      preferencesOpen.value = false;
      filterReminderOpen.value = false;
      filterReminderFilters.value = {};
      showFilterReminderWhenDue(authoritativeSettings);
    }
    if (props.serverSide) emitQuery();
  } catch {
    preferenceSaveError.value =
      "We couldn't reload the latest table preferences. Try again.";
  } finally {
    preferenceSaving.value = false;
  }
}

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
  rows: TRow[],
  column: Column | undefined,
  getValue: (row: unknown) => unknown,
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
  rows: TRow[],
  column: Column | undefined,
  getValue: (row: unknown) => unknown,
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

  if (props.serverSide) {
    props.columns.forEach((column) => {
      const filterConfig = getColumnFilterConfig(column);
      if (filterConfig.enabled === false) return;
      const manualGroup = manualGroups.get(column.key);
      const optionPage = props.filterOptionPages[column.key];
      resolvedGroups.push({
        key: column.key,
        label: filterConfig.label ?? manualGroup?.label ?? column.label,
        options:
          optionPage?.items ??
          filterConfig.options ??
          manualGroup?.options ??
          [],
        getValue:
          filterConfig.getValue ??
          manualGroup?.getValue ??
          ((row: unknown) => getRowValue(row, column.key)),
        source: "column",
      });
      if (manualGroup) usedGroupKeys.add(manualGroup.key);
    });

    props.filterGroups.forEach((group) => {
      if (usedGroupKeys.has(group.key)) return;
      resolvedGroups.push({
        ...group,
        options: props.filterOptionPages[group.key]?.items ?? group.options,
        source: "group",
      });
    });
    return resolvedGroups;
  }

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
      ((row: unknown) => getRowValue(row, column.key));

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
    const getValue =
      group.getValue ?? ((row: unknown) => getRowValue(row, group.key));
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

const preferenceFilterGroups = computed<ResolvedFilterGroup[]>(() => {
  const columnsByKey = new Map(
    props.columns.map((column) => [column.key, column]),
  );
  return resolvedFilterGroups.value.filter(
    (group) => columnsByKey.get(group.key)?.persistFilter !== false,
  );
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

const mobileBodyInsetStyle = computed(() => {
  if (!isMobileViewport.value) return undefined;

  return {
    paddingBottom: hasMobileToolbar.value
      ? "calc(env(safe-area-inset-bottom, 0px) + 9.25rem)"
      : "4.25rem",
  };
});

const toolbarFilterDropdownVisibility = computed<
  "always" | "mobile-only" | "hidden"
>(() => (hasColumnFilters.value ? "mobile-only" : "always"));

function syncViewport() {
  isMobileViewport.value = window.innerWidth < 768;
}

function getSearchValues(row: TRow): unknown[] {
  if (props.searchAccessor) {
    return props.searchAccessor(row);
  }

  return props.columns.map((column) => getRowValue(row, column.key));
}

onMounted(async () => {
  syncViewport();
  window.addEventListener("resize", syncViewport);
  await loadPreferences();
  if (props.serverSide) emitQuery();
});

onUnmounted(() => {
  window.removeEventListener("resize", syncViewport);
});

function matchesSearch(row: TRow): boolean {
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

function matchesSelectedFilters(row: TRow): boolean {
  return resolvedFilterGroups.value.every((group) => {
    const selectedValues = selectedFilterState.value[group.key] ?? [];

    if (!selectedValues.length) {
      return true;
    }

    const rawValue = group.getValue
      ? group.getValue(row)
      : getRowValue(row, group.key);
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

  if (props.serverSide) {
    return props.data;
  }

  return props.data.filter(
    (row) => matchesSearch(row) && matchesSelectedFilters(row),
  );
});

const compareValues = (
  aValue: unknown,
  bValue: unknown,
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
      const aBool = Boolean(aValue) ? 1 : 0;
      const bBool = Boolean(bValue) ? 1 : 0;
      return (aBool - bBool) * dir;
    }
    case "date": {
      const aTime = toDateTime(aValue);
      const bTime = toDateTime(bValue);
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
  if (props.serverSide) return filteredData.value;
  if (!sorts.value.length) return filteredData.value;

  const withIndex = filteredData.value.map((row, index) => ({ row, index }));
  withIndex.sort((a, b) => {
    for (const sort of sorts.value) {
      const column = props.columns.find((item) => item.key === sort.key);
      if (!column) continue;
      const result = compareValues(
        getRowValue(a.row, sort.key),
        getRowValue(b.row, sort.key),
        column,
        sort.direction,
      );
      if (result !== 0) return result;
    }
    return a.index - b.index;
  });
  return withIndex.map((entry) => entry.row);
});

const totalPages = computed(() => {
  if (props.serverSide) {
    return props.totalItems > 0
      ? Math.ceil(props.totalItems / pageSizeValue.value)
      : 0;
  }
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
  if (props.serverSide) return sortedData.value;
  return sortedData.value.slice(startIndex.value, endIndex.value);
});

const totalItemCount = computed(() =>
  props.serverSide ? props.totalItems : filteredData.value.length,
);

const totalResultsLabel = computed(() => {
  const total = totalItemCount.value;
  return `${total.toLocaleString()} ${total === 1 ? "result" : "results"}`;
});

watch(
  () => props.pageSize,
  (value) => {
    if (value <= 0 || value > 100 || value === pageSizeValue.value) return;
    pageSizeValue.value = value;
    currentPage.value = 1;
    emit("update:page", 1);
    if (props.serverSide) emitQuery();
  },
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

function sortPriority(columnKey: string): number {
  const index = sorts.value.findIndex((sort) => sort.key === columnKey);
  return index < 0 ? 0 : index + 1;
}

function sortDirectionFor(columnKey: string): SortDirection | null {
  return sorts.value.find((sort) => sort.key === columnKey)?.direction ?? null;
}

function toggleSort(column: Column, event?: MouseEvent) {
  if (column.sortable === false) return;
  const existingIndex = sorts.value.findIndex((sort) => sort.key === column.key);
  const existing = existingIndex >= 0 ? sorts.value[existingIndex] : undefined;
  let nextDirection: SortDirection | null = "asc";
  if (existing?.direction === "asc") nextDirection = "desc";
  else if (existing?.direction === "desc") nextDirection = null;

  const next = event?.shiftKey ? [...sorts.value] : [];
  const index = next.findIndex((sort) => sort.key === column.key);
  if (index >= 0) next.splice(index, 1);
  if (nextDirection && next.length < 5) {
    next.push({ key: column.key, direction: nextDirection });
  }
  sorts.value = next;

  if (props.serverSide) {
    currentPage.value = 1;
    emit("update:page", 1);
    emitQuery();
  }
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
    if (props.serverSide) {
      currentPage.value = 1;
      emit("update:page", 1);
      emitQuery();
    }
  }, 250);
}

function handleFiltersUpdate(
  value: Record<string, FilterValue[]>,
  emitRemoteQuery = true,
) {
  selectedFilterState.value = cloneFilters(value);
  emit("update:selectedFilters", cloneFilters(value));
  if (props.serverSide && emitRemoteQuery) {
    currentPage.value = 1;
    emit("update:page", 1);
    emitQuery();
  }
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

  handleFiltersUpdate(nextFilters, false);
  if (props.serverSide) {
    currentPage.value = 1;
    emit("update:page", 1);
    emitQuery();
    handleFilterOptionsRequest(columnKey, {
      page: 1,
      pageSize: getColumnFilterOptionPage(columnKey)?.pageSize ?? 25,
      search: "",
    });
  }
}

function handlePageChange(page: number) {
  currentPage.value = page;
  emit("update:page", page);
  if (props.serverSide) emitQuery();
}

function handlePageSizeChange(pageSize: number): void {
  if (
    pageSize === pageSizeValue.value ||
    !props.pageSizeOptions.includes(pageSize)
  ) {
    return;
  }

  pageSizeValue.value = pageSize;
  currentPage.value = 1;
  emit("update:pageSize", pageSize);
  emit("update:page", 1);
  if (props.serverSide) emitQuery();
}

function buildQuery(): NieDataTableQuery {
  return {
    page: currentPage.value,
    pageSize: pageSizeValue.value,
    search: searchQuery.value.trim(),
    sortBy: sortKey.value,
    sortDirection: sortDirection.value,
    sorts: sorts.value.map((sort) => ({ ...sort })),
    filters: cloneFilters(selectedFilterState.value),
  };
}

function emitQuery(): void {
  emit("query-change", buildQuery());
}

function getColumnFilterOptionPage(
  columnKey: string,
): NieDataTableFilterOptionPage | undefined {
  return props.filterOptionPages[columnKey];
}

function handleFilterOptionsRequest(
  columnKey: string,
  request: { page: number; pageSize: number; search: string },
  filters = selectedFilterState.value,
): void {
  emit("filter-options-request", {
    columnKey,
    page: request.page,
    pageSize: request.pageSize,
    search: request.search,
    tableSearch: searchQuery.value.trim(),
    filters: cloneFilters(filters),
  });
}

function handlePreferenceFilterOptionsRequest(
  request: NieDataTablePreferenceFilterOptionsRequest,
): void {
  handleFilterOptionsRequest(request.columnKey, request, request.filters);
}

function canDeleteRow(row: TRow): boolean {
  try {
    return props.canDelete?.(row) ?? true;
  } catch {
    return true;
  }
}

function deleteDisabledTitleForRow(row: TRow): string | undefined {
  if (canDeleteRow(row)) return undefined;
  try {
    return props.deleteDisabledTitle?.(row);
  } catch {
    return undefined;
  }
}

function handleRowClick(row: TRow, event?: Event) {
  if (!props.rowClickable) {
    return;
  }

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

  emit("row-click", row);
}

function getRowAriaLabel(row: TRow): string {
  try {
    const customLabel = props.rowAriaLabel?.(row)?.trim();
    if (customLabel) return customLabel;
  } catch {
    // Fall through to a stable generic label when a consumer formatter fails.
  }

  const identifier =
    typeof row === "object" && row !== null
      ? (row as Record<string, unknown>)[props.rowKey]
      : undefined;
  return identifier !== undefined && identifier !== null && identifier !== ""
    ? `Open record ${String(identifier)}`
    : "Open record";
}

function toDateTime(value: unknown): number {
  if (value instanceof Date) return value.getTime();
  if (typeof value === "string" || typeof value === "number") {
    return new Date(value).getTime();
  }
  return Number.NaN;
}

function formatCellValue(value: unknown, column: Column): string {
  if (value === null || value === undefined) return "-";

  if (column.format) {
    return column.format(value);
  }

  switch (column.type) {
    case "boolean":
      return value ? "Yes" : "No";
    case "date": {
      const timestamp = toDateTime(value);
      return Number.isFinite(timestamp)
        ? new Date(timestamp).toLocaleDateString()
        : String(value);
    }
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

function chipConfig(column: Column): NieDataTableChipConfig | null {
  if (!column.chip) return null;
  return column.chip === true ? {} : column.chip;
}

function chipTone(value: unknown, column: Column): NieDataTableChipTone {
  const config = chipConfig(column);
  if (!config) return "default";
  return (
    config.toneMap?.[String(value)] ??
    config.tone ??
    "default"
  );
}

function chipDot(column: Column): boolean {
  return chipConfig(column)?.dot ?? false;
}

function chipLabel(value: unknown, row: TRow, column: Column): string {
  const config = chipConfig(column);
  if (config?.label) return config.label(value, row);
  return formatCellValue(value, column);
}
</script>

<style scoped>
.data-table-container {
  display: flex;
  flex-direction: column;
  height: var(--nie-data-table-height, calc(100dvh - 8rem));
  max-height: var(--nie-data-table-max-height, calc(100dvh - 8rem));
  min-height: 0;
}

.data-table--density-compact tbody td {
  padding-block: var(--theme-space-2);
}

.data-table--density-comfortable tbody td {
  padding-block: var(--theme-space-4);
}

.data-table--density-spacious tbody td {
  padding-block: var(--theme-space-5);
}

.data-table--elevated {
  box-shadow: var(--theme-shadow-card);
}

.data-table--minimal {
  border-radius: var(--theme-radius-control);
  box-shadow: none;
}

.data-table--minimal thead {
  background: var(--theme-color-surface-panel);
}

.data-table--minimal th {
  letter-spacing: 0.06em;
  text-transform: none;
}

.data-table--striped {
  box-shadow: var(--theme-shadow-soft);
}

.data-table--striped tbody tr:nth-child(even) > td {
  background: var(--theme-color-neutral-50);
}

.dark .data-table--striped tbody tr:nth-child(even) > td {
  background: var(--theme-color-neutral-800);
}

.data-table-body {
  display: flex;
  flex-direction: column;
  flex: 1 1 auto;
  min-height: 0;
  overflow: auto;
  overscroll-behavior: contain;
  -webkit-overflow-scrolling: touch;
  scrollbar-width: thin;
  scrollbar-color: color-mix(in srgb, var(--theme-color-neutral-400) 50%, transparent) transparent;
}

.data-table-body::-webkit-scrollbar {
  width: 6px;
  height: 8px;
}

.data-table-body::-webkit-scrollbar-track {
  background: transparent;
}

.data-table-body::-webkit-scrollbar-thumb {
  background: color-mix(in srgb, var(--theme-color-neutral-400) 50%, transparent);
  border-radius: var(--theme-radius-pill);
}

.data-table-sticky-actions {
  box-shadow: var(--theme-shadow-soft);
}

.data-table-body::-webkit-scrollbar-thumb:hover {
  background: color-mix(in srgb, var(--theme-color-neutral-400) 70%, transparent);
}

@media (min-width: 768px) {
  .data-table-body {
    min-height: 400px;
  }
}

@media (max-width: 767px) {
  .data-table-container {
    --nie-data-table-mobile-height: calc(100dvh - 5.25rem - env(safe-area-inset-bottom, 0px));
    height: var(--nie-data-table-mobile-height);
    max-height: var(--nie-data-table-mobile-height);
    min-height: min(32rem, var(--nie-data-table-mobile-height));
    border-radius: var(--theme-radius-control);
  }

  .data-table-body {
    padding-inline: var(--theme-space-2-5);
  }

  .data-table-pagination-footer {
    position: fixed;
    right: var(--theme-space-4);
    bottom: calc(env(safe-area-inset-bottom, 0px) + var(--theme-space-3));
    left: var(--theme-space-4);
    z-index: 60;
    overflow: hidden;
    border: 1px solid var(--color-border);
    border-radius: var(--theme-radius-control);
    box-shadow: var(--theme-shadow-soft);
  }

  .data-table-pagination-footer--with-mobile-toolbar {
    bottom: calc(env(safe-area-inset-bottom, 0px) + 5.9rem);
  }

  .data-table--density-compact [role="row"] {
    padding: var(--theme-space-2);
  }

  .data-table--density-comfortable [role="row"] {
    padding: var(--theme-space-2-5);
  }

  .data-table--density-spacious [role="row"] {
    padding: var(--theme-space-4);
  }
}
</style>

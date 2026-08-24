<script setup lang="ts">
import { computed, ref, watch } from "vue";
import {
  ArrowDownIcon,
  ArrowUpIcon,
  ExclamationTriangleIcon,
  XMarkIcon,
} from "@heroicons/vue/24/outline";
import { NieButton } from "../../ui/button";
import { NieModal } from "../../ui/modal";
import NieDataTableDefaultFilterBuilder from "./NieDataTableDefaultFilterBuilder.vue";
import type {
  NieDataTableColumn,
  NieDataTableFilterGroup,
  NieDataTableFilterOptionPage,
  NieDataTablePreferenceFilterOptionsRequest,
  NieDataTablePreferenceSettings,
  NieDataTablePreferenceState,
  NieDataTableSort,
} from "./types";

const props = withDefaults(
  defineProps<{
    modelValue: boolean;
    columns: NieDataTableColumn[];
    filterGroups?: NieDataTableFilterGroup[];
    filterOptionPages?: Record<string, NieDataTableFilterOptionPage>;
    remoteFilters?: boolean;
    settings: NieDataTablePreferenceSettings;
    preferenceState?: NieDataTablePreferenceState;
    saving?: boolean;
    saveError?: string | null;
    loadError?: string | null;
    saveConflict?: boolean;
    refreshVersion?: number;
    dismissible?: boolean;
  }>(),
  {
    filterGroups: () => [],
    filterOptionPages: () => ({}),
    remoteFilters: false,
    preferenceState: () => ({ repairRequired: false, reasons: [] }),
    saving: false,
    saveError: null,
    loadError: null,
    saveConflict: false,
    refreshVersion: 0,
    dismissible: true,
  },
);

const emit = defineEmits<{
  "update:modelValue": [value: boolean];
  save: [settings: NieDataTablePreferenceSettings];
  reset: [];
  "filter-options-request": [request: NieDataTablePreferenceFilterOptionsRequest];
  reload: [];
}>();

function cloneSettings(
  settings: NieDataTablePreferenceSettings,
): NieDataTablePreferenceSettings {
  return {
    ...settings,
    sorts: settings.sorts.map((sort) => ({ ...sort })),
    filters: Object.fromEntries(
      Object.entries(settings.filters).map(([key, values]) => [key, [...values]]),
    ),
    columnOrder: [...settings.columnOrder],
    hiddenColumns: [...settings.hiddenColumns],
  };
}

const draft = ref(cloneSettings(props.settings));
const activeSection = ref<"columns" | "sorting" | "filters" | "display">(
  "columns",
);

watch(
  () => props.modelValue,
  (open) => {
    if (!open) return;
    draft.value = cloneSettings(props.settings);
    activeSection.value = props.preferenceState.repairRequired
      ? "columns"
      : "columns";
  },
);

watch(
  () => props.refreshVersion,
  () => {
    if (props.modelValue) draft.value = cloneSettings(props.settings);
  },
);

const orderedColumns = computed(() => {
  const byKey = new Map(props.columns.map((column) => [column.key, column]));
  const ordered = draft.value.columnOrder
    .map((key) => byKey.get(key))
    .filter((column): column is NieDataTableColumn => Boolean(column));
  props.columns.forEach((column) => {
    if (!ordered.some((item) => item.key === column.key)) ordered.push(column);
  });
  return ordered;
});

const availableSortColumns = computed(() =>
  props.columns.filter((column) => column.sortable !== false),
);
const visibleColumnCount = computed(
  () => props.columns.length - draft.value.hiddenColumns.length,
);

function close(): void {
  emit("update:modelValue", false);
}

function moveColumn(index: number, offset: -1 | 1): void {
  const order = orderedColumns.value.map((column) => column.key);
  const nextIndex = index + offset;
  if (nextIndex < 0 || nextIndex >= order.length) return;
  [order[index], order[nextIndex]] = [order[nextIndex]!, order[index]!];
  draft.value.columnOrder = order;
}

function setColumnVisible(column: NieDataTableColumn, visible: boolean): void {
  if (column.hideable === false) return;
  if (!visible && visibleColumnCount.value <= 1) return;
  const hidden = new Set(draft.value.hiddenColumns);
  if (visible) hidden.delete(column.key);
  else hidden.add(column.key);
  draft.value.hiddenColumns = [...hidden];
}

function addSort(): void {
  if (draft.value.sorts.length >= 5) return;
  const used = new Set(draft.value.sorts.map((sort) => sort.key));
  const column = availableSortColumns.value.find((item) => !used.has(item.key));
  if (!column) return;
  draft.value.sorts.push({ key: column.key, direction: "asc" });
}

function updateSort(index: number, update: Partial<NieDataTableSort>): void {
  const current = draft.value.sorts[index];
  if (!current) return;
  draft.value.sorts[index] = { ...current, ...update };
}

function save(): void {
  emit("save", cloneSettings(draft.value));
}
</script>

<template>
  <NieModal
    :model-value="modelValue"
    aria-label="Configure table preferences"
    size="xl"
    class="sm:max-w-3xl"
    placement="mobile-sheet"
    :show-close="false"
    :close-on-overlay="dismissible"
    :close-on-escape="dismissible"
    @update:model-value="emit('update:modelValue', $event)"
  >
    <div
      class="-m-6 flex h-[calc(100dvh-2rem)] min-h-0 flex-col sm:h-[min(42rem,calc(100dvh-2rem))] sm:min-h-[32rem]"
    >
      <header
        class="flex items-start justify-between gap-4 border-b border-secondary-200 px-5 py-4 dark:border-secondary-700 sm:px-6"
      >
        <div>
          <h2 class="text-lg font-semibold text-secondary-950 dark:text-white">
            Table preferences
          </h2>
          <p class="mt-1 text-sm text-secondary-600 dark:text-secondary-300">
            Choose the view this table loads for you on every visit.
          </p>
        </div>
        <button
          v-if="dismissible"
          type="button"
          class="inline-flex size-11 shrink-0 items-center justify-center rounded-lg text-secondary-500 hover:bg-secondary-100 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary-500 dark:hover:bg-secondary-700"
          aria-label="Close table preferences"
          @click="close"
        >
          <XMarkIcon class="size-5" />
        </button>
      </header>

      <div
        v-if="preferenceState.repairRequired"
        class="mx-5 mt-4 flex gap-3 rounded-xl border border-warning-300 bg-warning-50 p-3 text-sm text-warning-900 dark:border-warning-700 dark:bg-warning-950/40 dark:text-warning-100 sm:mx-6"
      >
        <ExclamationTriangleIcon class="mt-0.5 size-5 shrink-0" />
        <div>
          <p class="font-semibold">Your saved view needs repair</p>
          <ul class="mt-1 list-disc space-y-0.5 pl-5">
            <li v-for="reason in preferenceState.reasons" :key="reason">
              {{ reason }}
            </li>
          </ul>
          <p class="mt-1">Review the safe settings below, then repair and save.</p>
        </div>
      </div>

      <div
        v-if="saveError"
        class="mx-5 mt-4 flex gap-3 rounded-xl border border-danger-300 bg-danger-50 p-3 text-sm text-danger-900 dark:border-danger-700 dark:bg-danger-950/40 dark:text-danger-100 sm:mx-6"
        role="alert"
      >
        <ExclamationTriangleIcon class="mt-0.5 size-5 shrink-0" />
        <div class="min-w-0 flex-1">
          <p v-if="saveConflict" class="font-semibold">Saved view changed elsewhere</p>
          <p :class="{ 'mt-1': saveConflict }">{{ saveError }}</p>
          <NieButton
            v-if="saveConflict"
            class="mt-3"
            variant="secondary"
            size="sm"
            :loading="saving"
            @click="emit('reload')"
          >
            Reload latest
          </NieButton>
        </div>
      </div>

      <div
        v-if="loadError"
        class="mx-5 mt-4 flex gap-3 rounded-xl border border-warning-300 bg-warning-50 p-3 text-sm text-warning-900 dark:border-warning-700 dark:bg-warning-950/40 dark:text-warning-100 sm:mx-6"
        role="status"
      >
        <ExclamationTriangleIcon class="mt-0.5 size-5 shrink-0" />
        <div>
          <p class="font-semibold">Saved view unavailable</p>
          <p class="mt-1">{{ loadError }}</p>
        </div>
      </div>

      <div class="grid min-h-0 flex-1 grid-cols-1 sm:grid-cols-[11rem_minmax(0,1fr)]">
        <nav
          aria-label="Table preference sections"
          class="grid grid-cols-4 gap-1 border-b border-secondary-200 p-2 dark:border-secondary-700 sm:block sm:border-b-0 sm:border-r sm:p-3"
        >
          <button
            v-for="section in [
              ['columns', 'Columns'],
              ['sorting', 'Sorting'],
              ['filters', 'Default filters'],
              ['display', 'Display'],
            ] as const"
            :key="section[0]"
            type="button"
            class="min-h-11 rounded-lg px-2 py-2 text-center text-xs font-semibold transition sm:mb-1 sm:w-full sm:px-3 sm:text-left sm:text-sm"
            :class="
              activeSection === section[0]
                ? 'bg-primary-50 text-primary-700 dark:bg-primary-950/50 dark:text-primary-300'
                : 'text-secondary-600 hover:bg-secondary-50 dark:text-secondary-300 dark:hover:bg-secondary-800'
            "
            @click="activeSection = section[0]"
          >
            {{ section[1] }}
          </button>
        </nav>

        <section class="preference-editor min-h-0 overflow-y-auto px-5 py-4 sm:px-6">
          <div v-if="activeSection === 'columns'">
            <h3 class="text-base font-semibold text-secondary-950 dark:text-white">
              Columns
            </h3>
            <p class="mt-1 text-sm text-secondary-600 dark:text-secondary-300">
              Set visibility and the left-to-right display order.
            </p>
            <ul class="mt-4 space-y-2">
              <li
                v-for="(column, index) in orderedColumns"
                :key="column.key"
                class="flex min-h-12 items-center gap-3 rounded-xl border border-secondary-200 px-3 py-2 dark:border-secondary-700"
              >
                <label
                  data-preference-column-visibility
                  class="inline-flex size-11 shrink-0 cursor-pointer items-center justify-center rounded-lg hover:bg-secondary-50 has-[:disabled]:cursor-not-allowed dark:hover:bg-secondary-800"
                >
                  <input
                    type="checkbox"
                    class="size-4 rounded border-secondary-300 text-primary-600 focus:ring-primary-500"
                    :checked="!draft.hiddenColumns.includes(column.key)"
                    :disabled="
                      column.hideable === false ||
                      (!draft.hiddenColumns.includes(column.key) && visibleColumnCount <= 1)
                    "
                    :aria-label="`Show ${column.label}`"
                    @change="setColumnVisible(column, ($event.target as HTMLInputElement).checked)"
                  />
                </label>
                <span class="min-w-0 flex-1 truncate text-sm font-medium text-secondary-900 dark:text-secondary-100">
                  {{ column.label }}
                </span>
                <button
                  type="button"
                  data-preference-column-move
                  class="inline-flex size-11 items-center justify-center rounded-lg text-secondary-600 hover:bg-secondary-100 disabled:opacity-30 dark:text-secondary-300 dark:hover:bg-secondary-700"
                  :disabled="index === 0"
                  :aria-label="`Move ${column.label} up`"
                  @click="moveColumn(index, -1)"
                >
                  <ArrowUpIcon class="size-4" />
                </button>
                <button
                  type="button"
                  data-preference-column-move
                  class="inline-flex size-11 items-center justify-center rounded-lg text-secondary-600 hover:bg-secondary-100 disabled:opacity-30 dark:text-secondary-300 dark:hover:bg-secondary-700"
                  :disabled="index === orderedColumns.length - 1"
                  :aria-label="`Move ${column.label} down`"
                  @click="moveColumn(index, 1)"
                >
                  <ArrowDownIcon class="size-4" />
                </button>
              </li>
            </ul>
          </div>

          <div v-else-if="activeSection === 'sorting'">
            <div class="flex flex-wrap items-start justify-between gap-4">
              <div>
                <h3 class="text-base font-semibold text-secondary-950 dark:text-white">Sorting</h3>
                <p class="mt-1 text-sm text-secondary-600 dark:text-secondary-300">
                  Add up to five rules. The first rule has the highest priority.
                </p>
              </div>
              <NieButton class="shrink-0 whitespace-nowrap" variant="secondary" size="sm" :disabled="draft.sorts.length >= 5" @click="addSort">
                Add sort
              </NieButton>
            </div>
            <div v-if="draft.sorts.length" class="mt-4 space-y-3">
              <div
                v-for="(sort, index) in draft.sorts"
                :key="`${sort.key}-${index}`"
                data-preference-sort-row
                class="preference-sort-row grid grid-cols-[2rem_minmax(0,1fr)] gap-2 rounded-xl border border-secondary-200 p-3 dark:border-secondary-700"
              >
                <span class="flex size-7 items-center justify-center rounded-full bg-primary-50 text-xs font-bold text-primary-700 dark:bg-primary-950/50 dark:text-primary-300">{{ index + 1 }}</span>
                <div
                  data-preference-sort-controls
                  class="preference-sort-controls col-start-2 grid min-w-0 gap-2"
                >
                  <select
                    class="min-h-11 min-w-0 rounded-lg border-secondary-300 bg-white text-sm dark:border-secondary-600 dark:bg-secondary-800"
                    :value="sort.key"
                    :aria-label="`Sort ${index + 1} column`"
                    @change="updateSort(index, { key: ($event.target as HTMLSelectElement).value })"
                  >
                    <option
                      v-for="column in availableSortColumns"
                      :key="column.key"
                      :value="column.key"
                      :disabled="draft.sorts.some((item, itemIndex) => itemIndex !== index && item.key === column.key)"
                    >
                      {{ column.label }}
                    </option>
                  </select>
                  <select
                    class="min-h-11 rounded-lg border-secondary-300 bg-white text-sm dark:border-secondary-600 dark:bg-secondary-800"
                    :value="sort.direction"
                    :aria-label="`Sort ${index + 1} direction`"
                    @change="updateSort(index, { direction: ($event.target as HTMLSelectElement).value as 'asc' | 'desc' })"
                  >
                    <option value="asc">Ascending</option>
                    <option value="desc">Descending</option>
                  </select>
                  <button type="button" class="min-h-11 justify-self-start rounded-lg px-3 text-sm font-semibold text-danger-600 hover:bg-danger-50" @click="draft.sorts.splice(index, 1)">Remove</button>
                </div>
              </div>
            </div>
            <p v-else class="mt-6 rounded-xl bg-secondary-50 p-4 text-sm text-secondary-600 dark:bg-secondary-800 dark:text-secondary-300">No default sort. Records use the screen's standard order.</p>
          </div>

          <div v-else-if="activeSection === 'filters'">
            <h3 class="text-base font-semibold text-secondary-950 dark:text-white">Default filters</h3>
            <p class="mt-1 text-sm text-secondary-600 dark:text-secondary-300">Choose the filter values applied before the first API request.</p>
            <NieDataTableDefaultFilterBuilder
              v-model="draft.filters"
              :groups="filterGroups"
              :option-pages="filterOptionPages"
              :remote="remoteFilters"
              @request-options="emit('filter-options-request', $event)"
            />
          </div>

          <div v-else>
            <h3 class="text-base font-semibold text-secondary-950 dark:text-white">Display</h3>
            <p class="mt-1 text-sm text-secondary-600 dark:text-secondary-300">Adjust information density and visual treatment without changing the data.</p>
            <div class="mt-5 grid gap-5 sm:grid-cols-2">
              <label class="text-sm font-semibold text-secondary-800 dark:text-secondary-200">
                Density
                <select v-model="draft.density" class="mt-2 min-h-11 w-full rounded-lg border-secondary-300 bg-white text-sm dark:border-secondary-600 dark:bg-secondary-800">
                  <option value="compact">Compact</option>
                  <option value="comfortable">Comfortable</option>
                  <option value="spacious">Spacious</option>
                </select>
              </label>
              <label class="text-sm font-semibold text-secondary-800 dark:text-secondary-200">
                Table style
                <select v-model="draft.appearance" class="mt-2 min-h-11 w-full rounded-lg border-secondary-300 bg-white text-sm dark:border-secondary-600 dark:bg-secondary-800">
                  <option value="elevated">Elevated</option>
                  <option value="minimal">Minimal</option>
                  <option value="striped">Striped</option>
                </select>
              </label>
              <label class="text-sm font-semibold text-secondary-800 dark:text-secondary-200">
                Rows per page
                <select v-model.number="draft.pageSize" class="mt-2 min-h-11 w-full rounded-lg border-secondary-300 bg-white text-sm dark:border-secondary-600 dark:bg-secondary-800">
                  <option v-for="size in [10, 20, 50, 100]" :key="size" :value="size">{{ size }} rows</option>
                </select>
              </label>
            </div>
          </div>
        </section>
      </div>

      <footer class="flex flex-col-reverse gap-2 border-t border-secondary-200 px-5 py-4 dark:border-secondary-700 sm:flex-row sm:items-center sm:justify-between sm:px-6">
        <button v-if="dismissible" type="button" class="min-h-11 rounded-lg px-3 text-sm font-semibold text-secondary-600 hover:bg-secondary-100 dark:text-secondary-300 dark:hover:bg-secondary-700" @click="emit('reset')">Reset to screen defaults</button>
        <div class="flex flex-col-reverse gap-2 sm:flex-row">
          <NieButton v-if="dismissible" variant="secondary" @click="close">Cancel</NieButton>
          <NieButton :loading="saving" :disabled="saveConflict" @click="save">
            {{ preferenceState.repairRequired ? "Repair and save" : "Save as my default" }}
          </NieButton>
        </div>
      </footer>
    </div>
  </NieModal>
</template>

<style scoped>
.preference-editor {
  container-name: preference-editor;
  container-type: inline-size;
}

@container preference-editor (min-width: 32rem) {
  .preference-sort-row {
    align-items: center;
  }

  .preference-sort-controls {
    grid-template-columns: minmax(0, 1fr) 10rem auto;
    align-items: center;
  }
}
</style>

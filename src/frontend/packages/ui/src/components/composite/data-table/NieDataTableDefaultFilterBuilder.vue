<script setup lang="ts">
import { computed, onBeforeUnmount, ref, watch } from "vue";
import {
  ChevronDoubleLeftIcon,
  ChevronDoubleRightIcon,
  ChevronLeftIcon,
  ChevronRightIcon,
  MagnifyingGlassIcon,
  XMarkIcon,
} from "@heroicons/vue/24/outline";
import type {
  NieDataTableFilterGroup,
  NieDataTableFilterOptionPage,
  NieDataTableFilterValue,
  NieDataTablePreferenceFilterOptionsRequest,
} from "./types";

const props = withDefaults(
  defineProps<{
    modelValue: Record<string, NieDataTableFilterValue[]>;
    groups: NieDataTableFilterGroup[];
    optionPages?: Record<string, NieDataTableFilterOptionPage>;
    remote?: boolean;
  }>(),
  {
    optionPages: () => ({}),
    remote: false,
  },
);

const emit = defineEmits<{
  "update:modelValue": [value: Record<string, NieDataTableFilterValue[]>];
  "request-options": [request: NieDataTablePreferenceFilterOptionsRequest];
}>();

const selectedColumnKey = ref(
  props.groups.find((group) => (props.modelValue[group.key]?.length ?? 0) > 0)?.key ??
    props.groups[0]?.key ??
    "",
);
const search = ref("");
const requestedPage = ref(1);
let searchTimer: ReturnType<typeof setTimeout> | undefined;
let suppressNextSearchRequest = false;

const selectedGroup = computed(() =>
  props.groups.find((group) => group.key === selectedColumnKey.value),
);
const selectedOptionPage = computed(
  () => props.optionPages[selectedColumnKey.value],
);
const displayedOptions = computed(() => {
  const options = selectedOptionPage.value?.items ?? selectedGroup.value?.options ?? [];
  if (props.remote || !search.value.trim()) return options;
  const query = search.value.trim().toLocaleLowerCase();
  return options.filter((option) =>
    option.label.toLocaleLowerCase().includes(query),
  );
});
const currentPage = computed(
  () => selectedOptionPage.value?.page ?? requestedPage.value,
);
const totalPages = computed(() =>
  Math.max(1, selectedOptionPage.value?.totalPages ?? 1),
);
const selectedValues = computed(
  () => props.modelValue[selectedColumnKey.value] ?? [],
);
const configuredGroups = computed(() =>
  props.groups
    .map((group) => ({
      ...group,
      selected: props.modelValue[group.key] ?? [],
    }))
    .filter((group) => group.selected.length > 0),
);

function cloneFilters(): Record<string, NieDataTableFilterValue[]> {
  return Object.fromEntries(
    Object.entries(props.modelValue).map(([key, values]) => [key, [...values]]),
  );
}

function requestOptions(page = 1): void {
  if (!props.remote || !selectedColumnKey.value) return;
  requestedPage.value = page;
  emit("request-options", {
    columnKey: selectedColumnKey.value,
    page,
    pageSize: selectedOptionPage.value?.pageSize ?? 25,
    search: search.value.trim(),
    filters: cloneFilters(),
  });
}

function selectColumn(key: string): void {
  const sameColumn = selectedColumnKey.value === key;
  if (search.value) {
    suppressNextSearchRequest = true;
    search.value = "";
  }
  selectedColumnKey.value = key;
  if (sameColumn) requestOptions(1);
}

function includesValue(value: NieDataTableFilterValue): boolean {
  return selectedValues.value.some((item) => String(item) === String(value));
}

function toggleValue(value: NieDataTableFilterValue): void {
  const next = cloneFilters();
  const current = next[selectedColumnKey.value] ?? [];
  next[selectedColumnKey.value] = includesValue(value)
    ? current.filter((item) => String(item) !== String(value))
    : [...current, value];
  if (!next[selectedColumnKey.value]?.length) delete next[selectedColumnKey.value];
  emit("update:modelValue", next);
}

function removeGroup(key: string): void {
  const next = cloneFilters();
  delete next[key];
  emit("update:modelValue", next);
}

watch(
  () => props.groups.map((group) => group.key).join("|"),
  () => {
    if (!props.groups.some((group) => group.key === selectedColumnKey.value)) {
      selectedColumnKey.value = props.groups[0]?.key ?? "";
    }
  },
);

watch(
  selectedColumnKey,
  (key) => {
    if (!key) return;
    requestedPage.value = 1;
    requestOptions(1);
  },
  { immediate: true },
);

watch(search, () => {
  if (suppressNextSearchRequest) {
    suppressNextSearchRequest = false;
    return;
  }
  if (!props.remote) return;
  if (searchTimer) clearTimeout(searchTimer);
  searchTimer = setTimeout(() => requestOptions(1), 250);
});

onBeforeUnmount(() => {
  if (searchTimer) clearTimeout(searchTimer);
});
</script>

<template>
  <div class="mt-4 space-y-4">
    <div
      v-if="configuredGroups.length"
      class="rounded-xl border border-primary-100 bg-primary-50/60 p-3 dark:border-primary-900 dark:bg-primary-950/30"
    >
      <p class="text-xs font-semibold uppercase tracking-wide text-primary-700 dark:text-primary-300">
        Applied on first load
      </p>
      <div class="mt-2 flex flex-wrap gap-2">
        <span
          v-for="group in configuredGroups"
          :key="group.key"
          class="inline-flex min-h-11 max-w-full items-stretch overflow-hidden rounded-full border border-primary-200 bg-white text-xs font-semibold text-primary-800 dark:border-primary-800 dark:bg-secondary-900 dark:text-primary-200"
        >
          <button
            type="button"
            class="min-w-0 px-3 py-1.5 hover:bg-primary-50 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-inset focus-visible:ring-primary-500 dark:hover:bg-primary-950/40"
            :aria-label="`Edit ${group.label} default filter`"
            @click="selectColumn(group.key)"
          >
            <span class="block truncate">{{ group.label }} · {{ group.selected.length }}</span>
          </button>
          <button
            type="button"
            class="inline-flex min-w-11 items-center justify-center border-l border-primary-100 hover:bg-primary-50 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-inset focus-visible:ring-primary-500 dark:border-primary-800 dark:hover:bg-primary-950/40"
            :aria-label="`Remove ${group.label} default filter`"
            @click="removeGroup(group.key)"
          >
            <XMarkIcon class="size-4" aria-hidden="true" />
          </button>
        </span>
      </div>
    </div>

    <div v-if="groups.length" class="default-filter-builder-grid">
      <label class="text-sm font-semibold text-secondary-800 dark:text-secondary-200">
        Filter column
        <select
          :value="selectedColumnKey"
          aria-label="Default filter column"
          class="mt-2 min-h-11 w-full rounded-lg border-secondary-300 bg-white text-sm dark:border-secondary-600 dark:bg-secondary-800"
          @change="selectColumn(($event.target as HTMLSelectElement).value)"
        >
          <option v-for="group in groups" :key="group.key" :value="group.key">
            {{ group.label }}
          </option>
        </select>
      </label>

      <div class="min-w-0 rounded-xl border border-secondary-200 p-3 dark:border-secondary-700">
        <div class="flex flex-col gap-2 sm:flex-row sm:items-center sm:justify-between">
          <div>
            <p class="text-sm font-semibold text-secondary-950 dark:text-white">
              {{ selectedGroup?.label }} values
            </p>
            <p class="mt-0.5 text-xs text-secondary-500 dark:text-secondary-400">
              Select one or more values. Selections remain while you browse pages.
            </p>
          </div>
          <span
            v-if="selectedValues.length"
            class="shrink-0 text-xs font-semibold text-primary-700 dark:text-primary-300"
          >
            {{ selectedValues.length }} selected
          </span>
        </div>

        <label class="relative mt-3 block">
          <span class="sr-only">Search {{ selectedGroup?.label }} values</span>
          <MagnifyingGlassIcon
            class="pointer-events-none absolute left-3 top-1/2 size-4 -translate-y-1/2 text-secondary-400"
          />
          <input
            v-model="search"
            type="search"
            :placeholder="`Search ${selectedGroup?.label ?? 'filter'} values`"
            class="min-h-11 w-full rounded-lg border-secondary-300 bg-white pl-10 pr-3 text-sm dark:border-secondary-600 dark:bg-secondary-800"
          />
        </label>

        <div
          v-if="selectedOptionPage?.loading && !displayedOptions.length"
          class="flex min-h-36 items-center justify-center text-sm text-secondary-500"
          role="status"
        >
          Loading values…
        </div>
        <div
          v-else-if="selectedOptionPage?.error"
          class="mt-3 flex min-h-28 flex-col items-center justify-center rounded-lg bg-danger-50 px-4 text-center text-sm text-danger-800 dark:bg-danger-950/30 dark:text-danger-200"
          role="alert"
        >
          <p class="font-semibold">Values could not be loaded</p>
          <button type="button" class="mt-2 min-h-10 px-3 font-semibold underline" @click="requestOptions(1)">
            Try again
          </button>
        </div>
        <div
          v-else-if="!displayedOptions.length"
          class="flex min-h-36 items-center justify-center px-4 text-center text-sm text-secondary-500 dark:text-secondary-400"
        >
          No values match this search.
        </div>
        <div
          v-else
          :data-preference-filter-values="selectedColumnKey"
          class="mt-3 grid max-h-56 gap-1 overflow-y-auto sm:grid-cols-2"
        >
          <button
            v-for="option in displayedOptions"
            :key="String(option.value)"
            type="button"
            :data-default-filter-value="String(option.value)"
            :aria-pressed="includesValue(option.value)"
            class="flex min-h-11 min-w-0 items-center gap-2 rounded-lg px-2 text-left text-sm transition hover:bg-secondary-50 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary-500 dark:hover:bg-secondary-800"
            @click="toggleValue(option.value)"
          >
            <span
              class="flex size-5 shrink-0 items-center justify-center rounded border text-xs font-bold"
              :class="
                includesValue(option.value)
                  ? 'border-primary-600 bg-primary-600 text-on-brand'
                  : 'border-secondary-300 bg-white text-transparent dark:border-secondary-600 dark:bg-secondary-900'
              "
              aria-hidden="true"
            >✓</span>
            <span class="min-w-0 flex-1 truncate text-secondary-800 dark:text-secondary-100">
              {{ option.label }}
            </span>
            <span v-if="option.count !== undefined" class="shrink-0 text-xs text-secondary-500">
              {{ option.count }}
            </span>
          </button>
        </div>

        <div
          v-if="remote && totalPages > 1"
          class="mt-3 flex items-center justify-center gap-1 border-t border-secondary-100 pt-3 dark:border-secondary-700"
          aria-label="Default filter value pages"
        >
          <button type="button" class="inline-flex size-11 items-center justify-center rounded-lg hover:bg-secondary-100 disabled:opacity-30 dark:hover:bg-secondary-800" :disabled="currentPage <= 1" aria-label="First default filter values page" @click="requestOptions(1)"><ChevronDoubleLeftIcon class="size-4" /></button>
          <button type="button" class="inline-flex size-11 items-center justify-center rounded-lg hover:bg-secondary-100 disabled:opacity-30 dark:hover:bg-secondary-800" :disabled="currentPage <= 1" aria-label="Previous default filter values page" @click="requestOptions(currentPage - 1)"><ChevronLeftIcon class="size-4" /></button>
          <span class="inline-flex min-h-11 min-w-11 items-center justify-center rounded-lg bg-primary-600 px-2 text-sm font-semibold text-on-brand" :aria-label="`Page ${currentPage} of ${totalPages}`">{{ currentPage }}</span>
          <button type="button" class="inline-flex size-11 items-center justify-center rounded-lg hover:bg-secondary-100 disabled:opacity-30 dark:hover:bg-secondary-800" :disabled="currentPage >= totalPages" aria-label="Next default filter values page" @click="requestOptions(currentPage + 1)"><ChevronRightIcon class="size-4" /></button>
          <button type="button" class="inline-flex size-11 items-center justify-center rounded-lg hover:bg-secondary-100 disabled:opacity-30 dark:hover:bg-secondary-800" :disabled="currentPage >= totalPages" aria-label="Last default filter values page" @click="requestOptions(totalPages)"><ChevronDoubleRightIcon class="size-4" /></button>
        </div>
      </div>
    </div>

    <p
      v-else
      class="rounded-xl bg-secondary-50 p-4 text-sm text-secondary-600 dark:bg-secondary-800 dark:text-secondary-300"
    >
      This table has no columns available for default filtering.
    </p>
  </div>
</template>

<style scoped>
.default-filter-builder-grid {
  display: grid;
  gap: var(--theme-space-4);
}

@container preference-editor (min-width: 32rem) {
  .default-filter-builder-grid {
    grid-template-columns: 13rem minmax(0, 1fr);
  }
}
</style>

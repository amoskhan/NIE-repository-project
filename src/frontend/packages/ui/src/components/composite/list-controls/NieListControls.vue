<script setup lang="ts">
import { computed, onMounted, onUnmounted, ref } from "vue";
import {
  ArrowLeftIcon,
  FunnelIcon,
  MagnifyingGlassIcon,
  XMarkIcon,
} from "@heroicons/vue/24/outline";
import { NieButton } from "../../ui/button";
import { NieInput } from "../../ui/input";
import { NieSmartFilterDropdown } from "../smart-filter-dropdown";

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
}

interface ActiveFilterChip {
  key: string;
  groupKey: string;
  groupLabel: string;
  optionLabel: string;
  value: FilterValue;
}

interface Props {
  searchTerm?: string;
  searchPlaceholder?: string;
  selectedFilters?: Record<string, FilterValue[]>;
  filterGroups?: FilterGroup[];
  summary?: string;
  showReset?: boolean;
  showSearch?: boolean;
  filterDropdownVisibility?: "always" | "mobile-only" | "hidden";
  mobileShowBackButton?: boolean;
  mobileBackAriaLabel?: string;
  mobileSearchAriaLabel?: string;
  mobileFilterAriaLabel?: string;
}

const props = withDefaults(defineProps<Props>(), {
  searchTerm: "",
  searchPlaceholder: "Search all records",
  selectedFilters: () => ({}),
  filterGroups: () => [],
  summary: "",
  showReset: true,
  showSearch: true,
  filterDropdownVisibility: "always",
  mobileShowBackButton: false,
  mobileBackAriaLabel: "Go back",
  mobileSearchAriaLabel: "Search records",
  mobileFilterAriaLabel: "Open filters",
});

const emit = defineEmits<{
  "update:searchTerm": [value: string];
  "update:selectedFilters": [value: Record<string, FilterValue[]>];
  reset: [];
  back: [];
}>();

const isFilterPanelOpen = ref(false);
const isMobileViewport = ref(false);

const activeFilterCount = computed(() =>
  Object.values(props.selectedFilters ?? {}).reduce(
    (sum, values) => sum + values.length,
    0,
  ),
);

const filterGroupsWithOptions = computed(() =>
  props.filterGroups.filter((group) => group.options.length > 0),
);

const hasActiveControls = computed(
  () => props.searchTerm.trim().length > 0 || activeFilterCount.value > 0,
);

const showFilterDropdown = computed(
  () =>
    props.filterDropdownVisibility !== "hidden" &&
    filterGroupsWithOptions.value.length > 0,
);

const showDesktopSearch = computed(() => props.showSearch);

const filterTriggerVisibility = computed<"always" | "desktop-only" | "hidden">(
  () => {
    if (!showFilterDropdown.value) {
      return "hidden";
    }

    return props.filterDropdownVisibility === "always"
      ? "desktop-only"
      : "hidden";
  },
);

const showMobileToolbar = computed(
  () =>
    isMobileViewport.value &&
    (props.mobileShowBackButton ||
      props.showSearch ||
      showFilterDropdown.value),
);

const activeFilterChips = computed<ActiveFilterChip[]>(() =>
  Object.entries(props.selectedFilters ?? {}).flatMap(([groupKey, values]) => {
    const group = filterGroupsWithOptions.value.find(
      (item) => item.key === groupKey,
    );

    return values.map((value, index) => {
      const option = group?.options.find(
        (item) => String(item.value) === String(value),
      );

      return {
        key: `${groupKey}-${String(value)}-${index}`,
        groupKey,
        groupLabel: group?.label ?? groupKey,
        optionLabel: option?.label ?? String(value),
        value,
      };
    });
  }),
);

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

function handleReset() {
  isFilterPanelOpen.value = false;
  emit("update:searchTerm", "");
  emit("update:selectedFilters", {});
  emit("reset");
}

function removeSearchChip() {
  emit("update:searchTerm", "");
}

function handleSearchUpdate(value: string) {
  emit("update:searchTerm", value);
}

function toggleFilterPanel() {
  if (!showFilterDropdown.value) {
    return;
  }

  isFilterPanelOpen.value = !isFilterPanelOpen.value;
}

function syncViewport() {
  isMobileViewport.value = window.innerWidth < 768;
}

function removeFilterChip(groupKey: string, value: FilterValue) {
  const next = cloneFilters(props.selectedFilters);
  const remaining = (next[groupKey] ?? []).filter(
    (item) => String(item) !== String(value),
  );

  if (remaining.length > 0) {
    next[groupKey] = remaining;
  } else {
    delete next[groupKey];
  }

  emit("update:selectedFilters", next);
}

onMounted(() => {
  syncViewport();
  window.addEventListener("resize", syncViewport);
});

onUnmounted(() => {
  window.removeEventListener("resize", syncViewport);
});
</script>

<template>
  <section
    class="border-b border-secondary-200 px-4 py-3 dark:border-secondary-700 md:px-6"
  >
    <div class="flex flex-col gap-3">
      <div
        class="flex flex-wrap items-center justify-between gap-2.5 lg:flex-nowrap"
      >
        <div class="flex min-w-0 flex-wrap items-center gap-2.5">
          <div
            v-if="showDesktopSearch"
            class="hidden max-w-full w-[13rem] sm:w-56 md:block md:w-60 lg:w-64 xl:w-72"
          >
            <NieInput
              :model-value="searchTerm"
              type="search"
              :placeholder="searchPlaceholder"
              class="py-2"
              @update:model-value="handleSearchUpdate(String($event ?? ''))"
            />
          </div>

          <NieSmartFilterDropdown
            v-if="showFilterDropdown"
            v-model:open="isFilterPanelOpen"
            :model-value="selectedFilters"
            :groups="filterGroupsWithOptions"
            :trigger-visibility="filterTriggerVisibility"
            @update:model-value="emit('update:selectedFilters', $event)"
          />
        </div>

        <div class="flex flex-wrap items-center gap-2 lg:justify-end">
          <slot name="actions"></slot>

          <NieButton
            v-if="showReset && hasActiveControls"
            variant="ghost"
            @click="handleReset"
          >
            <XMarkIcon class="h-4 w-4" />
            <span>Reset</span>
          </NieButton>
        </div>
      </div>

      <div v-if="hasActiveControls" class="flex flex-wrap items-center gap-2">
        <button
          v-if="searchTerm.trim()"
          type="button"
          class="inline-flex items-center gap-1.5 rounded-full border border-primary-200 bg-primary-50 px-3 py-1.5 text-xs font-semibold text-primary-700 transition hover:bg-primary-100 dark:border-primary-500/40 dark:bg-primary-500/10 dark:text-primary-100 dark:hover:bg-primary-500/20"
          @click="removeSearchChip"
        >
          <span class="uppercase tracking-wide text-caption">Search</span>
          <span class="max-w-[16rem] truncate text-left">{{ searchTerm }}</span>
          <XMarkIcon class="h-3.5 w-3.5" />
        </button>

        <button
          v-for="chip in activeFilterChips"
          :key="chip.key"
          type="button"
          class="inline-flex items-center gap-1.5 rounded-full border border-secondary-200 bg-white px-3 py-1.5 text-xs font-semibold text-secondary-700 transition hover:border-secondary-300 hover:bg-secondary-50 dark:border-secondary-700 dark:bg-secondary-900 dark:text-secondary-200 dark:hover:border-secondary-600 dark:hover:bg-secondary-800"
          @click="removeFilterChip(chip.groupKey, chip.value)"
        >
          <span
            class="uppercase tracking-wide text-caption text-secondary-400 dark:text-secondary-500"
          >
            {{ chip.groupLabel }}
          </span>
          <span>{{ chip.optionLabel }}</span>
          <XMarkIcon class="h-3.5 w-3.5" />
        </button>
      </div>

      <div
        v-if="summary || $slots.summary"
        class="flex flex-wrap items-center gap-3 text-sm text-secondary-500 dark:text-secondary-400"
      >
        <span v-if="summary">{{ summary }}</span>
        <slot name="summary"></slot>
      </div>
    </div>

    <div v-if="showMobileToolbar" class="nie-list-mobile-toolbar">
      <div class="nie-list-mobile-toolbar__inner">
        <button
          v-if="mobileShowBackButton"
          class="nie-list-mobile-toolbar__back"
          type="button"
          :aria-label="mobileBackAriaLabel"
          @click="emit('back')"
        >
          <ArrowLeftIcon class="h-5 w-5" />
        </button>

        <label class="nie-list-mobile-toolbar__search">
          <MagnifyingGlassIcon
            class="h-5 w-5 shrink-0 text-[var(--color-text-muted)]"
          />
          <input
            :value="searchTerm"
            :aria-label="mobileSearchAriaLabel"
            :placeholder="searchPlaceholder"
            class="nie-list-mobile-toolbar__input"
            type="text"
            @input="
              handleSearchUpdate(
                String(($event.target as HTMLInputElement).value),
              )
            "
          />
          <button
            v-if="searchTerm"
            class="nie-list-mobile-toolbar__clear"
            type="button"
            aria-label="Clear search"
            @click="removeSearchChip"
          >
            <XMarkIcon class="h-4 w-4" />
          </button>
        </label>

        <button
          v-if="showFilterDropdown"
          class="nie-list-mobile-toolbar__filter"
          :class="{
            'nie-list-mobile-toolbar__filter--active':
              activeFilterCount > 0 || isFilterPanelOpen,
          }"
          type="button"
          :aria-label="mobileFilterAriaLabel"
          @click="toggleFilterPanel"
        >
          <FunnelIcon class="h-5 w-5" />
          <span
            v-if="activeFilterCount > 0"
            class="nie-list-mobile-toolbar__badge"
          >
            {{ activeFilterCount }}
          </span>
        </button>
      </div>
    </div>
  </section>
</template>

<style scoped>
.nie-list-mobile-toolbar {
  position: fixed;
  left: 0.75rem;
  right: 0.75rem;
  bottom: calc(env(safe-area-inset-bottom, 0px) + 0.9rem);
  z-index: 68;
  pointer-events: none;
}

.nie-list-mobile-toolbar__inner {
  display: flex;
  align-items: center;
  gap: var(--theme-space-3);
  pointer-events: auto;
}

.nie-list-mobile-toolbar__search {
  display: flex;
  flex: 1 1 auto;
  align-items: center;
  gap: var(--theme-space-3);
  min-width: 0;
  height: 3.5rem;
  padding: var(--theme-space-0) var(--theme-space-4);
  border: 1px solid color-mix(in srgb, var(--color-border) 90%, transparent);
  border-radius: var(--theme-radius-panel);
  background: color-mix(in srgb, var(--color-surface) 96%, transparent);
  box-shadow: var(--theme-shadow-card);
  backdrop-filter: blur(18px);
}

.nie-list-mobile-toolbar__input {
  width: 100%;
  min-width: 0;
  border: none;
  background: transparent;
  color: var(--color-text);
  font-size: var(--theme-font-size-body);
  min-height: var(--theme-control-height-md);
  border-radius: var(--theme-radius-control);
  outline: none;
}

.nie-list-mobile-toolbar__input::placeholder {
  color: var(--color-text-muted);
}

.nie-list-mobile-toolbar__back,
.nie-list-mobile-toolbar__filter,
.nie-list-mobile-toolbar__clear {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
  transition:
    border-color 0.18s ease,
    color 0.18s ease,
    background-color 0.18s ease,
    transform 0.18s ease,
    box-shadow 0.18s ease;
}

.nie-list-mobile-toolbar__back,
.nie-list-mobile-toolbar__filter {
  width: 3.5rem;
  height: 3.5rem;
  border-radius: var(--theme-radius-panel);
  box-shadow: var(--theme-shadow-card);
}

.nie-list-mobile-toolbar__back {
  border: 1px solid
    color-mix(in srgb, var(--color-primary) 18%, var(--color-border));
  background: var(--color-primary);
  color: var(--theme-color-on-brand);
}

.nie-list-mobile-toolbar__filter {
  position: relative;
  border: 1px solid color-mix(in srgb, var(--color-border) 90%, transparent);
  background: color-mix(in srgb, var(--color-surface) 96%, transparent);
  color: var(--color-text-muted);
  backdrop-filter: blur(18px);
}

.nie-list-mobile-toolbar__clear {
  width: 2rem;
  height: 2rem;
  border: none;
  border-radius: var(--theme-radius-pill);
  background: var(--color-surface-alt, var(--color-surface));
  color: var(--color-text-muted);
  box-shadow: none;
}

.nie-list-mobile-toolbar__back:hover,
.nie-list-mobile-toolbar__filter:hover,
.nie-list-mobile-toolbar__clear:hover {
  border-color: color-mix(
    in srgb,
    var(--color-primary) 36%,
    var(--color-border)
  );
}

.nie-list-mobile-toolbar__filter:hover,
.nie-list-mobile-toolbar__clear:hover {
  color: var(--color-primary);
}

.nie-list-mobile-toolbar__back:active,
.nie-list-mobile-toolbar__filter:active,
.nie-list-mobile-toolbar__clear:active {
  transform: scale(0.96);
}

.nie-list-mobile-toolbar__filter--active {
  border-color: color-mix(
    in srgb,
    var(--color-primary) 44%,
    var(--color-border)
  );
  background: var(--color-sidebar-active);
  color: var(--color-primary);
}

.nie-list-mobile-toolbar__badge {
  position: absolute;
  top: -0.18rem;
  right: -0.12rem;
  display: inline-flex;
  min-width: 1.15rem;
  height: 1.15rem;
  align-items: center;
  justify-content: center;
  padding: var(--theme-space-0) var(--theme-space-1);
  border-radius: var(--theme-radius-pill);
  background: var(--color-primary);
  color: var(--theme-color-on-brand);
  font-size: var(--theme-font-size-caption);
  font-weight: var(--theme-font-weight-bold);
  line-height: 1;
}

@media (min-width: 768px) {
  .nie-list-mobile-toolbar {
    display: none;
  }
}
</style>

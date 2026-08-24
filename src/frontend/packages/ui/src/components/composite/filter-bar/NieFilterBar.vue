<script setup lang="ts">
import { computed } from "vue";
import { MagnifyingGlassIcon, XMarkIcon } from "@heroicons/vue/24/outline";
import { NieButton } from "../../ui/button";
import { cn } from "../../../lib/utils";

export interface FilterOption {
  label: string;
  value: string;
  count?: number;
}

interface Props {
  searchTerm?: string;
  searchPlaceholder?: string;
  activeFilter?: string;
  filters?: FilterOption[];
  summary?: string;
  showReset?: boolean;
  class?: string;
}

const props = withDefaults(defineProps<Props>(), {
  searchTerm: "",
  searchPlaceholder: "Search records, owners, or keywords",
  activeFilter: "",
  filters: () => [],
  summary: "",
  showReset: true,
});

const emit = defineEmits<{
  "update:searchTerm": [value: string];
  "update:activeFilter": [value: string];
  reset: [];
}>();

const hasActiveControls = computed(() => {
  const firstFilter = props.filters[0]?.value ?? "";
  return (
    props.searchTerm.trim().length > 0 ||
    (!!props.activeFilter && props.activeFilter !== firstFilter)
  );
});

const wrapperClasses = computed(() =>
  cn(
    "rounded-2xl border border-secondary-200 bg-white/90 p-4 shadow-[var(--theme-shadow-soft)] ring-1 ring-white/60 backdrop-blur dark:border-secondary-700 dark:bg-secondary-900/80 dark:ring-secondary-800/80",
    props.class,
  ),
);

const handleReset = () => {
  emit("update:searchTerm", "");
  emit("update:activeFilter", props.filters[0]?.value ?? "");
  emit("reset");
};

const handleSearch = (event: Event) => {
  const target = event.target as HTMLInputElement;
  emit("update:searchTerm", target.value);
};
</script>

<template>
  <section :class="wrapperClasses">
    <div class="flex flex-col gap-3">
      <div
        class="flex flex-wrap items-center justify-between gap-2.5 lg:flex-nowrap"
      >
        <div class="flex min-w-0 flex-wrap items-center gap-2.5">
          <div
            class="relative max-w-full w-[13rem] sm:w-56 md:w-60 lg:w-64 xl:w-72"
          >
            <MagnifyingGlassIcon
              class="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-secondary-400"
            />
            <input
              :value="searchTerm"
              type="search"
              :placeholder="searchPlaceholder"
              :aria-label="searchPlaceholder"
              class="min-h-11 w-full rounded-xl border border-secondary-300 bg-white py-2 pl-10 pr-4 text-sm text-secondary-900 shadow-[var(--theme-shadow-soft)] outline-none transition focus:border-primary-500 focus:ring-2 focus:ring-primary-500 dark:border-secondary-600 dark:bg-secondary-800 dark:text-secondary-50 dark:placeholder:text-secondary-500"
              @input="handleSearch"
            />
          </div>

          <div class="flex flex-wrap gap-2">
            <NieButton
              v-for="filter in filters"
              :key="filter.value"
              :variant="activeFilter === filter.value ? 'primary' : 'outline'"
              @click="emit('update:activeFilter', filter.value)"
            >
              <span>{{ filter.label }}</span>
              <span
                v-if="typeof filter.count === 'number'"
                class="rounded-full bg-black/10 px-2 py-0.5 text-xs text-current dark:bg-white/10"
              >
                {{ filter.count }}
              </span>
            </NieButton>
          </div>
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

      <div
        v-if="summary || $slots.summary"
        class="flex flex-wrap items-center gap-3 text-sm text-secondary-500 dark:text-secondary-400"
      >
        <span v-if="summary">{{ summary }}</span>
        <slot name="summary"></slot>
      </div>
    </div>
  </section>
</template>


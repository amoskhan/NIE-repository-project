<script setup lang="ts">
import { ExclamationTriangleIcon, FunnelIcon } from "@heroicons/vue/24/outline";
import { NieButton } from "../../ui/button";
import { NieModal } from "../../ui/modal";

interface FilterSummary {
  key: string;
  label: string;
  count: number;
}

withDefaults(
  defineProps<{
    modelValue: boolean;
    filters: FilterSummary[];
    saving?: boolean;
    error?: string | null;
  }>(),
  {
    saving: false,
    error: null,
  },
);

const emit = defineEmits<{
  keep: [];
  remove: [];
}>();
</script>

<template>
  <NieModal
    :model-value="modelValue"
    aria-label="Review saved table filters"
    initial-focus="[data-filter-reminder-keep]"
    size="md"
    placement="mobile-sheet"
    :close-on-overlay="false"
    :close-on-escape="false"
    :show-close="false"
  >
    <section data-table-filter-reminder class="space-y-5">
      <div class="flex items-start gap-4">
        <span
          class="inline-flex size-12 shrink-0 items-center justify-center rounded-full bg-warning-100 text-warning-700 dark:bg-warning-950/60 dark:text-warning-300"
          aria-hidden="true"
        >
          <ExclamationTriangleIcon class="size-6" />
        </span>
        <div class="min-w-0">
          <h2 class="text-lg font-semibold text-secondary-950 dark:text-white">
            Saved filters are active
          </h2>
          <p class="mt-1 text-sm leading-6 text-secondary-600 dark:text-secondary-300">
            This table is using your saved default filters. Some records may be
            hidden from this view.
          </p>
        </div>
      </div>

      <div
        class="rounded-xl border border-secondary-200 bg-secondary-50 p-4 dark:border-secondary-700 dark:bg-secondary-900"
      >
        <p class="text-xs font-semibold uppercase tracking-wide text-secondary-500 dark:text-secondary-400">
          Active default filters
        </p>
        <ul class="mt-3 flex flex-wrap gap-2" aria-label="Active saved filters">
          <li
            v-for="filter in filters"
            :key="filter.key"
            class="inline-flex min-h-8 items-center gap-1.5 rounded-full border border-primary-200 bg-primary-50 px-3 text-xs font-semibold text-primary-700 dark:border-primary-800 dark:bg-primary-950/50 dark:text-primary-300"
          >
            <FunnelIcon class="size-3.5" aria-hidden="true" />
            {{ filter.label }} ({{ filter.count }})
          </li>
        </ul>
      </div>

      <p class="text-sm leading-6 text-secondary-700 dark:text-secondary-200">
        Keep these filters for another week, or remove only the default filters
        and show all available records now.
      </p>

      <div
        v-if="error"
        class="flex gap-3 rounded-xl border border-danger-300 bg-danger-50 p-3 text-sm text-danger-900 dark:border-danger-700 dark:bg-danger-950/40 dark:text-danger-100"
        role="alert"
      >
        <ExclamationTriangleIcon class="mt-0.5 size-5 shrink-0" />
        <p>{{ error }}</p>
      </div>
    </section>

    <template #footer>
      <div class="grid gap-2 sm:grid-cols-2">
        <NieButton
          class="w-full"
          variant="danger"
          :disabled="saving"
          aria-label="Remove saved default filters"
          @click="emit('remove')"
        >
          Remove default filters
        </NieButton>
        <NieButton
          class="w-full"
          :loading="saving"
          data-filter-reminder-keep
          aria-label="Keep saved filters for another week"
          @click="emit('keep')"
        >
          Keep for another week
        </NieButton>
      </div>
    </template>
  </NieModal>
</template>

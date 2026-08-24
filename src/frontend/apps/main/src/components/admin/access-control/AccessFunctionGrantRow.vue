<script setup lang="ts">
import { computed, ref } from "vue";
import type { AccessFunction } from "@/types";

const props = withDefaults(
  defineProps<{
    accessFunction: AccessFunction;
    selected?: boolean;
    disabled?: boolean;
    selectable?: boolean;
  }>(),
  { selected: false, selectable: true },
);

const emit = defineEmits<{
  "update:selected": [value: boolean];
}>();

const showDescription = ref(false);
const typeLabel = computed(() => {
  const value = String(props.accessFunction.type).toLowerCase();
  return value === "1" || value === "screen" ? "Screen" : "API";
});
</script>

<template>
  <div
    class="access-function-grant rounded-2xl border border-secondary-200 bg-white p-3 transition hover:border-primary-200 hover:bg-primary-50/30 dark:border-secondary-700 dark:bg-secondary-900 dark:hover:border-primary-800"
  >
    <div class="flex items-start gap-3">
      <label
        class="flex min-w-0 flex-1 items-start gap-3"
        :class="selectable ? 'cursor-pointer' : 'cursor-default'"
      >
        <input
          v-if="selectable"
          type="checkbox"
          class="mt-1 size-5 shrink-0 rounded-full border-2 border-secondary-300 text-primary-600 focus:ring-primary-500 dark:border-secondary-600 dark:bg-secondary-800"
          :checked="selected"
          :disabled="disabled"
          :aria-label="`Grant ${accessFunction.name}`"
          @change="
            emit('update:selected', ($event.target as HTMLInputElement).checked)
          "
        />
        <span class="min-w-0">
          <span class="flex flex-wrap items-center gap-2">
            <span
              class="font-semibold text-secondary-900 dark:text-secondary-100"
            >
              {{ accessFunction.name }}
            </span>
            <span
              class="access-function-type rounded-full px-2 py-0.5 text-caption font-bold uppercase tracking-wide"
              :class="
                typeLabel === 'Screen'
                  ? 'bg-info-100 text-info-700 dark:bg-info-950/50 dark:text-info-300'
                  : 'bg-success-100 text-success-700 dark:bg-success-950/50 dark:text-success-300'
              "
            >
              {{ typeLabel }}
            </span>
          </span>
          <span
            class="mt-1 block break-all font-mono text-xs text-secondary-500 dark:text-secondary-400"
          >
            {{ accessFunction.code }}
          </span>
        </span>
      </label>

      <button
        type="button"
        class="inline-flex size-11 shrink-0 items-center justify-center rounded-full text-secondary-500 transition hover:bg-secondary-100 hover:text-primary-700 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary-500 dark:hover:bg-secondary-800"
        :aria-label="`About ${accessFunction.name}`"
        :aria-expanded="showDescription"
        :title="accessFunction.description || 'No description available'"
        @click="showDescription = !showDescription"
      >
        <span class="material-symbols-outlined text-section-title" aria-hidden="true">
          info
        </span>
      </button>
    </div>

    <div
      v-if="showDescription"
      class="mt-3 rounded-xl bg-secondary-50 px-3 py-2 text-sm leading-5 text-secondary-600 dark:bg-secondary-800 dark:text-secondary-300"
      role="note"
    >
      {{ accessFunction.description || "No description is available." }}
      <span
        v-if="accessFunction.route || accessFunction.httpMethod"
        class="mt-1 block font-mono text-xs text-secondary-500"
      >
        {{
          [accessFunction.httpMethod, accessFunction.route]
            .filter(Boolean)
            .join(" ")
        }}
      </span>
    </div>
  </div>
</template>

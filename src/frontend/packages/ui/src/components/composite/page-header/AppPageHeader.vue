<script setup lang="ts">
import { computed } from "vue";
import { cn } from "../../../lib/utils";

interface Props {
  eyebrow?: string;
  title: string;
  subtitle?: string;
  compact?: boolean;
  class?: string;
}

const props = withDefaults(defineProps<Props>(), {
  compact: false,
});

const headerClasses = computed(() =>
  cn(
    "rounded-2xl border border-secondary-200 bg-white/90 shadow-sm ring-1 ring-white/60 backdrop-blur dark:border-secondary-700 dark:bg-secondary-900/80 dark:ring-secondary-800/80",
    "bg-[radial-gradient(circle_at_top_right,rgba(37,99,235,0.12),transparent_32%),radial-gradient(circle_at_bottom_left,rgba(14,165,233,0.08),transparent_26%)]",
    props.compact ? "p-5" : "p-6 md:p-8",
    props.class,
  ),
);
</script>

<template>
  <section :class="headerClasses">
    <div
      class="flex flex-col gap-6 lg:flex-row lg:items-end lg:justify-between"
    >
      <div class="min-w-0 space-y-3">
        <p
          v-if="eyebrow"
          class="text-xs font-semibold uppercase tracking-[0.24em] text-primary-600 dark:text-primary-300"
        >
          {{ eyebrow }}
        </p>

        <div class="space-y-2">
          <div class="flex flex-wrap items-center gap-3">
            <h1
              class="text-2xl font-semibold tracking-tight text-secondary-900 dark:text-secondary-50 md:text-3xl"
            >
              {{ title }}
            </h1>
            <slot name="meta"></slot>
          </div>

          <p
            v-if="subtitle"
            class="max-w-3xl text-sm leading-6 text-secondary-600 dark:text-secondary-300 md:text-base"
          >
            {{ subtitle }}
          </p>
        </div>
      </div>

      <div
        v-if="$slots.actions"
        class="flex flex-wrap items-center gap-3 lg:justify-end"
      >
        <slot name="actions"></slot>
      </div>
    </div>
  </section>
</template>

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
    "rounded-2xl border border-secondary-200 bg-white/90 shadow-[var(--theme-shadow-soft)] ring-1 ring-white/60 backdrop-blur dark:border-secondary-700 dark:bg-secondary-900/80 dark:ring-secondary-800/80",
    "nie-page-header",
    props.compact ? "p-5" : "p-6 md:p-8",
    props.class,
  ),
);
</script>

<template>
  <section :class="headerClasses">
    <div class="flex flex-col gap-6 lg:flex-row lg:items-end lg:justify-between">
      <div class="min-w-0 space-y-3">
        <p
          v-if="eyebrow"
          class="text-xs font-semibold uppercase tracking-wide text-primary-600 dark:text-primary-300"
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

<style scoped>
.nie-page-header {
  background-image:
    radial-gradient(
      circle at top right,
      color-mix(in srgb, var(--theme-color-brand-600) 12%, transparent),
      transparent 32%
    ),
    radial-gradient(
      circle at bottom left,
      color-mix(in srgb, var(--theme-color-info-500) 8%, transparent),
      transparent 26%
    );
}
</style>

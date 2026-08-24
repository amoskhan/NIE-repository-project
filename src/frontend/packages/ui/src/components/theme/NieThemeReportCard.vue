<script setup lang="ts">
interface Props {
  title: string;
  summary: string;
  accentClass?: string;
  tag?: string;
}

withDefaults(defineProps<Props>(), {
  accentClass: "from-primary-600 via-info-500 to-info-400",
  tag: "",
});
</script>

<template>
  <article
    class="overflow-hidden rounded-3xl border border-secondary-200 bg-white/95 shadow-[var(--theme-shadow-soft)] dark:border-secondary-700 dark:bg-secondary-900/90"
  >
    <div class="h-2 w-full bg-gradient-to-r" :class="accentClass"></div>
    <div class="space-y-4 p-6">
      <div class="flex items-center justify-between gap-3">
        <h3 class="text-lg font-bold text-secondary-900 dark:text-secondary-50">
          {{ title }}
        </h3>
        <span
          v-if="tag"
          class="rounded-full bg-secondary-100 px-3 py-1 text-caption font-semibold uppercase tracking-wide text-secondary-600 dark:bg-secondary-800 dark:text-secondary-300"
        >
          {{ tag }}
        </span>
      </div>
      <p class="text-sm leading-6 text-secondary-600 dark:text-secondary-300">
        {{ summary }}
      </p>
      <slot></slot>
    </div>
  </article>
</template>

<script setup lang="ts">
import { computed } from "vue";
import { cn } from "../../lib/utils";

type ThemeStatTone = "brand" | "info" | "success" | "warning" | "danger";

interface Props {
  label: string;
  value: string;
  delta?: string;
  icon?: string;
  tone?: ThemeStatTone;
  class?: string;
}

const props = withDefaults(defineProps<Props>(), {
  delta: "",
  icon: "",
  tone: "brand",
});

const toneClasses: Record<ThemeStatTone, string> = {
  brand:
    "bg-primary-50 text-primary-700 dark:bg-primary-900/25 dark:text-primary-300",
  info: "bg-sky-50 text-sky-700 dark:bg-sky-950/30 dark:text-sky-300",
  success:
    "bg-emerald-50 text-emerald-700 dark:bg-emerald-950/30 dark:text-emerald-300",
  warning:
    "bg-amber-50 text-amber-700 dark:bg-amber-950/30 dark:text-amber-300",
  danger: "bg-rose-50 text-rose-700 dark:bg-rose-950/30 dark:text-rose-300",
};

const cardClasses = computed(() =>
  cn(
    "rounded-3xl border border-secondary-200 bg-white/95 p-5 shadow-sm transition-all dark:border-secondary-700 dark:bg-secondary-900/90",
    props.class,
  ),
);
</script>

<template>
  <article :class="cardClasses">
    <div class="flex items-start justify-between gap-4">
      <div class="min-w-0">
        <p
          class="text-xs font-semibold uppercase tracking-[0.2em] text-secondary-500 dark:text-secondary-400"
        >
          {{ label }}
        </p>
        <p
          class="mt-3 text-3xl font-black tracking-tight text-secondary-900 dark:text-secondary-50"
        >
          {{ value }}
        </p>
        <p
          v-if="delta"
          class="mt-2 text-sm text-secondary-500 dark:text-secondary-400"
        >
          {{ delta }}
        </p>
      </div>

      <div
        v-if="icon"
        class="flex h-12 w-12 shrink-0 items-center justify-center rounded-2xl"
        :class="toneClasses[tone]"
      >
        <span class="material-symbols-outlined text-[22px]">{{ icon }}</span>
      </div>
    </div>
  </article>
</template>

<script setup lang="ts">
import { computed } from "vue";
import {
  CheckCircleIcon,
  ExclamationTriangleIcon,
  InboxIcon,
  InformationCircleIcon,
} from "@heroicons/vue/24/outline";
import AppLoaderSymbol from "../loading/AppLoaderSymbol.vue";
import { cn } from "../../../lib/utils";

export type StatePanelVariant =
  "info" | "success" | "warning" | "error" | "empty" | "loading";

interface Props {
  variant?: StatePanelVariant;
  title: string;
  description?: string;
  compact?: boolean;
  class?: string;
}

const props = withDefaults(defineProps<Props>(), {
  variant: "info",
  description: "",
  compact: false,
});

const wrapperClasses = computed(() =>
  cn(
    "rounded-2xl border bg-white/95 text-center shadow-sm dark:bg-secondary-900/90",
    props.compact ? "px-5 py-6" : "px-6 py-10",
    props.variant === "error" &&
      "border-red-200 dark:border-red-900/60 dark:bg-red-950/20",
    props.variant === "warning" &&
      "border-amber-200 dark:border-amber-900/60 dark:bg-amber-950/20",
    props.variant === "success" &&
      "border-emerald-200 dark:border-emerald-900/60 dark:bg-emerald-950/20",
    (props.variant === "info" ||
      props.variant === "empty" ||
      props.variant === "loading") &&
      "border-secondary-200 dark:border-secondary-700",
    props.class,
  ),
);

const iconComponent = computed(() => {
  switch (props.variant) {
    case "success":
      return CheckCircleIcon;
    case "warning":
    case "error":
      return ExclamationTriangleIcon;
    case "empty":
      return InboxIcon;
    default:
      return InformationCircleIcon;
  }
});

const iconWrapperClasses = computed(() => {
  switch (props.variant) {
    case "success":
      return "bg-emerald-100 text-emerald-600 dark:bg-emerald-900/30 dark:text-emerald-300";
    case "warning":
      return "bg-amber-100 text-amber-600 dark:bg-amber-900/30 dark:text-amber-300";
    case "error":
      return "bg-red-100 text-red-600 dark:bg-red-900/30 dark:text-red-300";
    default:
      return "bg-primary-100 text-primary-600 dark:bg-primary-900/30 dark:text-primary-300";
  }
});
</script>

<template>
  <section :class="wrapperClasses">
    <div class="mx-auto flex max-w-xl flex-col items-center gap-4">
      <div
        :class="[
          'flex h-14 w-14 items-center justify-center rounded-2xl',
          iconWrapperClasses,
        ]"
      >
        <AppLoaderSymbol
          v-if="variant === 'loading'"
          size="md"
          tone="current"
          label="Loading"
        />
        <component v-else :is="iconComponent" class="h-7 w-7" />
      </div>

      <div class="space-y-2">
        <h3
          class="text-lg font-semibold text-secondary-900 dark:text-secondary-50"
        >
          {{ title }}
        </h3>
        <p
          v-if="description"
          class="text-sm leading-6 text-secondary-600 dark:text-secondary-300"
        >
          {{ description }}
        </p>
      </div>

      <div v-if="$slots.actions" class="flex flex-wrap justify-center gap-3">
        <slot name="actions"></slot>
      </div>
    </div>
  </section>
</template>

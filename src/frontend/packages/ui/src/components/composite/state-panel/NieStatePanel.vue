<script setup lang="ts">
import { computed } from "vue";
import {
  CheckCircleIcon,
  ExclamationTriangleIcon,
  InboxIcon,
  InformationCircleIcon,
} from "@heroicons/vue/24/outline";
import NieLoaderSymbol from "../loading/NieLoaderSymbol.vue";
import { cn } from "../../../lib/utils";

export type StatePanelVariant =
  | "info"
  | "success"
  | "warning"
  | "error"
  | "empty"
  | "loading";

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
    "rounded-2xl border bg-white/95 text-center shadow-[var(--theme-shadow-soft)] dark:bg-secondary-900/90",
    props.compact ? "px-5 py-6" : "px-6 py-10",
    props.variant === "error" &&
      "border-danger-200 dark:border-danger-900/60 dark:bg-danger-950/20",
    props.variant === "warning" &&
      "border-warning-200 dark:border-warning-900/60 dark:bg-warning-950/20",
    props.variant === "success" &&
      "border-success-200 dark:border-success-900/60 dark:bg-success-950/20",
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
      return "bg-success-100 text-success-600 dark:bg-success-900/30 dark:text-success-300";
    case "warning":
      return "bg-warning-100 text-warning-600 dark:bg-warning-900/30 dark:text-warning-300";
    case "error":
      return "bg-danger-100 text-danger-600 dark:bg-danger-900/30 dark:text-danger-300";
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
        <NieLoaderSymbol
          v-if="variant === 'loading'"
          size="md"
          tone="current"
          label="Loading"
        />
        <component v-else :is="iconComponent" class="h-7 w-7" />
      </div>

      <div class="space-y-2">
        <h3 class="text-lg font-semibold text-secondary-900 dark:text-secondary-50">
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

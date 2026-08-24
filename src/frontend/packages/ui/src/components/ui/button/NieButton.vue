<script setup lang="ts">
import { computed } from "vue";
import NieLoaderSymbol from "../../composite/loading/NieLoaderSymbol.vue";
import { cn } from "../../../lib/utils";

type ButtonVariant = "primary" | "secondary" | "danger" | "ghost" | "outline";
type ButtonSize = "sm" | "md" | "lg";

interface Props {
  variant?: ButtonVariant;
  size?: ButtonSize;
  disabled?: boolean;
  loading?: boolean;
  type?: "button" | "submit" | "reset";
  class?: string;
}

const props = withDefaults(defineProps<Props>(), {
  variant: "primary",
  size: "md",
  disabled: false,
  loading: false,
  type: "button",
});

const emit = defineEmits<{
  click: [event: MouseEvent];
}>();

const variantClasses: Record<ButtonVariant, string> = {
  primary:
    "bg-primary-600 text-on-brand hover:bg-primary-700 focus:ring-primary-500 dark:bg-primary-600 dark:hover:bg-primary-700",
  secondary:
    "bg-secondary-100 text-secondary-700 hover:bg-secondary-200 focus:ring-secondary-500 dark:bg-secondary-700 dark:text-secondary-100 dark:hover:bg-secondary-600",
  danger:
    "bg-status-danger text-on-danger focus:ring-danger-500",
  ghost:
    "bg-transparent text-secondary-600 hover:bg-secondary-100 focus:ring-secondary-500 dark:text-secondary-300 dark:hover:bg-secondary-800",
  outline:
    "border border-secondary-300 bg-transparent text-secondary-700 hover:bg-secondary-50 focus:ring-secondary-500 dark:border-secondary-600 dark:text-secondary-300 dark:hover:bg-secondary-800",
};

const sizeClasses: Record<ButtonSize, string> = {
  sm: "min-h-10 px-3 py-0 text-sm",
  md: "min-h-11 px-4 py-0 text-sm",
  lg: "min-h-12 px-6 py-0 text-base",
};

const buttonClasses = computed(() =>
  cn(
    "inline-flex items-center justify-center gap-2 rounded-[var(--theme-radius-control)] font-medium transition-all duration-200 focus:outline-none focus:ring-2 focus:ring-offset-2 disabled:opacity-50 disabled:cursor-not-allowed",
    variantClasses[props.variant],
    sizeClasses[props.size],
    props.class,
  ),
);

const handleClick = (event: MouseEvent) => {
  if (!props.disabled && !props.loading) {
    emit("click", event);
  }
};
</script>

<template>
  <button
    data-nie-control="button"
    :type="type"
    :class="buttonClasses"
    :disabled="disabled || loading"
    @click="handleClick"
  >
    <NieLoaderSymbol
      v-if="loading"
      size="xs"
      tone="current"
      label="Loading"
      class="-ml-0.5"
    />
    <slot></slot>
  </button>
</template>


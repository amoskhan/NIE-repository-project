<script setup lang="ts">
import { computed } from "vue";
import { cn } from "../../../lib/utils";

export type NieBadgeVariant =
  | "default"
  | "primary"
  | "success"
  | "warning"
  | "danger"
  | "info";
type BadgeSize = "sm" | "md" | "lg";

interface Props {
  variant?: NieBadgeVariant;
  size?: BadgeSize;
  rounded?: boolean;
  dot?: boolean;
  class?: string;
}

const props = withDefaults(defineProps<Props>(), {
  variant: "default",
  size: "md",
  rounded: false,
  dot: false,
});

const variantClasses: Record<NieBadgeVariant, string> = {
  default:
    "border-secondary-200 bg-secondary-50 text-secondary-700 dark:border-secondary-700 dark:bg-secondary-800 dark:text-secondary-200",
  primary:
    "border-primary-200 bg-primary-50 text-primary-700 dark:border-primary-800 dark:bg-primary-950/50 dark:text-primary-200",
  success:
    "border-success-200 bg-success-50 text-success-700 dark:border-success-800 dark:bg-success-950/50 dark:text-success-200",
  warning:
    "border-warning-200 bg-warning-50 text-warning-800 dark:border-warning-800 dark:bg-warning-950/50 dark:text-warning-200",
  danger:
    "border-danger-200 bg-danger-50 text-danger-700 dark:border-danger-800 dark:bg-danger-950/50 dark:text-danger-200",
  info: "border-info-200 bg-info-50 text-info-700 dark:border-info-800 dark:bg-info-950/50 dark:text-info-200",
};

const dotClasses: Record<NieBadgeVariant, string> = {
  default: "bg-secondary-400",
  primary: "bg-primary-500",
  success: "bg-success-500",
  warning: "bg-warning-500",
  danger: "bg-danger-500",
  info: "bg-info-500",
};

const sizeClasses: Record<BadgeSize, string> = {
  sm: "px-2 py-0.5 text-xs",
  md: "px-2.5 py-0.5 text-sm",
  lg: "px-3 py-1 text-sm",
};

const badgeClasses = computed(() =>
  cn(
    "nie-badge inline-flex items-center gap-1.5 border font-semibold leading-5 shadow-[var(--theme-shadow-soft)]",
    `nie-badge--${props.variant}`,
    props.rounded ? "rounded-full" : "rounded-md",
    variantClasses[props.variant],
    sizeClasses[props.size],
    props.class
  )
);
</script>

<template>
  <span :class="badgeClasses">
    <span
      v-if="dot"
      class="h-1.5 w-1.5 shrink-0 rounded-full"
      :class="dotClasses[variant]"
      aria-hidden="true"
      data-testid="nie-badge-dot"
    ></span>
    <slot></slot>
  </span>
</template>

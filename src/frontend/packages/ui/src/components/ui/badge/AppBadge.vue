<script setup lang="ts">
import { computed } from "vue";
import { cn } from "../../../lib/utils";

type BadgeVariant =
  "default" | "primary" | "success" | "warning" | "danger" | "info";
type BadgeSize = "sm" | "md" | "lg";

interface Props {
  variant?: BadgeVariant;
  size?: BadgeSize;
  rounded?: boolean;
  class?: string;
}

const props = withDefaults(defineProps<Props>(), {
  variant: "default",
  size: "md",
  rounded: false,
});

const variantClasses: Record<BadgeVariant, string> = {
  default:
    "bg-secondary-100 text-secondary-800 dark:bg-secondary-700 dark:text-secondary-200",
  primary:
    "bg-primary-100 text-primary-800 dark:bg-primary-900 dark:text-primary-200",
  success: "bg-green-100 text-green-800 dark:bg-green-900 dark:text-green-200",
  warning:
    "bg-yellow-100 text-yellow-800 dark:bg-yellow-900 dark:text-yellow-200",
  danger: "bg-red-100 text-red-800 dark:bg-red-900 dark:text-red-200",
  info: "bg-blue-100 text-blue-800 dark:bg-blue-900 dark:text-blue-200",
};

const sizeClasses: Record<BadgeSize, string> = {
  sm: "px-2 py-0.5 text-xs",
  md: "px-2.5 py-0.5 text-sm",
  lg: "px-3 py-1 text-sm",
};

const badgeClasses = computed(() =>
  cn(
    "inline-flex items-center font-medium",
    props.rounded ? "rounded-full" : "rounded-md",
    variantClasses[props.variant],
    sizeClasses[props.size],
    props.class,
  ),
);
</script>

<template>
  <span :class="badgeClasses">
    <slot></slot>
  </span>
</template>

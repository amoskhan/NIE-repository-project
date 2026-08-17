<script setup lang="ts">
import { computed } from "vue";
import { cn } from "../../../lib/utils";

export type AppLoaderSymbolSize = "xs" | "sm" | "md" | "lg" | "xl";
export type AppLoaderSymbolTone =
  | "primary"
  | "secondary"
  | "success"
  | "warning"
  | "error"
  | "white"
  | "current";

interface Props {
  size?: AppLoaderSymbolSize;
  tone?: AppLoaderSymbolTone;
  label?: string;
  class?: string;
}

const props = withDefaults(defineProps<Props>(), {
  size: "md",
  tone: "primary",
  label: "Loading",
});

const sizeClasses: Record<AppLoaderSymbolSize, string> = {
  xs: "h-4 w-4",
  sm: "h-5 w-5",
  md: "h-8 w-8",
  lg: "h-12 w-12",
  xl: "h-16 w-16",
};

const toneClasses: Record<AppLoaderSymbolTone, string> = {
  primary: "text-primary-600 dark:text-primary-400",
  secondary: "text-secondary-600 dark:text-secondary-300",
  success: "text-emerald-600 dark:text-emerald-300",
  warning: "text-amber-600 dark:text-amber-300",
  error: "text-red-600 dark:text-red-300",
  white: "text-white",
  current: "text-current",
};

const symbolClasses = computed(() =>
  cn(
    "app-loader-symbol inline-flex shrink-0 items-center justify-center",
    sizeClasses[props.size],
    toneClasses[props.tone],
    props.class,
  ),
);
</script>

<template>
  <span
    :class="symbolClasses"
    role="status"
    :aria-label="label"
    data-testid="app-loader-symbol"
  >
    <svg
      class="app-loader-symbol__svg h-full w-full"
      viewBox="0 0 24 24"
      fill="none"
      aria-hidden="true"
    >
      <circle
        class="app-loader-symbol__track"
        cx="12"
        cy="12"
        r="9"
        stroke="currentColor"
        stroke-width="2.5"
      />
      <circle
        class="app-loader-symbol__outer"
        cx="12"
        cy="12"
        r="9"
        stroke="currentColor"
        stroke-width="2.5"
        stroke-linecap="round"
        pathLength="100"
      />
      <circle
        class="app-loader-symbol__inner"
        cx="12"
        cy="12"
        r="5"
        stroke="currentColor"
        stroke-width="2"
        stroke-linecap="round"
        pathLength="100"
      />
      <circle
        class="app-loader-symbol__core"
        cx="12"
        cy="12"
        r="1.35"
        fill="currentColor"
      />
    </svg>
  </span>
</template>

<style scoped>
.app-loader-symbol__svg {
  animation: app-loader-rotate 1.1s linear infinite;
}

.app-loader-symbol__track {
  opacity: 0.16;
}

.app-loader-symbol__outer {
  stroke-dasharray: 58 100;
  transform-origin: center;
  animation: app-loader-dash 1.35s ease-in-out infinite;
}

.app-loader-symbol__inner {
  opacity: 0.72;
  stroke-dasharray: 34 100;
  transform-origin: center;
  animation: app-loader-dash 1.35s ease-in-out infinite reverse;
}

.app-loader-symbol__core {
  opacity: 0.9;
}

@keyframes app-loader-rotate {
  to {
    transform: rotate(360deg);
  }
}

@keyframes app-loader-dash {
  0% {
    stroke-dashoffset: 0;
  }

  50% {
    stroke-dashoffset: -36;
  }

  100% {
    stroke-dashoffset: -100;
  }
}

@media (prefers-reduced-motion: reduce) {
  .app-loader-symbol__svg,
  .app-loader-symbol__outer,
  .app-loader-symbol__inner {
    animation-duration: 2.5s;
  }
}
</style>

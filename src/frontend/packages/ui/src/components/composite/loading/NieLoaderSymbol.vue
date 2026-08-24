<script setup lang="ts">
import { computed } from "vue";
import { cn } from "../../../lib/utils";

export type NieLoaderSymbolSize = "xs" | "sm" | "md" | "lg" | "xl";
export type NieLoaderSymbolVariant = "orbit" | "brand";
export type NieLoaderSymbolTone =
  | "primary"
  | "secondary"
  | "success"
  | "warning"
  | "error"
  | "white"
  | "current";

interface Props {
  size?: NieLoaderSymbolSize;
  variant?: NieLoaderSymbolVariant;
  tone?: NieLoaderSymbolTone;
  label?: string;
  class?: string;
}

const props = withDefaults(defineProps<Props>(), {
  size: "md",
  variant: "orbit",
  tone: "primary",
  label: "Loading",
});

const sizeClasses: Record<NieLoaderSymbolSize, string> = {
  xs: "h-4 w-4",
  sm: "h-5 w-5",
  md: "h-8 w-8",
  lg: "h-12 w-12",
  xl: "h-16 w-16",
};

const toneClasses: Record<NieLoaderSymbolTone, string> = {
  primary: "text-primary-600 dark:text-primary-400",
  secondary: "text-secondary-600 dark:text-secondary-300",
  success: "text-success-600 dark:text-success-300",
  warning: "text-warning-600 dark:text-warning-300",
  error: "text-danger-600 dark:text-danger-300",
  white: "text-white",
  current: "text-current",
};

const symbolClasses = computed(() =>
  cn(
    "nie-loader-symbol inline-flex shrink-0 items-center justify-center",
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
    :data-loader-variant="variant"
    data-testid="nie-loader-symbol"
  >
    <svg
      v-if="variant === 'brand'"
      class="nie-loader-symbol__brand h-full w-full overflow-visible"
      viewBox="0 0 96 96"
      fill="none"
      aria-hidden="true"
    >
      <g class="nie-loader-symbol__brand-orbit" data-loader-orbit>
        <circle
          cx="48"
          cy="48"
          r="42"
          stroke="currentColor"
          stroke-width="2.5"
          stroke-linecap="round"
          stroke-dasharray="74 190"
        />
        <circle cx="48" cy="6" r="3.25" fill="currentColor" />
      </g>

      <g
        class="nie-loader-symbol__brand-monogram"
        fill="currentColor"
        text-anchor="middle"
        font-family="'Plus Jakarta Sans', ui-sans-serif, system-ui, sans-serif"
        font-size="30"
        font-weight="800"
      >
        <text
          class="nie-loader-symbol__brand-letter nie-loader-symbol__brand-letter--n"
          x="27"
          y="59"
          data-loader-letter
        >
          N
        </text>
        <text
          class="nie-loader-symbol__brand-letter nie-loader-symbol__brand-letter--i"
          x="48"
          y="59"
          data-loader-letter
        >
          I
        </text>
        <text
          class="nie-loader-symbol__brand-letter nie-loader-symbol__brand-letter--e"
          x="69"
          y="59"
          data-loader-letter
        >
          E
        </text>
      </g>
    </svg>

    <svg
      v-else
      class="nie-loader-symbol__compact nie-loader-symbol__svg h-full w-full"
      viewBox="0 0 24 24"
      fill="none"
      aria-hidden="true"
    >
      <circle
        class="nie-loader-symbol__track"
        cx="12"
        cy="12"
        r="9"
        stroke="currentColor"
        stroke-width="2.5"
      />
      <circle
        class="nie-loader-symbol__outer"
        cx="12"
        cy="12"
        r="9"
        stroke="currentColor"
        stroke-width="2.5"
        stroke-linecap="round"
        pathLength="100"
      />
      <circle
        class="nie-loader-symbol__inner"
        cx="12"
        cy="12"
        r="5"
        stroke="currentColor"
        stroke-width="2"
        stroke-linecap="round"
        pathLength="100"
      />
      <circle
        class="nie-loader-symbol__core"
        cx="12"
        cy="12"
        r="1.35"
        fill="currentColor"
      />
    </svg>
  </span>
</template>

<style scoped>
.nie-loader-symbol__svg {
  animation: nie-loader-rotate 1.1s linear infinite;
}

.nie-loader-symbol__track {
  opacity: 0.16;
}

.nie-loader-symbol__outer {
  stroke-dasharray: 58 100;
  transform-origin: center;
  animation: nie-loader-dash 1.35s ease-in-out infinite;
}

.nie-loader-symbol__inner {
  opacity: 0.72;
  stroke-dasharray: 34 100;
  transform-origin: center;
  animation: nie-loader-dash 1.35s ease-in-out infinite reverse;
}

.nie-loader-symbol__core {
  opacity: 0.9;
}

.nie-loader-symbol__brand-orbit {
  transform-origin: center;
  animation: nie-loader-brand-orbit 2.8s linear infinite;
}

.nie-loader-symbol__brand-letter {
  transform-box: fill-box;
  transform-origin: center bottom;
  animation: nie-loader-brand-jump 1.8s cubic-bezier(0.33, 1, 0.68, 1)
    infinite;
}

.nie-loader-symbol__brand-letter--n {
  animation-delay: 0s;
}

.nie-loader-symbol__brand-letter--i {
  animation-delay: 0.12s;
}

.nie-loader-symbol__brand-letter--e {
  animation-delay: 0.24s;
}

@keyframes nie-loader-rotate {
  to {
    transform: rotate(360deg);
  }
}

@keyframes nie-loader-dash {
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

@keyframes nie-loader-brand-orbit {
  to {
    transform: rotate(360deg);
  }
}

@keyframes nie-loader-brand-jump {
  0%,
  34%,
  100% {
    transform: translateY(0);
  }

  17% {
    transform: translateY(-5px);
  }
}

@keyframes nie-loader-reduced-pulse {
  0%,
  100% {
    opacity: 0.45;
  }

  50% {
    opacity: 1;
  }
}

@media (prefers-reduced-motion: reduce) {
  .nie-loader-symbol__svg,
  .nie-loader-symbol__brand-letter {
    animation: none;
  }

  .nie-loader-symbol__outer,
  .nie-loader-symbol__inner,
  .nie-loader-symbol__brand-orbit {
    animation: nie-loader-reduced-pulse 2.5s ease-in-out infinite;
  }
}
</style>

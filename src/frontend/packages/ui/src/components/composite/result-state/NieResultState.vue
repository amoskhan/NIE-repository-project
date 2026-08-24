<script setup lang="ts">
import { computed } from "vue";
import type { Component } from "vue";
import {
  CheckCircleIcon,
  ClockIcon,
  ExclamationTriangleIcon,
  InboxIcon,
  InformationCircleIcon,
  LockClosedIcon,
  MagnifyingGlassIcon,
  ShieldExclamationIcon,
  SignalSlashIcon,
} from "@heroicons/vue/24/outline";
import { cn } from "../../../lib/utils";
import NieLoaderSymbol from "../loading/NieLoaderSymbol.vue";

export type NieResultStatus = 401 | 403 | 404 | 408 | 429 | 500 | 502 | 503;
export type NieResultVariant =
  | "empty"
  | "info"
  | "success"
  | "warning"
  | "error"
  | "loading";

type ResultTone =
  | "primary"
  | "warning"
  | "danger"
  | "info"
  | "success"
  | "neutral";

interface ResultDefinition {
  title: string;
  description: string;
  visualLabel: string;
  icon: Component;
  tone: ResultTone;
}

interface Props {
  statusCode?: NieResultStatus;
  variant?: NieResultVariant;
  title?: string;
  description?: string;
  compact?: boolean;
  class?: string;
}

const props = withDefaults(defineProps<Props>(), {
  variant: "empty",
  title: "",
  description: "",
  compact: false,
});

defineSlots<{
  actions?: (props: {
    statusCode?: NieResultStatus;
    variant: NieResultVariant;
  }) => unknown;
}>();

const statusDefinitions: Record<NieResultStatus, ResultDefinition> = {
  401: {
    title: "Sign in required",
    description: "Your session is missing or has expired. Sign in to continue.",
    visualLabel: "Authentication required",
    icon: ShieldExclamationIcon,
    tone: "warning",
  },
  403: {
    title: "Access denied",
    description:
      "You do not have permission to view this page. Ask an administrator if you need access.",
    visualLabel: "Protected by access control",
    icon: LockClosedIcon,
    tone: "warning",
  },
  404: {
    title: "Page not found",
    description:
      "The page may have moved, been removed, or the address may be incorrect.",
    visualLabel: "The requested route is unavailable",
    icon: MagnifyingGlassIcon,
    tone: "primary",
  },
  408: {
    title: "Request timed out",
    description:
      "The request took too long to complete. Check your connection and try again.",
    visualLabel: "The request exceeded its time limit",
    icon: ClockIcon,
    tone: "warning",
  },
  429: {
    title: "Too many requests",
    description:
      "This application is receiving requests too quickly. Wait a moment and try again.",
    visualLabel: "Request limit reached",
    icon: ClockIcon,
    tone: "warning",
  },
  500: {
    title: "Something went wrong",
    description:
      "An unexpected error occurred. Try again, or contact support if it continues.",
    visualLabel: "The application encountered an error",
    icon: ExclamationTriangleIcon,
    tone: "danger",
  },
  502: {
    title: "Upstream service unavailable",
    description:
      "A required service returned an invalid response. Please wait a moment and try again.",
    visualLabel: "A connected service did not respond",
    icon: SignalSlashIcon,
    tone: "info",
  },
  503: {
    title: "Service unavailable",
    description:
      "This service is temporarily unavailable. Please wait a moment and try again.",
    visualLabel: "The service is temporarily offline",
    icon: SignalSlashIcon,
    tone: "info",
  },
};

const variantDefinitions: Record<NieResultVariant, ResultDefinition> = {
  empty: {
    title: "No records found",
    description: "There is nothing to display yet.",
    visualLabel: "No records to display",
    icon: InboxIcon,
    tone: "neutral",
  },
  info: {
    title: "Information",
    description: "Review the information below.",
    visualLabel: "Information notice",
    icon: InformationCircleIcon,
    tone: "info",
  },
  success: {
    title: "Complete",
    description: "The operation completed successfully.",
    visualLabel: "Operation complete",
    icon: CheckCircleIcon,
    tone: "success",
  },
  warning: {
    title: "Attention required",
    description: "Review this state before continuing.",
    visualLabel: "Action may be required",
    icon: ExclamationTriangleIcon,
    tone: "warning",
  },
  error: {
    title: "Something went wrong",
    description: "The operation could not be completed.",
    visualLabel: "The operation was unsuccessful",
    icon: ExclamationTriangleIcon,
    tone: "danger",
  },
  loading: {
    title: "Loading",
    description: "Please wait while the latest information is loaded.",
    visualLabel: "Loading the latest information",
    icon: InformationCircleIcon,
    tone: "primary",
  },
};

const definition = computed(() =>
  props.statusCode
    ? statusDefinitions[props.statusCode]
    : variantDefinitions[props.variant],
);
const resolvedTitle = computed(() => props.title || definition.value.title);
const resolvedDescription = computed(
  () => props.description || definition.value.description,
);
const eyebrow = computed(() =>
  props.statusCode ? `HTTP ${props.statusCode}` : props.variant,
);
const resultClasses = computed(() =>
  cn(
    "nie-result-state mx-auto grid w-full grid-cols-1 items-center text-center md:text-left",
    props.compact
      ? "min-h-[20rem] max-w-4xl gap-6 px-5 py-8 md:grid-cols-[minmax(0,1fr)_minmax(11rem,0.52fr)]"
      : "min-h-[30rem] max-w-6xl gap-10 px-6 py-12 md:grid-cols-[minmax(0,1fr)_minmax(15rem,0.7fr)] lg:gap-16",
    props.class,
  ),
);

const toneClasses: Record<ResultTone, string> = {
  primary: "text-primary-700 dark:text-primary-400",
  warning: "text-warning-800 dark:text-warning-300",
  danger: "text-danger-700 dark:text-danger-300",
  info: "text-info-700 dark:text-info-300",
  success: "text-success-700 dark:text-success-300",
  neutral: "text-secondary-600 dark:text-secondary-400",
};

const visualSurfaceClasses: Record<ResultTone, string> = {
  primary:
    "border-primary-200 bg-primary-50 text-primary-600 shadow-primary-100 dark:border-primary-800 dark:bg-primary-950/60 dark:text-primary-300 dark:shadow-none",
  warning:
    "border-warning-200 bg-warning-50 text-warning-600 shadow-warning-100 dark:border-warning-800 dark:bg-warning-950/60 dark:text-warning-300 dark:shadow-none",
  danger:
    "border-danger-200 bg-danger-50 text-danger-600 shadow-danger-100 dark:border-danger-800 dark:bg-danger-950/60 dark:text-danger-300 dark:shadow-none",
  info: "border-info-200 bg-info-50 text-info-600 shadow-info-100 dark:border-info-800 dark:bg-info-950/60 dark:text-info-300 dark:shadow-none",
  success:
    "border-success-200 bg-success-50 text-success-600 shadow-success-100 dark:border-success-800 dark:bg-success-950/60 dark:text-success-300 dark:shadow-none",
  neutral:
    "border-secondary-200 bg-secondary-50 text-secondary-600 shadow-secondary-100 dark:border-secondary-700 dark:bg-secondary-800 dark:text-secondary-300 dark:shadow-none",
};

const orbitClasses: Record<ResultTone, string> = {
  primary: "border-primary-200/80 dark:border-primary-800/70",
  warning: "border-warning-200/80 dark:border-warning-800/70",
  danger: "border-danger-200/80 dark:border-danger-800/70",
  info: "border-info-200/80 dark:border-info-800/70",
  success: "border-success-200/80 dark:border-success-800/70",
  neutral: "border-secondary-200/90 dark:border-secondary-700/70",
};
</script>

<template>
  <section
    :class="resultClasses"
    :aria-label="resolvedTitle"
    :aria-live="variant === 'loading' ? 'polite' : undefined"
    :data-result-status="statusCode"
    data-result-layout="split"
    data-testid="nie-result-state"
  >
    <div data-result-content class="flex min-w-0 flex-col items-center md:items-start">
      <p
        class="text-xs font-bold uppercase tracking-wide"
        :class="toneClasses[definition.tone]"
      >
        {{ eyebrow }}
      </p>

      <p
        v-if="statusCode"
        class="mt-3 select-none text-6xl font-bold leading-none tracking-tight text-secondary-950 dark:text-secondary-50 sm:text-7xl"
        aria-hidden="true"
      >
        {{ statusCode }}
      </p>

      <h1
        :class="[
          'font-bold tracking-tight text-secondary-950 dark:text-secondary-50',
          statusCode
            ? 'mt-4 text-3xl sm:text-4xl'
            : 'mt-3 text-2xl sm:text-3xl',
        ]"
      >
        {{ resolvedTitle }}
      </h1>
      <p
        class="mt-4 max-w-xl text-sm leading-7 text-secondary-600 dark:text-secondary-300"
        :class="statusCode ? 'sm:text-base' : ''"
      >
        {{ resolvedDescription }}
      </p>

      <div
        v-if="$slots.actions"
        class="mt-6 flex flex-wrap justify-center gap-3 md:justify-start"
      >
        <slot
          name="actions"
          :status-code="statusCode"
          :variant="variant"
        ></slot>
      </div>
    </div>

    <div
      data-result-visual
      class="flex min-w-0 flex-col items-center justify-center"
      :class="toneClasses[definition.tone]"
      aria-hidden="true"
    >
      <div
        class="relative aspect-square w-full"
        :class="compact ? 'max-w-48' : 'max-w-64'"
      >
        <span
          data-result-orbit
          class="absolute inset-0 rounded-full border"
          :class="orbitClasses[definition.tone]"
        ></span>
        <span
          data-result-orbit
          class="absolute inset-[14%] rounded-full border border-dashed"
          :class="orbitClasses[definition.tone]"
        ></span>
        <span
          class="absolute inset-[28%] flex items-center justify-center rounded-[28%] border shadow-[var(--theme-shadow-float)]"
          :class="visualSurfaceClasses[definition.tone]"
        >
          <NieLoaderSymbol
            v-if="variant === 'loading' && !statusCode"
            :size="compact ? 'lg' : 'xl'"
            variant="brand"
            tone="current"
            :label="resolvedTitle"
          />
          <component
            v-else
            :is="definition.icon"
            :class="compact ? 'h-12 w-12 stroke-[1.35]' : 'h-16 w-16 stroke-[1.25]'"
          />
        </span>
        <span
          class="absolute right-[8%] top-[16%] h-2.5 w-2.5 rounded-full bg-current opacity-60"
        ></span>
        <span
          class="absolute bottom-[10%] left-[18%] h-1.5 w-1.5 rounded-full bg-current opacity-35"
        ></span>
      </div>
      <p
        class="mt-4 max-w-64 text-center text-xs font-semibold uppercase tracking-wide opacity-80"
      >
        {{ definition.visualLabel }}
      </p>
    </div>
  </section>
</template>

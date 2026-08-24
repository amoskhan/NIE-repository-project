<script setup lang="ts">
import { computed } from "vue";
import { cn } from "../../../lib/utils";
import {
  CheckCircleIcon,
  ExclamationTriangleIcon,
  XCircleIcon,
  InformationCircleIcon,
} from "@heroicons/vue/24/outline";

type AlertVariant = "info" | "success" | "warning" | "danger";

interface Props {
  variant?: AlertVariant;
  title?: string;
  dismissible?: boolean;
  class?: string;
}

const props = withDefaults(defineProps<Props>(), {
  variant: "info",
  dismissible: false,
});

const emit = defineEmits<{
  dismiss: [];
}>();

const icons = {
  info: InformationCircleIcon,
  success: CheckCircleIcon,
  warning: ExclamationTriangleIcon,
  danger: XCircleIcon,
};

const variantClasses: Record<AlertVariant, string> = {
  info: "bg-info-50 text-info-800 dark:bg-info-900/20 dark:text-info-200",
  success: "bg-success-50 text-success-800 dark:bg-success-900/20 dark:text-success-200",
  warning: "bg-warning-50 text-warning-800 dark:bg-warning-900/20 dark:text-warning-200",
  danger: "bg-danger-50 text-danger-800 dark:bg-danger-900/20 dark:text-danger-200",
};

const iconClasses: Record<AlertVariant, string> = {
  info: "text-info-500",
  success: "text-success-500",
  warning: "text-warning-500",
  danger: "text-danger-500",
};

const alertClasses = computed(() =>
  cn(
    "rounded-lg p-4",
    variantClasses[props.variant],
    props.class
  )
);

const IconComponent = computed(() => icons[props.variant]);
</script>

<template>
  <div :class="alertClasses" role="alert">
    <div class="flex">
      <div class="flex-shrink-0">
        <component :is="IconComponent" :class="['h-5 w-5', iconClasses[variant]]" />
      </div>
      <div class="ml-3 flex-1">
        <h3 v-if="title" class="text-sm font-medium">{{ title }}</h3>
        <div class="text-sm" :class="title ? 'mt-2' : ''">
          <slot></slot>
        </div>
      </div>
      <button
        v-if="dismissible"
        type="button"
        class="ml-auto -mx-1.5 -my-1.5 rounded-lg p-1.5 inline-flex h-8 w-8 hover:bg-black/5 dark:hover:bg-white/5"
        @click="emit('dismiss')"
      >
        <span class="sr-only">Dismiss</span>
        <svg class="h-5 w-5" viewBox="0 0 20 20" fill="currentColor">
          <path fill-rule="evenodd" d="M4.293 4.293a1 1 0 011.414 0L10 8.586l4.293-4.293a1 1 0 111.414 1.414L11.414 10l4.293 4.293a1 1 0 01-1.414 1.414L10 11.414l-4.293 4.293a1 1 0 01-1.414-1.414L8.586 10 4.293 5.707a1 1 0 010-1.414z" clip-rule="evenodd" />
        </svg>
      </button>
    </div>
  </div>
</template>

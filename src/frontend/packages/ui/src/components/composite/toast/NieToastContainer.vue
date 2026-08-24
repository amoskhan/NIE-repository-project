<script setup lang="ts">
import {
  CheckCircleIcon,
  XCircleIcon,
  ExclamationTriangleIcon,
  InformationCircleIcon,
  XMarkIcon,
} from "@heroicons/vue/24/outline";

export type ToastType = "success" | "error" | "warning" | "info";

export interface Toast {
  id: string;
  type: ToastType;
  title?: string;
  message: string;
  duration?: number;
}

interface Props {
  toasts: readonly Toast[];
}

defineProps<Props>();

const emit = defineEmits<{
  dismiss: [id: string];
}>();

const icons = {
  success: CheckCircleIcon,
  error: XCircleIcon,
  warning: ExclamationTriangleIcon,
  info: InformationCircleIcon,
};

const iconClasses = {
  success: "text-success-500",
  error: "text-danger-500",
  warning: "text-warning-500",
  info: "text-info-500",
};

const bgClasses = {
  success: "bg-success-50 dark:bg-success-900/20",
  error: "bg-danger-50 dark:bg-danger-900/20",
  warning: "bg-warning-50 dark:bg-warning-900/20",
  info: "bg-info-50 dark:bg-info-900/20",
};
</script>

<template>
  <div
    class="fixed left-4 right-4 top-4 z-50 flex max-w-sm flex-col gap-2 sm:left-auto sm:w-full"
  >
    <TransitionGroup name="toast">
      <div
        v-for="toast in toasts"
        :key="toast.id"
        :role="toast.type === 'error' ? 'alert' : 'status'"
        :aria-live="toast.type === 'error' ? 'assertive' : 'polite'"
        :class="[
          'rounded-lg p-4 shadow-[var(--theme-shadow-float)] border',
          bgClasses[toast.type],
          'border-secondary-200 dark:border-secondary-700',
        ]"
      >
        <div class="flex items-start gap-3">
          <component
            :is="icons[toast.type]"
            :class="['h-5 w-5 flex-shrink-0', iconClasses[toast.type]]"
          />
          <div class="flex-1 min-w-0">
            <p
              v-if="toast.title"
              class="text-sm font-medium text-secondary-900 dark:text-secondary-100"
            >
              {{ toast.title }}
            </p>
            <p class="text-sm text-secondary-700 dark:text-secondary-300">
              {{ toast.message }}
            </p>
          </div>
          <button
            type="button"
            :aria-label="`Dismiss ${toast.type} notification`"
            class="flex-shrink-0 rounded-lg p-1 text-secondary-400 hover:text-secondary-600 dark:hover:text-secondary-300"
            @click="emit('dismiss', toast.id)"
          >
            <XMarkIcon class="h-4 w-4" />
          </button>
        </div>
      </div>
    </TransitionGroup>
  </div>
</template>

<style scoped>
.toast-enter-active {
  transition: all 0.3s ease-out;
}

.toast-leave-active {
  transition: all 0.2s ease-in;
}

.toast-enter-from {
  opacity: 0;
  transform: translateX(100%);
}

.toast-leave-to {
  opacity: 0;
  transform: translateX(100%);
}

.toast-move {
  transition: transform 0.3s ease;
}
</style>

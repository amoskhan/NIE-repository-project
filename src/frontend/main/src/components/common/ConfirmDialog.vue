<template>
  <Teleport to="body">
    <Transition name="modal">
      <div
        v-if="show"
        class="fixed inset-0 z-[10000] overflow-y-auto"
        @click.self="handleBackdropClick"
      >
        <!-- Backdrop -->
        <div class="fixed inset-0 bg-black/50 transition-opacity"></div>

        <!-- Modal -->
        <div class="flex min-h-screen items-center justify-center p-4">
          <div
            class="relative w-full max-w-md transform rounded-lg bg-white dark:bg-gray-800 shadow-xl transition-all"
            @click.stop
          >
            <!-- Content -->
            <div class="p-6 text-center">
              <!-- Icon -->
              <div
                :class="[
                  'mx-auto flex h-14 w-14 items-center justify-center rounded-full mb-4',
                  variant === 'danger'
                    ? 'bg-red-100 dark:bg-red-900/30'
                    : 'bg-yellow-100 dark:bg-yellow-900/30',
                ]"
              >
                <ExclamationTriangleIcon
                  :class="[
                    'h-7 w-7',
                    variant === 'danger'
                      ? 'text-red-600 dark:text-red-400'
                      : 'text-yellow-600 dark:text-yellow-400',
                  ]"
                />
              </div>

              <!-- Title -->
              <h3
                class="text-lg font-semibold text-gray-900 dark:text-white mb-2"
              >
                {{ title }}
              </h3>

              <!-- Message -->
              <p class="text-sm text-gray-600 dark:text-gray-400 mb-6">
                <slot>{{ message }}</slot>
              </p>

              <!-- Actions -->
              <div class="flex items-center justify-center gap-3">
                <button
                  type="button"
                  class="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-lg hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-gray-500 dark:bg-gray-700 dark:text-gray-200 dark:border-gray-600 dark:hover:bg-gray-600 transition-colors"
                  @click="handleCancel"
                  :disabled="loading"
                >
                  {{ cancelText }}
                </button>
                <button
                  type="button"
                  :class="[
                    'px-4 py-2 text-sm font-medium text-white rounded-lg focus:outline-none focus:ring-2 focus:ring-offset-2 transition-colors',
                    variant === 'danger'
                      ? 'bg-red-600 hover:bg-red-700 focus:ring-red-500'
                      : 'bg-yellow-600 hover:bg-yellow-700 focus:ring-yellow-500',
                    loading && 'opacity-50 cursor-not-allowed',
                  ]"
                  @click="handleConfirm"
                  :disabled="loading"
                >
                  <span v-if="loading" class="flex items-center gap-2">
                    <div
                      class="animate-spin h-4 w-4 border-2 border-white border-t-transparent rounded-full"
                    ></div>
                    <span>{{ loadingText }}</span>
                  </span>
                  <span v-else>{{ confirmText }}</span>
                </button>
              </div>
            </div>
          </div>
        </div>
      </div>
    </Transition>
  </Teleport>
</template>

<script setup lang="ts">
import { ExclamationTriangleIcon } from "@heroicons/vue/24/outline";

interface Props {
  show: boolean;
  title?: string;
  message?: string;
  confirmText?: string;
  cancelText?: string;
  loadingText?: string;
  loading?: boolean;
  variant?: "danger" | "warning";
  closeOnBackdrop?: boolean;
}

const props = withDefaults(defineProps<Props>(), {
  title: "Confirm Action",
  message: "Are you sure you want to proceed?",
  confirmText: "Confirm",
  cancelText: "Cancel",
  loadingText: "Processing...",
  loading: false,
  variant: "danger",
  closeOnBackdrop: true,
});

const emit = defineEmits<{
  confirm: [];
  cancel: [];
  close: [];
}>();

const handleConfirm = () => {
  if (!props.loading) {
    emit("confirm");
  }
};

const handleCancel = () => {
  if (!props.loading) {
    emit("cancel");
    emit("close");
  }
};

const handleBackdropClick = () => {
  if (props.closeOnBackdrop && !props.loading) {
    emit("cancel");
    emit("close");
  }
};
</script>

<style scoped>
.modal-enter-active,
.modal-leave-active {
  transition: opacity 0.2s ease;
}

.modal-enter-from,
.modal-leave-to {
  opacity: 0;
}

.modal-enter-active .relative,
.modal-leave-active .relative {
  transition: transform 0.2s ease;
}

.modal-enter-from .relative,
.modal-leave-to .relative {
  transform: scale(0.95);
}
</style>

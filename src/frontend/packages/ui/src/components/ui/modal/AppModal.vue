<script setup lang="ts">
import { computed, watch, onMounted, onUnmounted } from "vue";
import { XMarkIcon } from "@heroicons/vue/24/outline";
import { cn } from "../../../lib/utils";

interface Props {
  modelValue: boolean;
  title?: string;
  size?: "sm" | "md" | "lg" | "xl" | "full";
  closeOnOverlay?: boolean;
  closeOnEscape?: boolean;
  showClose?: boolean;
  class?: string;
}

const props = withDefaults(defineProps<Props>(), {
  size: "md",
  closeOnOverlay: true,
  closeOnEscape: true,
  showClose: true,
});

const emit = defineEmits<{
  "update:modelValue": [value: boolean];
  close: [];
}>();

const sizeClasses: Record<string, string> = {
  sm: "max-w-sm",
  md: "max-w-md",
  lg: "max-w-lg",
  xl: "max-w-xl",
  full: "max-w-full mx-4",
};

const modalClasses = computed(() =>
  cn(
    "relative flex max-h-[calc(100vh-2rem)] w-full flex-col overflow-hidden rounded-xl bg-white shadow-xl dark:bg-secondary-800",
    "transform transition-all duration-300",
    sizeClasses[props.size],
    props.class,
  ),
);

const close = () => {
  emit("update:modelValue", false);
  emit("close");
};

const handleOverlayClick = () => {
  if (props.closeOnOverlay) {
    close();
  }
};

const handleEscape = (event: KeyboardEvent) => {
  if (event.key === "Escape" && props.closeOnEscape && props.modelValue) {
    close();
  }
};

watch(
  () => props.modelValue,
  (isOpen) => {
    if (isOpen) {
      document.body.style.overflow = "hidden";
    } else {
      document.body.style.overflow = "";
    }
  },
);

onMounted(() => {
  document.addEventListener("keydown", handleEscape);
});

onUnmounted(() => {
  document.removeEventListener("keydown", handleEscape);
  document.body.style.overflow = "";
});
</script>

<template>
  <Teleport to="body">
    <Transition name="modal">
      <div
        v-if="modelValue"
        class="fixed inset-0 z-50 flex items-center justify-center p-4"
      >
        <!-- Overlay -->
        <div
          class="fixed inset-0 bg-black/50 transition-opacity"
          @click="handleOverlayClick"
        ></div>

        <!-- Modal -->
        <div :class="modalClasses">
          <!-- Header -->
          <div
            v-if="title || showClose"
            class="shrink-0 flex items-center justify-between border-b border-secondary-200 px-6 py-4 dark:border-secondary-700"
          >
            <h3
              v-if="title"
              class="text-lg font-semibold text-secondary-900 dark:text-secondary-100"
            >
              {{ title }}
            </h3>
            <button
              v-if="showClose"
              type="button"
              class="rounded-lg p-1 text-secondary-400 hover:bg-secondary-100 hover:text-secondary-600 dark:hover:bg-secondary-700 dark:hover:text-secondary-300"
              @click="close"
            >
              <XMarkIcon class="h-5 w-5" />
            </button>
          </div>

          <!-- Content -->
          <div class="min-h-0 flex-1 overflow-y-auto p-6">
            <slot></slot>
          </div>

          <!-- Footer -->
          <div
            v-if="$slots.footer"
            class="shrink-0 border-t border-secondary-200 px-6 py-4 dark:border-secondary-700"
          >
            <slot name="footer"></slot>
          </div>
        </div>
      </div>
    </Transition>
  </Teleport>
</template>

<style scoped>
.modal-enter-active,
.modal-leave-active {
  transition: opacity 0.2s ease;
}

.modal-enter-from,
.modal-leave-to {
  opacity: 0;
}

.modal-enter-active > div:last-child,
.modal-leave-active > div:last-child {
  transition: transform 0.2s ease;
}

.modal-enter-from > div:last-child,
.modal-leave-to > div:last-child {
  transform: scale(0.95);
}
</style>

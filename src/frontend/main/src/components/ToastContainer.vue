<script setup lang="ts">
import { useToast } from "@/composables/useToast";

const { toasts, remove } = useToast();

const typeClasses: Record<string, string> = {
  success: "bg-success text-white",
  error: "bg-red-500 text-white",
  warning: "bg-warning-bg text-warning-text",
  info: "bg-accent text-white",
};

const typeIcons: Record<string, string> = {
  success: "check_circle",
  error: "error",
  warning: "warning",
  info: "info",
};
</script>

<template>
  <div
    class="fixed left-4 right-4 top-4 z-50 flex max-w-sm flex-col gap-2 sm:left-auto"
  >
    <TransitionGroup name="toast">
      <div
        v-for="toast in toasts"
        :key="toast.id"
        class="px-4 py-3 rounded-md shadow-lg flex items-center gap-3 cursor-pointer"
        :class="typeClasses[toast.type]"
        @click="remove(toast.id)"
      >
        <span class="material-symbols-outlined text-[20px]">
          {{ typeIcons[toast.type] }}
        </span>
        <p class="text-sm font-semibold flex-1">{{ toast.message }}</p>
      </div>
    </TransitionGroup>
  </div>
</template>

<style scoped>
.toast-enter-active {
  transition: all 0.3s ease;
}
.toast-leave-active {
  transition: all 0.2s ease;
}
.toast-enter-from {
  opacity: 0;
  transform: translateX(40px);
}
.toast-leave-to {
  opacity: 0;
  transform: translateX(40px);
}
</style>

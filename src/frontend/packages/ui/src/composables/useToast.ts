import { ref, readonly } from "vue";
import type { Toast, ToastType } from "../components/composite/toast";

const toasts = ref<Toast[]>([]);

let toastId = 0;

function generateId(): string {
  return `toast-${++toastId}`;
}

function addToast(
  type: ToastType,
  message: string,
  title?: string,
  duration = 5000,
): string {
  const id = generateId();
  const toast: Toast = { id, type, message, title, duration };
  toasts.value.push(toast);

  if (duration > 0) {
    setTimeout(() => {
      removeToast(id);
    }, duration);
  }

  return id;
}

function removeToast(id: string): void {
  const index = toasts.value.findIndex((t) => t.id === id);
  if (index > -1) {
    toasts.value.splice(index, 1);
  }
}

function clearAll(): void {
  toasts.value = [];
}

export function useToast() {
  return {
    toasts: readonly(toasts),
    success: (message: string, title?: string, duration?: number) =>
      addToast("success", message, title, duration),
    error: (message: string, title?: string, duration?: number) =>
      addToast("error", message, title, duration),
    warning: (message: string, title?: string, duration?: number) =>
      addToast("warning", message, title, duration),
    info: (message: string, title?: string, duration?: number) =>
      addToast("info", message, title, duration),
    remove: removeToast,
    clear: clearAll,
  };
}

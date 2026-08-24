<script setup lang="ts">
import {
  computed,
  nextTick,
  onUnmounted,
  ref,
  useId,
  watch,
} from "vue";
import { XMarkIcon } from "@heroicons/vue/24/outline";
import { cn } from "../../../lib/utils";

interface Props {
  modelValue: boolean;
  title?: string;
  ariaLabel?: string;
  initialFocus?: string;
  size?: "sm" | "md" | "lg" | "xl" | "full";
  placement?: "center" | "mobile-sheet";
  closeOnOverlay?: boolean;
  closeOnEscape?: boolean;
  showClose?: boolean;
  class?: string;
}

const props = withDefaults(defineProps<Props>(), {
  ariaLabel: "Dialog",
  size: "md",
  placement: "center",
  closeOnOverlay: true,
  closeOnEscape: true,
  showClose: true,
});

const emit = defineEmits<{
  "update:modelValue": [value: boolean];
  close: [];
}>();

const modalToken = Symbol("nie-modal");
const titleId = `nie-modal-title-${useId()}`;
const dialogRef = ref<HTMLElement | null>(null);
const previouslyFocused = ref<HTMLElement | null>(null);
const resolvedAriaLabel = computed(() => props.ariaLabel || "Dialog");

const openModalStack: symbol[] = ((globalThis as typeof globalThis & {
  __nieOpenModalStack?: symbol[];
}).__nieOpenModalStack ??= []);
let registered = false;

const bodyLockState = (globalThis as typeof globalThis & {
  __nieBodyLockState?: { count: number; overflow: string };
});
bodyLockState.__nieBodyLockState ??= { count: 0, overflow: "" };

const sizeClasses: Record<NonNullable<Props["size"]>, string> = {
  sm: "max-w-sm",
  md: "max-w-md",
  lg: "max-w-lg",
  xl: "max-w-xl",
  full: "max-w-full mx-4",
};

const wrapperClasses = computed(() =>
  cn(
    "fixed inset-0 z-[200] flex p-4",
    props.placement === "mobile-sheet"
      ? "items-end justify-center sm:items-center"
      : "items-center justify-center",
  ),
);

const modalClasses = computed(() =>
  cn(
    "relative flex max-h-[calc(100dvh-2rem)] w-full flex-col overflow-hidden rounded-[var(--theme-radius-dialog)] bg-white shadow-[var(--theme-shadow-float)] outline-none dark:bg-secondary-800",
    "transform transition-all duration-300",
    props.placement === "mobile-sheet" &&
      "max-sm:-mb-4 max-sm:rounded-b-none",
    sizeClasses[props.size],
    props.class,
  ),
);

const focusableSelector = [
  "[autofocus]",
  "[data-autofocus]",
  "button:not([disabled])",
  "a[href]",
  "input:not([disabled]):not([type='hidden'])",
  "select:not([disabled])",
  "textarea:not([disabled])",
  "[tabindex]:not([tabindex='-1'])",
].join(",");

function focusableElements(): HTMLElement[] {
  if (!dialogRef.value) return [];
  return [...dialogRef.value.querySelectorAll<HTMLElement>(focusableSelector)]
    .filter((element) => {
      const style = window.getComputedStyle(element);
      return !element.hidden && style.display !== "none" && style.visibility !== "hidden";
    });
}

function isTopmost(): boolean {
  return openModalStack[openModalStack.length - 1] === modalToken;
}

async function focusInitialControl() {
  await nextTick();
  const requested = props.initialFocus
    ? dialogRef.value?.querySelector<HTMLElement>(props.initialFocus)
    : null;
  (requested ?? focusableElements()[0] ?? dialogRef.value)?.focus();
}

function registerModal() {
  if (registered || typeof document === "undefined") return;
  registered = true;
  previouslyFocused.value =
    document.activeElement instanceof HTMLElement ? document.activeElement : null;
  openModalStack.push(modalToken);
  const state = bodyLockState.__nieBodyLockState!;
  if (state.count === 0) {
    state.overflow = document.body.style.overflow;
    document.body.style.overflow = "hidden";
  }
  state.count += 1;
  void focusInitialControl();
}

function unregisterModal(restoreFocus = true) {
  if (!registered || typeof document === "undefined") return;
  registered = false;
  const index = openModalStack.lastIndexOf(modalToken);
  if (index >= 0) openModalStack.splice(index, 1);
  const state = bodyLockState.__nieBodyLockState!;
  state.count = Math.max(0, state.count - 1);
  if (state.count === 0) {
    document.body.style.overflow = state.overflow;
  }
  const focusTarget = previouslyFocused.value;
  previouslyFocused.value = null;
  if (restoreFocus && focusTarget?.isConnected) {
    void nextTick(() => focusTarget.focus());
  }
}

const close = () => {
  emit("update:modelValue", false);
  emit("close");
};

const handleOverlayClick = () => {
  if (props.closeOnOverlay && isTopmost()) close();
};

const handleEscape = (event: KeyboardEvent) => {
  if (
    event.key === "Escape" &&
    props.closeOnEscape &&
    props.modelValue &&
    isTopmost()
  ) {
    event.preventDefault();
    close();
  }
};

const containFocus = (event: KeyboardEvent) => {
  if (!isTopmost()) return;
  const controls = focusableElements();
  if (controls.length === 0) {
    event.preventDefault();
    dialogRef.value?.focus();
    return;
  }
  const first = controls[0]!;
  const last = controls[controls.length - 1]!;
  const active = document.activeElement;
  if (event.shiftKey && (active === first || !dialogRef.value?.contains(active))) {
    event.preventDefault();
    last.focus();
  } else if (!event.shiftKey && (active === last || !dialogRef.value?.contains(active))) {
    event.preventDefault();
    first.focus();
  }
};

watch(
  () => props.modelValue,
  (isOpen) => (isOpen ? registerModal() : unregisterModal()),
  { immediate: true },
);

if (typeof document !== "undefined") {
  document.addEventListener("keydown", handleEscape);
}

onUnmounted(() => {
  if (typeof document !== "undefined") {
    document.removeEventListener("keydown", handleEscape);
  }
  unregisterModal(false);
});
</script>

<template>
  <Teleport to="body">
    <Transition name="modal">
      <div v-if="modelValue" :class="wrapperClasses">
        <div
          aria-hidden="true"
          class="fixed inset-0 bg-black/50 transition-opacity"
          @click="handleOverlayClick"
        ></div>

        <div
          ref="dialogRef"
          :class="modalClasses"
          role="dialog"
          aria-modal="true"
          :aria-labelledby="title ? titleId : undefined"
          :aria-label="!title ? resolvedAriaLabel : undefined"
          tabindex="-1"
          @keydown.tab="containFocus"
        >
          <div
            v-if="title || showClose"
            class="flex shrink-0 items-center justify-between border-b border-secondary-200 px-6 py-4 dark:border-secondary-700"
          >
            <h3
              v-if="title"
              :id="titleId"
              class="text-lg font-semibold text-secondary-900 dark:text-secondary-100"
            >
              {{ title }}
            </h3>
            <button
              v-if="showClose"
              type="button"
              aria-label="Close dialog"
              class="ml-auto inline-flex size-11 items-center justify-center rounded-lg text-secondary-500 transition-colors hover:bg-secondary-100 hover:text-secondary-700 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary-500 focus-visible:ring-offset-2 dark:text-secondary-300 dark:hover:bg-secondary-700 dark:hover:text-secondary-100"
              @click="close"
            >
              <XMarkIcon class="h-5 w-5" aria-hidden="true" />
            </button>
          </div>

          <div class="min-h-0 flex-1 overflow-y-auto p-6">
            <slot></slot>
          </div>

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

@media (prefers-reduced-motion: reduce) {
  .modal-enter-active,
  .modal-leave-active,
  .modal-enter-active > div:last-child,
  .modal-leave-active > div:last-child {
    transition-duration: 0.01ms;
  }
}
</style>

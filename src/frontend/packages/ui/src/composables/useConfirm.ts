import { ref, readonly } from "vue";
import type { ConfirmOptions } from "../components/composite/confirm";

interface ConfirmState {
  options: ConfirmOptions | null;
  resolve: ((value: boolean) => void) | null;
}

const state = ref<ConfirmState>({
  options: null,
  resolve: null,
});

export function useConfirm() {
  function confirm(options: ConfirmOptions | string): Promise<boolean> {
    return new Promise((resolve) => {
      state.value = {
        options: typeof options === "string" ? { message: options } : options,
        resolve,
      };
    });
  }

  function handleConfirm(): void {
    state.value.resolve?.(true);
    state.value = { options: null, resolve: null };
  }

  function handleCancel(): void {
    state.value.resolve?.(false);
    state.value = { options: null, resolve: null };
  }

  return {
    state: readonly(state),
    confirm,
    handleConfirm,
    handleCancel,
  };
}

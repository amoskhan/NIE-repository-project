<script setup lang="ts">
import { computed } from "vue";
import { AppButton } from "../../ui/button";
import { AppModal } from "../../ui/modal";

export interface ConfirmOptions {
  title?: string;
  message: string;
  confirmText?: string;
  cancelText?: string;
  variant?: "primary" | "danger";
}

interface Props {
  options: ConfirmOptions | null;
}

const props = defineProps<Props>();

const emit = defineEmits<{
  confirm: [];
  cancel: [];
}>();

const isOpen = computed(() => !!props.options);

const handleConfirm = () => {
  emit("confirm");
};

const handleCancel = () => {
  emit("cancel");
};

const handleModalValueChange = (value: boolean) => {
  if (!value) {
    handleCancel();
  }
};
</script>

<template>
  <AppModal
    :model-value="isOpen"
    :title="options?.title || 'Confirm'"
    size="sm"
    @update:model-value="handleModalValueChange"
  >
    <p class="text-secondary-600 dark:text-secondary-400">
      {{ options?.message }}
    </p>

    <template #footer>
      <div class="flex justify-end gap-3">
        <AppButton variant="ghost" @click="handleCancel">
          {{ options?.cancelText || "Cancel" }}
        </AppButton>
        <AppButton
          :variant="options?.variant || 'primary'"
          @click="handleConfirm"
        >
          {{ options?.confirmText || "Confirm" }}
        </AppButton>
      </div>
    </template>
  </AppModal>
</template>

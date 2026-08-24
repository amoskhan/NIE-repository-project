<script setup lang="ts">
import { computed } from "vue";
import { NieButton } from "../../ui/button";
import { NieModal } from "../../ui/modal";

export interface ConfirmOptions {
  title?: string;
  message: string;
  confirmText?: string;
  cancelText?: string;
  variant?: "primary" | "danger";
}

interface Props {
  options: ConfirmOptions | null;
  loading?: boolean;
}

const props = withDefaults(defineProps<Props>(), {
  loading: false,
});

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
  <NieModal
    :model-value="isOpen"
    :title="options?.title || 'Confirm'"
    size="sm"
    :close-on-overlay="!loading"
    :close-on-escape="!loading"
    :show-close="!loading"
    @update:model-value="handleModalValueChange"
  >
    <p class="text-secondary-600 dark:text-secondary-400">
      {{ options?.message }}
    </p>

    <template #footer>
      <div class="flex justify-end gap-3">
        <NieButton variant="ghost" :disabled="loading" @click="handleCancel">
          {{ options?.cancelText || 'Cancel' }}
        </NieButton>
        <NieButton
          :variant="options?.variant || 'primary'"
          :loading="loading"
          @click="handleConfirm"
        >
          {{ options?.confirmText || 'Confirm' }}
        </NieButton>
      </div>
    </template>
  </NieModal>
</template>

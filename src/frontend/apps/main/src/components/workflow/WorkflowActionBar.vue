<script setup lang="ts">
/**
 * WorkflowActionBar — action buttons for available state transitions.
 * Shows Submit, Approve, Reject, etc. based on current state and user role.
 */
import { ref } from "vue";
import { NieButton, NieLoaderSymbol, NieModal, NieTextarea } from "@nie/ui";
import type { WorkflowTransition, TransitionRequest } from "@/types/workflow";

defineProps<{
  transitions: WorkflowTransition[];
  loading?: boolean;
  ownerType: string;
  ownerId: string;
}>();

const emit = defineEmits<{
  (e: "transition", request: TransitionRequest): void;
}>();

const selectedTransition = ref<WorkflowTransition | null>(null);
const remarks = ref("");
const showRemarksDialog = ref(false);

const openDialog = (transition: WorkflowTransition) => {
  selectedTransition.value = transition;
  remarks.value = "";
  showRemarksDialog.value = true;
};

const submit = () => {
  if (!selectedTransition.value) return;
  emit("transition", {
    toState: selectedTransition.value.toState,
    remarks: remarks.value || undefined,
  });
  showRemarksDialog.value = false;
};

const cancel = () => {
  showRemarksDialog.value = false;
  selectedTransition.value = null;
  remarks.value = "";
};
</script>

<template>
  <div class="action-bar">
    <span class="action-label">Actions:</span>
    <div v-if="loading" class="inline-flex items-center gap-2 text-sm text-secondary-500 dark:text-secondary-300">
      <NieLoaderSymbol size="xs" label="Loading workflow actions" />
      <span>Loading...</span>
    </div>
    <div v-else-if="transitions.length === 0" class="text-sm text-secondary-400">
      No actions available
    </div>
    <NieButton
      v-for="t in transitions"
      :key="t.id"
      size="sm"
      variant="outline"
      @click="openDialog(t)"
    >
      {{ t.displayLabel || `Move to ${t.toState}` }}
    </NieButton>
  </div>

  <NieModal
    v-model="showRemarksDialog"
    size="md"
    placement="mobile-sheet"
    :title="selectedTransition?.displayLabel || 'Confirm Action'"
    @close="cancel"
  >
        <p class="dialog-desc">
          Transition from
          <strong>{{ selectedTransition?.fromState }}</strong> to
          <strong>{{ selectedTransition?.toState }}</strong>
        </p>
        <NieTextarea
          v-if="selectedTransition?.requiresRemarks"
          v-model="remarks"
          label="Remarks"
          :rows="3"
          placeholder="Enter remarks..."
        />
    <template #footer>
      <div class="flex justify-end gap-3">
        <NieButton variant="outline" @click="cancel">Cancel</NieButton>
        <NieButton @click="submit">Confirm</NieButton>
      </div>
    </template>
  </NieModal>
</template>

<style scoped>
.action-bar {
  display: flex;
  align-items: center;
  gap: var(--theme-space-2);
  padding: var(--theme-space-3) var(--theme-space-4);
  background: var(--theme-color-surface-subtle);
  border-radius: var(--theme-radius-control);
  margin: var(--theme-space-2) 0;
}
.action-label {
  font-size: var(--theme-font-size-label);
  color: var(--theme-color-text-muted);
  font-weight: var(--theme-font-weight-semibold);
}
.dialog-desc {
  font-size: var(--theme-font-size-label);
  color: var(--theme-color-text-muted);
  margin-bottom: var(--theme-space-4);
}
</style>

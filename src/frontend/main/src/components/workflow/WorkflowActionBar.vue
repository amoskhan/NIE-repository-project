<script setup lang="ts">
/**
 * WorkflowActionBar — action buttons for available state transitions.
 * Shows Submit, Approve, Reject, etc. based on current state and user role.
 */
import { ref, computed } from "vue";
import type { WorkflowTransition, TransitionRequest } from "@/types/workflow";

const props = defineProps<{
  transitions: WorkflowTransition[];
  loading?: boolean;
  ownerType: string;
  ownerId: number;
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
    <div v-if="loading" class="text-sm text-gray-400">Loading...</div>
    <div v-else-if="transitions.length === 0" class="text-sm text-gray-400">
      No actions available
    </div>
    <button
      v-for="t in transitions"
      :key="t.id"
      class="action-btn"
      @click="openDialog(t)"
    >
      {{ t.displayLabel || `Move to ${t.toState}` }}
    </button>
  </div>

  <!-- Remarks Dialog -->
  <Teleport to="body">
    <div v-if="showRemarksDialog" class="dialog-overlay" @click.self="cancel">
      <div class="dialog-box">
        <h3>{{ selectedTransition?.displayLabel || "Confirm Action" }}</h3>
        <p class="dialog-desc">
          Transition from
          <strong>{{ selectedTransition?.fromState }}</strong> to
          <strong>{{ selectedTransition?.toState }}</strong>
        </p>
        <div v-if="selectedTransition?.requiresRemarks" class="form-group">
          <label>Remarks</label>
          <textarea v-model="remarks" rows="3" placeholder="Enter remarks..." />
        </div>
        <div class="dialog-actions">
          <button class="btn-cancel" @click="cancel">Cancel</button>
          <button class="btn-submit" @click="submit">Confirm</button>
        </div>
      </div>
    </div>
  </Teleport>
</template>

<style scoped>
.action-bar {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 12px 16px;
  background: #f9fafb;
  border-radius: 8px;
  margin: 8px 0;
}
.action-label {
  font-size: 13px;
  color: #6b7280;
  font-weight: 600;
}
.action-btn {
  padding: 6px 14px;
  border-radius: 6px;
  border: 1px solid #d1d5db;
  background: white;
  font-size: 13px;
  cursor: pointer;
  transition: all 0.15s;
}
.action-btn:hover {
  background: #f3f4f6;
  border-color: #9ca3af;
}
.dialog-overlay {
  position: fixed;
  inset: 0;
  background: rgba(0, 0, 0, 0.3);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 1000;
}
.dialog-box {
  background: white;
  border-radius: 12px;
  padding: 24px;
  width: 420px;
  max-width: 90vw;
  box-shadow: 0 20px 60px rgba(0, 0, 0, 0.15);
}
.dialog-box h3 {
  margin: 0 0 8px;
  font-size: 16px;
}
.dialog-desc {
  font-size: 13px;
  color: #6b7280;
  margin-bottom: 16px;
}
.form-group {
  margin-bottom: 16px;
}
.form-group label {
  display: block;
  font-size: 13px;
  font-weight: 600;
  margin-bottom: 4px;
}
.form-group textarea {
  width: 100%;
  border: 1px solid #d1d5db;
  border-radius: 6px;
  padding: 8px;
  font-size: 13px;
  resize: vertical;
}
.dialog-actions {
  display: flex;
  justify-content: flex-end;
  gap: 8px;
}
.btn-cancel {
  padding: 8px 16px;
  border-radius: 6px;
  border: 1px solid #d1d5db;
  background: white;
  cursor: pointer;
  font-size: 13px;
}
.btn-submit {
  padding: 8px 16px;
  border-radius: 6px;
  border: none;
  background: #3b82f6;
  color: white;
  cursor: pointer;
  font-size: 13px;
  font-weight: 600;
}
</style>

<script setup lang="ts">
/**
 * WorkflowTimeline — vertical timeline showing state transition history.
 * Used inside PurchaseOrderDetail and any other entity detail page.
 */
import { computed } from "vue";
import type { WorkflowStateLog } from "@/types/workflow";
import { EWorkflowState } from "@/types/workflow";

const props = defineProps<{
  history: WorkflowStateLog[];
  loading?: boolean;
}>();

const stateConfig: Record<
  string,
  { icon: string; color: string; bgColor: string }
> = {
  [EWorkflowState.Draft]: { icon: "📝", color: "#6b7280", bgColor: "#f3f4f6" },
  [EWorkflowState.Submitted]: {
    icon: "📤",
    color: "#3b82f6",
    bgColor: "#eff6ff",
  },
  [EWorkflowState.UnderReview]: {
    icon: "🔍",
    color: "#f59e0b",
    bgColor: "#fffbeb",
  },
  [EWorkflowState.Approved]: {
    icon: "✅",
    color: "#10b981",
    bgColor: "#ecfdf5",
  },
  [EWorkflowState.Rejected]: {
    icon: "❌",
    color: "#ef4444",
    bgColor: "#fef2f2",
  },
  [EWorkflowState.Completed]: {
    icon: "🏁",
    color: "#8b5cf6",
    bgColor: "#f5f3ff",
  },
  [EWorkflowState.Cancelled]: {
    icon: "🚫",
    color: "#9ca3af",
    bgColor: "#f9fafb",
  },
  [EWorkflowState.ReturnedForRevision]: {
    icon: "↩️",
    color: "#f97316",
    bgColor: "#fff7ed",
  },
};

const formatDate = (dateStr: string) => {
  const d = new Date(dateStr);
  return d.toLocaleString("en-SG", {
    day: "2-digit",
    month: "short",
    year: "numeric",
    hour: "2-digit",
    minute: "2-digit",
  });
};

const reversedHistory = computed(() => [...props.history].reverse());
</script>

<template>
  <div class="workflow-timeline">
    <div v-if="loading" class="text-gray-400 text-sm py-4">
      Loading history...
    </div>
    <div v-else-if="history.length === 0" class="text-gray-400 text-sm py-4">
      No workflow history yet.
    </div>
    <div v-else class="timeline-list">
      <div
        v-for="(log, index) in reversedHistory"
        :key="log.id"
        class="timeline-item"
        :class="{ first: index === 0 }"
      >
        <!-- Line -->
        <div class="timeline-line">
          <div
            class="line-dot"
            :style="{
              background: stateConfig[log.toState]?.color || '#6b7280',
            }"
          />
          <div v-if="index < history.length - 1" class="line-connector" />
        </div>
        <!-- Content -->
        <div
          class="timeline-content"
          :style="{ borderColor: stateConfig[log.toState]?.color || '#6b7280' }"
        >
          <div class="timeline-header">
            <span
              class="state-badge"
              :style="{
                background: stateConfig[log.toState]?.bgColor || '#f3f4f6',
                color: stateConfig[log.toState]?.color || '#6b7280',
              }"
            >
              {{ stateConfig[log.toState]?.icon }} {{ log.toState }}
            </span>
            <span class="timeline-date">{{
              formatDate(log.transitionedAt)
            }}</span>
          </div>
          <div v-if="log.remarks" class="timeline-remarks">
            {{ log.remarks }}
          </div>
          <div class="timeline-meta">
            <span v-if="log.performedByName">by {{ log.performedByName }}</span>
            <span v-if="log.performedByRole" class="role-tag">{{
              log.performedByRole
            }}</span>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.timeline-list {
  position: relative;
  padding-left: 8px;
}
.timeline-item {
  display: flex;
  gap: 12px;
  padding-bottom: 4px;
}
.timeline-item.first .timeline-content {
  border-width: 2px;
}
.timeline-line {
  display: flex;
  flex-direction: column;
  align-items: center;
  width: 24px;
  flex-shrink: 0;
}
.line-dot {
  width: 12px;
  height: 12px;
  border-radius: 50%;
  margin-top: 6px;
}
.line-connector {
  width: 2px;
  flex: 1;
  background: #e5e7eb;
  margin: 4px 0;
}
.timeline-content {
  flex: 1;
  border: 1px solid #e5e7eb;
  border-radius: 8px;
  padding: 10px 14px;
  margin-bottom: 8px;
}
.timeline-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 4px;
}
.state-badge {
  font-size: 12px;
  padding: 2px 8px;
  border-radius: 12px;
  font-weight: 600;
}
.timeline-date {
  font-size: 11px;
  color: #9ca3af;
}
.timeline-remarks {
  font-size: 13px;
  color: #374151;
  margin-top: 4px;
  font-style: italic;
}
.timeline-meta {
  font-size: 11px;
  color: #6b7280;
  margin-top: 4px;
}
.role-tag {
  margin-left: 8px;
  padding: 1px 6px;
  background: #f3f4f6;
  border-radius: 4px;
  font-size: 10px;
}
</style>

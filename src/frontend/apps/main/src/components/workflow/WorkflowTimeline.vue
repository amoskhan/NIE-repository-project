<script setup lang="ts">
/**
 * WorkflowTimeline — vertical timeline showing state transition history.
 * Used inside PurchaseOrderDetail and any other entity detail page.
 */
import { computed } from "vue";
import { NieLoaderSymbol } from "@nie/ui";
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
  [EWorkflowState.Draft]: { icon: "📝", color: "var(--theme-color-text-muted)", bgColor: "var(--theme-color-surface-subtle)" },
  [EWorkflowState.Submitted]: {
    icon: "📤",
    color: "var(--theme-color-info-solid)",
    bgColor: "var(--theme-color-info-surface)",
  },
  [EWorkflowState.UnderReview]: {
    icon: "🔍",
    color: "var(--theme-color-warning-solid)",
    bgColor: "var(--theme-color-warning-surface)",
  },
  [EWorkflowState.Approved]: {
    icon: "✅",
    color: "var(--theme-color-success-solid)",
    bgColor: "var(--theme-color-success-50)",
  },
  [EWorkflowState.Rejected]: {
    icon: "❌",
    color: "var(--theme-color-danger-500)",
    bgColor: "var(--theme-color-danger-surface)",
  },
  [EWorkflowState.Completed]: {
    icon: "🏁",
    color: "var(--theme-color-brand-500)",
    bgColor: "var(--theme-color-brand-50)",
  },
  [EWorkflowState.Cancelled]: {
    icon: "🚫",
    color: "var(--theme-color-neutral-400)",
    bgColor: "var(--theme-color-surface-subtle)",
  },
  [EWorkflowState.ReturnedForRevision]: {
    icon: "↩️",
    color: "var(--theme-color-warning-600)",
    bgColor: "var(--theme-color-warning-surface)",
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
    <div
      v-if="loading"
      class="flex items-center gap-2 py-4 text-sm text-secondary-400"
    >
      <NieLoaderSymbol size="sm" label="Loading workflow history" />
      <span>Loading history...</span>
    </div>
    <div v-else-if="history.length === 0" class="text-secondary-400 text-sm py-4">
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
              background: stateConfig[log.toState]?.color || 'var(--theme-color-text-muted)',
            }"
          />
          <div v-if="index < history.length - 1" class="line-connector" />
        </div>
        <!-- Content -->
        <div
          class="timeline-content"
          :style="{ borderColor: stateConfig[log.toState]?.color || 'var(--theme-color-text-muted)' }"
        >
          <div class="timeline-header">
            <span
              class="state-badge"
              :style="{
                background: stateConfig[log.toState]?.bgColor || 'var(--theme-color-surface-subtle)',
                color: stateConfig[log.toState]?.color || 'var(--theme-color-text-muted)',
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
  padding-left: var(--theme-space-2);
}
.timeline-item {
  display: flex;
  gap: var(--theme-space-3);
  padding-bottom: var(--theme-space-1);
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
  border-radius: var(--theme-radius-circle);
  margin-top: var(--theme-space-1-5);
}
.line-connector {
  width: 2px;
  flex: 1;
  background: var(--theme-color-border-default);
  margin: var(--theme-space-1) 0;
}
.timeline-content {
  flex: 1;
  border: 1px solid var(--theme-color-border-default);
  border-radius: var(--theme-radius-control);
  padding: var(--theme-space-2) var(--theme-space-3);
  margin-bottom: var(--theme-space-2);
}
.timeline-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: var(--theme-space-1);
}
.state-badge {
  font-size: var(--theme-font-size-caption);
  padding: var(--theme-space-1) var(--theme-space-2);
  border-radius: var(--theme-radius-control);
  font-weight: var(--theme-font-weight-semibold);
}
.timeline-date {
  font-size: var(--theme-font-size-caption);
  color: var(--theme-color-neutral-400);
}
.timeline-remarks {
  font-size: var(--theme-font-size-label);
  color: var(--theme-color-text-soft);
  margin-top: var(--theme-space-1);
  font-style: italic;
}
.timeline-meta {
  font-size: var(--theme-font-size-caption);
  color: var(--theme-color-text-muted);
  margin-top: var(--theme-space-1);
}
.role-tag {
  margin-left: var(--theme-space-2);
  padding: var(--theme-space-1) var(--theme-space-1);
  background: var(--theme-color-surface-subtle);
  border-radius: var(--theme-radius-control);
  font-size: var(--theme-font-size-caption);
}
</style>

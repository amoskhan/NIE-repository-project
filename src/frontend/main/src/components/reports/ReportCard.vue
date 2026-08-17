<script setup lang="ts">
import type { ReportTypeDefinition } from "@/services/reportService";

defineProps<{
  report: ReportTypeDefinition;
}>();

const emit = defineEmits<{
  select: [report: ReportTypeDefinition];
}>();
</script>

<template>
  <button type="button" class="report-card" @click="emit('select', report)">
    <span
      class="material-symbols-outlined report-card__icon"
      aria-hidden="true"
    >
      {{ report.icon }}
    </span>
    <span class="report-card__name">{{ report.name }}</span>
    <span class="report-card__description">
      {{ report.description }}
    </span>
  </button>
</template>

<style scoped>
.report-card {
  --rpt-primary: var(--color-primary, #4f46e5);
  --rpt-border: color-mix(
    in srgb,
    var(--rpt-primary) 9%,
    var(--color-border, #e2e8f0) 91%
  );
  --rpt-panel: var(--color-surface, #ffffff);
  --rpt-active: var(--color-sidebar-active, #eef0fe);
  --rpt-bg: var(--color-bg-light, #f7f6fb);
  --rpt-text: var(--color-text, #0f172a);
  --rpt-muted: var(--color-text-muted, #64748b);

  min-height: 160px;
  display: grid;
  grid-template-rows: auto auto 1fr;
  gap: 12px;
  padding: 20px;
  border: 1px solid color-mix(in srgb, var(--rpt-border) 70%, transparent);
  border-radius: 14px;
  background:
    radial-gradient(
      circle at 0% 0%,
      color-mix(in srgb, var(--rpt-primary) 4%, transparent) 0,
      transparent 9rem
    ),
    color-mix(in srgb, var(--rpt-panel) 96%, transparent);
  color: inherit;
  text-align: left;
  cursor: pointer;
  box-shadow:
    inset 0 1px 0 rgba(255, 255, 255, 0.85),
    0 16px 36px -32px rgba(15, 23, 42, 0.28);
  transition:
    border-color 0.16s ease,
    box-shadow 0.16s ease,
    transform 0.16s ease;
}

.report-card:hover {
  border-color: color-mix(
    in srgb,
    var(--rpt-primary) 32%,
    var(--rpt-border) 68%
  );
  transform: translateY(-2px);
  box-shadow:
    inset 0 1px 0 rgba(255, 255, 255, 0.85),
    0 22px 48px -28px color-mix(in srgb, var(--rpt-primary) 32%, transparent);
}

.report-card:focus-visible {
  outline: 2px solid var(--rpt-primary);
  outline-offset: 2px;
}

.report-card__icon {
  width: 44px;
  height: 44px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  border-radius: 999px;
  background:
    radial-gradient(
      circle at 34% 24%,
      rgba(255, 255, 255, 0.95) 0,
      transparent 2rem
    ),
    color-mix(in srgb, var(--rpt-active) 82%, var(--rpt-panel) 18%);
  color: var(--rpt-primary);
  font-size: 24px;
  box-shadow: inset 0 1px 0 rgba(255, 255, 255, 0.8);
}

.report-card__name {
  color: var(--rpt-text);
  font-size: 15px;
  font-weight: 700;
  letter-spacing: 0;
  line-height: 1.32;
}

.report-card__description {
  color: var(--rpt-muted);
  font-size: 13px;
  line-height: 1.5;
}
</style>

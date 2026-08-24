<script setup lang="ts">
import type { ReportTypeDefinition } from "@/services/reports/reportService";

defineProps<{
  report: ReportTypeDefinition;
}>();

const emit = defineEmits<{
  select: [report: ReportTypeDefinition];
}>();
</script>

<template>
  <button
    type="button"
    class="report-card"
    @click="emit('select', report)"
  >
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
  --rpt-primary: var(--color-primary, var(--theme-color-brand-600));
  --rpt-border: color-mix(
    in srgb,
    var(--rpt-primary) 9%,
    var(--color-border, var(--theme-color-border-default)) 91%
  );
  --rpt-panel: var(--color-surface, var(--theme-color-static-white));
  --rpt-active: var(--color-sidebar-active, var(--theme-color-brand-50));
  --rpt-bg: var(--color-bg-light, var(--theme-color-surface-canvas));
  --rpt-text: var(--color-text, var(--theme-color-text-strong));
  --rpt-muted: var(--color-text-muted, var(--theme-color-text-muted));

  min-height: 160px;
  display: grid;
  grid-template-rows: auto auto 1fr;
  gap: var(--theme-space-3);
  padding: var(--theme-space-5);
  border: 1px solid color-mix(in srgb, var(--rpt-border) 70%, transparent);
  border-radius: var(--theme-radius-panel);
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
  box-shadow: var(--theme-shadow-inset), var(--theme-shadow-soft);
  transition:
    border-color 0.16s ease,
    box-shadow 0.16s ease,
    transform 0.16s ease;
}

.report-card:hover {
  border-color: color-mix(in srgb, var(--rpt-primary) 32%, var(--rpt-border) 68%);
  transform: translateY(-2px);
  box-shadow: var(--theme-shadow-inset), var(--theme-shadow-card);
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
  border-radius: var(--theme-radius-pill);
  background:
    radial-gradient(
      circle at 34% 24%,
      color-mix(in srgb, var(--theme-color-static-white) 95%, transparent) 0,
      transparent 2rem
    ),
    color-mix(in srgb, var(--rpt-active) 82%, var(--rpt-panel) 18%);
  color: var(--rpt-primary);
  font-size: var(--theme-font-size-page-title);
  box-shadow: var(--theme-shadow-inset);
}

.report-card__name {
  color: var(--rpt-text);
  font-size: var(--theme-font-size-body);
  font-weight: var(--theme-font-weight-bold);
  letter-spacing: 0;
  line-height: 1.32;
}

.report-card__description {
  color: var(--rpt-muted);
  font-size: var(--theme-font-size-label);
  line-height: 1.5;
}
</style>

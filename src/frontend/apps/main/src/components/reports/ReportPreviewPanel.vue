<script setup lang="ts">
import { useTemplateRef } from "vue";
import { NieLoaderSymbol } from "@nie/ui";

defineProps<{
  loading: boolean;
  /** Rendered report HTML returned by /api/Report/preview. */
  html: string | null;
  emptyText: string;
  /** Whether the PDF actions (print / download) should be enabled. */
  hasPdf?: boolean;
}>();

const emit = defineEmits<{
  refresh: [];
  download: [];
}>();

const previewFrame = useTemplateRef<HTMLIFrameElement>("previewFrame");

function print() {
  previewFrame.value?.contentWindow?.focus();
  previewFrame.value?.contentWindow?.print();
}
</script>

<template>
  <section class="report-preview-panel" aria-label="Report preview">
    <div class="report-preview-panel__actions" role="toolbar" aria-label="Report actions">
      <button
        type="button"
        class="report-preview-panel__icon-btn"
        aria-label="Refresh preview"
        :disabled="loading"
        @click="emit('refresh')"
      >
        <span class="material-symbols-outlined" aria-hidden="true">refresh</span>
      </button>
      <button
        type="button"
        class="report-preview-panel__icon-btn"
        aria-label="Print report"
        :disabled="!hasPdf"
        @click="print()"
      >
        <span class="material-symbols-outlined" aria-hidden="true">print</span>
      </button>
      <button
        type="button"
        class="report-preview-panel__download"
        aria-label="Download report"
        :disabled="loading || !hasPdf"
        @click="emit('download')"
      >
        <span class="material-symbols-outlined" aria-hidden="true">download</span>
        <span class="report-preview-panel__download-label">Download</span>
      </button>
    </div>

    <div v-if="loading" class="report-preview-panel__loading">
      <NieLoaderSymbol size="lg" variant="brand" label="Loading report preview" />
    </div>

    <iframe
      v-if="html"
      ref="previewFrame"
      class="report-preview-panel__frame"
      title="Report preview"
      sandbox="allow-same-origin allow-modals"
      :srcdoc="html"
    ></iframe>

    <div v-else class="report-preview-panel__empty">
      {{ emptyText }}
    </div>
  </section>
</template>

<style scoped>
.report-preview-panel {
  --rpt-primary: var(--color-primary, var(--theme-color-brand-600));
  --rpt-border: color-mix(
    in srgb,
    var(--rpt-primary) 9%,
    var(--color-border, var(--theme-color-border-default)) 91%
  );
  --rpt-panel: var(--color-surface, var(--theme-color-static-white));
  --rpt-bg: var(--color-bg-light, var(--theme-color-surface-canvas));
  --rpt-muted: var(--color-text-muted, var(--theme-color-text-muted));
  --rpt-text: var(--color-text, var(--theme-color-text-strong));

  position: relative;
  min-height: 720px;
  flex: 1;
  overflow: hidden;
  border: 1px solid color-mix(in srgb, var(--rpt-border) 70%, transparent);
  border-radius: var(--theme-radius-panel);
  background: color-mix(in srgb, var(--rpt-bg) 70%, var(--rpt-panel) 30%);
  box-shadow: var(--theme-shadow-inset), var(--theme-shadow-soft);
}

.report-preview-panel__actions {
  position: absolute;
  top: 14px;
  right: 14px;
  z-index: 3;
  display: inline-flex;
  align-items: center;
  gap: var(--theme-space-1);
  padding: var(--theme-space-1);
  border: 1px solid color-mix(in srgb, var(--rpt-border) 60%, transparent);
  border-radius: var(--theme-radius-pill);
  background: color-mix(in srgb, var(--rpt-panel) 88%, transparent);
  backdrop-filter: blur(12px);
  box-shadow: var(--theme-shadow-soft);
}

.report-preview-panel__icon-btn {
  width: 36px;
  height: 36px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  border: 0;
  border-radius: var(--theme-radius-pill);
  background: transparent;
  color: var(--rpt-muted);
  cursor: pointer;
  transition: background-color 0.16s ease, color 0.16s ease, transform 0.16s ease;
}

.report-preview-panel__icon-btn:hover:not(:disabled) {
  background: color-mix(in srgb, var(--rpt-primary) 8%, transparent);
  color: var(--rpt-primary);
}

.report-preview-panel__icon-btn:disabled,
.report-preview-panel__download:disabled {
  opacity: 0.45;
  cursor: not-allowed;
}

.report-preview-panel__icon-btn .material-symbols-outlined {
  font-size: var(--theme-font-size-section-title);
}

.report-preview-panel__download {
  display: inline-flex;
  align-items: center;
  gap: var(--theme-space-1);
  padding: 0 var(--theme-space-3);
  height: 36px;
  border: 0;
  border-radius: var(--theme-radius-pill);
  background: var(--rpt-primary);
  color: var(--theme-color-on-brand);
  font-size: var(--theme-font-size-label);
  font-weight: var(--theme-font-weight-bold);
  cursor: pointer;
  box-shadow: var(--theme-shadow-card);
  transition: transform 0.16s ease, box-shadow 0.16s ease;
}

.report-preview-panel__download:hover:not(:disabled) {
  transform: translateY(-1px);
  box-shadow: var(--theme-shadow-float);
}

.report-preview-panel__download .material-symbols-outlined {
  font-size: var(--theme-font-size-card-title);
}

.report-preview-panel__loading {
  position: absolute;
  inset: 0;
  z-index: 2;
  display: flex;
  flex-direction: column;
  gap: var(--theme-space-3);
  align-items: center;
  justify-content: center;
  background: color-mix(in srgb, var(--rpt-panel) 70%, transparent);
  backdrop-filter: blur(4px);
}


.report-preview-panel__frame {
  width: 100%;
  height: 100%;
  min-height: 720px;
  border: 0;
  background-color: var(--theme-color-surface-panel);
}

.report-preview-panel__empty {
  display: flex;
  min-height: 300px;
  align-items: center;
  justify-content: center;
  color: var(--rpt-muted);
  font-size: var(--theme-font-size-body);
}

@media (max-width: 760px) {
  .report-preview-panel {
    min-height: calc(100dvh - 9.25rem);
    border-radius: var(--theme-radius-control);
  }

  .report-preview-panel,
  .report-preview-panel__frame {
    min-height: calc(100dvh - 9.25rem);
  }

  .report-preview-panel__actions {
    top: 10px;
    right: 10px;
    gap: var(--theme-space-1);
    padding: var(--theme-space-1);
  }

  .report-preview-panel__icon-btn,
  .report-preview-panel__download {
    width: 34px;
    height: 34px;
  }

  .report-preview-panel__download-label {
    display: none;
  }

  .report-preview-panel__download {
    justify-content: center;
    padding: 0;
  }
}

</style>

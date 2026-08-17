<script setup lang="ts">
import { useTemplateRef } from "vue";

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
    <div
      class="report-preview-panel__actions"
      role="toolbar"
      aria-label="Report actions"
    >
      <button
        type="button"
        class="report-preview-panel__icon-btn"
        aria-label="Refresh preview"
        :disabled="loading"
        @click="emit('refresh')"
      >
        <span class="material-symbols-outlined" aria-hidden="true"
          >refresh</span
        >
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
        <span class="material-symbols-outlined" aria-hidden="true"
          >download</span
        >
        <span class="report-preview-panel__download-label">Download</span>
      </button>
    </div>

    <div v-if="loading" class="report-preview-panel__loading">
      <div class="report-preview-panel__spinner"></div>
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
  --rpt-primary: var(--color-primary, #4f46e5);
  --rpt-border: color-mix(
    in srgb,
    var(--rpt-primary) 9%,
    var(--color-border, #e2e8f0) 91%
  );
  --rpt-panel: var(--color-surface, #ffffff);
  --rpt-bg: var(--color-bg-light, #f7f6fb);
  --rpt-muted: var(--color-text-muted, #64748b);
  --rpt-text: var(--color-text, #0f172a);

  position: relative;
  min-height: 720px;
  flex: 1;
  overflow: hidden;
  border: 1px solid color-mix(in srgb, var(--rpt-border) 70%, transparent);
  border-radius: 14px;
  background: color-mix(in srgb, var(--rpt-bg) 70%, var(--rpt-panel) 30%);
  box-shadow:
    inset 0 1px 0 rgba(255, 255, 255, 0.65),
    0 18px 50px -40px rgba(15, 23, 42, 0.28);
}

.report-preview-panel__actions {
  position: absolute;
  top: 14px;
  right: 14px;
  z-index: 3;
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 6px;
  border: 1px solid color-mix(in srgb, var(--rpt-border) 60%, transparent);
  border-radius: 999px;
  background: color-mix(in srgb, var(--rpt-panel) 88%, transparent);
  backdrop-filter: blur(12px);
  box-shadow: 0 14px 30px -20px rgba(15, 23, 42, 0.22);
}

.report-preview-panel__icon-btn {
  width: 36px;
  height: 36px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  border: 0;
  border-radius: 999px;
  background: transparent;
  color: var(--rpt-muted);
  cursor: pointer;
  transition:
    background-color 0.16s ease,
    color 0.16s ease,
    transform 0.16s ease;
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
  font-size: 20px;
}

.report-preview-panel__download {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 0 14px;
  height: 36px;
  border: 0;
  border-radius: 999px;
  background: linear-gradient(
    135deg,
    color-mix(in srgb, var(--rpt-primary) 72%, #ffffff 28%),
    var(--rpt-primary)
  );
  color: #ffffff;
  font-size: 13px;
  font-weight: 700;
  cursor: pointer;
  box-shadow: 0 12px 22px -14px
    color-mix(in srgb, var(--rpt-primary) 70%, transparent);
  transition:
    transform 0.16s ease,
    box-shadow 0.16s ease;
}

.report-preview-panel__download:hover:not(:disabled) {
  transform: translateY(-1px);
  box-shadow: 0 16px 26px -12px
    color-mix(in srgb, var(--rpt-primary) 72%, transparent);
}

.report-preview-panel__download .material-symbols-outlined {
  font-size: 18px;
}

.report-preview-panel__loading {
  position: absolute;
  inset: 0;
  z-index: 2;
  display: flex;
  flex-direction: column;
  gap: 12px;
  align-items: center;
  justify-content: center;
  background: color-mix(in srgb, var(--rpt-panel) 70%, transparent);
  backdrop-filter: blur(4px);
}

.report-preview-panel__spinner {
  width: 42px;
  height: 42px;
  border: 4px solid color-mix(in srgb, var(--rpt-primary) 14%, transparent);
  border-top-color: var(--rpt-primary);
  border-radius: 999px;
  animation: report-preview-spin 0.85s linear infinite;
}

.report-preview-panel__frame {
  width: 100%;
  height: 100%;
  min-height: 720px;
  border: 0;
  background: #fff;
}

.report-preview-panel__empty {
  display: flex;
  min-height: 300px;
  align-items: center;
  justify-content: center;
  color: var(--rpt-muted);
  font-size: 14px;
}

@media (max-width: 760px) {
  .report-preview-panel {
    min-height: calc(100dvh - 9.25rem);
    border-radius: 12px;
  }

  .report-preview-panel,
  .report-preview-panel__frame {
    min-height: calc(100dvh - 9.25rem);
  }

  .report-preview-panel__actions {
    top: 10px;
    right: 10px;
    gap: 4px;
    padding: 5px;
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

@keyframes report-preview-spin {
  to {
    transform: rotate(360deg);
  }
}
</style>

<script setup lang="ts">
import { computed, onMounted, onUnmounted, shallowRef } from "vue";
import { useRoute, useRouter } from "vue-router";
import { useToast } from "@apptemplate/ui";
import ReportFilterBar from "@/components/reports/ReportFilterBar.vue";
import ReportPreviewPanel from "@/components/reports/ReportPreviewPanel.vue";
import { useReportPreview } from "@/composables/useReportPreview";
import { useReports } from "@/composables/useReports";
import reportService, {
  isReportRequestCanceled,
  type ReportTypeDefinition,
} from "@/services/reportService";

const route = useRoute();
const router = useRouter();
const toast = useToast();
const { reports, loading, loadReports } = useReports();
const isFilterSheetOpen = shallowRef(false);

let loadController: AbortController | undefined;

const reportType = computed(() => String(route.params.type ?? ""));
const selectedReport = computed<ReportTypeDefinition | undefined>(() =>
  reports.value.find((report) => report.id === reportType.value),
);

const {
  applyFilters,
  filters,
  previewHtml,
  previewLoading,
  previewRequest,
  refreshPreview,
  updateFilters,
} = useReportPreview({
  reportType,
  selectedReport,
  onError: (message) => toast.error(message),
});

const previewEmptyText = computed(() =>
  selectedReport.value?.filters.some((filter) => filter.name === "dateRange")
    ? "Preparing preview."
    : "No preview.",
);

onMounted(async () => {
  loadController = new AbortController();

  try {
    const source = await loadReports(loadController.signal);
    if (source === "fallback") {
      toast.info("Showing default report filters until the API is available");
    }
  } catch (error) {
    if (!isReportRequestCanceled(error)) {
      toast.error("Failed to load report");
    }
  }
});

onUnmounted(() => {
  loadController?.abort();
});

function goBack() {
  router.push({ name: "reports" });
}

function openFilterSheet() {
  isFilterSheetOpen.value = true;
}

function closeFilterSheet() {
  isFilterSheetOpen.value = false;
}

async function handleApplyFilters() {
  await applyFilters();
  closeFilterSheet();
}

async function downloadReport() {
  const report = selectedReport.value;
  if (!report) return;

  try {
    await reportService.downloadPdf(report.id, previewRequest.value);
  } catch (error) {
    if (!isReportRequestCanceled(error)) {
      toast.error("Failed to download report");
    }
  }
}
</script>

<template>
  <div class="report-detail">
    <div v-if="loading" class="report-detail__loading">
      <div class="report-detail__spinner"></div>
    </div>

    <template v-else-if="selectedReport">
      <!-- Breadcrumb: replaces the old large report-header card. The report
           name now reads "Reports > <Name>" — the report's own title is
           rendered inside the iframe header, so this is just navigation. -->
      <nav class="report-breadcrumb" aria-label="Breadcrumb">
        <button type="button" class="report-breadcrumb__link" @click="goBack">
          <span
            class="material-symbols-outlined text-[16px]"
            aria-hidden="true"
          >
            arrow_back
          </span>
          Reports
        </button>
        <span class="report-breadcrumb__sep" aria-hidden="true">›</span>
        <span class="report-breadcrumb__current">{{
          selectedReport.name
        }}</span>
      </nav>

      <div class="report-detail__desktop-filters">
        <ReportFilterBar
          :filters="selectedReport.filters"
          :value="filters"
          :page-setup="selectedReport.pageSetup"
          @apply="applyFilters"
          @update="updateFilters"
        />
      </div>

      <ReportPreviewPanel
        :loading="previewLoading"
        :html="previewHtml"
        :empty-text="previewEmptyText"
        :has-pdf="Boolean(previewHtml)"
        @refresh="refreshPreview"
        @download="downloadReport"
      />

      <button
        type="button"
        class="report-filter-fab"
        aria-label="Open report filters"
        :aria-expanded="isFilterSheetOpen"
        @click="openFilterSheet"
      >
        <span class="material-symbols-outlined" aria-hidden="true">
          filter_alt
        </span>
      </button>

      <Teleport to="body">
        <Transition name="report-filter-sheet">
          <div v-if="isFilterSheetOpen" class="report-filter-sheet-shell">
            <button
              type="button"
              class="report-filter-sheet-backdrop"
              aria-label="Close report filters"
              @click="closeFilterSheet"
            />

            <section
              class="report-filter-sheet"
              role="dialog"
              aria-modal="true"
              aria-labelledby="report-filter-sheet-title"
            >
              <div class="report-filter-sheet__grip"></div>

              <header class="report-filter-sheet__header">
                <h3
                  id="report-filter-sheet-title"
                  class="report-filter-sheet__title"
                >
                  Filters
                </h3>
                <button
                  type="button"
                  class="report-filter-sheet__close"
                  aria-label="Close report filters"
                  @click="closeFilterSheet"
                >
                  <span class="material-symbols-outlined" aria-hidden="true">
                    close
                  </span>
                </button>
              </header>

              <div class="report-filter-sheet__body">
                <ReportFilterBar
                  :filters="selectedReport.filters"
                  :value="filters"
                  :page-setup="selectedReport.pageSetup"
                  @apply="handleApplyFilters"
                  @update="updateFilters"
                />
              </div>
            </section>
          </div>
        </Transition>
      </Teleport>
    </template>

    <div v-else class="report-detail__empty">
      <button
        type="button"
        class="report-detail__back report-detail__empty-back"
        aria-label="Back to reports"
        @click="goBack"
      >
        <span class="material-symbols-outlined" aria-hidden="true">
          arrow_back
        </span>
      </button>
      Report not found.
    </div>
  </div>
</template>

<style scoped>
.report-detail {
  --rpt-primary: var(--color-primary, #4f46e5);
  --rpt-border: color-mix(
    in srgb,
    var(--rpt-primary) 9%,
    var(--color-border, #e2e8f0) 91%
  );
  --rpt-panel: var(--color-surface, #ffffff);
  --rpt-muted: var(--color-text-muted, #64748b);

  display: flex;
  min-height: calc(100dvh - 132px);
  flex-direction: column;
  gap: 12px;
}

.report-breadcrumb {
  display: inline-flex;
  align-items: center;
  gap: 8px;
  font-size: 13px;
  color: var(--rpt-muted);
}

.report-breadcrumb__link {
  display: inline-flex;
  align-items: center;
  gap: 4px;
  padding: 4px 8px 4px 4px;
  border: 0;
  border-radius: 6px;
  background: transparent;
  color: var(--rpt-muted);
  font: inherit;
  font-weight: 600;
  cursor: pointer;
  transition:
    color 0.16s ease,
    background-color 0.16s ease;
}

.report-breadcrumb__link:hover {
  color: var(--rpt-primary);
  background: color-mix(in srgb, var(--rpt-primary) 6%, transparent);
}

.report-breadcrumb__sep {
  color: var(--rpt-muted);
  font-size: 14px;
  line-height: 1;
}

.report-breadcrumb__current {
  color: var(--color-text, #0f172a);
  font-weight: 700;
}

.report-filter-fab {
  display: none;
}

.report-filter-sheet-shell {
  --rpt-primary: var(--color-primary, #4f46e5);
  --rpt-panel: var(--color-surface, #ffffff);
  --rpt-muted: var(--color-text-muted, #64748b);

  position: fixed;
  inset: 0;
  z-index: 150;
  display: flex;
  align-items: flex-end;
  justify-content: center;
  padding: 0 0.75rem max(env(safe-area-inset-bottom, 0px), 0.75rem);
}

.report-filter-sheet-backdrop {
  position: absolute;
  inset: 0;
  border: 0;
  background: rgba(15, 23, 42, 0.56);
}

.report-filter-sheet {
  position: relative;
  display: flex;
  width: min(100%, 32rem);
  max-height: min(84dvh, 42rem);
  flex-direction: column;
  overflow: hidden;
  padding: 0.25rem 1rem calc(env(safe-area-inset-bottom, 0px) + 1rem);
  border: 1px solid var(--color-border);
  border-bottom: 0;
  border-top-left-radius: 1.6rem;
  border-top-right-radius: 1.6rem;
  background: color-mix(in srgb, var(--rpt-panel) 98%, transparent);
  box-shadow:
    0 30px 60px -30px rgba(15, 23, 42, 0.42),
    0 18px 30px -24px rgba(15, 23, 42, 0.28);
}

.report-filter-sheet__grip {
  width: 3.4rem;
  height: 0.32rem;
  margin: 0.6rem auto 0.35rem;
  border-radius: 999px;
  background: color-mix(
    in srgb,
    var(--color-border) 78%,
    var(--color-text-muted) 22%
  );
}

.report-filter-sheet__header {
  position: sticky;
  top: 0;
  z-index: 1;
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 0.75rem;
  padding: 0.3rem 0 1rem;
  background: inherit;
}

.report-filter-sheet__title {
  margin: 0;
  color: var(--color-text);
  font-size: 0.95rem;
  font-weight: 700;
}

.report-filter-sheet__close {
  display: inline-flex;
  width: 2.25rem;
  height: 2.25rem;
  align-items: center;
  justify-content: center;
  border: 1px solid var(--color-border);
  border-radius: 999px;
  background: var(--rpt-panel);
  color: var(--rpt-muted);
}

.report-filter-sheet__close .material-symbols-outlined {
  font-size: 1.25rem;
}

.report-filter-sheet__body {
  min-height: 0;
  overflow-y: auto;
  padding-bottom: 0.25rem;
}

.report-filter-sheet-enter-active,
.report-filter-sheet-leave-active {
  transition: opacity 0.24s ease;
}

.report-filter-sheet-enter-active .report-filter-sheet,
.report-filter-sheet-leave-active .report-filter-sheet {
  transition:
    transform 0.24s ease,
    opacity 0.24s ease;
}

.report-filter-sheet-enter-from,
.report-filter-sheet-leave-to {
  opacity: 0;
}

.report-filter-sheet-enter-from .report-filter-sheet,
.report-filter-sheet-leave-to .report-filter-sheet {
  transform: translateY(1.5rem);
  opacity: 0;
}

.report-detail__loading,
.report-detail__empty {
  display: flex;
  min-height: 300px;
  align-items: center;
  justify-content: center;
  gap: 4px;
  border: 1px dashed color-mix(in srgb, var(--rpt-border) 70%, transparent);
  border-radius: 14px;
  background: color-mix(in srgb, var(--rpt-panel) 70%, transparent);
  color: var(--rpt-muted);
  font-size: 14px;
}

.report-detail__spinner {
  width: 42px;
  height: 42px;
  border: 4px solid color-mix(in srgb, var(--rpt-primary) 14%, transparent);
  border-top-color: var(--rpt-primary);
  border-radius: 999px;
  animation: report-spin 0.85s linear infinite;
}

.report-detail__back {
  width: 42px;
  height: 42px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  border: 1px solid color-mix(in srgb, var(--rpt-border) 75%, transparent);
  border-radius: 999px;
  background: var(--rpt-panel);
  color: var(--rpt-muted);
  cursor: pointer;
  transition:
    border-color 0.16s ease,
    color 0.16s ease,
    transform 0.16s ease;
}

.report-detail__back:hover {
  border-color: color-mix(
    in srgb,
    var(--rpt-primary) 30%,
    var(--rpt-border) 70%
  );
  color: var(--rpt-primary);
  transform: translateY(-1px);
}

.report-detail__empty-back {
  margin-right: 12px;
}

@media (max-width: 1024px) {
  .report-detail {
    min-height: calc(100dvh - 96px);
    gap: 0.75rem;
    padding-bottom: 4rem;
  }

  .report-detail__desktop-filters {
    display: none;
  }

  .report-filter-fab {
    position: fixed;
    right: calc(env(safe-area-inset-right, 0px) + 1rem);
    bottom: calc(env(safe-area-inset-bottom, 0px) + 4.85rem);
    z-index: 55;
    display: inline-flex;
    width: 3rem;
    height: 3rem;
    align-items: center;
    justify-content: center;
    border: 1px solid color-mix(in srgb, var(--rpt-primary) 18%, transparent);
    border-radius: 999px;
    background: linear-gradient(
      135deg,
      color-mix(in srgb, var(--rpt-primary) 72%, #ffffff 28%),
      var(--rpt-primary)
    );
    color: #fff;
    box-shadow:
      0 18px 32px -18px color-mix(in srgb, var(--rpt-primary) 80%, transparent),
      0 10px 20px -20px rgba(15, 23, 42, 0.35);
  }

  .report-filter-fab .material-symbols-outlined {
    font-size: 1.35rem;
  }
}

@keyframes report-spin {
  to {
    transform: rotate(360deg);
  }
}
</style>

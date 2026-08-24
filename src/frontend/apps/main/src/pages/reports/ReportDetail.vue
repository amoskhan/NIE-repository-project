<script setup lang="ts">
import { computed, onMounted, onUnmounted, shallowRef } from "vue";
import { useRoute, useRouter } from "vue-router";
import {
  NieButton,
  NieLoaderSymbol,
  NieModal,
  NieResultState,
  useToast,
} from "@nie/ui";
import ReportFilterBar from "@/components/reports/ReportFilterBar.vue";
import ReportPreviewPanel from "@/components/reports/ReportPreviewPanel.vue";
import { useReportPreview } from "@/composables/reports/useReportPreview";
import { useReports } from "@/composables/reports/useReports";
import reportService, {
  isReportRequestCanceled,
  type ReportTypeDefinition,
} from "@/services/reports/reportService";

const route = useRoute();
const router = useRouter();
const toast = useToast();
const { reports, loading, loadReports } = useReports();
const isFilterSheetOpen = shallowRef(false);
const loadError = shallowRef<string | null>(null);

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

async function loadReportCatalog() {
  loadError.value = null;
  loadController?.abort();
  loadController = new AbortController();

  try {
    const source = await loadReports(loadController.signal);
    if (source === "fallback") {
      toast.info("Showing default report filters until the API is available");
    }
  } catch (error) {
    if (!isReportRequestCanceled(error)) {
      loadError.value = "The report definition could not be loaded.";
      toast.error(loadError.value);
    }
  }
}

onMounted(loadReportCatalog);

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
      <NieLoaderSymbol size="lg" variant="brand" label="Loading report" />
    </div>

    <NieResultState
      v-else-if="loadError"
      variant="error"
      title="Unable to load report"
      :description="loadError"
    >
      <template #actions>
        <NieButton variant="outline" @click="loadReportCatalog">Try again</NieButton>
      </template>
    </NieResultState>

    <template v-else-if="selectedReport">
      <!-- Breadcrumb: replaces the old large report-header card. The report
           name now reads "Reports > <Name>" — the report's own title is
           rendered inside the iframe header, so this is just navigation. -->
      <nav class="report-breadcrumb" aria-label="Breadcrumb">
        <button
          type="button"
          class="report-breadcrumb__link"
          @click="goBack"
        >
          <span class="material-symbols-outlined text-body-lg" aria-hidden="true">
            arrow_back
          </span>
          Reports
        </button>
        <span class="report-breadcrumb__sep" aria-hidden="true">›</span>
        <span class="report-breadcrumb__current">{{ selectedReport.name }}</span>
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

      <NieModal
        v-model="isFilterSheetOpen"
        title="Filters"
        size="lg"
        placement="mobile-sheet"
      >
        <ReportFilterBar
          :filters="selectedReport.filters"
          :value="filters"
          :page-setup="selectedReport.pageSetup"
          @apply="handleApplyFilters"
          @update="updateFilters"
        />
      </NieModal>
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
  --rpt-primary: var(--color-primary, var(--theme-color-brand-600));
  --rpt-border: color-mix(
    in srgb,
    var(--rpt-primary) 9%,
    var(--color-border, var(--theme-color-border-default)) 91%
  );
  --rpt-panel: var(--color-surface, var(--theme-color-static-white));
  --rpt-muted: var(--color-text-muted, var(--theme-color-text-muted));

  display: flex;
  min-height: calc(100dvh - 132px);
  flex-direction: column;
  gap: var(--theme-space-3);
}

.report-breadcrumb {
  display: inline-flex;
  align-items: center;
  gap: var(--theme-space-2);
  font-size: var(--theme-font-size-label);
  color: var(--color-text, var(--theme-color-text-strong));
}

.report-breadcrumb__link {
  display: inline-flex;
  align-items: center;
  gap: var(--theme-space-1);
  padding: var(--theme-space-1) var(--theme-space-2) var(--theme-space-1) var(--theme-space-1);
  border: 0;
  border-radius: var(--theme-radius-control);
  background: transparent;
  color: var(--color-text, var(--theme-color-text-strong));
  font: inherit;
  font-weight: var(--theme-font-weight-semibold);
  cursor: pointer;
  transition: color 0.16s ease, background-color 0.16s ease;
}

.report-breadcrumb__link:hover {
  color: var(--rpt-primary);
  background: color-mix(in srgb, var(--rpt-primary) 6%, transparent);
}

.report-breadcrumb__sep {
  color: var(--rpt-muted);
  font-size: var(--theme-font-size-body);
  line-height: 1;
}

.report-breadcrumb__current {
  color: var(--color-text, var(--theme-color-text-strong));
  font-weight: var(--theme-font-weight-bold);
}

.report-filter-fab {
  display: none;
}

.report-detail__loading,
.report-detail__empty {
  display: flex;
  min-height: 300px;
  align-items: center;
  justify-content: center;
  gap: var(--theme-space-1);
  border: 1px dashed color-mix(in srgb, var(--rpt-border) 70%, transparent);
  border-radius: var(--theme-radius-panel);
  background: color-mix(in srgb, var(--rpt-panel) 70%, transparent);
  color: var(--rpt-muted);
  font-size: var(--theme-font-size-body);
}


.report-detail__back {
  width: 42px;
  height: 42px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  border: 1px solid color-mix(in srgb, var(--rpt-border) 75%, transparent);
  border-radius: var(--theme-radius-pill);
  background: var(--rpt-panel);
  color: var(--rpt-muted);
  cursor: pointer;
  transition: border-color 0.16s ease, color 0.16s ease, transform 0.16s ease;
}

.report-detail__back:hover {
  border-color: color-mix(in srgb, var(--rpt-primary) 30%, var(--rpt-border) 70%);
  color: var(--rpt-primary);
  transform: translateY(-1px);
}

.report-detail__empty-back {
  margin-right: var(--theme-space-3);
}

@media (max-width: 1024px) {
  .report-detail {
    min-height: calc(100dvh - 96px);
    gap: var(--theme-space-3);
    padding-bottom: var(--theme-space-16);
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
    border-radius: var(--theme-radius-pill);
    background: var(--rpt-primary);
    color: var(--theme-color-on-brand);
    box-shadow: var(--theme-shadow-card);
  }

  .report-filter-fab .material-symbols-outlined {
    font-size: var(--theme-font-size-section-title);
  }
}

</style>

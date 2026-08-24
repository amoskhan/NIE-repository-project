import {
  computed,
  onUnmounted,
  reactive,
  readonly,
  shallowRef,
  type ComputedRef,
  watch,
} from "vue";
import reportService, {
  normalizeReportPageSetup,
  isReportRequestCanceled,
  type ReportPageFormat,
  type ReportPageOrientation,
  type ReportPageSetupDefinition,
  type ReportRequest,
  type ReportTypeDefinition,
} from "@/services/reports/reportService";

export interface ReportFilterState {
  status: string;
  category: string;
  dateFrom: string;
  dateTo: string;
  vendorId: string | null;
  userId: string;
  /** Paper format used by both HTML preview sizing and PDF generation. */
  format: ReportPageFormat;
  /** Page orientation used by both HTML preview sizing and PDF generation. */
  orientation: ReportPageOrientation;
}

interface UseReportPreviewOptions {
  reportType: ComputedRef<string>;
  selectedReport: ComputedRef<ReportTypeDefinition | undefined>;
  onError: (message: string) => void;
}

export function useReportPreview(options: UseReportPreviewOptions) {
  const previewLoading = shallowRef(false);
  // HTML returned by /api/Report/preview — rendered via iframe srcdoc.
  // We deliberately do NOT use the PDF endpoint for preview because it
  // requires Playwright + Chromium to be installed on the API host, which
  // is a separate operational concern (the Download button still uses PDF).
  const previewHtml = shallowRef<string | null>(null);
  const filters = reactive<ReportFilterState>(createDefaultFilterState());
  const appliedFilters = reactive<ReportFilterState>(createDefaultFilterState());

  let activePreviewController: AbortController | undefined;

  const previewRequest = computed<ReportRequest>(() =>
    toReportRequest(options.reportType.value, appliedFilters),
  );
  const pageSetup = computed<ReportPageSetupDefinition>(() =>
    normalizeReportPageSetup(options.selectedReport.value?.pageSetup),
  );

  watch(
    options.selectedReport,
    (report) => {
      cancelPreviewRequest();
      clearPreviewHtml();
      if (report) {
        resetFilters(report);
        applyCurrentFilters();
        void refreshPreview();
      }
    },
    { immediate: true },
  );

  onUnmounted(() => {
    cancelPreviewRequest();
    clearPreviewHtml();
  });

  function updateFilters(patch: Partial<ReportFilterState>) {
    Object.assign(filters, constrainPageSetupPatch(patch));
  }

  function constrainPageSetupPatch(
    patch: Partial<ReportFilterState>,
  ): Partial<ReportFilterState> {
    const constrained: Partial<ReportFilterState> = { ...patch };
    if (patch.format) {
      constrained.format = isAllowedFormat(patch.format, pageSetup.value)
        ? patch.format
        : pageSetup.value.defaultFormat;
    }

    if (patch.orientation) {
      constrained.orientation = isAllowedOrientation(
        patch.orientation,
        pageSetup.value,
      )
        ? patch.orientation
        : pageSetup.value.defaultOrientation;
    }

    return constrained;
  }

  function resetFilters(report: ReportTypeDefinition) {
    const today = new Date();
    const startOfMonth = new Date(today.getFullYear(), today.getMonth(), 1);
    const reportPageSetup = normalizeReportPageSetup(report.pageSetup);

    updateFilters({
      status: "All",
      category: "All",
      dateFrom: toDateInputValue(startOfMonth),
      dateTo: toDateInputValue(today),
      vendorId: null,
      userId: "",
      format: reportPageSetup.defaultFormat,
      orientation: reportPageSetup.defaultOrientation,
    });
  }

  async function applyFilters() {
    applyCurrentFilters();
    await refreshPreview();
  }

  async function refreshPreview() {
    const report = options.selectedReport.value;
    if (!report) return;

    cancelPreviewRequest();
    const controller = new AbortController();
    activePreviewController = controller;
    previewLoading.value = true;

    try {
      const html = await reportService.generatePreview(
        report.id,
        previewRequest.value,
        { signal: controller.signal },
      );

      if (activePreviewController !== controller) return;

      previewHtml.value = html;
    } catch (error) {
      if (
        activePreviewController === controller
        && !isReportRequestCanceled(error)
      ) {
        clearPreviewHtml();
        options.onError("Failed to generate report preview");
      }
    } finally {
      if (activePreviewController === controller) {
        previewLoading.value = false;
        activePreviewController = undefined;
      }
    }
  }

  function cancelPreviewRequest() {
    if (activePreviewController) {
      activePreviewController.abort();
      previewLoading.value = false;
    }
    activePreviewController = undefined;
  }

  function clearPreviewHtml() {
    previewHtml.value = null;
  }

  function applyCurrentFilters() {
    Object.assign(appliedFilters, constrainPageSetupPatch(filters));
  }

  return {
    filters,
    applyFilters,
    previewHtml: readonly(previewHtml),
    previewLoading: readonly(previewLoading),
    previewRequest,
    refreshPreview,
    updateFilters,
  };
}

function isAllowedFormat(
  format: ReportPageFormat,
  pageSetup: ReportPageSetupDefinition,
): boolean {
  return pageSetup.allowFormatChange && pageSetup.formats.includes(format);
}

function isAllowedOrientation(
  orientation: ReportPageOrientation,
  pageSetup: ReportPageSetupDefinition,
): boolean {
  return pageSetup.allowOrientationChange
    && pageSetup.orientations.includes(orientation);
}

function createDefaultFilterState(): ReportFilterState {
  return {
    status: "All",
    category: "All",
    dateFrom: "",
    dateTo: "",
    vendorId: null,
    userId: "",
    format: "A4",
    orientation: "Portrait",
  };
}

function toReportRequest(
  reportType: string,
  filters: ReportFilterState,
): ReportRequest {
  return {
    reportType,
    status: filters.status && filters.status !== "All" ? filters.status : undefined,
    category:
      filters.category && filters.category !== "All"
        ? filters.category
        : undefined,
    dateFrom: filters.dateFrom || null,
    dateTo: filters.dateTo || null,
    vendorId: filters.vendorId,
    userId: filters.userId.trim() || undefined,
    format: filters.format,
    orientation: filters.orientation,
  };
}

function toDateInputValue(date: Date): string {
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, "0");
  const day = String(date.getDate()).padStart(2, "0");
  return `${year}-${month}-${day}`;
}

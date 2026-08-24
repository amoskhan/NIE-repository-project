import { computed, readonly, shallowRef } from "vue";
import reportService, {
  DEFAULT_REPORT_TYPES,
  isReportRequestCanceled,
  type ReportTypeDefinition,
} from "@/services/reports/reportService";

export interface ReportGroup {
  category: string;
  items: ReportTypeDefinition[];
}

export function useReports() {
  const reports = shallowRef<ReportTypeDefinition[]>([]);
  const loading = shallowRef(false);
  const usingFallback = shallowRef(false);

  const groupedReports = computed<ReportGroup[]>(() => {
    const groups = new Map<string, ReportTypeDefinition[]>();
    for (const report of reports.value) {
      const current = groups.get(report.category) ?? [];
      current.push(report);
      groups.set(report.category, current);
    }

    return Array.from(groups, ([category, items]) => ({ category, items }));
  });

  async function loadReports(signal?: AbortSignal): Promise<"api" | "fallback"> {
    loading.value = true;
    try {
      reports.value = await reportService.getReportTypes({ signal });
      usingFallback.value = false;
      return "api";
    } catch (error) {
      if (isReportRequestCanceled(error)) {
        throw error;
      }

      reports.value = DEFAULT_REPORT_TYPES;
      usingFallback.value = true;
      return "fallback";
    } finally {
      loading.value = false;
    }
  }

  return {
    reports: readonly(reports),
    loading: readonly(loading),
    usingFallback: readonly(usingFallback),
    groupedReports,
    loadReports,
  };
}

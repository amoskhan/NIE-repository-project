<script setup lang="ts">
import { onMounted, onUnmounted } from "vue";
import { useRouter } from "vue-router";
import { useToast } from "@apptemplate/ui";
import ReportCard from "@/components/reports/ReportCard.vue";
import { useReports } from "@/composables/useReports";
import {
  isReportRequestCanceled,
  type ReportTypeDefinition,
} from "@/services/reportService";

const router = useRouter();
const toast = useToast();
const { reports, loading, groupedReports, loadReports } = useReports();

let loadController: AbortController | undefined;

onMounted(async () => {
  loadController = new AbortController();

  try {
    const source = await loadReports(loadController.signal);
    if (source === "fallback") {
      toast.info("Showing default reports until the API is available");
    }
  } catch (error) {
    if (!isReportRequestCanceled(error)) {
      toast.error("Failed to load reports");
    }
  }
});

onUnmounted(() => {
  loadController?.abort();
});

function openReport(report: ReportTypeDefinition) {
  router.push({ name: "report-detail", params: { type: report.id } });
}
</script>

<template>
  <div class="reports-index">
    <div v-if="loading" class="reports-index__loading">
      <div class="reports-index__spinner"></div>
    </div>

    <template v-else>
      <section
        v-for="group in groupedReports"
        :key="group.category"
        class="reports-index__group"
      >
        <div class="reports-index__group-header">
          <h2 class="reports-index__group-title">{{ group.category }}</h2>
          <span class="reports-index__group-count">
            {{ group.items.length }}
          </span>
        </div>

        <div class="reports-index__grid">
          <ReportCard
            v-for="report in group.items"
            :key="report.id"
            :report="report"
            @select="openReport"
          />
        </div>
      </section>

      <div v-if="reports.length === 0" class="reports-index__empty">
        No reports available.
      </div>
    </template>
  </div>
</template>

<style scoped>
.reports-index {
  --rpt-primary: var(--color-primary, #4f46e5);
  --rpt-border: color-mix(
    in srgb,
    var(--rpt-primary) 9%,
    var(--color-border, #e2e8f0) 91%
  );
  --rpt-active: var(--color-sidebar-active, #eef0fe);
  --rpt-panel: var(--color-surface, #ffffff);
  --rpt-text: var(--color-text, #0f172a);
  --rpt-muted: var(--color-text-muted, #64748b);

  display: flex;
  flex-direction: column;
  gap: 32px;
}

.reports-index__loading {
  display: flex;
  justify-content: center;
  padding: 64px 0;
}

.reports-index__spinner {
  width: 42px;
  height: 42px;
  border: 4px solid color-mix(in srgb, var(--rpt-primary) 14%, transparent);
  border-top-color: var(--rpt-primary);
  border-radius: 999px;
  animation: reports-spin 0.85s linear infinite;
}

.reports-index__group {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.reports-index__group-header {
  display: flex;
  align-items: center;
  gap: 12px;
}

.reports-index__group-title {
  margin: 0;
  color: var(--rpt-text);
  font-size: 18px;
  font-weight: 700;
  letter-spacing: 0;
}

.reports-index__group-count {
  display: inline-flex;
  min-width: 28px;
  height: 24px;
  padding: 0 8px;
  align-items: center;
  justify-content: center;
  border-radius: 999px;
  background: color-mix(in srgb, var(--rpt-active) 70%, var(--rpt-panel) 30%);
  color: var(--rpt-primary);
  font-size: 12px;
  font-weight: 700;
}

.reports-index__grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(260px, 1fr));
  gap: 16px;
}

.reports-index__empty {
  border: 1px dashed color-mix(in srgb, var(--rpt-border) 70%, transparent);
  border-radius: 14px;
  padding: 48px 32px;
  background: color-mix(in srgb, var(--rpt-panel) 70%, transparent);
  color: var(--rpt-muted);
  text-align: center;
  font-size: 14px;
}

@keyframes reports-spin {
  to {
    transform: rotate(360deg);
  }
}
</style>

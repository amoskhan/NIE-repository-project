<script setup lang="ts">
import { onMounted, onUnmounted, shallowRef } from "vue";
import { useRouter } from "vue-router";
import { NieAlert, NieButton, NieLoaderSymbol } from "@nie/ui";
import ReportCard from "@/components/reports/ReportCard.vue";
import { useReports } from "@/composables/reports/useReports";
import { isReportRequestCanceled, type ReportTypeDefinition } from "@/services/reports/reportService";

const router = useRouter();
const { reports, loading, groupedReports, loadReports } = useReports();
const degradedMessage = shallowRef<string | null>(null);

let loadController: AbortController | undefined;

async function loadReportCatalog() {
  loadController?.abort();
  loadController = new AbortController();
  const activeController = loadController;

  try {
    const source = await loadReports(activeController.signal);
    if (loadController === activeController) {
      degradedMessage.value =
        source === "fallback"
          ? "The live report catalog is unavailable. Default reports remain available while you retry."
          : null;
    }
  } catch (error) {
    if (!isReportRequestCanceled(error) && loadController === activeController) {
      degradedMessage.value =
        "The live report catalog could not be loaded. Existing reports remain available while you retry.";
    }
  }
}

onMounted(loadReportCatalog);

onUnmounted(() => {
  loadController?.abort();
});

function openReport(report: ReportTypeDefinition) {
  router.push({ name: "report-detail", params: { type: report.id } });
}
</script>

<template>
  <div class="reports-index">
    <div v-if="loading && reports.length === 0" class="reports-index__loading">
      <NieLoaderSymbol size="lg" variant="brand" label="Loading reports" />
    </div>

    <template v-else>
      <NieAlert
        v-if="degradedMessage"
        variant="warning"
        title="Showing default reports"
      >
        <div class="reports-index__degraded-content">
          <p>{{ degradedMessage }}</p>
          <NieButton
            size="sm"
            variant="outline"
            :loading="loading"
            aria-label="Retry loading report catalog"
            @click="loadReportCatalog"
          >
            Try again
          </NieButton>
        </div>
      </NieAlert>

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
  --rpt-primary: var(--color-primary, var(--theme-color-brand-600));
  --rpt-border: color-mix(
    in srgb,
    var(--rpt-primary) 9%,
    var(--color-border, var(--theme-color-border-default)) 91%
  );
  --rpt-active: var(--color-sidebar-active, var(--theme-color-brand-50));
  --rpt-panel: var(--color-surface, var(--theme-color-static-white));
  --rpt-text: var(--color-text, var(--theme-color-text-strong));
  --rpt-muted: var(--color-text-muted, var(--theme-color-text-muted));

  display: flex;
  flex-direction: column;
  gap: var(--theme-space-8);
}

.reports-index__loading {
  display: flex;
  justify-content: center;
  padding: var(--theme-space-16) 0;
}

.reports-index__degraded-content {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: var(--theme-space-4);
}

.reports-index__degraded-content p {
  margin: 0;
}


.reports-index__group {
  display: flex;
  flex-direction: column;
  gap: var(--theme-space-4);
}

.reports-index__group-header {
  display: flex;
  align-items: center;
  gap: var(--theme-space-3);
}

.reports-index__group-title {
  margin: 0;
  color: var(--rpt-text);
  font-size: var(--theme-font-size-card-title);
  font-weight: var(--theme-font-weight-bold);
  letter-spacing: 0;
}

.reports-index__group-count {
  display: inline-flex;
  min-width: 28px;
  height: 24px;
  padding: 0 var(--theme-space-2);
  align-items: center;
  justify-content: center;
  border-radius: var(--theme-radius-pill);
  background: color-mix(in srgb, var(--rpt-active) 70%, var(--rpt-panel) 30%);
  color: var(--rpt-text);
  font-size: var(--theme-font-size-caption);
  font-weight: var(--theme-font-weight-bold);
}

.reports-index__grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(260px, 1fr));
  gap: var(--theme-space-4);
}

.reports-index__empty {
  border: 1px dashed color-mix(in srgb, var(--rpt-border) 70%, transparent);
  border-radius: var(--theme-radius-panel);
  padding: var(--theme-space-12) var(--theme-space-8);
  background: color-mix(in srgb, var(--rpt-panel) 70%, transparent);
  color: var(--rpt-muted);
  text-align: center;
  font-size: var(--theme-font-size-body);
}

@media (max-width: 640px) {
  .reports-index__degraded-content {
    align-items: flex-start;
    flex-direction: column;
  }
}

</style>

<script setup lang="ts">
import { computed } from "vue";
import type {
  ReportFilter,
  ReportPageFormat,
  ReportPageOrientation,
  ReportPageSetupDefinition,
} from "@/services/reports/reportService";
import { normalizeReportPageSetup } from "@/services/reports/reportService";
import type { ReportFilterState } from "@/composables/reports/useReportPreview";

const props = defineProps<{
  filters: ReportFilter[];
  value: ReportFilterState;
  pageSetup?: ReportPageSetupDefinition;
}>();

const emit = defineEmits<{
  apply: [];
  update: [patch: Partial<ReportFilterState>];
}>();

function updateVendorId(event: Event) {
  const value = (event.target as HTMLInputElement).value;
  emit("update", { vendorId: value === "" ? null : Number(value) });
}

function filterOptions(filter: ReportFilter): string[] {
  return filter.options ?? [];
}

const pageSetup = computed(() => normalizeReportPageSetup(props.pageSetup));
const pageFormats = computed(() => pageSetup.value.formats);
const orientations = computed(() => pageSetup.value.orientations);
const showFormatField = computed(
  () => pageSetup.value.allowFormatChange && pageFormats.value.length > 1,
);
const showOrientationField = computed(
  () =>
    pageSetup.value.allowOrientationChange && orientations.value.length > 1,
);
</script>

<template>
  <form class="report-filter-bar" @submit.prevent="emit('apply')">
    <!-- Data filters (status, daterange, vendor, etc.) — driven by the
         backend's ReportTypeDefinition. -->
    <template
      v-for="filter in filters"
      :key="filter.name"
    >
      <template v-if="filter.type === 'daterange'">
        <label class="report-filter-bar__field">
          <span class="report-filter-bar__label">From</span>
          <input
            class="report-filter-bar__input"
            type="date"
            :value="value.dateFrom"
            @input="emit('update', {
              dateFrom: ($event.target as HTMLInputElement).value,
            })"
          />
        </label>
        <label class="report-filter-bar__field">
          <span class="report-filter-bar__label">To</span>
          <input
            class="report-filter-bar__input"
            type="date"
            :value="value.dateTo"
            @input="emit('update', {
              dateTo: ($event.target as HTMLInputElement).value,
            })"
          />
        </label>
      </template>

      <label
        v-else-if="filter.name === 'status'"
        class="report-filter-bar__field"
      >
        <span class="report-filter-bar__label">{{ filter.label }}</span>
        <select
          class="report-filter-bar__input"
          :value="value.status"
          @change="emit('update', {
            status: ($event.target as HTMLSelectElement).value,
          })"
        >
          <option
            v-for="option in filterOptions(filter)"
            :key="option"
            :value="option"
          >
            {{ option }}
          </option>
        </select>
      </label>

      <label
        v-else-if="filter.name === 'category'"
        class="report-filter-bar__field"
      >
        <span class="report-filter-bar__label">{{ filter.label }}</span>
        <select
          class="report-filter-bar__input"
          :value="value.category"
          @change="emit('update', {
            category: ($event.target as HTMLSelectElement).value,
          })"
        >
          <option
            v-for="option in filterOptions(filter)"
            :key="option"
            :value="option"
          >
            {{ option }}
          </option>
        </select>
      </label>

      <label
        v-else-if="filter.name === 'vendorId'"
        class="report-filter-bar__field"
      >
        <span class="report-filter-bar__label">{{ filter.label }}</span>
        <input
          class="report-filter-bar__input"
          min="1"
          type="number"
          :value="value.vendorId ?? ''"
          @input="updateVendorId"
        />
      </label>

      <label
        v-else-if="filter.name === 'userId'"
        class="report-filter-bar__field"
      >
        <span class="report-filter-bar__label">{{ filter.label }}</span>
        <input
          class="report-filter-bar__input"
          type="text"
          :value="value.userId"
          @input="emit('update', {
            userId: ($event.target as HTMLInputElement).value,
          })"
        />
      </label>
    </template>

    <label
      v-if="showFormatField"
      class="report-filter-bar__field report-filter-bar__field--page"
    >
      <span class="report-filter-bar__label">Format</span>
      <select
        class="report-filter-bar__input"
        :value="value.format"
        @change="emit('update', {
          format: ($event.target as HTMLSelectElement).value as ReportPageFormat,
        })"
      >
        <option v-for="format in pageFormats" :key="format" :value="format">
          {{ format }}
        </option>
      </select>
    </label>

    <label
      v-if="showOrientationField"
      class="report-filter-bar__field report-filter-bar__field--page"
    >
      <span class="report-filter-bar__label">Orientation</span>
      <select
        class="report-filter-bar__input"
        :value="value.orientation"
        @change="emit('update', {
          orientation: ($event.target as HTMLSelectElement).value as ReportPageOrientation,
        })"
      >
        <option
          v-for="orientation in orientations"
          :key="orientation"
          :value="orientation"
        >
          {{ orientation }}
        </option>
      </select>
    </label>

    <button type="submit" class="report-filter-bar__apply" aria-label="Apply filters">
      <span class="material-symbols-outlined" aria-hidden="true">filter_alt</span>
      <span class="report-filter-bar__apply-label">Apply</span>
    </button>
  </form>
</template>

<style scoped>
/* Compact single-row layout. Each filter is an auto-sized inline column so
   the bar takes one line on desktop and wraps gracefully on narrow viewports.
   Labels sit above their inputs (small uppercase) so they don't steal width. */

.report-filter-bar {
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

  display: flex;
  flex-wrap: wrap;
  align-items: flex-end;
  gap: var(--theme-space-2);
  padding: var(--theme-space-2) var(--theme-space-3);
  border: 1px solid color-mix(in srgb, var(--rpt-border) 70%, transparent);
  border-radius: var(--theme-radius-control);
  background: linear-gradient(
    135deg,
    color-mix(in srgb, var(--rpt-panel) 98%, var(--rpt-active) 2%) 0%,
    color-mix(in srgb, var(--rpt-panel) 94%, var(--rpt-bg) 6%) 100%
  );
  box-shadow: var(--theme-shadow-inset), var(--theme-shadow-soft);
}

.report-filter-bar__field {
  display: flex;
  flex-direction: column;
  gap: var(--theme-space-1);
  min-width: 130px;
  flex: 1 1 130px;
  max-width: 180px;
}

.report-filter-bar__field--page {
  min-width: 110px;
  max-width: 140px;
}

.report-filter-bar__label {
  color: var(--rpt-muted);
  font-size: var(--theme-font-size-caption);
  font-weight: var(--theme-font-weight-bold);
  letter-spacing: 0.06em;
  text-transform: uppercase;
}

.report-filter-bar__input {
  width: 100%;
  min-width: 0;
  height: var(--theme-control-height-md);
  padding: 0 var(--theme-space-2);
  border: 1px solid color-mix(in srgb, var(--rpt-border) 80%, transparent);
  border-radius: var(--theme-radius-control);
  background: var(--rpt-panel);
  color: var(--rpt-text);
  font-size: var(--theme-font-size-label);
  font-family: inherit;
  transition: border-color 0.16s ease, box-shadow 0.16s ease;
}

.report-filter-bar__input:hover:not(:focus) {
  border-color: color-mix(in srgb, var(--rpt-primary) 18%, var(--rpt-border) 82%);
}

.report-filter-bar__input:focus {
  border-color: var(--rpt-primary);
  box-shadow: 0 0 0 3px color-mix(in srgb, var(--rpt-primary) 16%, transparent);
  outline: none;
}

.report-filter-bar__apply {
  margin-left: auto;
  align-self: flex-end;
  display: inline-flex;
  align-items: center;
  gap: var(--theme-space-1);
  height: var(--theme-control-height-md);
  padding: 0 var(--theme-space-3);
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

.report-filter-bar__apply:hover {
  transform: translateY(-1px);
  box-shadow: var(--theme-shadow-float);
}

.report-filter-bar__apply .material-symbols-outlined {
  font-size: var(--theme-font-size-body-lg);
}

@media (max-width: 1024px) {
  .report-filter-bar {
    display: grid;
    gap: var(--theme-space-3);
    padding: 0;
    border: 0;
    border-radius: var(--theme-radius-control);
    background: transparent;
    box-shadow: none;
  }

  .report-filter-bar__field,
  .report-filter-bar__field--page {
    width: 100%;
    min-width: 0;
    max-width: none;
  }

  .report-filter-bar__input {
    height: var(--theme-control-height-md);
    border-radius: var(--theme-radius-panel);
  }

  .report-filter-bar__apply {
    margin-left: 0;
    width: 100%;
    height: var(--theme-control-height-md);
    justify-content: center;
    border-radius: var(--theme-radius-pill);
  }
}
</style>

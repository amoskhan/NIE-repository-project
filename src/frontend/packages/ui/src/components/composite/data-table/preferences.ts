import type { InjectionKey } from "vue";
import type {
  NieDataTablePreferenceSettings,
  NieDataTablePreferenceStore,
} from "./types";

export const NIE_DATA_TABLE_FILTER_REMINDER_INTERVAL_MS =
  7 * 24 * 60 * 60 * 1_000;

export function hasActiveDataTablePreferenceFilters(
  settings: Pick<NieDataTablePreferenceSettings, "filters">,
): boolean {
  return Object.values(settings.filters).some((values) => values.length > 0);
}

export function isDataTableFilterReminderDue(
  settings: Pick<
    NieDataTablePreferenceSettings,
    "filters" | "filterReminderAcknowledgedAtUtc"
  >,
  nowMilliseconds = Date.now(),
): boolean {
  if (!hasActiveDataTablePreferenceFilters(settings)) return false;

  const acknowledgedAt = settings.filterReminderAcknowledgedAtUtc;
  if (!acknowledgedAt) return true;

  const acknowledgedAtMilliseconds = Date.parse(acknowledgedAt);
  if (!Number.isFinite(acknowledgedAtMilliseconds)) return true;

  return (
    nowMilliseconds - acknowledgedAtMilliseconds >=
    NIE_DATA_TABLE_FILTER_REMINDER_INTERVAL_MS
  );
}

export class NieDataTablePreferenceConflictError extends Error {
  constructor() {
    super("The saved table preference changed in another session.");
    this.name = "NieDataTablePreferenceConflictError";
  }
}

export const nieDataTablePreferenceStoreKey: InjectionKey<NieDataTablePreferenceStore> =
  Symbol("nie-data-table-preference-store");

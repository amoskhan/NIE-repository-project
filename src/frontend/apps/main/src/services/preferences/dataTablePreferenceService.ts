import {
  NieDataTablePreferenceConflictError,
  type NieDataTableFilterValue,
  type NieDataTablePreferenceRecord,
  type NieDataTablePreferenceSettings,
  type NieDataTablePreferenceStore,
} from "@nie/ui";
import api from "../core/api";

interface ApiPreferenceSettings {
  pageSize: number;
  sorts: Array<{ key: string; direction: "asc" | "desc" }>;
  filters: Array<{ key: string; values: string[] }>;
  filterReminderAcknowledgedAtUtc?: string | null;
  columnOrder: string[];
  hiddenColumns: string[];
  density: "compact" | "comfortable" | "spacious";
  appearance: "elevated" | "minimal" | "striped";
}

interface ApiPreferenceRecord {
  tableKey: string;
  definitionVersion: number;
  revision: number;
  settings: ApiPreferenceSettings;
  repairRequired: boolean;
  repairReasons: string[];
}

function fromApi(record: ApiPreferenceRecord): NieDataTablePreferenceRecord {
  const filtersByKey = new Map<string, NieDataTableFilterValue[]>();
  let duplicateFilterKey = false;
  for (const filter of record.settings.filters ?? []) {
    const existing = filtersByKey.get(filter.key);
    if (existing) duplicateFilterKey = true;
    filtersByKey.set(filter.key, [
      ...(existing ?? []),
      ...filter.values,
    ] as NieDataTableFilterValue[]);
  }

  const duplicateReason = "A duplicate saved filter was consolidated.";
  const repairReasons = duplicateFilterKey
    ? [...new Set([...(record.repairReasons ?? []), duplicateReason])]
    : [...(record.repairReasons ?? [])];

  return {
    ...record,
    repairRequired: record.repairRequired || duplicateFilterKey,
    repairReasons,
    settings: {
      ...record.settings,
      filters: Object.fromEntries(filtersByKey),
    },
  };
}

function toApi(settings: NieDataTablePreferenceSettings): ApiPreferenceSettings {
  return {
    ...settings,
    sorts: settings.sorts.map((sort) => ({ ...sort })),
    filters: Object.entries(settings.filters)
      .filter(([, values]) => values.length > 0)
      .map(([key, values]) => ({ key, values: values.map(String) })),
  };
}

const cache = new Map<string, NieDataTablePreferenceRecord | null>();
const pending = new Map<string, Promise<NieDataTablePreferenceRecord | null>>();

async function fetchPreference(
  tableKey: string,
): Promise<NieDataTablePreferenceRecord | null> {
  const response = await api.get<ApiPreferenceRecord | "">(
    "/api/UserDataTablePreference/Get",
    {
      params: { tableKey },
      signal: AbortSignal.timeout(5_000),
    },
  );
  const record =
    response.status === 204 || !response.data
      ? null
      : fromApi(response.data as ApiPreferenceRecord);
  cache.set(tableKey, record);
  return record;
}

function isConflict(error: unknown): boolean {
  return (
    typeof error === "object" &&
    error !== null &&
    "response" in error &&
    typeof error.response === "object" &&
    error.response !== null &&
    "status" in error.response &&
    error.response.status === 409
  );
}

export const dataTablePreferenceStore: NieDataTablePreferenceStore = {
  async get(tableKey) {
    if (cache.has(tableKey)) return cache.get(tableKey) ?? null;
    const existing = pending.get(tableKey);
    if (existing) return existing;

    const request = fetchPreference(tableKey).finally(() => pending.delete(tableKey));
    pending.set(tableKey, request);
    return request;
  },

  refresh(tableKey) {
    pending.delete(tableKey);
    return fetchPreference(tableKey);
  },

  async save(tableKey, definitionVersion, settings, revision) {
    try {
      const response = await api.put<ApiPreferenceRecord>(
        "/api/UserDataTablePreference/Upsert",
        {
          definitionVersion,
          revision,
          settings: toApi(settings),
        },
        { params: { tableKey } },
      );
      const record = fromApi(response.data);
      cache.set(tableKey, record);
      return record;
    } catch (error) {
      if (isConflict(error)) throw new NieDataTablePreferenceConflictError();
      throw error;
    }
  },

  async remove(tableKey) {
    await api.delete("/api/UserDataTablePreference/Delete", {
      params: { tableKey },
    });
    cache.set(tableKey, null);
  },
};

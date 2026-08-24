import { beforeEach, describe, expect, it, vi } from "vitest";
import {
  NieDataTablePreferenceConflictError,
  type NieDataTablePreferenceSettings,
} from "@nie/ui";

const apiMock = vi.hoisted(() => ({
  get: vi.fn(),
  put: vi.fn(),
  delete: vi.fn(),
}));

vi.mock("../../core/api", () => ({ default: apiMock }));

import { dataTablePreferenceStore } from "../dataTablePreferenceService";

const settings: NieDataTablePreferenceSettings = {
  pageSize: 20,
  sorts: [],
  filters: {},
  columnOrder: ["name"],
  hiddenColumns: [],
  density: "comfortable",
  appearance: "elevated",
};

function apiRecord(tableKey: string, revision: number) {
  return {
    tableKey,
    definitionVersion: 1,
    revision,
    settings: { ...settings, filters: [] },
    repairRequired: false,
    repairReasons: [],
  };
}

describe("dataTablePreferenceStore", () => {
  beforeEach(() => {
    apiMock.get.mockReset();
    apiMock.put.mockReset();
    apiMock.delete.mockReset();
  });

  it("bypasses a cached missing value when the user explicitly reloads", async () => {
    const tableKey = "test.refresh-after-conflict";
    apiMock.get
      .mockResolvedValueOnce({ status: 204, data: "" })
      .mockResolvedValueOnce({ status: 200, data: apiRecord(tableKey, 3) });

    expect(await dataTablePreferenceStore.get(tableKey)).toBeNull();
    expect((await dataTablePreferenceStore.refresh?.(tableKey))?.revision).toBe(3);
    expect(apiMock.get).toHaveBeenCalledTimes(2);
  });

  it("maps HTTP 409 to the provider-neutral concurrency error", async () => {
    apiMock.put.mockRejectedValue({ response: { status: 409 } });

    await expect(
      dataTablePreferenceStore.save(
        "test.conflict-mapping",
        1,
        settings,
        undefined,
      ),
    ).rejects.toBeInstanceOf(NieDataTablePreferenceConflictError);
  });

  it("consolidates duplicate saved filter keys and marks the preference for repair", async () => {
    const tableKey = "test.duplicate-filter-keys";
    const record = apiRecord(tableKey, 2);
    record.settings.filters = [
      { key: "category", values: ["Education"] },
      { key: "category", values: ["Technology"] },
    ];
    apiMock.get.mockResolvedValue({ status: 200, data: record });

    const preference = await dataTablePreferenceStore.get(tableKey);

    expect(preference?.settings.filters.category).toEqual([
      "Education",
      "Technology",
    ]);
    expect(preference?.repairRequired).toBe(true);
    expect(preference?.repairReasons).toContain(
      "A duplicate saved filter was consolidated.",
    );
  });
});

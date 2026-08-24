import { flushPromises, mount } from "@vue/test-utils";
import { afterEach, describe, expect, it, vi } from "vitest";
import {
  NieDataTable,
  NieDataTablePreferenceConflictError,
  nieDataTablePreferenceStoreKey,
  type NieDataTablePreferenceRecord,
  type NieDataTablePreferenceSettings,
} from "@nie/ui";

const columns = [
  { key: "name", label: "Name" },
  { key: "status", label: "Status", filter: true },
];

const serverPage = [
  { id: "11", name: "Record 11", status: "Active" },
  { id: "12", name: "Record 12", status: "Pending" },
];

function mountServerTable() {
  return mount(NieDataTable, {
    attachTo: document.body,
    props: {
      columns,
      data: serverPage,
      rowKey: "id",
      serverSide: true,
      totalItems: 64,
      page: 2,
      pageSize: 10,
      filterOptionPages: {
        status: {
          items: [
            { label: "Active", value: "Active", count: 40 },
            { label: "Pending", value: "Pending", count: 24 },
          ],
          page: 1,
          pageSize: 2,
          totalCount: 4,
          totalPages: 2,
          loading: false,
        },
      },
      hideActions: true,
      preferenceKey: "test.records",
      definitionVersion: 1,
    } as never,
    global: {
      stubs: { Teleport: true, Transition: false },
    },
  });
}

afterEach(() => {
  document.body.innerHTML = "";
});

describe("NieDataTable server contract", () => {
  it("provides a built-in table preferences action and professional configuration dialog", async () => {
    const wrapper = mountServerTable();

    await wrapper.get('[data-table-preferences-action]').trigger("click");

    const dialog = wrapper.get(
      '[role="dialog"][aria-label="Configure table preferences"]',
    );
    expect(dialog.text()).toContain("Columns");
    expect(dialog.text()).toContain("Sorting");
    expect(dialog.text()).toContain("Default filters");
    expect(dialog.text()).toContain("Display");
    expect(dialog.text()).toContain("Save as my default");
  });

  it("treats the first successful save as a healthy preference", async () => {
    const saved: Array<{
      settings: NieDataTablePreferenceSettings;
      revision?: number;
    }> = [];
    const wrapper = mount(NieDataTable, {
      attachTo: document.body,
      props: {
        columns,
        data: serverPage,
        rowKey: "id",
        serverSide: true,
        totalItems: 2,
        hideActions: true,
        preferenceKey: "test.first-save",
        definitionVersion: 1,
      } as never,
      global: {
        provide: {
          [nieDataTablePreferenceStoreKey as symbol]: {
            get: async () => null,
            refresh: async () => null,
            save: async (
              tableKey: string,
              definitionVersion: number,
              settings: NieDataTablePreferenceSettings,
              revision?: number,
            ) => {
              saved.push({ settings, revision });
              return {
                tableKey,
                definitionVersion,
                revision: 1,
                settings,
                repairRequired: false,
                repairReasons: [],
              };
            },
            remove: async () => undefined,
          },
        },
        stubs: { Teleport: true, Transition: false },
      },
    });

    await flushPromises();
    await wrapper.get("[data-table-preferences-action]").trigger("click");
    const saveButton = wrapper
      .findAll("button")
      .find((button) => button.text().includes("Save as my default"));
    expect(saveButton).toBeDefined();
    await saveButton!.trigger("click");
    await flushPromises();

    expect(saved).toHaveLength(1);
    expect(saved[0]?.revision).toBeUndefined();
    expect(wrapper.find("[data-table-preference-warning]").exists()).toBe(false);

    await wrapper.get("[data-table-preferences-action]").trigger("click");
    const dialog = wrapper.get(
      '[role="dialog"][aria-label="Configure table preferences"]',
    );
    expect(dialog.text()).not.toContain("needs repair");
    expect(dialog.text()).toContain("Save as my default");
  });

  it("reminds the user when saved default filters have been active for a week", async () => {
    vi.useFakeTimers();
    vi.setSystemTime(new Date("2026-08-07T02:00:00.000Z"));
    try {
      const savedPreference: NieDataTablePreferenceRecord = {
        tableKey: "test.weekly-filter-reminder",
        definitionVersion: 1,
        revision: 4,
        settings: {
          pageSize: 20,
          sorts: [{ key: "name", direction: "asc" }],
          filters: { status: ["Active"] },
          columnOrder: ["name", "status"],
          hiddenColumns: [],
          density: "comfortable",
          appearance: "elevated",
          filterReminderAcknowledgedAtUtc: "2026-07-31T02:00:00.000Z",
        },
      };
      const wrapper = mount(NieDataTable, {
        attachTo: document.body,
        props: {
          columns,
          data: serverPage,
          rowKey: "id",
          serverSide: true,
          totalItems: 2,
          hideActions: true,
          preferenceKey: savedPreference.tableKey,
        } as never,
        global: {
          provide: {
            [nieDataTablePreferenceStoreKey as symbol]: {
              get: async () => savedPreference,
              refresh: async () => savedPreference,
              save: async () => savedPreference,
              remove: async () => undefined,
            },
          },
          stubs: { Teleport: true, Transition: false },
        },
      });

      await flushPromises();

      const reminder = wrapper.get(
        '[role="dialog"][aria-label="Review saved table filters"]',
      );
      expect(reminder.text()).toContain("Saved filters are active");
      expect(reminder.text()).toContain("Some records may be hidden");
      expect(reminder.text()).toContain("Status (1)");
      expect(wrapper.emitted("query-change")?.at(-1)?.[0]).toMatchObject({
        filters: { status: ["Active"] },
      });
    } finally {
      vi.useRealTimers();
    }
  });

  it("does not remind again before seven days have elapsed", async () => {
    vi.useFakeTimers();
    vi.setSystemTime(new Date("2026-08-07T02:00:00.000Z"));
    try {
      const savedPreference: NieDataTablePreferenceRecord = {
        tableKey: "test.recent-filter-reminder",
        definitionVersion: 1,
        revision: 2,
        settings: {
          pageSize: 20,
          sorts: [],
          filters: { status: ["Pending"] },
          columnOrder: ["name", "status"],
          hiddenColumns: [],
          density: "comfortable",
          appearance: "elevated",
          filterReminderAcknowledgedAtUtc: "2026-08-01T02:00:01.000Z",
        },
      };
      const wrapper = mount(NieDataTable, {
        attachTo: document.body,
        props: {
          columns,
          data: serverPage,
          rowKey: "id",
          serverSide: true,
          totalItems: 2,
          hideActions: true,
          preferenceKey: savedPreference.tableKey,
        } as never,
        global: {
          provide: {
            [nieDataTablePreferenceStoreKey as symbol]: {
              get: async () => savedPreference,
              refresh: async () => savedPreference,
              save: async () => savedPreference,
              remove: async () => undefined,
            },
          },
          stubs: { Teleport: true, Transition: false },
        },
      });

      await flushPromises();

      expect(
        wrapper.find('[role="dialog"][aria-label="Review saved table filters"]').exists(),
      ).toBe(false);
    } finally {
      vi.useRealTimers();
    }
  });

  it("keeps saved filters for another week through the preference store", async () => {
    vi.useFakeTimers();
    vi.setSystemTime(new Date("2026-08-07T02:00:00.000Z"));
    try {
      const saves: NieDataTablePreferenceSettings[] = [];
      const savedPreference: NieDataTablePreferenceRecord = {
        tableKey: "test.keep-weekly-filter",
        definitionVersion: 1,
        revision: 4,
        settings: {
          pageSize: 50,
          sorts: [{ key: "name", direction: "desc" }],
          filters: { status: ["Active"] },
          columnOrder: ["status", "name"],
          hiddenColumns: [],
          density: "compact",
          appearance: "striped",
          filterReminderAcknowledgedAtUtc: null,
        },
      };
      const wrapper = mount(NieDataTable, {
        attachTo: document.body,
        props: {
          columns,
          data: serverPage,
          rowKey: "id",
          serverSide: true,
          totalItems: 2,
          hideActions: true,
          preferenceKey: savedPreference.tableKey,
        } as never,
        global: {
          provide: {
            [nieDataTablePreferenceStoreKey as symbol]: {
              get: async () => savedPreference,
              refresh: async () => savedPreference,
              save: async (
                tableKey: string,
                definitionVersion: number,
                settings: NieDataTablePreferenceSettings,
              ) => {
                saves.push(settings);
                return {
                  tableKey,
                  definitionVersion,
                  revision: 5,
                  settings: {
                    ...settings,
                    filterReminderAcknowledgedAtUtc: "2026-08-07T02:00:00.000Z",
                  },
                };
              },
              remove: async () => undefined,
            },
          },
          stubs: { Teleport: true, Transition: false },
        },
      });

      await flushPromises();
      await wrapper
        .get('[role="dialog"][aria-label="Review saved table filters"]')
        .get('button[aria-label="Keep saved filters for another week"]')
        .trigger("click");
      await flushPromises();

      expect(saves).toHaveLength(1);
      expect(saves[0]).toMatchObject({
        pageSize: 50,
        sorts: [{ key: "name", direction: "desc" }],
        filters: { status: ["Active"] },
        columnOrder: ["status", "name"],
      });
      expect(
        wrapper.find('[role="dialog"][aria-label="Review saved table filters"]').exists(),
      ).toBe(false);
    } finally {
      vi.useRealTimers();
    }
  });

  it("removes only saved filters and immediately reloads the unfiltered first page", async () => {
    const saves: NieDataTablePreferenceSettings[] = [];
    const savedPreference: NieDataTablePreferenceRecord = {
      tableKey: "test.remove-weekly-filter",
      definitionVersion: 1,
      revision: 8,
      settings: {
        pageSize: 50,
        sorts: [{ key: "name", direction: "desc" }],
        filters: { status: ["Active"] },
        columnOrder: ["status", "name"],
        hiddenColumns: ["name"],
        density: "compact",
        appearance: "striped",
        filterReminderAcknowledgedAtUtc: null,
      },
    };
    const wrapper = mount(NieDataTable, {
      attachTo: document.body,
      props: {
        columns,
        data: serverPage,
        rowKey: "id",
        serverSide: true,
        totalItems: 2,
        hideActions: true,
        preferenceKey: savedPreference.tableKey,
      } as never,
      global: {
        provide: {
          [nieDataTablePreferenceStoreKey as symbol]: {
            get: async () => savedPreference,
            refresh: async () => savedPreference,
            save: async (
              tableKey: string,
              definitionVersion: number,
              settings: NieDataTablePreferenceSettings,
            ) => {
              saves.push(settings);
              return { tableKey, definitionVersion, revision: 9, settings };
            },
            remove: async () => undefined,
          },
        },
        stubs: { Teleport: true, Transition: false },
      },
    });

    await flushPromises();
    await wrapper
      .get('[role="dialog"][aria-label="Review saved table filters"]')
      .get('button[aria-label="Remove saved default filters"]')
      .trigger("click");
    await flushPromises();

    expect(saves).toHaveLength(1);
    expect(saves[0]).toMatchObject({
      pageSize: 50,
      sorts: [{ key: "name", direction: "desc" }],
      filters: {},
      columnOrder: ["status", "name"],
      hiddenColumns: ["name"],
      density: "compact",
      appearance: "striped",
    });
    expect(wrapper.emitted("query-change")?.at(-1)?.[0]).toMatchObject({
      page: 1,
      pageSize: 50,
      sorts: [{ key: "name", direction: "desc" }],
      filters: {},
    });
    expect(
      wrapper.find('[role="dialog"][aria-label="Review saved table filters"]').exists(),
    ).toBe(false);
  });

  it("keeps the reminder and active filters when acknowledgement persistence fails", async () => {
    const savedPreference: NieDataTablePreferenceRecord = {
      tableKey: "test.failed-weekly-filter-reminder",
      definitionVersion: 1,
      revision: 3,
      settings: {
        pageSize: 20,
        sorts: [],
        filters: { status: ["Pending"] },
        columnOrder: ["name", "status"],
        hiddenColumns: [],
        density: "comfortable",
        appearance: "elevated",
        filterReminderAcknowledgedAtUtc: null,
      },
    };
    const wrapper = mount(NieDataTable, {
      attachTo: document.body,
      props: {
        columns,
        data: serverPage,
        rowKey: "id",
        serverSide: true,
        totalItems: 2,
        hideActions: true,
        preferenceKey: savedPreference.tableKey,
      } as never,
      global: {
        provide: {
          [nieDataTablePreferenceStoreKey as symbol]: {
            get: async () => savedPreference,
            refresh: async () => savedPreference,
            save: async () => {
              throw new Error("transport unavailable");
            },
            remove: async () => undefined,
          },
        },
        stubs: { Teleport: true, Transition: false },
      },
    });

    await flushPromises();
    const reminder = wrapper.get(
      '[role="dialog"][aria-label="Review saved table filters"]',
    );
    await reminder
      .get('button[aria-label="Keep saved filters for another week"]')
      .trigger("click");
    await flushPromises();

    expect(reminder.text()).toContain("We couldn't update your saved filters");
    expect(
      wrapper.find('[role="dialog"][aria-label="Review saved table filters"]').exists(),
    ).toBe(true);
    expect(wrapper.emitted("query-change")?.at(-1)?.[0]).toMatchObject({
      filters: { status: ["Pending"] },
    });
  });

  it("routes a weekly reminder conflict into the existing reload and rebase flow", async () => {
    let refreshes = 0;
    const savedPreference: NieDataTablePreferenceRecord = {
      tableKey: "test.conflicted-weekly-filter-reminder",
      definitionVersion: 1,
      revision: 11,
      settings: {
        pageSize: 20,
        sorts: [],
        filters: { status: ["Active"] },
        columnOrder: ["name", "status"],
        hiddenColumns: [],
        density: "comfortable",
        appearance: "elevated",
        filterReminderAcknowledgedAtUtc: null,
      },
    };
    const wrapper = mount(NieDataTable, {
      attachTo: document.body,
      props: {
        columns,
        data: serverPage,
        rowKey: "id",
        serverSide: true,
        totalItems: 2,
        hideActions: true,
        preferenceKey: savedPreference.tableKey,
      } as never,
      global: {
        provide: {
          [nieDataTablePreferenceStoreKey as symbol]: {
            get: async () => savedPreference,
            refresh: async () => {
              refreshes += 1;
              if (refreshes === 1) throw new Error("refresh unavailable");
              return { ...savedPreference, revision: 12 };
            },
            save: async () => {
              throw new NieDataTablePreferenceConflictError();
            },
            remove: async () => undefined,
          },
        },
        stubs: { Teleport: true, Transition: false },
      },
    });

    await flushPromises();
    await wrapper
      .get('[role="dialog"][aria-label="Review saved table filters"]')
      .get('button[aria-label="Remove saved default filters"]')
      .trigger("click");
    await flushPromises();

    expect(
      wrapper.find('[role="dialog"][aria-label="Review saved table filters"]').exists(),
    ).toBe(false);
    const preferences = wrapper.get(
      '[role="dialog"][aria-label="Configure table preferences"]',
    );
    expect(preferences.text()).toContain("Saved view changed elsewhere");
    expect(preferences.text()).toContain("Reload latest");
    expect(preferences.find('[aria-label="Close table preferences"]').exists()).toBe(false);
    expect(preferences.findAll("button").some((button) => button.text() === "Cancel")).toBe(false);
    expect(
      preferences.findAll("button").some((button) =>
        button.text().includes("Reset to screen defaults"),
      ),
    ).toBe(false);
    expect(
      preferences
        .findAll("button")
        .find((button) => button.text().includes("Save as my default"))
        ?.attributes("disabled"),
    ).toBeDefined();

    await preferences
      .findAll("button")
      .find((button) => button.text().includes("Reload latest"))!
      .trigger("click");
    await flushPromises();

    expect(refreshes).toBe(1);
    expect(
      wrapper.find('[role="dialog"][aria-label="Configure table preferences"]').exists(),
    ).toBe(true);
    expect(wrapper.text()).toContain("We couldn't reload the latest table preferences");
    expect(wrapper.find('[aria-label="Close table preferences"]').exists()).toBe(false);

    await wrapper
      .get('[role="dialog"][aria-label="Configure table preferences"]')
      .findAll("button")
      .find((button) => button.text().includes("Reload latest"))!
      .trigger("click");
    await flushPromises();

    expect(refreshes).toBe(2);
    expect(
      wrapper.find('[role="dialog"][aria-label="Configure table preferences"]').exists(),
    ).toBe(false);
    expect(
      wrapper.find('[role="dialog"][aria-label="Review saved table filters"]').exists(),
    ).toBe(true);
  });

  it("builds default filters from any eligible column with API-backed values", async () => {
    const wrapper = mountServerTable();

    await wrapper.get("[data-table-preferences-action]").trigger("click");
    await wrapper
      .findAll("button")
      .find((button) => button.text() === "Default filters")!
      .trigger("click");

    const columnSelect = wrapper.get<HTMLSelectElement>(
      '[aria-label="Default filter column"]',
    );
    expect(
      columnSelect.findAll("option").map((option) => option.text()),
    ).toEqual(["Name", "Status"]);
    expect(wrapper.emitted("filter-options-request")?.at(-1)?.[0]).toMatchObject({
      columnKey: "name",
      page: 1,
      search: "",
      filters: {},
    });

    await wrapper.setProps({
      filterOptionPages: {
        ...wrapper.props("filterOptionPages"),
        name: {
          items: [{ label: "Record 11", value: "Record 11", count: 1 }],
          page: 1,
          pageSize: 1,
          totalCount: 2,
          totalPages: 2,
          loading: false,
        },
      },
    });
    await wrapper.get('[data-default-filter-value="Record 11"]').trigger("click");

    await wrapper
      .get('[aria-label="Next default filter values page"]')
      .trigger("click");
    expect(wrapper.emitted("filter-options-request")?.at(-1)?.[0]).toMatchObject({
      columnKey: "name",
      page: 2,
      pageSize: 1,
      filters: { name: ["Record 11"] },
    });
    await wrapper.setProps({
      filterOptionPages: {
        ...wrapper.props("filterOptionPages"),
        name: {
          items: [{ label: "Record 12", value: "Record 12", count: 1 }],
          page: 2,
          pageSize: 1,
          totalCount: 2,
          totalPages: 2,
          loading: false,
        },
      },
    });
    await wrapper.get('[data-default-filter-value="Record 12"]').trigger("click");

    await wrapper.get('input[placeholder="Search Name values"]').setValue("Record");
    await new Promise((resolve) => setTimeout(resolve, 300));
    expect(wrapper.emitted("filter-options-request")?.at(-1)?.[0]).toMatchObject({
      columnKey: "name",
      page: 1,
      search: "Record",
      filters: { name: ["Record 11", "Record 12"] },
    });

    await columnSelect.setValue("status");
    expect(wrapper.emitted("filter-options-request")?.at(-1)?.[0]).toMatchObject({
      columnKey: "status",
      page: 1,
      filters: { name: ["Record 11", "Record 12"] },
    });
    await wrapper.get('[data-default-filter-value="Active"]').trigger("click");

    await wrapper
      .findAll("button")
      .find((button) => button.text() === "Save as my default")!
      .trigger("click");

    expect(wrapper.emitted("query-change")?.at(-1)?.[0]).toMatchObject({
      filters: {
        name: ["Record 11", "Record 12"],
        status: ["Active"],
      },
    });
  });

  it("excludes columns that explicitly prohibit persisted filters", async () => {
    const wrapper = mount(NieDataTable, {
      attachTo: document.body,
      props: {
        columns: [
          ...columns,
          {
            key: "currentSession",
            label: "Current session",
            filter: true,
            persistFilter: false,
          },
        ],
        data: serverPage.map((row) => ({ ...row, currentSession: "This browser" })),
        rowKey: "id",
        serverSide: true,
        totalItems: 2,
        hideActions: true,
        preferenceKey: "test.non-persistable-filter",
        filterOptionPages: {
          currentSession: {
            items: [{ label: "This browser", value: "This browser", count: 2 }],
            page: 1,
            pageSize: 25,
            totalCount: 1,
            totalPages: 1,
            loading: false,
          },
        },
      } as never,
      global: { stubs: { Teleport: true, Transition: false } },
    });

    await wrapper.get("[data-table-preferences-action]").trigger("click");
    await wrapper
      .findAll("button")
      .find((button) => button.text() === "Default filters")!
      .trigger("click");

    expect(
      wrapper
        .get('[aria-label="Default filter column"]')
        .findAll("option")
        .map((option) => option.text()),
    ).toEqual(["Name", "Status"]);
  });

  it("requests searched and paged default-filter values through the API contract", async () => {
    vi.useFakeTimers();
    try {
      const wrapper = mountServerTable();
      await wrapper.get("[data-table-preferences-action]").trigger("click");
      await wrapper
        .findAll("button")
        .find((button) => button.text() === "Default filters")!
        .trigger("click");
      await wrapper.setProps({
        filterOptionPages: {
          ...wrapper.props("filterOptionPages"),
          name: {
            items: [{ label: "Record 11", value: "Record 11", count: 1 }],
            page: 2,
            pageSize: 25,
            totalCount: 61,
            totalPages: 3,
            loading: false,
          },
        },
      });

      await wrapper.get('input[placeholder="Search Name values"]').setValue("record");
      await vi.advanceTimersByTimeAsync(251);
      expect(wrapper.emitted("filter-options-request")?.at(-1)?.[0]).toMatchObject({
        columnKey: "name",
        page: 1,
        search: "record",
      });

      const expectedPages = [1, 1, 3, 3];
      for (const [label, expectedPage] of [
        ["First default filter values page", expectedPages[0]],
        ["Previous default filter values page", expectedPages[1]],
        ["Next default filter values page", expectedPages[2]],
        ["Last default filter values page", expectedPages[3]],
      ] as const) {
        await wrapper.get(`[aria-label="${label}"]`).trigger("click");
        expect(wrapper.emitted("filter-options-request")?.at(-1)?.[0]).toMatchObject({
          columnKey: "name",
          page: expectedPage,
          search: "record",
        });
      }
    } finally {
      vi.useRealTimers();
    }
  });

  it("reports a save failure without misclassifying it as schema repair", async () => {
    const wrapper = mount(NieDataTable, {
      attachTo: document.body,
      props: {
        columns,
        data: serverPage,
        rowKey: "id",
        serverSide: true,
        totalItems: 2,
        hideActions: true,
        preferenceKey: "test.failed-save",
      } as never,
      global: {
        provide: {
          [nieDataTablePreferenceStoreKey as symbol]: {
            get: async () => null,
            refresh: async () => null,
            save: async () => Promise.reject(new Error("unavailable")),
            remove: async () => undefined,
          },
        },
        stubs: { Teleport: true, Transition: false },
      },
    });

    await flushPromises();
    await wrapper.get("[data-table-preferences-action]").trigger("click");
    await wrapper
      .findAll("button")
      .find((button) => button.text() === "Save as my default")!
      .trigger("click");
    await flushPromises();

    const dialog = wrapper.get(
      '[role="dialog"][aria-label="Configure table preferences"]',
    );
    expect(dialog.text()).toContain("couldn't save your table preferences");
    expect(dialog.text()).not.toContain("needs repair");
    expect(dialog.text()).toContain("Save as my default");
  });

  it("requires an explicit reload before saving over a concurrent change", async () => {
    const revisions: Array<number | undefined> = [];
    let refreshes = 0;
    const latestSettings: NieDataTablePreferenceSettings = {
      pageSize: 20,
      sorts: [{ key: "name", direction: "desc" }],
      filters: { status: ["Pending"] },
      columnOrder: ["name", "status"],
      hiddenColumns: [],
      density: "compact",
      appearance: "elevated",
    };
    const wrapper = mount(NieDataTable, {
      attachTo: document.body,
      props: {
        columns,
        data: serverPage,
        rowKey: "id",
        serverSide: true,
        totalItems: 2,
        hideActions: true,
        preferenceKey: "test.conflict",
      } as never,
      global: {
        provide: {
          [nieDataTablePreferenceStoreKey as symbol]: {
            get: async () => null,
            refresh: async () => {
              refreshes += 1;
              return {
                tableKey: "test.conflict",
                definitionVersion: 1,
                revision: 4,
                settings: latestSettings,
              };
            },
            save: async (
              tableKey: string,
              definitionVersion: number,
              settings: NieDataTablePreferenceSettings,
              revision?: number,
            ) => {
              revisions.push(revision);
              if (revisions.length === 1) {
                throw new NieDataTablePreferenceConflictError();
              }
              return {
                tableKey,
                definitionVersion,
                revision: 5,
                settings,
              };
            },
            remove: async () => undefined,
          },
        },
        stubs: { Teleport: true, Transition: false },
      },
    });

    await flushPromises();
    await wrapper.get("[data-table-preferences-action]").trigger("click");
    await wrapper
      .findAll("button")
      .find((button) => button.text() === "Save as my default")!
      .trigger("click");
    await flushPromises();

    expect(wrapper.text()).toContain("Saved view changed elsewhere");
    const blockedSave = wrapper
      .findAll("button")
      .find((button) => button.text() === "Save as my default")!;
    expect(blockedSave.attributes("disabled")).toBeDefined();

    await wrapper
      .findAll("button")
      .find((button) => button.text() === "Reload latest")!
      .trigger("click");
    await flushPromises();
    expect(refreshes).toBe(1);
    expect(wrapper.text()).not.toContain("Saved view changed elsewhere");
    expect(wrapper.emitted("query-change")?.at(-1)?.[0]).toMatchObject({
      sorts: [{ key: "name", direction: "desc" }],
      filters: { status: ["Pending"] },
    });

    const rebasedSave = wrapper
      .findAll("button")
      .find((button) => button.text() === "Save as my default")!;
    expect(rebasedSave.attributes("disabled")).toBeUndefined();
    await rebasedSave.trigger("click");
    await flushPromises();
    expect(revisions).toEqual([undefined, 4]);
  });

  it("keeps screen defaults after a load failure without entering repair mode", async () => {
    const wrapper = mount(NieDataTable, {
      attachTo: document.body,
      props: {
        columns,
        data: serverPage,
        rowKey: "id",
        serverSide: true,
        totalItems: 2,
        hideActions: true,
        preferenceKey: "test.failed-load",
      } as never,
      global: {
        provide: {
          [nieDataTablePreferenceStoreKey as symbol]: {
            get: async () => Promise.reject(new Error("unavailable")),
            refresh: async () => Promise.reject(new Error("unavailable")),
            save: async () => Promise.reject(new Error("unavailable")),
            remove: async () => undefined,
          },
        },
        stubs: { Teleport: true, Transition: false },
      },
    });

    await flushPromises();
    const warning = wrapper.get("[data-table-preference-warning]");
    expect(warning.text()).toContain("saved table view could not be loaded");
    await warning.get("button").trigger("click");

    const dialog = wrapper.get(
      '[role="dialog"][aria-label="Configure table preferences"]',
    );
    expect(dialog.text()).toContain("Saved view unavailable");
    expect(dialog.text()).not.toContain("needs repair");
    expect(dialog.text()).toContain("Save as my default");
    expect(wrapper.emitted("query-change")?.at(-1)?.[0]).toMatchObject({
      pageSize: 20,
      filters: {},
    });
  });

  it("keeps ordered multi-column sorts and exposes their priority", async () => {
    const wrapper = mountServerTable();
    const sortButtons = wrapper.findAll(".data-table-sort-button");

    await sortButtons[0]!.trigger("click");
    await sortButtons[1]!.trigger("click", { shiftKey: true });

    expect(wrapper.emitted("query-change")?.at(-1)?.[0]).toMatchObject({
      sorts: [
        { key: "name", direction: "asc" },
        { key: "status", direction: "asc" },
      ],
    });
    expect(wrapper.findAll("[data-sort-priority]").map((item) => item.text())).toEqual([
      "1",
      "2",
    ]);
  });

  it("warns about incompatible stored settings and opens repair in the preferences dialog", async () => {
    const wrapper = mount(NieDataTable, {
      attachTo: document.body,
      props: {
        columns,
        data: serverPage,
        rowKey: "id",
        serverSide: true,
        totalItems: 2,
        hideActions: true,
        preferenceKey: "test.records",
        definitionVersion: 2,
        preferenceState: {
          repairRequired: true,
          reasons: ["A saved column is no longer available."],
        },
      } as never,
      global: { stubs: { Teleport: true, Transition: false } },
    });

    const warning = wrapper.get('[data-table-preference-warning]');
    expect(warning.text()).toContain("saved table view needs attention");
    await warning.get("button").trigger("click");
    expect(
      wrapper
        .get('[role="dialog"][aria-label="Configure table preferences"]')
        .text(),
    ).toContain("Repair and save");
  });

  it("normalizes a changed stored JSON definition before the first API query and repairs it explicitly", async () => {
    const saved: NieDataTablePreferenceSettings[] = [];
    const incompatible: NieDataTablePreferenceRecord = {
      tableKey: "test.records",
      definitionVersion: 1,
      revision: 4,
      settings: {
        pageSize: 50,
        sorts: [
          { key: "removedColumn", direction: "asc" },
          { key: "name", direction: "desc" },
        ],
        filters: {
          removedColumn: ["legacy"],
          status: ["Active", "Active"],
        },
        columnOrder: ["removedColumn", "name", "name"],
        hiddenColumns: ["removedColumn"],
        density: "compact",
        appearance: "striped",
      },
    };
    const wrapper = mount(NieDataTable, {
      attachTo: document.body,
      props: {
        columns,
        data: serverPage,
        rowKey: "id",
        serverSide: true,
        totalItems: 2,
        hideActions: true,
        preferenceKey: "test.records",
        definitionVersion: 2,
      },
      global: {
        provide: {
          [nieDataTablePreferenceStoreKey as symbol]: {
            get: async () => incompatible,
            refresh: async () => incompatible,
            save: async (_key: string, definitionVersion: number, settings: NieDataTablePreferenceSettings) => {
              saved.push(settings);
              return { ...incompatible, definitionVersion, revision: 5, settings };
            },
            remove: async () => undefined,
          },
        },
        stubs: { Teleport: true, Transition: false },
      },
    });

    await flushPromises();
    expect(wrapper.get("[data-table-preference-warning]").text()).toContain(
      "saved table view needs attention",
    );
    expect(wrapper.emitted("query-change")?.at(-1)?.[0]).toMatchObject({
      pageSize: 50,
      sorts: [{ key: "name", direction: "desc" }],
      filters: { status: ["Active"] },
    });

    await wrapper.get("[data-table-preference-warning] button").trigger("click");
    expect(wrapper.text()).toContain("duplicate saved column");
    expect(wrapper.text()).toContain("duplicate saved filter value");
    const saveButton = wrapper
      .findAll("button")
      .find((button) => button.text().includes("Repair and save"));
    expect(saveButton).toBeDefined();
    await saveButton!.trigger("click");
    await flushPromises();

    expect(saved[0]).toMatchObject({
      columnOrder: ["name", "status"],
      hiddenColumns: [],
    });
  });

  it("renders the complete server page and reports the server total in the top toolbar", () => {
    const wrapper = mountServerTable();

    expect(wrapper.text()).toContain("Record 11");
    expect(wrapper.text()).toContain("Record 12");
    expect(wrapper.get("[data-table-total-results]").text()).toBe("64 results");
    expect(wrapper.text()).not.toContain("Showing 11 to 20");
  });

  it("shows only the current page between first, previous, next, and last controls", async () => {
    const wrapper = mountServerTable();
    const pagination = wrapper.get("[data-pagination-pages]");

    expect(pagination.get("[data-pagination-current-page]").text()).toBe("2");
    expect(pagination.findAll("button")).toHaveLength(4);

    await pagination.get('[aria-label="First page"]').trigger("click");
    expect(wrapper.emitted("update:page")?.at(-1)?.[0]).toBe(1);

    await pagination.get('[aria-label="Last page"]').trigger("click");
    expect(wrapper.emitted("update:page")?.at(-1)?.[0]).toBe(7);
  });

  it("pins every desktop header cell with no scrollable gap above it", () => {
    const wrapper = mountServerTable();

    expect(wrapper.get("thead").classes()).not.toContain("sticky");
    expect(wrapper.findAll("thead th")).not.toHaveLength(0);
    expect(
      wrapper.findAll("thead th").every((cell) => cell.classes().includes("sticky")),
    ).toBe(true);
    expect(wrapper.get(".data-table-body").classes()).not.toContain("pt-6");
  });

  it("keeps the filter open while scrolling and after selecting multiple values", async () => {
    const wrapper = mountServerTable();
    const trigger = wrapper.get('[aria-label="Filter Status"]');

    await trigger.trigger("click");
    expect(wrapper.find('[role="dialog"][aria-label="Filter Status values"]').exists()).toBe(true);

    window.dispatchEvent(new Event("scroll"));
    await wrapper.vm.$nextTick();
    expect(wrapper.find('[role="dialog"][aria-label="Filter Status values"]').exists()).toBe(true);

    await wrapper.get('[data-filter-value="Active"]').trigger("click");
    await wrapper.setProps({ loading: true });
    await wrapper.vm.$nextTick();
    expect(
      wrapper
        .find('[role="dialog"][aria-label="Filter Status values"]')
        .exists(),
    ).toBe(true);

    await wrapper.get('[data-filter-value="Pending"]').trigger("click");
    expect(wrapper.find('[role="dialog"][aria-label="Filter Status values"]').exists()).toBe(true);

    const updates = wrapper.emitted("update:selectedFilters") ?? [];
    expect(updates.at(-1)?.[0]).toEqual({ status: ["Active", "Pending"] });
  });

  it("requests API-backed option pages and emits one complete row query", async () => {
    const wrapper = mountServerTable();
    await wrapper.get('[aria-label="Filter Status"]').trigger("click");

    expect(wrapper.emitted("filter-options-request")?.[0]?.[0]).toMatchObject({
      columnKey: "status",
      page: 1,
      pageSize: 2,
      search: "",
      filters: {},
    });

    await wrapper.get('[data-filter-value="Active"]').trigger("click");
    expect(wrapper.emitted("query-change")?.at(-1)?.[0]).toMatchObject({
      page: 1,
      pageSize: 10,
      filters: { status: ["Active"] },
    });

    await wrapper.get('[aria-label="Next filter values page"]').trigger("click");
    expect(wrapper.emitted("filter-options-request")?.at(-1)?.[0]).toMatchObject({
      columnKey: "status",
      page: 2,
      pageSize: 2,
    });
  });

  it("closes the filter only after an outside pointer interaction", async () => {
    const wrapper = mountServerTable();
    await wrapper.get('[aria-label="Filter Status"]').trigger("click");

    document.body.dispatchEvent(new MouseEvent("mousedown", { bubbles: true }));
    await wrapper.vm.$nextTick();

    expect(wrapper.find('[role="dialog"][aria-label="Filter Status values"]').exists()).toBe(false);
  });

  it("closes the filter with Escape and restores focus to its trigger", async () => {
    const wrapper = mountServerTable();
    const trigger = wrapper.get<HTMLButtonElement>('[aria-label="Filter Status"]');
    await trigger.trigger("click");

    document.dispatchEvent(
      new KeyboardEvent("keydown", { key: "Escape", bubbles: true }),
    );
    await wrapper.vm.$nextTick();

    expect(wrapper.find('[role="dialog"][aria-label="Filter Status values"]').exists()).toBe(false);
    expect(document.activeElement).toBe(trigger.element);
  });
});

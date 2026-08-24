import { flushPromises, mount } from "@vue/test-utils";
import { afterEach, describe, expect, it, vi } from "vitest";
import { h, nextTick } from "vue";
import NieDataTable from "../NieDataTable.vue";
import {
  NieDataTablePreferenceConflictError,
  nieDataTablePreferenceStoreKey,
} from "../preferences";
import type {
  NieDataTableColumn,
  NieDataTablePreferenceRecord,
  NieDataTablePreferenceSettings,
  NieDataTablePreferenceStore,
  NieDataTableQuery,
} from "../types";

interface Order extends Record<string, unknown> {
  id: string;
  reference: string;
  vendor: string;
  amount: number;
  approved: boolean;
  status: string;
}

const columns: NieDataTableColumn[] = [
  { key: "reference", label: "Reference" },
  { key: "vendor", label: "Vendor" },
  { key: "amount", label: "Amount", type: "number", decimals: 2 },
  { key: "approved", label: "Approved", type: "boolean", filter: false },
  {
    key: "status",
    label: "Status",
    chip: { toneMap: { open: "warning" }, dot: true },
  },
];

const rows: Order[] = [
  {
    id: "1",
    reference: "PO-3",
    vendor: "Acme",
    amount: 300.5,
    approved: true,
    status: "open",
  },
  {
    id: "2",
    reference: "PO-1",
    vendor: "Bolt",
    amount: 100,
    approved: false,
    status: "closed",
  },
  {
    id: "3",
    reference: "PO-2",
    vendor: "Acme",
    amount: 200,
    approved: true,
    status: "open",
  },
];

type TableRow = Record<string, unknown>;
type TableProps = Parameters<typeof NieDataTable<TableRow>>[0];

function mountTable(
  props: Partial<TableProps> = {},
  store?: NieDataTablePreferenceStore,
) {
  return mount(NieDataTable<TableRow>, {
    attachTo: document.body,
    global: store
      ? { provide: { [nieDataTablePreferenceStoreKey]: store } }
      : {},
    props: {
      columns,
      data: rows,
      rowKey: "id",
      ...props,
    } as TableProps,
  });
}

const baseProps = { columns, data: rows, rowKey: "id" };

function lastEmitted(
  wrapper: ReturnType<typeof mountTable>,
  event: string,
): unknown[] | undefined {
  const emitted = wrapper.emitted(event) ?? [];
  return emitted[emitted.length - 1];
}

function bodyRows(wrapper: ReturnType<typeof mountTable>) {
  return wrapper.findAll("tbody tr");
}

function cellTexts(wrapper: ReturnType<typeof mountTable>, index = 0) {
  const cells = bodyRows(wrapper)[index].findAll("td");
  const hasActionsColumn = wrapper.find("tbody .data-table-edit-action").exists();
  return (hasActionsColumn ? cells.slice(0, -1) : cells).map((cell) =>
    cell.text(),
  );
}

function headerButtons(wrapper: ReturnType<typeof mountTable>) {
  return wrapper.findAll(".data-table-sort-button");
}

function headerLabels(wrapper: ReturnType<typeof mountTable>) {
  return headerButtons(wrapper).map((button) => button.get("span").text());
}

function settings(
  overrides: Partial<NieDataTablePreferenceSettings> = {},
): NieDataTablePreferenceSettings {
  return {
    pageSize: 20,
    sorts: [],
    filters: {},
    filterReminderAcknowledgedAtUtc: null,
    columnOrder: columns.map((column) => column.key),
    hiddenColumns: [],
    density: "comfortable",
    appearance: "elevated",
    ...overrides,
  };
}

function record(
  overrides: Partial<NieDataTablePreferenceRecord> = {},
): NieDataTablePreferenceRecord {
  return {
    tableKey: "orders",
    definitionVersion: 1,
    revision: 4,
    settings: settings(),
    ...overrides,
  };
}

function createStore(
  overrides: Partial<NieDataTablePreferenceStore> = {},
): NieDataTablePreferenceStore {
  return {
    get: vi.fn().mockResolvedValue(null),
    refresh: vi.fn().mockResolvedValue(null),
    save: vi.fn().mockResolvedValue(record()),
    remove: vi.fn().mockResolvedValue(undefined),
    ...overrides,
  };
}

function preferencesModalButton(text: string): HTMLButtonElement {
  const match = [
    ...document.querySelectorAll<HTMLButtonElement>("footer button"),
  ].find((button) => button.textContent?.trim() === text);
  if (!match) throw new Error(`No preferences button labelled ${text}`);
  return match;
}

afterEach(() => {
  document.body.innerHTML = "";
  document.body.style.overflow = "";
  vi.useRealTimers();
});

describe("NieDataTable rendering", () => {
  it("renders one header and one row per record", () => {
    const wrapper = mountTable();

    expect(headerLabels(wrapper)).toEqual([
      "Reference",
      "Vendor",
      "Amount",
      "Approved",
      "Status",
    ]);
    expect(bodyRows(wrapper)).toHaveLength(3);
    wrapper.unmount();
  });

  it("formats each column according to its type", () => {
    const wrapper = mountTable();

    expect(cellTexts(wrapper)).toEqual([
      "PO-3",
      "Acme",
      "300.50",
      "Yes",
      "open",
    ]);
    expect(cellTexts(wrapper, 1)[2]).toBe("100");
    expect(cellTexts(wrapper, 1)[3]).toBe("No");
    wrapper.unmount();
  });

  it("renders a dash for missing values", () => {
    const wrapper = mountTable({
      data: [{ id: "1", reference: null, vendor: undefined }],
    });

    expect(cellTexts(wrapper)[0]).toBe("-");
    expect(cellTexts(wrapper)[1]).toBe("-");
    wrapper.unmount();
  });

  it("uses a column formatter when one is supplied", () => {
    const wrapper = mountTable({
      columns: [
        {
          key: "amount",
          label: "Amount",
          format: (value: unknown) => `SGD ${String(value)}`,
        },
      ],
    });

    expect(cellTexts(wrapper)[0]).toBe("SGD 300.5");
    wrapper.unmount();
  });

  it("formats dates and non-numeric numbers defensively", () => {
    const wrapper = mountTable({
      columns: [
        { key: "created", label: "Created", type: "date" },
        { key: "amount", label: "Amount", type: "number" },
      ],
      data: [{ id: "1", created: "2026-08-07T00:00:00.000Z", amount: "n/a" }],
    });

    expect(cellTexts(wrapper)[0]).toBe(
      new Date("2026-08-07T00:00:00.000Z").toLocaleDateString(),
    );
    expect(cellTexts(wrapper)[1]).toBe("n/a");
    wrapper.unmount();
  });

  it("supports numeric row keys and preserves unparseable date values", () => {
    const date = new Date("2026-08-07T00:00:00.000Z");
    const wrapper = mountTable({
      columns: [{ key: "created", label: "Created", type: "date" }],
      data: [
        { id: 7, created: date },
        { id: { source: "legacy" }, created: { source: "unknown" } },
      ],
    });

    expect(cellTexts(wrapper)[0]).toBe(date.toLocaleDateString());
    expect(cellTexts(wrapper, 1)[0]).toBe("[object Object]");
    wrapper.unmount();
  });

  it("renders chip columns as badges with their configured tone", () => {
    const wrapper = mountTable();
    const chips = wrapper.findAll("tbody [data-testid='nie-data-table-chip']");

    expect(chips).toHaveLength(3);
    expect(chips[0].classes()).toContain("nie-badge--warning");
    expect(chips[1].classes()).toContain("nie-badge--default");
    expect(chips[0].find("[data-testid='nie-badge-dot']").exists()).toBe(true);
    wrapper.unmount();
  });

  it("supports a chip label formatter", () => {
    const wrapper = mountTable({
      columns: [
        {
          key: "status",
          label: "Status",
          chip: { label: (value: unknown) => String(value).toUpperCase() },
        },
      ],
    });

    expect(
      wrapper.get("tbody [data-testid='nie-data-table-chip']").text(),
    ).toBe("OPEN");
    wrapper.unmount();
  });

  it("lets a cell slot override the default rendering", () => {
    const wrapper = mount(NieDataTable<Order>, {
      attachTo: document.body,
      props: baseProps,
      slots: {
        "cell-reference": (slotProps: { row: Order }) =>
          h("span", { class: "custom" }, `#${slotProps.row.reference}`),
      },
    });

    expect(wrapper.get("tbody .custom").text()).toBe("#PO-3");
    wrapper.unmount();
  });

  it("reports the result count as a live region", () => {
    const wrapper = mountTable();
    const total = wrapper.get("[data-table-total-results]");

    expect(total.attributes("role")).toBe("status");
    expect(total.attributes("aria-live")).toBe("polite");
    expect(total.text()).toBe("3 results");
    wrapper.unmount();
  });

  it("uses the singular result noun for one record", () => {
    const wrapper = mountTable({ data: [rows[0]] });

    expect(wrapper.get("[data-table-total-results]").text()).toBe("1 result");
    wrapper.unmount();
  });
});

describe("NieDataTable states", () => {
  it("shows a loading state while there is nothing to display", () => {
    const wrapper = mountTable({ data: null, loading: true });

    expect(wrapper.get("[data-testid='nie-result-state']").attributes("aria-live")).toBe(
      "polite",
    );
    expect(wrapper.get(".data-table-body").attributes("aria-busy")).toBe("true");
    wrapper.unmount();
  });

  it("keeps the rows and announces a background refresh", () => {
    const wrapper = mountTable({ loading: true });

    expect(bodyRows(wrapper)).toHaveLength(3);
    expect(wrapper.get(".sr-only").text()).toBe("Updating table data...");
    wrapper.unmount();
  });

  it("shows a generic error with a retry action", async () => {
    const wrapper = mountTable({ error: "Something broke" });

    expect(wrapper.get("h1").text()).toBe("Unable to load records");
    await wrapper.get("[data-testid='nie-result-state'] button").trigger("click");

    expect(wrapper.emitted("retry")).toHaveLength(1);
    wrapper.unmount();
  });

  it("recognises a supported HTTP status inside the error text", () => {
    const wrapper = mountTable({ error: "Request failed with status code 403" });

    expect(
      wrapper.get("[data-testid='nie-result-state']").attributes("data-result-status"),
    ).toBe("403");
    expect(wrapper.get("h1").text()).toBe("Access denied");
    wrapper.unmount();
  });

  it("prefers an explicit error status", () => {
    const wrapper = mountTable({ error: "Boom", errorStatus: 503 });

    expect(
      wrapper.get("[data-testid='nie-result-state']").attributes("data-result-status"),
    ).toBe("503");
    wrapper.unmount();
  });

  it("ignores an unsupported status code in the error text", () => {
    const wrapper = mountTable({ error: "Failed with status 418" });

    expect(wrapper.get("h1").text()).toBe("Unable to load records");
    wrapper.unmount();
  });

  it("shows the configured empty state", () => {
    const wrapper = mountTable({
      data: [],
      emptyStateTitle: "No purchase orders",
      emptyStateMessage: "Raise one to get started.",
    });

    expect(wrapper.get("[data-table-result-frame] h1").text()).toBe(
      "No purchase orders",
    );
    expect(wrapper.text()).toContain("Raise one to get started.");
    wrapper.unmount();
  });

  it("explains an empty result caused by the active query", async () => {
    vi.useFakeTimers();
    const wrapper = mountTable();

    await wrapper.get('input[type="search"]').setValue("nothing-matches");

    expect(wrapper.get("[data-table-result-frame] h1").text()).toBe(
      "No matching records",
    );
    expect(wrapper.text()).toContain(
      "Try changing or clearing the active search and filters.",
    );
    wrapper.unmount();
  });
});

describe("NieDataTable sorting", () => {
  it("cycles a column through ascending, descending and unsorted", async () => {
    const wrapper = mountTable();

    await headerButtons(wrapper)[0].trigger("click");
    expect(bodyRows(wrapper).map((row) => row.findAll("td")[0].text())).toEqual([
      "PO-1",
      "PO-2",
      "PO-3",
    ]);
    expect(wrapper.get("[data-sort-priority]").text()).toBe("1");

    await headerButtons(wrapper)[0].trigger("click");
    expect(bodyRows(wrapper).map((row) => row.findAll("td")[0].text())).toEqual([
      "PO-3",
      "PO-2",
      "PO-1",
    ]);

    await headerButtons(wrapper)[0].trigger("click");
    expect(bodyRows(wrapper).map((row) => row.findAll("td")[0].text())).toEqual([
      "PO-3",
      "PO-1",
      "PO-2",
    ]);
    expect(wrapper.find("[data-sort-priority]").exists()).toBe(false);
    wrapper.unmount();
  });

  it("replaces the sort unless shift is held", async () => {
    const wrapper = mountTable();

    await headerButtons(wrapper)[0].trigger("click");
    await headerButtons(wrapper)[1].trigger("click");
    expect(wrapper.findAll("[data-sort-priority]")).toHaveLength(1);

    await headerButtons(wrapper)[2].trigger("click", { shiftKey: true });
    expect(wrapper.findAll("[data-sort-priority]").map((n) => n.text())).toEqual(
      ["1", "2"],
    );
    wrapper.unmount();
  });

  it("sorts numbers numerically and booleans by truthiness", async () => {
    const wrapper = mountTable();

    await headerButtons(wrapper)[2].trigger("click");
    expect(bodyRows(wrapper).map((row) => row.findAll("td")[2].text())).toEqual([
      "100",
      "200",
      "300.50",
    ]);

    await headerButtons(wrapper)[3].trigger("click");
    expect(bodyRows(wrapper).map((row) => row.findAll("td")[3].text())).toEqual([
      "No",
      "Yes",
      "Yes",
    ]);
    wrapper.unmount();
  });

  it("pushes blank values to the end", async () => {
    const wrapper = mountTable({
      columns: [{ key: "vendor", label: "Vendor" }],
      data: [
        { id: "1", vendor: "Bolt" },
        { id: "2", vendor: null },
        { id: "3", vendor: "Acme" },
      ],
    });

    await headerButtons(wrapper)[0].trigger("click");

    expect(bodyRows(wrapper).map((row) => row.findAll("td")[0].text())).toEqual([
      "Acme",
      "Bolt",
      "-",
    ]);
    wrapper.unmount();
  });

  it("sorts dates chronologically", async () => {
    const wrapper = mountTable({
      columns: [{ key: "created", label: "Created", type: "date" }],
      data: [
        { id: "1", created: "2026-03-01" },
        { id: "2", created: "2025-01-01" },
        { id: "3", created: "2026-01-01" },
      ],
    });

    await headerButtons(wrapper)[0].trigger("click");

    expect(bodyRows(wrapper)[0].findAll("td")[0].text()).toBe(
      new Date("2025-01-01").toLocaleDateString(),
    );
    wrapper.unmount();
  });

  it("ignores a column that opts out of sorting", async () => {
    const wrapper = mountTable({
      columns: [{ key: "vendor", label: "Vendor", sortable: false }],
    });

    await headerButtons(wrapper)[0].trigger("click");

    expect(wrapper.find("[data-sort-priority]").exists()).toBe(false);
    wrapper.unmount();
  });
});

describe("NieDataTable search and filters", () => {
  it("filters rows by the search term across every column", async () => {
    vi.useFakeTimers();
    const wrapper = mountTable();

    await wrapper.get('input[type="search"]').setValue("acme");

    expect(bodyRows(wrapper)).toHaveLength(2);
    expect(wrapper.emitted("update:search")).toEqual([["acme"]]);
    wrapper.unmount();
  });

  it("debounces the search event", async () => {
    vi.useFakeTimers();
    const wrapper = mountTable();

    await wrapper.get('input[type="search"]').setValue("acme");
    expect(wrapper.emitted("search")).toBeUndefined();

    vi.advanceTimersByTime(250);
    expect(wrapper.emitted("search")).toEqual([["acme"]]);
    wrapper.unmount();
  });

  it("uses a custom search accessor when one is supplied", async () => {
    vi.useFakeTimers();
    const wrapper = mountTable({
      searchAccessor: (row) => [row.reference],
    });

    await wrapper.get('input[type="search"]').setValue("acme");

    expect(bodyRows(wrapper)).toHaveLength(0);
    wrapper.unmount();
  });

  it("searches inside array values", async () => {
    vi.useFakeTimers();
    const wrapper = mountTable({
      columns: [{ key: "tags", label: "Tags" }],
      data: [
        { id: "1", tags: ["urgent", "capex"] },
        { id: "2", tags: ["opex"] },
      ],
    });

    await wrapper.get('input[type="search"]').setValue("capex");

    expect(bodyRows(wrapper)).toHaveLength(1);
    wrapper.unmount();
  });

  it("can turn searching off entirely", () => {
    const wrapper = mountTable({ searchable: false });

    expect(wrapper.find('input[type="search"]').exists()).toBe(false);
    wrapper.unmount();
  });

  it("derives a column filter menu with counts from the data", async () => {
    const wrapper = mountTable();

    const vendorMenu = wrapper.get('[aria-label="Filter Vendor"]');
    await vendorMenu.trigger("click");
    await nextTick();

    const values = [
      ...document.querySelectorAll<HTMLButtonElement>("[data-filter-value]"),
    ];
    expect(values.map((button) => button.dataset.filterValue)).toEqual([
      "Acme",
      "Bolt",
    ]);
    expect(values[0].textContent).toContain("2");
    wrapper.unmount();
  });

  it("omits a filter menu for columns that opt out", () => {
    const wrapper = mountTable();

    expect(wrapper.find('[aria-label="Filter Approved"]').exists()).toBe(false);
    wrapper.unmount();
  });

  it("applies a column filter selection to the rows", async () => {
    const wrapper = mountTable();
    await wrapper.get('[aria-label="Filter Vendor"]').trigger("click");
    await nextTick();

    document
      .querySelector<HTMLButtonElement>('[data-filter-value="Bolt"]')
      ?.click();
    await nextTick();

    expect(bodyRows(wrapper)).toHaveLength(1);
    expect(wrapper.emitted("update:selectedFilters")).toEqual([
      [{ vendor: ["Bolt"] }],
    ]);
    wrapper.unmount();
  });

  it("clearing a column filter removes the key entirely", async () => {
    const wrapper = mountTable({ selectedFilters: { vendor: ["Bolt"] } });
    await wrapper.get('[aria-label="Filter Vendor"]').trigger("click");
    await nextTick();

    document
      .querySelector<HTMLButtonElement>('[data-filter-value="Bolt"]')
      ?.click();
    await nextTick();

    expect(lastEmitted(wrapper, "update:selectedFilters")).toEqual([{}]);
    wrapper.unmount();
  });

  it("honours filters supplied by the host", () => {
    const wrapper = mountTable({ selectedFilters: { vendor: ["Acme"] } });

    expect(bodyRows(wrapper)).toHaveLength(2);
    wrapper.unmount();
  });

  it("merges caller-supplied filter groups with the derived counts", async () => {
    const wrapper = mountTable({
      filterGroups: [
        {
          key: "status",
          label: "Order status",
          options: [
            { label: "Open", value: "open" },
            { label: "Cancelled", value: "cancelled" },
          ],
        },
      ],
    });

    // The header menu is labelled by the column, not by the filter group.
    await wrapper.get('[aria-label="Filter Status"]').trigger("click");
    await nextTick();

    const values = [
      ...document.querySelectorAll<HTMLButtonElement>("[data-filter-value]"),
    ];
    expect(values.map((button) => button.dataset.filterValue)).toEqual([
      "open",
      "cancelled",
      "closed",
    ]);
    expect(values[1].disabled).toBe(true);
    wrapper.unmount();
  });

  it("drops filter menus once there are no rows and no selection", () => {
    const wrapper = mountTable({ data: [] });

    expect(wrapper.find("[aria-label^='Filter ']").exists()).toBe(false);
    wrapper.unmount();
  });
});

describe("NieDataTable pagination", () => {
  it("pages the data client-side", async () => {
    const wrapper = mountTable({ pageSize: 2 });

    expect(bodyRows(wrapper)).toHaveLength(2);
    await wrapper.get('[aria-label="Next page"]').trigger("click");

    expect(bodyRows(wrapper)).toHaveLength(1);
    expect(wrapper.emitted("update:page")).toEqual([[2]]);
    wrapper.unmount();
  });

  it("changes the page size and returns to the first page", async () => {
    const wrapper = mountTable({ pageSize: 10 });

    await wrapper.get("select").setValue("50");

    expect(wrapper.emitted("update:pageSize")).toEqual([[50]]);
    expect(wrapper.emitted("update:page")).toEqual([[1]]);
    wrapper.unmount();
  });

  it("clamps the current page when the data shrinks", async () => {
    const wrapper = mountTable({ pageSize: 2 });
    await wrapper.get('[aria-label="Next page"]').trigger("click");

    await wrapper.setProps({ data: [rows[0]] });
    await nextTick();

    expect(lastEmitted(wrapper, "update:page")).toEqual([1]);
    wrapper.unmount();
  });

  it("follows the page prop when the host controls it", async () => {
    const wrapper = mountTable({ pageSize: 1, page: 1 });

    await wrapper.setProps({ page: 3 });
    await nextTick();

    expect(bodyRows(wrapper)[0].findAll("td")[0].text()).toBe("PO-2");
    wrapper.unmount();
  });

  it("hides the footer when there is nothing to page through", () => {
    const wrapper = mountTable({ data: [] });

    expect(wrapper.find("[data-table-pagination-footer]").exists()).toBe(false);
    wrapper.unmount();
  });

  it("always shows the footer for server-side tables", () => {
    const wrapper = mountTable({ serverSide: true, data: [], totalItems: 0 });

    expect(wrapper.find("[data-table-pagination-footer]").exists()).toBe(true);
    wrapper.unmount();
  });
});

describe("NieDataTable row actions", () => {
  it("emits create, edit and delete", async () => {
    const wrapper = mountTable();

    await wrapper.get("[data-table-create-action]").trigger("click");
    await wrapper.get("tbody .data-table-edit-action").trigger("click");
    await wrapper.get("tbody .data-table-delete-action").trigger("click");

    expect(wrapper.emitted("create")).toHaveLength(1);
    expect(wrapper.emitted("edit")).toEqual([[rows[0]]]);
    expect(wrapper.emitted("delete")).toEqual([[rows[0]]]);
    wrapper.unmount();
  });

  it("hides the actions the host does not want", () => {
    const wrapper = mountTable({
      hideCreate: true,
      hideEdit: true,
      hideDelete: true,
    });

    expect(wrapper.find("[data-table-create-action]").exists()).toBe(false);
    expect(wrapper.find(".data-table-edit-action").exists()).toBe(false);
    expect(wrapper.find(".data-table-delete-action").exists()).toBe(false);
    wrapper.unmount();
  });

  it("hides the whole actions column", () => {
    const wrapper = mountTable({ hideActions: true });

    expect(wrapper.findAll("thead th")).toHaveLength(5);
    wrapper.unmount();
  });

  it("disables deletion per row and explains why", () => {
    const wrapper = mountTable({
      canDelete: (row) => row.status !== "closed",
      deleteDisabledTitle: () => "Closed orders cannot be deleted",
    });
    const buttons = wrapper.findAll("tbody .data-table-delete-action");

    expect(buttons[0].attributes("disabled")).toBeUndefined();
    expect(buttons[0].attributes("title")).toBe("Delete");
    expect(buttons[1].attributes("disabled")).toBeDefined();
    expect(buttons[1].attributes("title")).toBe(
      "Closed orders cannot be deleted",
    );
    wrapper.unmount();
  });

  it("treats a failing canDelete callback as deletable", () => {
    const wrapper = mountTable({
      canDelete: () => {
        throw new Error("boom");
      },
    });

    expect(
      wrapper.get("tbody .data-table-delete-action").attributes("disabled"),
    ).toBeUndefined();
    wrapper.unmount();
  });

  it("renders the extra-actions slot per row", () => {
    const wrapper = mount(NieDataTable<Order>, {
      attachTo: document.body,
      props: baseProps,
      slots: {
        "extra-actions": (slotProps: { row: Order }) =>
          h(
            "button",
            { type: "button", class: "extra" },
            slotProps.row.reference,
          ),
      },
    });

    expect(wrapper.findAll("tbody .extra")).toHaveLength(3);
    wrapper.unmount();
  });

  it("renders the toolbar-actions slot with the filtered rows", () => {
    const wrapper = mount(NieDataTable<Order>, {
      attachTo: document.body,
      props: baseProps,
      slots: {
        "toolbar-actions": (slotProps: { filteredData: Order[] }) =>
          h("span", { class: "count" }, String(slotProps.filteredData.length)),
      },
    });

    expect(wrapper.get(".count").text()).toBe("3");
    wrapper.unmount();
  });
});

describe("NieDataTable row activation", () => {
  it("adds no row affordances unless rows are clickable", async () => {
    const wrapper = mountTable();

    expect(bodyRows(wrapper)[0].attributes("tabindex")).toBeUndefined();
    await bodyRows(wrapper)[0].trigger("click");
    expect(wrapper.emitted("row-click")).toBeUndefined();
    wrapper.unmount();
  });

  it("makes clickable rows keyboard reachable with a generated label", async () => {
    const wrapper = mountTable({ rowClickable: true });
    const row = bodyRows(wrapper)[0];

    expect(row.attributes("tabindex")).toBe("0");
    expect(row.attributes("aria-label")).toBe("Open record 1");

    await row.trigger("click");
    await row.trigger("keydown.enter");
    await row.trigger("keydown.space");
    expect(wrapper.emitted("row-click")).toHaveLength(3);
    wrapper.unmount();
  });

  it("uses a caller-supplied row label and survives a failing one", () => {
    const labelled = mountTable({
      rowClickable: true,
      rowAriaLabel: (row: unknown) => `Open ${(row as Order).reference}`,
    });
    expect(bodyRows(labelled)[0].attributes("aria-label")).toBe("Open PO-3");
    labelled.unmount();

    const failing = mountTable({
      rowClickable: true,
      rowAriaLabel: () => {
        throw new Error("boom");
      },
    });
    expect(bodyRows(failing)[0].attributes("aria-label")).toBe("Open record 1");
    failing.unmount();
  });

  it("falls back to a generic label without a row key value", () => {
    const wrapper = mountTable({
      rowClickable: true,
      data: [{ id: "", reference: "PO-9" }],
    });

    expect(bodyRows(wrapper)[0].attributes("aria-label")).toBe("Open record");
    wrapper.unmount();
  });

  it("lets a nested control handle its own click", async () => {
    const wrapper = mountTable({ rowClickable: true });

    await wrapper.get("tbody .data-table-edit-action").trigger("click");

    expect(wrapper.emitted("row-click")).toBeUndefined();
    wrapper.unmount();
  });
});

describe("NieDataTable server-side mode", () => {
  it("emits the initial query on mount", async () => {
    const wrapper = mountTable({ serverSide: true, totalItems: 40 });
    await flushPromises();

    expect(wrapper.emitted("query-change")?.[0][0]).toEqual({
      page: 1,
      pageSize: 20,
      search: "",
      sortBy: null,
      sortDirection: null,
      sorts: [],
      filters: {},
    });
    wrapper.unmount();
  });

  it("leaves the rows exactly as the server returned them", async () => {
    vi.useFakeTimers();
    const wrapper = mountTable({ serverSide: true, totalItems: 40 });

    await wrapper.get('input[type="search"]').setValue("nothing-matches");

    expect(bodyRows(wrapper)).toHaveLength(3);
    wrapper.unmount();
  });

  it("re-queries from page one after a search, a sort and a filter", async () => {
    vi.useFakeTimers();
    const wrapper = mountTable({ serverSide: true, totalItems: 40, page: 3 });
    await vi.advanceTimersByTimeAsync(0);

    await wrapper.get('input[type="search"]').setValue("acme");
    await vi.advanceTimersByTimeAsync(250);
    await headerButtons(wrapper)[0].trigger("click");

    const queries = (wrapper.emitted("query-change") ?? []).map(
      (call) => call[0] as NieDataTableQuery,
    );
    expect(queries[queries.length - 2]).toMatchObject({
      page: 1,
      search: "acme",
    });
    expect(queries[queries.length - 1]).toMatchObject({
      page: 1,
      sortBy: "reference",
      sortDirection: "asc",
    });
    wrapper.unmount();
  });

  it("derives the page count from the server total", async () => {
    const wrapper = mountTable({
      serverSide: true,
      totalItems: 45,
      pageSize: 20,
    });
    await flushPromises();

    expect(wrapper.get("[data-table-total-results]").text()).toBe("45 results");
    expect(
      wrapper.get('[aria-label="Last page"]').attributes("disabled"),
    ).toBeUndefined();
    wrapper.unmount();
  });

  it("asks the host to load filter values on demand", async () => {
    const wrapper = mountTable({
      serverSide: true,
      totalItems: 3,
      filterOptionPages: {
        vendor: {
          items: [{ label: "Acme", value: "Acme" }],
          page: 1,
          pageSize: 25,
          totalCount: 1,
          totalPages: 1,
        },
      },
    });
    await flushPromises();

    await wrapper.get('[aria-label="Filter Vendor"]').trigger("click");
    await nextTick();

    expect(wrapper.emitted("filter-options-request")?.[0][0]).toEqual({
      columnKey: "vendor",
      page: 1,
      pageSize: 25,
      search: "",
      tableSearch: "",
      filters: {},
    });
    wrapper.unmount();
  });

  it("re-queries both the rows and the filter values after a column filter change", async () => {
    const wrapper = mountTable({
      serverSide: true,
      totalItems: 3,
      filterOptionPages: {
        vendor: {
          items: [{ label: "Acme", value: "Acme" }],
          page: 1,
          pageSize: 25,
          totalCount: 1,
          totalPages: 1,
        },
      },
    });
    await flushPromises();
    await wrapper.get('[aria-label="Filter Vendor"]').trigger("click");
    await nextTick();

    document
      .querySelector<HTMLButtonElement>('[data-filter-value="Acme"]')
      ?.click();
    await nextTick();

    expect(
      (lastEmitted(wrapper, "query-change")?.[0] as NieDataTableQuery).filters,
    ).toEqual({ vendor: ["Acme"] });
    expect(wrapper.emitted("filter-options-request")).toHaveLength(2);
    wrapper.unmount();
  });
});

describe("NieDataTable preferences", () => {
  it("shows no preferences affordance without a preference key", () => {
    const wrapper = mountTable();

    expect(wrapper.find("[data-table-preferences-action]").exists()).toBe(false);
    wrapper.unmount();
  });

  it("loads and applies the saved view", async () => {
    const store = createStore({
      get: vi.fn().mockResolvedValue(
        record({
          settings: settings({
            pageSize: 10,
            hiddenColumns: ["vendor"],
            sorts: [{ key: "reference", direction: "desc" }],
            density: "compact",
            appearance: "striped",
          }),
        }),
      ),
    });
    const wrapper = mountTable({ preferenceKey: "orders" }, store);
    await flushPromises();

    expect(store.get).toHaveBeenCalledWith("orders");
    expect(headerLabels(wrapper)).toEqual([
      "Reference",
      "Amount",
      "Approved",
      "Status",
    ]);
    expect(wrapper.emitted("update:pageSize")).toEqual([[10]]);
    expect(wrapper.classes()).toContain("data-table--striped");
    expect(wrapper.classes()).toContain("data-table--density-compact");
    expect(bodyRows(wrapper)[0].findAll("td")[0].text()).toBe("PO-3");
    wrapper.unmount();
  });

  it("warns and falls back to defaults when the saved view cannot be read", async () => {
    const store = createStore({
      get: vi.fn().mockRejectedValue(new Error("offline")),
    });
    const wrapper = mountTable({ preferenceKey: "orders" }, store);
    await flushPromises();

    expect(wrapper.get("[data-table-preference-warning]").text()).toContain(
      "Your saved table view could not be loaded.",
    );
    wrapper.unmount();
  });

  it("repairs a saved view that no longer matches the table", async () => {
    const store = createStore({
      get: vi.fn().mockResolvedValue(
        record({
          definitionVersion: 0,
          settings: settings({
            pageSize: 7,
            columnOrder: ["vendor", "vendor", "gone", "reference"],
            hiddenColumns: ["gone"],
            sorts: [
              { key: "gone", direction: "asc" },
              { key: "reference", direction: "sideways" as never },
            ],
            filters: { gone: ["x"], vendor: ["Acme"] },
            density: "roomy" as never,
            appearance: "fancy" as never,
          }),
        }),
      ),
    });
    const wrapper = mountTable({ preferenceKey: "orders" }, store);
    await flushPromises();

    expect(wrapper.get("[data-table-preference-warning]").text()).toContain(
      "Your saved table view needs attention.",
    );
    expect(wrapper.classes()).toContain("data-table--density-comfortable");
    expect(wrapper.classes()).toContain("data-table--elevated");
    expect(headerLabels(wrapper)).toEqual([
      "Vendor",
      "Reference",
      "Amount",
      "Approved",
      "Status",
    ]);
    expect(wrapper.emitted("update:selectedFilters")?.[0]).toEqual([
      { vendor: ["Acme"] },
    ]);
    wrapper.unmount();
  });

  it("lists the repair reasons inside the preferences modal", async () => {
    const store = createStore({
      get: vi.fn().mockResolvedValue(
        record({
          settings: settings({ columnOrder: ["gone", "reference"] }),
        }),
      ),
    });
    const wrapper = mountTable({ preferenceKey: "orders" }, store);
    await flushPromises();

    await wrapper.get("[data-table-preferences-action]").trigger("click");
    await nextTick();

    expect(document.body.textContent).toContain(
      "One or more saved columns are no longer available.",
    );
    expect(() => preferencesModalButton("Repair and save")).not.toThrow();
    wrapper.unmount();
  });

  it("saves the edited view through the store", async () => {
    const store = createStore();
    const wrapper = mountTable({ preferenceKey: "orders" }, store);
    await flushPromises();

    await wrapper.get("[data-table-preferences-action]").trigger("click");
    await nextTick();
    document
      .querySelector<HTMLInputElement>('[aria-label="Show Vendor"]')
      ?.click();
    await nextTick();
    preferencesModalButton("Save as my default").click();
    await flushPromises();

    expect(store.save).toHaveBeenCalledWith(
      "orders",
      1,
      expect.objectContaining({ hiddenColumns: ["vendor"] }),
      undefined,
    );
    expect(document.querySelector('[role="dialog"]')).toBeNull();
    wrapper.unmount();
  });

  it("reports a save failure without closing the modal", async () => {
    const store = createStore({
      save: vi.fn().mockRejectedValue(new Error("offline")),
    });
    const wrapper = mountTable({ preferenceKey: "orders" }, store);
    await flushPromises();

    await wrapper.get("[data-table-preferences-action]").trigger("click");
    await nextTick();
    preferencesModalButton("Save as my default").click();
    await flushPromises();

    expect(document.querySelector('[role="alert"]')?.textContent).toContain(
      "We couldn't save your table preferences.",
    );
    wrapper.unmount();
  });

  it("offers a reload when another session changed the saved view", async () => {
    const store = createStore({
      save: vi.fn().mockRejectedValue(new NieDataTablePreferenceConflictError()),
    });
    const wrapper = mountTable({ preferenceKey: "orders" }, store);
    await flushPromises();

    await wrapper.get("[data-table-preferences-action]").trigger("click");
    await nextTick();
    preferencesModalButton("Save as my default").click();
    await flushPromises();

    expect(document.body.textContent).toContain("Saved view changed elsewhere");
    expect(preferencesModalButton("Save as my default").disabled).toBe(true);

    document
      .querySelector<HTMLButtonElement>('[role="alert"] button')
      ?.click();
    await flushPromises();

    expect(store.refresh).toHaveBeenCalledWith("orders");
    expect(document.querySelector('[role="alert"]')).toBeNull();
    wrapper.unmount();
  });

  it("resets back to the screen defaults", async () => {
    const store = createStore({
      get: vi.fn().mockResolvedValue(
        record({ settings: settings({ hiddenColumns: ["vendor"] }) }),
      ),
    });
    const wrapper = mountTable({ preferenceKey: "orders" }, store);
    await flushPromises();
    expect(headerButtons(wrapper)).toHaveLength(4);

    await wrapper.get("[data-table-preferences-action]").trigger("click");
    await nextTick();
    [...document.querySelectorAll<HTMLButtonElement>("footer button")]
      .find((button) => button.textContent?.includes("Reset to screen defaults"))
      ?.click();
    await flushPromises();

    expect(store.remove).toHaveBeenCalledWith("orders");
    expect(headerButtons(wrapper)).toHaveLength(5);
    wrapper.unmount();
  });
});

describe("NieDataTable saved-filter reminder", () => {
  const savedFilters = { vendor: ["Acme"] };

  function reminderStore(
    overrides: Partial<NieDataTablePreferenceStore> = {},
  ): NieDataTablePreferenceStore {
    return createStore({
      get: vi.fn().mockResolvedValue(
        record({ settings: settings({ filters: savedFilters }) }),
      ),
      save: vi.fn().mockResolvedValue(
        record({ revision: 5, settings: settings({ filters: savedFilters }) }),
      ),
      ...overrides,
    });
  }

  it("prompts when saved filters have never been acknowledged", async () => {
    const wrapper = mountTable({ preferenceKey: "orders" }, reminderStore());
    await flushPromises();

    expect(
      document.querySelector("[data-table-filter-reminder]"),
    ).not.toBeNull();
    expect(
      [
        ...document.querySelectorAll('[aria-label="Active saved filters"] li'),
      ].map((chip) => chip.textContent?.trim()),
    ).toEqual(["Vendor (1)"]);
    wrapper.unmount();
  });

  it("stays quiet for a recent acknowledgement", async () => {
    const store = reminderStore({
      get: vi.fn().mockResolvedValue(
        record({
          settings: settings({
            filters: savedFilters,
            filterReminderAcknowledgedAtUtc: new Date().toISOString(),
          }),
        }),
      ),
    });
    const wrapper = mountTable({ preferenceKey: "orders" }, store);
    await flushPromises();

    expect(document.querySelector("[data-table-filter-reminder]")).toBeNull();
    wrapper.unmount();
  });

  it("stays quiet when the saved view has no filters", async () => {
    const wrapper = mountTable({ preferenceKey: "orders" }, createStore({
      get: vi.fn().mockResolvedValue(record()),
    }));
    await flushPromises();

    expect(document.querySelector("[data-table-filter-reminder]")).toBeNull();
    wrapper.unmount();
  });

  it("keeps the filters for another week", async () => {
    const store = reminderStore();
    const wrapper = mountTable({ preferenceKey: "orders" }, store);
    await flushPromises();

    document
      .querySelector<HTMLButtonElement>(
        '[aria-label="Keep saved filters for another week"]',
      )
      ?.click();
    await flushPromises();

    expect(store.save).toHaveBeenCalledWith(
      "orders",
      1,
      expect.objectContaining({ filters: savedFilters }),
      4,
    );
    expect(document.querySelector("[data-table-filter-reminder]")).toBeNull();
    wrapper.unmount();
  });

  it("removes the default filters on request", async () => {
    const store = reminderStore({
      save: vi.fn().mockResolvedValue(record({ revision: 5 })),
    });
    const wrapper = mountTable({ preferenceKey: "orders" }, store);
    await flushPromises();

    document
      .querySelector<HTMLButtonElement>(
        '[aria-label="Remove saved default filters"]',
      )
      ?.click();
    await flushPromises();

    expect(store.save).toHaveBeenCalledWith(
      "orders",
      1,
      expect.objectContaining({ filters: {} }),
      4,
    );
    expect(bodyRows(wrapper)).toHaveLength(3);
    wrapper.unmount();
  });

  it("reports a failure and keeps the prompt open", async () => {
    const store = reminderStore({
      save: vi.fn().mockRejectedValue(new Error("offline")),
    });
    const wrapper = mountTable({ preferenceKey: "orders" }, store);
    await flushPromises();

    document
      .querySelector<HTMLButtonElement>(
        '[aria-label="Keep saved filters for another week"]',
      )
      ?.click();
    await flushPromises();

    expect(
      document.querySelector("[data-table-filter-reminder]"),
    ).not.toBeNull();
    expect(document.body.textContent).toContain(
      "We couldn't update your saved filters. Try again.",
    );
    wrapper.unmount();
  });

  it("escalates a conflict into the non-dismissible preferences modal", async () => {
    const store = reminderStore({
      save: vi.fn().mockRejectedValue(new NieDataTablePreferenceConflictError()),
    });
    const wrapper = mountTable({ preferenceKey: "orders" }, store);
    await flushPromises();

    document
      .querySelector<HTMLButtonElement>(
        '[aria-label="Keep saved filters for another week"]',
      )
      ?.click();
    await flushPromises();

    expect(document.querySelector("[data-table-filter-reminder]")).toBeNull();
    expect(document.body.textContent).toContain("Saved view changed elsewhere");
    expect(
      document.querySelector('[aria-label="Close table preferences"]'),
    ).toBeNull();
    wrapper.unmount();
  });
});

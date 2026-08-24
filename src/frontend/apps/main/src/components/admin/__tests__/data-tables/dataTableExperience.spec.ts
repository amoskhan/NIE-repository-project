import { mount } from "@vue/test-utils";
import { afterEach, describe, expect, it } from "vitest";
import { readFileSync } from "node:fs";
import { resolve } from "node:path";
import { NieDataTable, NieTable } from "@nie/ui";

const columns = [
  { key: "name", label: "Name" },
  {
    key: "status",
    label: "Status",
    chip: {
      toneMap: {
        Active: "success",
        Pending: "warning",
      },
      dot: true,
    },
  },
];

afterEach(() => {
  document.body.innerHTML = "";
});

describe("NieDataTable polished states", () => {
  it("keeps the table scaffold visible and centers a borderless empty result", () => {
    const wrapper = mount(NieDataTable, {
      props: {
        columns,
        data: [],
        rowKey: "id",
        serverSide: true,
        totalItems: 0,
        pageSize: 20,
        hideActions: true,
      } as never,
      global: { stubs: { Teleport: true, Transition: false } },
    });

    expect(wrapper.find("table").exists()).toBe(true);
    expect(wrapper.find("thead").exists()).toBe(true);
    expect(wrapper.get('[data-testid="nie-result-state"]').text()).toContain(
      "No records found",
    );
    expect(wrapper.get(".data-table-empty-state").classes()).toContain(
      "items-center",
    );
    expect(wrapper.get(".data-table-container").attributes("style")).toContain(
      "--nie-data-table-max-height",
    );
  });

  it("offers 10, 20, 50, and 100 rows and requests page one after a change", async () => {
    const wrapper = mount(NieDataTable, {
      props: {
        columns,
        data: [{ id: "1", name: "One", status: "Active" }],
        rowKey: "id",
        serverSide: true,
        totalItems: 64,
        page: 3,
        pageSize: 20,
        hideActions: true,
      } as never,
      global: { stubs: { Teleport: true, Transition: false } },
    });

    const selector = wrapper.get('select[aria-label="Rows per page"]');
    expect(selector.findAll("option").map((option) => option.text())).toEqual([
      "10",
      "20",
      "50",
      "100",
    ]);

    await selector.setValue("50");

    expect(wrapper.emitted("update:pageSize")?.at(-1)?.[0]).toBe(50);
    expect(wrapper.emitted("query-change")?.at(-1)?.[0]).toMatchObject({
      page: 1,
      pageSize: 50,
    });
  });

  it("renders configured categorical values as restrained semantic chips", () => {
    const wrapper = mount(NieDataTable, {
      props: {
        columns,
        data: [{ id: "1", name: "One", status: "Active" }],
        rowKey: "id",
        hideActions: true,
      } as never,
    });

    const chip = wrapper.get('[data-table-chip="status"]');
    expect(chip.text()).toContain("Active");
    expect(chip.find('[data-testid="nie-badge-dot"]').exists()).toBe(true);
  });

  it("activates clickable desktop and mobile rows from the keyboard", async () => {
    const wrapper = mount(NieDataTable, {
      props: {
        columns,
        data: [{ id: "1", name: "One", status: "Active" }],
        rowKey: "id",
        rowClickable: true,
        rowAriaLabel: (row: { name: string }) => `Open ${row.name}`,
        hideActions: true,
      } as never,
    });

    const rows = wrapper.findAll("[data-table-interactive-row]");
    expect(rows).toHaveLength(2);
    for (const row of rows) {
      expect(row.attributes("tabindex")).toBe("0");
      expect(row.attributes("role")).toBe(
        row.element.tagName === "TR" ? "row" : "button",
      );
      expect(row.attributes("aria-label")).toBe("Open One");
      await row.trigger("keydown", { key: "Enter" });
    }
    expect(wrapper.emitted("row-click")).toHaveLength(2);
  });

  it("does not activate a clickable row from a nested action", async () => {
    const wrapper = mount(NieDataTable, {
      props: {
        columns,
        data: [{ id: "1", name: "One", status: "Active" }],
        rowKey: "id",
        rowClickable: true,
      } as never,
    });

    const action = wrapper.get(".data-table-edit-action");
    await action.trigger("click");
    await action.trigger("keydown", {
      key: "Enter",
    });

    expect(wrapper.emitted("row-click")).toBeUndefined();
  });

  it("activates a mobile card when its non-interactive child content is clicked", async () => {
    const wrapper = mount(NieDataTable, {
      props: {
        columns,
        data: [{ id: "1", name: "One", status: "Active" }],
        rowKey: "id",
        rowClickable: true,
        hideActions: true,
      } as never,
    });

    const mobileCard = wrapper
      .findAll("[data-table-interactive-row]")
      .find((row) => row.element.tagName === "DIV");
    expect(mobileCard).toBeDefined();

    await mobileCard!.get("span").trigger("click");

    expect(wrapper.emitted("row-click")).toHaveLength(1);
  });

  it("uses real sortable controls and opt-in keyboard rows in NieTable", async () => {
    const wrapper = mount(NieTable, {
      props: {
        columns: [{ key: "name", label: "Name", sortable: true }],
        data: [{ name: "One" }],
        rowClickable: true,
        rowAriaLabel: (row: { name: string }) => `Open ${row.name}`,
      } as never,
    });

    const sortButton = wrapper.get("thead button");
    expect(sortButton.attributes("aria-label")).toBe("Sort by Name");
    expect(sortButton.element.closest("th")?.getAttribute("aria-sort")).toBe(
      "none",
    );

    const row = wrapper.get("[data-table-interactive-row]");
    expect(row.attributes("role")).toBe("row");
    expect(row.attributes("aria-label")).toBe("Open One");
    await row.trigger("keydown", { key: " " });
    expect(wrapper.emitted("row-click")).toHaveLength(1);
  });

  it("activates a NieTable row from non-interactive child content", async () => {
    const wrapper = mount(NieTable, {
      props: {
        columns: [{ key: "name", label: "Name" }],
        data: [{ name: "One" }],
        rowClickable: true,
      } as never,
      slots: {
        "cell-name": '<span data-testid="row-content">One</span>',
      },
    });

    await wrapper.get('[data-testid="row-content"]').trigger("click");

    expect(wrapper.emitted("row-click")).toHaveLength(1);
  });

  it("keeps events from slotted NieTable actions inside the action", async () => {
    const wrapper = mount(NieTable, {
      props: {
        columns: [{ key: "name", label: "Name" }],
        data: [{ name: "One" }],
        rowClickable: true,
      } as never,
      slots: {
        "cell-name": '<button type="button" data-testid="nested-action">Edit</button>',
      },
    });

    const action = wrapper.get('[data-testid="nested-action"]');
    await action.trigger("click");
    await action.trigger("keydown", {
      key: "Enter",
    });

    expect(wrapper.emitted("row-click")).toBeUndefined();
  });

  it.each([
    "staff/pages/admin/audit/AuditLog.vue",
    "staff/pages/procurement/CatalogItems.vue",
    "staff/pages/myinfo/MyInfoPage.vue",
    "staff/pages/procurement/OrderHistory.vue",
    "staff/pages/procurement/VendorManagement.vue",
    "components/admin/notifications/NotificationAdministration.vue",
  ])("uses shared semantic chip configuration in %s", (relativePath) => {
    const source = readFileSync(
      resolve(process.cwd(), "src", relativePath),
      "utf8",
    );

    expect(source).toContain("chip:");
  });

  it("uses a selectable initial page size across all live tables", () => {
    const dataTableApi = readFileSync(
      resolve(process.cwd(), "src/services/core/dataTableApi.ts"),
      "utf8",
    );
    const allConsumers = readFileSync(
      resolve(process.cwd(), "src/components/admin/notifications/NotificationAdministration.vue"),
      "utf8",
    ) + readFileSync(
      resolve(process.cwd(), "src/staff/pages/myinfo/MyInfoPage.vue"),
      "utf8",
    );

    expect(dataTableApi).toMatch(/createInitialDataTableQuery\(\s*pageSize\s*=\s*20/);
    expect(allConsumers).not.toMatch(/:page-size="(?:15|25)"/);
  });
});

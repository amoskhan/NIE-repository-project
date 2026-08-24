import { mount } from "@vue/test-utils";
import { describe, expect, it } from "vitest";
import { h } from "vue";
import NieTable from "../../table/NieTable.vue";
import type { Column } from "../../table/NieTable.vue";

interface Order extends Record<string, unknown> {
  reference: string;
  amount: number;
}

const columns: Column[] = [
  { key: "reference", label: "Reference", sortable: true, width: "12rem" },
  { key: "amount", label: "Amount" },
];

const data: Order[] = [
  { reference: "PO-1", amount: 100 },
  { reference: "PO-2", amount: 200 },
];

describe("NieTable header", () => {
  it("renders one header cell per column", () => {
    const wrapper = mount(NieTable, { props: { columns, data } });
    const headers = wrapper.findAll("th");

    expect(headers).toHaveLength(2);
    expect(headers[0].attributes("style")).toContain("width: 12rem");
    expect(headers[1].text()).toBe("Amount");
  });

  it("gives sortable columns a labelled sort button and non-sortable ones plain text", () => {
    const wrapper = mount(NieTable, { props: { columns, data } });
    const headers = wrapper.findAll("th");

    expect(headers[0].get("button").attributes("aria-label")).toBe(
      "Sort by Reference",
    );
    expect(headers[1].find("button").exists()).toBe(false);
  });

  it("reports aria-sort only for sortable columns", () => {
    const unsorted = mount(NieTable, { props: { columns, data } });
    expect(unsorted.findAll("th")[0].attributes("aria-sort")).toBe("none");
    expect(unsorted.findAll("th")[1].attributes("aria-sort")).toBeUndefined();

    const ascending = mount(NieTable, {
      props: { columns, data, sortBy: "reference", sortOrder: "asc" },
    });
    expect(ascending.findAll("th")[0].attributes("aria-sort")).toBe("ascending");

    const descending = mount(NieTable, {
      props: { columns, data, sortBy: "reference", sortOrder: "desc" },
    });
    expect(descending.findAll("th")[0].attributes("aria-sort")).toBe(
      "descending",
    );
  });

  it("asks for ascending order first, then flips it", async () => {
    const fresh = mount(NieTable, { props: { columns, data } });
    await fresh.get("th button").trigger("click");
    expect(fresh.emitted("sort")).toEqual([["reference", "asc"]]);

    const ascending = mount(NieTable, {
      props: { columns, data, sortBy: "reference", sortOrder: "asc" },
    });
    await ascending.get("th button").trigger("click");
    expect(ascending.emitted("sort")).toEqual([["reference", "desc"]]);

    const other = mount(NieTable, {
      props: { columns, data, sortBy: "amount", sortOrder: "asc" },
    });
    await other.get("th button").trigger("click");
    expect(other.emitted("sort")).toEqual([["reference", "asc"]]);
  });
});

describe("NieTable body states", () => {
  it("shows a loading row that spans the whole table", () => {
    const wrapper = mount(NieTable, {
      props: { columns, data, loading: true },
    });

    const cell = wrapper.get("tbody td");
    expect(cell.attributes("colspan")).toBe("2");
    expect(
      wrapper.get('[data-testid="nie-loader-symbol"]').attributes("aria-label"),
    ).toBe("Loading table data");
    expect(wrapper.findAll("tbody tr")).toHaveLength(1);
  });

  it("shows the empty message when there are no rows", () => {
    const wrapper = mount(NieTable, {
      props: { columns, data: [], emptyMessage: "No purchase orders" },
    });

    expect(wrapper.get("tbody td").text()).toBe("No purchase orders");
  });

  it("prefers the loading row over the empty row", () => {
    const wrapper = mount(NieTable, {
      props: { columns, data: [], loading: true },
    });

    expect(wrapper.find('[data-testid="nie-loader-symbol"]').exists()).toBe(
      true,
    );
  });

  it("renders one row per record", () => {
    const wrapper = mount(NieTable, { props: { columns, data } });

    const rows = wrapper.findAll("tbody tr");
    expect(rows).toHaveLength(2);
    expect(rows[0].findAll("td")[0].text()).toBe("PO-1");
    expect(rows[1].findAll("td")[1].text()).toBe("200");
  });

  it("uses a column renderer when one is supplied", () => {
    const wrapper = mount(NieTable, {
      props: {
        columns: [
          {
            key: "amount",
            label: "Amount",
            render: (row: unknown) =>
              `SGD ${(row as Order).amount.toFixed(2)}`,
          },
        ],
        data,
      },
    });

    expect(wrapper.findAll("tbody td")[0].text()).toBe("SGD 100.00");
  });

  it("stripes alternate rows only when asked", () => {
    const plain = mount(NieTable, { props: { columns, data } });
    expect(plain.findAll("tbody tr")[1].classes()).not.toContain(
      "bg-secondary-50",
    );

    const striped = mount(NieTable, {
      props: { columns, data, striped: true },
    });
    expect(striped.findAll("tbody tr")[1].classes()).toContain(
      "bg-secondary-50",
    );
    expect(striped.findAll("tbody tr")[0].classes()).not.toContain(
      "bg-secondary-50",
    );
  });

  it("drops the hover treatment when hovering is turned off", () => {
    const wrapper = mount(NieTable, {
      props: { columns, data, hoverable: false },
    });

    expect(wrapper.findAll("tbody tr")[0].classes().join(" ")).not.toContain(
      "hover:bg-secondary-50",
    );
  });
});

describe("NieTable row activation", () => {
  it("adds no row affordances when rows are not clickable", async () => {
    const wrapper = mount(NieTable, { props: { columns, data } });
    const row = wrapper.findAll("tbody tr")[0];

    expect(row.attributes("tabindex")).toBeUndefined();
    expect(row.attributes("aria-label")).toBeUndefined();
    expect(row.attributes("data-table-interactive-row")).toBeUndefined();

    await row.trigger("click");
    expect(wrapper.emitted("row-click")).toBeUndefined();
  });

  it("makes clickable rows keyboard reachable and labelled", () => {
    const wrapper = mount(NieTable, {
      props: { columns, data, rowClickable: true },
    });
    const row = wrapper.findAll("tbody tr")[1];

    expect(row.attributes("tabindex")).toBe("0");
    expect(row.attributes("aria-label")).toBe("Open row 2");
    expect(row.attributes("data-table-interactive-row")).toBe("");
  });

  it("uses a caller-supplied row label, falling back when it is blank", () => {
    const wrapper = mount(NieTable, {
      props: {
        columns,
        data,
        rowClickable: true,
        rowAriaLabel: (row: unknown, index: number) =>
          index === 0 ? `Open ${(row as Order).reference}` : "   ",
      },
    });

    const rows = wrapper.findAll("tbody tr");
    expect(rows[0].attributes("aria-label")).toBe("Open PO-1");
    expect(rows[1].attributes("aria-label")).toBe("Open row 2");
  });

  it("emits row-click on click, Enter and Space", async () => {
    const wrapper = mount(NieTable, {
      props: { columns, data, rowClickable: true },
    });
    const row = wrapper.findAll("tbody tr")[0];

    await row.trigger("click");
    await row.trigger("keydown.enter");
    await row.trigger("keydown.space");

    expect(wrapper.emitted("row-click")).toHaveLength(3);
    expect(wrapper.emitted("row-click")?.[0]).toEqual([data[0], 0]);
  });

  it("lets a nested control handle its own click", async () => {
    const wrapper = mount(NieTable, {
      props: {
        columns: [{ key: "reference", label: "Reference" }],
        data: [data[0]],
        rowClickable: true,
      },
      slots: {
        "cell-reference": () => h("button", { type: "button" }, "Approve"),
      },
    });

    await wrapper.get("tbody button").trigger("click");

    expect(wrapper.emitted("row-click")).toBeUndefined();
  });
});

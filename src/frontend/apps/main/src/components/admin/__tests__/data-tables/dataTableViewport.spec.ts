import { mount } from "@vue/test-utils";
import { describe, expect, it } from "vitest";
import { NieDataTable, NieFilterBar } from "@nie/ui";
import { readFileSync } from "node:fs";
import { resolve } from "node:path";

const columns = [
  { key: "name", label: "Name" },
  { key: "status", label: "Status" },
];
const data = Array.from({ length: 12 }, (_, index) => ({
  id: String(index + 1),
  name: `Record ${index + 1}`,
  status: index % 2 === 0 ? "Active" : "Pending",
}));

describe("NieDataTable viewport", () => {
  it("owns a labelled two-axis scroll region with a sticky header", () => {
    const wrapper = mount(NieDataTable, {
      props: {
        columns,
        data,
        rowKey: "id",
        showToolbar: false,
        hideActions: true,
      },
    });

    const container = wrapper.get(".data-table-container");
    expect(container.attributes("style")).toContain(
      "--nie-data-table-max-height: calc(100dvh - 8rem)",
    );

    const viewport = wrapper.get(".data-table-body");
    expect(viewport.attributes("role")).toBe("region");
    expect(viewport.attributes("tabindex")).toBe("0");
    expect(viewport.attributes("aria-label")).toBe("Scrollable data table");
    expect(viewport.classes()).toContain("md:pr-0");
    expect(viewport.find("table").exists()).toBe(true);
    expect(
      viewport.findAll("thead th").every((cell) => cell.classes().includes("sticky")),
    ).toBe(true);
  });

  it("keeps table header and row actions at the shared touch-target size", () => {
    const wrapper = mount(NieDataTable, {
      props: {
        columns,
        data,
        rowKey: "id",
      },
    });

    expect(wrapper.get(".data-table-sort-button").classes()).toContain(
      "min-h-11",
    );
    expect(wrapper.get(".data-table-edit-action").classes()).toContain(
      "size-11",
    );
    expect(wrapper.get(".data-table-delete-action").classes()).toContain(
      "size-11",
    );
    expect(wrapper.get(".data-table-sticky-actions").classes()).toContain(
      "z-10",
    );
  });

  it("keeps the table search and create action at the same control height", () => {
    const wrapper = mount(NieDataTable, {
      props: {
        columns,
        data,
        rowKey: "id",
      },
    });

    expect(wrapper.get('input[type="search"]').classes()).toContain(
      "min-h-11",
    );
    const createAction = wrapper.get("[data-table-create-action]");
    expect(createAction.classes()).toContain("min-h-11");
    expect(createAction.classes()).not.toContain("min-h-10");
  });

  it("uses compact 32-pixel column-filter triggers", () => {
    const wrapper = mount(NieDataTable, {
      props: {
        columns,
        data,
        rowKey: "id",
        hideActions: true,
      },
    });

    const trigger = wrapper.get(".column-filter-trigger");
    expect(trigger.classes()).toEqual(
      expect.arrayContaining(["h-8", "min-w-8", "gap-1", "px-1.5"]),
    );
    expect(trigger.classes()).not.toContain("min-h-11");
  });

  it("allows a screen to tune the contained control height", () => {
    const wrapper = mount(NieDataTable, {
      props: {
        columns,
        data,
        rowKey: "id",
        maxHeight: "42rem",
      },
    });

    expect(wrapper.get(".data-table-container").attributes("style")).toContain(
      "--nie-data-table-max-height: 42rem",
    );
  });

  it("keeps the total in the top toolbar and the footer controls on one compact row", () => {
    const sixteenRecords = Array.from({ length: 16 }, (_, index) => ({
      id: String(index + 1),
      name: `Record ${index + 1}`,
      status: "Active",
    }));
    const wrapper = mount(NieDataTable, {
      props: {
        columns,
        data: sixteenRecords,
        rowKey: "id",
        pageSize: 20,
        hideActions: true,
      },
    });

    expect(wrapper.get("[data-table-total-results]").text()).toBe("16 results");
    expect(wrapper.get("[data-table-pagination-footer]").classes()).toContain(
      "py-2",
    );
    expect(wrapper.get("[data-pagination-layout]").classes()).toEqual(
      expect.arrayContaining(["flex", "justify-between"]),
    );
    expect(wrapper.get("[data-pagination-page-size]").classes()).toContain(
      "justify-self-start",
    );
    expect(wrapper.find("[data-pagination-summary]").exists()).toBe(false);
    expect(wrapper.text()).not.toContain("Rows per page");

    const pageSize = wrapper.get('[data-testid="nie-page-size-select"]');
    expect(pageSize.classes()).toEqual(
      expect.arrayContaining(["appearance-none", "pl-3", "pr-8"]),
    );
    expect(wrapper.get("[data-pagination-page-size-icon]").exists()).toBe(
      true,
    );

    const pagination = wrapper.get("[data-pagination-pages]");
    expect(pagination.get('[aria-label="First page"]').exists()).toBe(true);
    expect(pagination.get('[aria-label="Previous page"]').exists()).toBe(true);
    expect(pagination.get('[aria-label="Next page"]').exists()).toBe(true);
    expect(pagination.get('[aria-label="Last page"]').exists()).toBe(true);
    expect(pagination.get("[data-pagination-current-page]").text()).toBe("1");
  });

  it("keeps shared filter-bar text and button controls at the medium height", () => {
    const wrapper = mount(NieFilterBar, {
      props: {
        activeFilter: "all",
        filters: [{ label: "All", value: "all" }],
        showReset: false,
      },
    });

    expect(wrapper.get('input[type="search"]').classes()).toContain(
      "min-h-11",
    );
    const filterButton = wrapper.get("button");
    expect(filterButton.classes()).toContain("min-h-11");
    expect(filterButton.classes()).not.toContain("min-h-10");
  });

  it.each(["elevated", "minimal", "striped"] as const)(
    "exposes the %s table treatment without forking the component",
    (appearance) => {
      const wrapper = mount(NieDataTable, {
        props: {
          columns,
          data,
          rowKey: "id",
          appearance,
        },
      });

      expect(wrapper.get(".data-table-container").classes()).toContain(
        `data-table--${appearance}`,
      );
    },
  );

  it.each(["compact", "comfortable", "spacious"] as const)(
    "exposes the %s density through one shared table",
    (density) => {
      const wrapper = mount(NieDataTable, {
        props: {
          columns,
          data,
          rowKey: "id",
          density,
          preferenceKey: "test.viewport",
        } as never,
      });

      expect(wrapper.get(".data-table-container").classes()).toContain(
        `data-table--density-${density}`,
      );
    },
  );

  it("uses a near-full viewport mobile list layout without document overflow", () => {
    const source = readFileSync(
      resolve(
        process.cwd(),
        "../../packages/ui/src/components/composite/data-table/NieDataTable.vue",
      ),
      "utf8",
    );

    expect(source).toMatch(
      /@media\s*\(max-width:\s*767px\)[\s\S]*?height:\s*var\(--nie-data-table-mobile-height/,
    );
    expect(source).toContain("overscroll-behavior: contain");
  });

  it("sizes preference sort controls from the modal content instead of the viewport", () => {
    const source = readFileSync(
      resolve(
        process.cwd(),
        "../../packages/ui/src/components/composite/data-table/NieDataTablePreferencesModal.vue",
      ),
      "utf8",
    );

    expect(source).toContain("data-preference-sort-row");
    expect(source).toContain("data-preference-sort-controls");
    expect(source).toMatch(/@container\s+preference-editor\s+\(min-width:\s*32rem\)/);
    expect(source).not.toContain(
      "sm:grid-cols-[2rem_minmax(0,1fr)_10rem_auto]",
    );
    expect(source).toMatch(
      /data-preference-column-visibility[\s\S]*?class="[^"]*size-11/,
    );
    expect(source).toMatch(
      /data-preference-column-move[\s\S]*?class="[^"]*size-11/,
    );
  });
});

describe("contained administration views", () => {
  it("keeps notification policy cards at their intrinsic height", () => {
    const source = readFileSync(
      resolve(
        process.cwd(),
        "src/components/admin/notifications/NotificationAdministration.vue",
      ),
      "utf8",
    );

    expect(source).toMatch(
      /\.policy-groups\s*\{[^}]*grid-auto-rows:\s*max-content;/s,
    );
  });
});

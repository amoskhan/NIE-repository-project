import { mount } from "@vue/test-utils";
import { afterEach, describe, expect, it } from "vitest";
import { nextTick } from "vue";
import NieDataTablePreferencesModal from "../NieDataTablePreferencesModal.vue";
import type {
  NieDataTableColumn,
  NieDataTablePreferenceSettings,
} from "../types";

const columns: NieDataTableColumn[] = [
  { key: "reference", label: "Reference", hideable: false },
  { key: "vendor", label: "Vendor" },
  { key: "amount", label: "Amount" },
  { key: "actions", label: "Actions", sortable: false },
];

function settings(
  overrides: Partial<NieDataTablePreferenceSettings> = {},
): NieDataTablePreferenceSettings {
  return {
    pageSize: 20,
    sorts: [],
    filters: {},
    columnOrder: ["reference", "vendor", "amount", "actions"],
    hiddenColumns: [],
    density: "comfortable",
    appearance: "elevated",
    ...overrides,
  };
}

type ModalProps = InstanceType<typeof NieDataTablePreferencesModal>["$props"];

function mountModal(props: Partial<ModalProps> = {}) {
  return mount(NieDataTablePreferencesModal, {
    attachTo: document.body,
    props: {
      modelValue: true,
      columns,
      settings: settings(),
      ...props,
    } as ModalProps,
  });
}

function section(name: string): HTMLButtonElement {
  const nav = document.querySelector('[aria-label="Table preference sections"]');
  const match = [...(nav?.querySelectorAll("button") ?? [])].find(
    (button) => button.textContent?.trim() === name,
  );
  if (!match) throw new Error(`No preference section named ${name}`);
  return match;
}

function footerButton(text: string): HTMLButtonElement {
  const match = [
    ...document.querySelectorAll<HTMLButtonElement>("footer button"),
  ].find((button) => button.textContent?.trim() === text);
  if (!match) throw new Error(`No footer button labelled ${text}`);
  return match;
}

function columnRows(): HTMLLIElement[] {
  return [...document.querySelectorAll<HTMLLIElement>("ul > li")];
}

function headingText(selector: string): string {
  return document.querySelector(selector)?.textContent?.trim() ?? "";
}

afterEach(() => {
  document.body.innerHTML = "";
  document.body.style.overflow = "";
});

describe("NieDataTablePreferencesModal shell", () => {
  it("renders nothing while closed", () => {
    const wrapper = mountModal({ modelValue: false });

    expect(document.querySelector('[role="dialog"]')).toBeNull();
    wrapper.unmount();
  });

  it("opens on the Columns section", () => {
    const wrapper = mountModal();

    expect(headingText("h2")).toBe("Table preferences");
    expect(headingText("h3")).toBe("Columns");
    expect(section("Columns").className).toContain("bg-primary-50");
    wrapper.unmount();
  });

  it("switches between the four sections", async () => {
    const wrapper = mountModal();

    for (const [name, heading] of [
      ["Sorting", "Sorting"],
      ["Default filters", "Default filters"],
      ["Display", "Display"],
      ["Columns", "Columns"],
    ]) {
      section(name).click();
      await nextTick();
      expect(headingText("h3")).toBe(heading);
    }
    wrapper.unmount();
  });

  it("hides every escape hatch when it is not dismissible", () => {
    const wrapper = mountModal({ dismissible: false });

    expect(
      document.querySelector('[aria-label="Close table preferences"]'),
    ).toBeNull();
    expect(
      [...document.querySelectorAll("footer button")].map((button) =>
        button.textContent?.trim(),
      ),
    ).toEqual(["Save as my default"]);
    wrapper.unmount();
  });

  it("closes from the header and the cancel button", async () => {
    const wrapper = mountModal();

    document
      .querySelector<HTMLButtonElement>('[aria-label="Close table preferences"]')
      ?.click();
    footerButton("Cancel").click();
    await nextTick();

    expect(wrapper.emitted("update:modelValue")).toEqual([[false], [false]]);
    wrapper.unmount();
  });

  it("emits reset from the reset button", async () => {
    const wrapper = mountModal();

    footerButton("Reset to screen defaults").click();
    await nextTick();

    expect(wrapper.emitted("reset")).toHaveLength(1);
    wrapper.unmount();
  });
});

describe("NieDataTablePreferencesModal columns", () => {
  it("lists the columns in the stored order", () => {
    const wrapper = mountModal({
      settings: settings({ columnOrder: ["amount", "reference"] }),
    });

    expect(
      columnRows().map((row) => row.querySelector("span")?.textContent?.trim()),
    ).toEqual(["Amount", "Reference", "Vendor", "Actions"]);
    wrapper.unmount();
  });

  it("appends columns that the stored order does not mention", () => {
    const wrapper = mountModal({
      settings: settings({ columnOrder: [] }),
    });

    expect(columnRows()).toHaveLength(4);
    wrapper.unmount();
  });

  it("ignores stored keys that no longer exist", () => {
    const wrapper = mountModal({
      settings: settings({ columnOrder: ["gone", "vendor"] }),
    });

    expect(
      columnRows().map((row) => row.querySelector("span")?.textContent?.trim()),
    ).toEqual(["Vendor", "Reference", "Amount", "Actions"]);
    wrapper.unmount();
  });

  it("disables the move controls at the ends of the list", () => {
    const wrapper = mountModal();
    const rows = columnRows();

    expect(
      rows[0].querySelector<HTMLButtonElement>("[data-preference-column-move]")
        ?.disabled,
    ).toBe(true);
    expect(
      rows[rows.length - 1].querySelectorAll<HTMLButtonElement>(
        "[data-preference-column-move]",
      )[1].disabled,
    ).toBe(true);
    wrapper.unmount();
  });

  it("moves a column up and down", async () => {
    const wrapper = mountModal();

    document
      .querySelector<HTMLButtonElement>('[aria-label="Move Vendor up"]')
      ?.click();
    await nextTick();
    expect(
      columnRows().map((row) => row.querySelector("span")?.textContent?.trim()),
    ).toEqual(["Vendor", "Reference", "Amount", "Actions"]);

    document
      .querySelector<HTMLButtonElement>('[aria-label="Move Vendor down"]')
      ?.click();
    await nextTick();
    expect(
      columnRows().map((row) => row.querySelector("span")?.textContent?.trim()),
    ).toEqual(["Reference", "Vendor", "Amount", "Actions"]);
    wrapper.unmount();
  });

  it("locks the visibility checkbox for columns that cannot be hidden", () => {
    const wrapper = mountModal();

    expect(
      document.querySelector<HTMLInputElement>('[aria-label="Show Reference"]')
        ?.disabled,
    ).toBe(true);
    expect(
      document.querySelector<HTMLInputElement>('[aria-label="Show Vendor"]')
        ?.disabled,
    ).toBe(false);
    wrapper.unmount();
  });

  it("hides and restores a column", async () => {
    const wrapper = mountModal();
    const vendor = document.querySelector<HTMLInputElement>(
      '[aria-label="Show Vendor"]',
    );

    vendor?.click();
    await nextTick();
    footerButton("Save as my default").click();
    expect(
      (wrapper.emitted("save")?.[0][0] as NieDataTablePreferenceSettings)
        .hiddenColumns,
    ).toEqual(["vendor"]);

    document
      .querySelector<HTMLInputElement>('[aria-label="Show Vendor"]')
      ?.click();
    await nextTick();
    footerButton("Save as my default").click();
    expect(
      (wrapper.emitted("save")?.[1][0] as NieDataTablePreferenceSettings)
        .hiddenColumns,
    ).toEqual([]);
    wrapper.unmount();
  });

  it("refuses to hide the last visible column", async () => {
    const wrapper = mountModal({
      settings: settings({ hiddenColumns: ["vendor", "amount"] }),
      columns: columns.filter((column) => column.key !== "reference"),
    });
    const actions = document.querySelector<HTMLInputElement>(
      '[aria-label="Show Actions"]',
    );

    expect(actions?.disabled).toBe(true);
    wrapper.unmount();
  });
});

describe("NieDataTablePreferencesModal sorting", () => {
  it("explains when there is no default sort", async () => {
    const wrapper = mountModal();

    section("Sorting").click();
    await nextTick();

    expect(document.body.textContent).toContain("No default sort.");
    wrapper.unmount();
  });

  it("adds a rule using the first unused sortable column", async () => {
    const wrapper = mountModal();
    section("Sorting").click();
    await nextTick();

    footerButton("Save as my default");
    const addSort = [
      ...document.querySelectorAll<HTMLButtonElement>("button"),
    ].find((button) => button.textContent?.trim() === "Add sort");
    addSort?.click();
    await nextTick();
    addSort?.click();
    await nextTick();

    const rows = document.querySelectorAll("[data-preference-sort-row]");
    expect(rows).toHaveLength(2);
    expect(
      document.querySelector<HTMLSelectElement>('[aria-label="Sort 1 column"]')
        ?.value,
    ).toBe("reference");
    expect(
      document.querySelector<HTMLSelectElement>('[aria-label="Sort 2 column"]')
        ?.value,
    ).toBe("vendor");
    wrapper.unmount();
  });

  it("offers only sortable columns", async () => {
    const wrapper = mountModal({
      settings: settings({ sorts: [{ key: "reference", direction: "asc" }] }),
    });
    section("Sorting").click();
    await nextTick();

    const options = [
      ...(document
        .querySelector<HTMLSelectElement>('[aria-label="Sort 1 column"]')
        ?.querySelectorAll("option") ?? []),
    ];
    expect(options.map((option) => option.value)).toEqual([
      "reference",
      "vendor",
      "amount",
    ]);
    wrapper.unmount();
  });

  it("stops a column being used by two rules at once", async () => {
    const wrapper = mountModal({
      settings: settings({
        sorts: [
          { key: "reference", direction: "asc" },
          { key: "vendor", direction: "desc" },
        ],
      }),
    });
    section("Sorting").click();
    await nextTick();

    const options = [
      ...(document
        .querySelector<HTMLSelectElement>('[aria-label="Sort 1 column"]')
        ?.querySelectorAll("option") ?? []),
    ];
    expect(
      options.filter((option) => option.disabled).map((option) => option.value),
    ).toEqual(["vendor"]);
    wrapper.unmount();
  });

  it("caps the rules at five", async () => {
    const wrapper = mountModal({
      settings: settings({
        sorts: [
          { key: "reference", direction: "asc" },
          { key: "vendor", direction: "asc" },
          { key: "amount", direction: "asc" },
        ],
      }),
    });
    section("Sorting").click();
    await nextTick();

    const addSort = [
      ...document.querySelectorAll<HTMLButtonElement>("button"),
    ].find((button) => button.textContent?.trim() === "Add sort");
    // Only three sortable columns exist, so a fourth rule has nothing to use.
    addSort?.click();
    await nextTick();

    expect(document.querySelectorAll("[data-preference-sort-row]")).toHaveLength(
      3,
    );
    wrapper.unmount();
  });

  it("changes the column and the direction of a rule", async () => {
    const wrapper = mountModal({
      settings: settings({ sorts: [{ key: "reference", direction: "asc" }] }),
    });
    section("Sorting").click();
    await nextTick();

    const columnSelect = document.querySelector<HTMLSelectElement>(
      '[aria-label="Sort 1 column"]',
    );
    if (columnSelect) {
      columnSelect.value = "amount";
      columnSelect.dispatchEvent(new Event("change"));
    }
    const directionSelect = document.querySelector<HTMLSelectElement>(
      '[aria-label="Sort 1 direction"]',
    );
    if (directionSelect) {
      directionSelect.value = "desc";
      directionSelect.dispatchEvent(new Event("change"));
    }
    await nextTick();

    footerButton("Save as my default").click();
    expect(
      (wrapper.emitted("save")?.[0][0] as NieDataTablePreferenceSettings).sorts,
    ).toEqual([{ key: "amount", direction: "desc" }]);
    wrapper.unmount();
  });

  it("removes a rule", async () => {
    const wrapper = mountModal({
      settings: settings({
        sorts: [
          { key: "reference", direction: "asc" },
          { key: "vendor", direction: "asc" },
        ],
      }),
    });
    section("Sorting").click();
    await nextTick();

    [...document.querySelectorAll<HTMLButtonElement>("button")]
      .find((button) => button.textContent?.trim() === "Remove")
      ?.click();
    await nextTick();

    expect(document.querySelectorAll("[data-preference-sort-row]")).toHaveLength(
      1,
    );
    wrapper.unmount();
  });
});

describe("NieDataTablePreferencesModal display", () => {
  it("edits density, style and page size", async () => {
    const wrapper = mountModal();
    section("Display").click();
    await nextTick();

    const selects = [
      ...document.querySelectorAll<HTMLSelectElement>(
        ".preference-editor select",
      ),
    ];
    selects[0].value = "compact";
    selects[0].dispatchEvent(new Event("change"));
    selects[1].value = "striped";
    selects[1].dispatchEvent(new Event("change"));
    selects[2].value = "50";
    selects[2].dispatchEvent(new Event("change"));
    await nextTick();

    footerButton("Save as my default").click();
    expect(
      wrapper.emitted("save")?.[0][0] as NieDataTablePreferenceSettings,
    ).toMatchObject({
      density: "compact",
      appearance: "striped",
      pageSize: 50,
    });
    wrapper.unmount();
  });
});

describe("NieDataTablePreferencesModal draft lifecycle", () => {
  it("saves a copy, so later edits do not mutate the caller's settings", async () => {
    const original = settings();
    const wrapper = mountModal({ settings: original });

    footerButton("Save as my default").click();
    await nextTick();

    const saved = wrapper.emitted("save")?.[0][0] as
      NieDataTablePreferenceSettings;
    expect(saved).toEqual(original);
    expect(saved).not.toBe(original);
    expect(saved.columnOrder).not.toBe(original.columnOrder);
    wrapper.unmount();
  });

  it("discards unsaved edits when it is reopened", async () => {
    const wrapper = mountModal({ modelValue: false });
    await wrapper.setProps({ modelValue: true });
    await nextTick();

    document
      .querySelector<HTMLInputElement>('[aria-label="Show Vendor"]')
      ?.click();
    await nextTick();
    await wrapper.setProps({ modelValue: false });
    await wrapper.setProps({ modelValue: true });
    await nextTick();

    expect(
      document.querySelector<HTMLInputElement>('[aria-label="Show Vendor"]')
        ?.checked,
    ).toBe(true);
    wrapper.unmount();
  });

  it("picks up refreshed settings while it stays open", async () => {
    const wrapper = mountModal();

    await wrapper.setProps({
      settings: settings({ hiddenColumns: ["amount"] }),
      refreshVersion: 1,
    });
    await nextTick();

    expect(
      document.querySelector<HTMLInputElement>('[aria-label="Show Amount"]')
        ?.checked,
    ).toBe(false);
    wrapper.unmount();
  });

  it("ignores a refresh while it is closed", async () => {
    const wrapper = mountModal({ modelValue: false });

    await wrapper.setProps({
      settings: settings({ hiddenColumns: ["amount"] }),
      refreshVersion: 1,
    });
    await wrapper.setProps({ modelValue: true });
    await nextTick();

    expect(
      document.querySelector<HTMLInputElement>('[aria-label="Show Amount"]')
        ?.checked,
    ).toBe(false);
    wrapper.unmount();
  });
});

describe("NieDataTablePreferencesModal problem states", () => {
  it("shows no banners when everything is healthy", () => {
    const wrapper = mountModal();

    expect(document.querySelector('[role="alert"]')).toBeNull();
    expect(document.querySelector('[role="status"]')).toBeNull();
    wrapper.unmount();
  });

  it("lists the repair reasons and relabels the save button", () => {
    const wrapper = mountModal({
      preferenceState: {
        repairRequired: true,
        reasons: ["Column 'legacy' no longer exists"],
      },
    });

    expect(document.body.textContent).toContain("Your saved view needs repair");
    expect(document.body.textContent).toContain(
      "Column 'legacy' no longer exists",
    );
    expect(() => footerButton("Repair and save")).not.toThrow();
    wrapper.unmount();
  });

  it("surfaces a save failure", () => {
    const wrapper = mountModal({ saveError: "Could not save preferences." });

    expect(document.querySelector('[role="alert"]')?.textContent).toContain(
      "Could not save preferences.",
    );
    expect(document.body.textContent).not.toContain("Reload latest");
    wrapper.unmount();
  });

  it("offers a reload and blocks saving on a conflict", async () => {
    const wrapper = mountModal({
      saveError: "The saved view changed in another session.",
      saveConflict: true,
    });

    expect(document.body.textContent).toContain("Saved view changed elsewhere");
    const reload = [
      ...document.querySelectorAll<HTMLButtonElement>('[role="alert"] button'),
    ][0];
    reload.click();
    await nextTick();

    expect(wrapper.emitted("reload")).toHaveLength(1);
    expect(footerButton("Save as my default").disabled).toBe(true);
    wrapper.unmount();
  });

  it("reports a load failure without blocking the editor", () => {
    const wrapper = mountModal({ loadError: "Saved view could not be read." });

    expect(document.querySelector('[role="status"]')?.textContent).toContain(
      "Saved view could not be read.",
    );
    expect(footerButton("Save as my default").disabled).toBe(false);
    wrapper.unmount();
  });

  it("shows progress on the save button while saving", () => {
    const wrapper = mountModal({ saving: true });

    expect(
      footerButton("Save as my default").querySelector(
        '[data-testid="nie-loader-symbol"]',
      ),
    ).not.toBeNull();
    wrapper.unmount();
  });
});

describe("NieDataTablePreferencesModal default filters", () => {
  it("forwards filter option requests from the builder", async () => {
    const wrapper = mountModal({
      remoteFilters: true,
      filterGroups: [
        {
          key: "status",
          label: "Status",
          options: [{ label: "Open", value: "open" }],
        },
      ],
    });

    section("Default filters").click();
    await nextTick();

    expect(wrapper.emitted("filter-options-request")?.[0]).toEqual([
      {
        columnKey: "status",
        page: 1,
        pageSize: 25,
        search: "",
        filters: {},
      },
    ]);
    wrapper.unmount();
  });

  it("keeps the chosen default filters in the saved settings", async () => {
    const wrapper = mountModal({
      filterGroups: [
        {
          key: "status",
          label: "Status",
          options: [{ label: "Open", value: "open" }],
        },
      ],
    });
    section("Default filters").click();
    await nextTick();

    document
      .querySelector<HTMLButtonElement>('[data-default-filter-value="open"]')
      ?.click();
    await nextTick();
    footerButton("Save as my default").click();

    expect(
      (wrapper.emitted("save")?.[0][0] as NieDataTablePreferenceSettings)
        .filters,
    ).toEqual({ status: ["open"] });
    wrapper.unmount();
  });
});

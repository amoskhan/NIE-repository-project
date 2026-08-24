import { mount } from "@vue/test-utils";
import { afterEach, describe, expect, it } from "vitest";
import { h, nextTick } from "vue";
import NieListControls from "../../list-controls/NieListControls.vue";

const filterGroups = [
  {
    key: "status",
    label: "Status",
    options: [
      { label: "Open", value: "open" },
      { label: "Approved", value: "approved" },
    ],
  },
  {
    key: "owner",
    label: "Owner",
    options: [{ label: "Ada", value: "ada" }],
  },
];

type ControlsProps = InstanceType<typeof NieListControls>["$props"];

function setViewportWidth(width: number): void {
  Object.defineProperty(window, "innerWidth", {
    configurable: true,
    writable: true,
    value: width,
  });
}

function mountControls(props: Partial<ControlsProps> = {}) {
  return mount(NieListControls, {
    attachTo: document.body,
    props: { filterGroups, ...props } as ControlsProps,
  });
}

function chips(wrapper: ReturnType<typeof mountControls>) {
  return wrapper.findAll("button.rounded-full");
}

function chipLabels(wrapper: ReturnType<typeof mountControls>): string[] {
  return chips(wrapper).map((chip) =>
    chip
      .findAll("span")
      .map((part) => part.text())
      .join(" "),
  );
}

afterEach(() => {
  setViewportWidth(1024);
  document.body.innerHTML = "";
});

describe("NieListControls search", () => {
  it("renders the desktop search field with the given placeholder", () => {
    const wrapper = mountControls({ searchPlaceholder: "Search orders" });
    const input = wrapper.get('input[type="search"]');

    expect(input.attributes("placeholder")).toBe("Search orders");
    wrapper.unmount();
  });

  it("can hide the search field", () => {
    const wrapper = mountControls({ showSearch: false });

    expect(wrapper.find('input[type="search"]').exists()).toBe(false);
    wrapper.unmount();
  });

  it("emits every keystroke", async () => {
    const wrapper = mountControls();

    await wrapper.get('input[type="search"]').setValue("acme");

    expect(wrapper.emitted("update:searchTerm")).toEqual([["acme"]]);
    wrapper.unmount();
  });
});

describe("NieListControls active chips", () => {
  it("renders no chip row when nothing is applied", () => {
    const wrapper = mountControls();

    expect(chipLabels(wrapper)).toEqual([]);
    wrapper.unmount();
  });

  it("treats a whitespace-only search as no search", () => {
    const wrapper = mountControls({ searchTerm: "   " });

    expect(chipLabels(wrapper)).toEqual([]);
    wrapper.unmount();
  });

  it("shows a chip for the search term", () => {
    const wrapper = mountControls({ searchTerm: "acme" });

    expect(chipLabels(wrapper)).toEqual(["Search acme"]);
    wrapper.unmount();
  });

  it("shows one chip per selected filter value, labelled by group", () => {
    const wrapper = mountControls({
      selectedFilters: { status: ["open", "approved"], owner: ["ada"] },
    });

    expect(chipLabels(wrapper)).toEqual([
      "Status Open",
      "Status Approved",
      "Owner Ada",
    ]);
    wrapper.unmount();
  });

  it("falls back to raw keys and values for unknown filters", () => {
    const wrapper = mountControls({
      selectedFilters: { region: ["apac"], status: ["archived"] },
    });

    expect(chipLabels(wrapper)).toEqual(["region apac", "Status archived"]);
    wrapper.unmount();
  });

  it("clears the search from its chip", async () => {
    const wrapper = mountControls({ searchTerm: "acme" });

    await chips(wrapper)[0].trigger("click");

    expect(wrapper.emitted("update:searchTerm")).toEqual([[""]]);
    wrapper.unmount();
  });

  it("removes just the one value from its chip", async () => {
    const wrapper = mountControls({
      selectedFilters: { status: ["open", "approved"], owner: ["ada"] },
    });

    await chips(wrapper)[0].trigger("click");

    expect(wrapper.emitted("update:selectedFilters")).toEqual([
      [{ status: ["approved"], owner: ["ada"] }],
    ]);
    wrapper.unmount();
  });

  it("drops the group entirely when its last value is removed", async () => {
    const wrapper = mountControls({
      selectedFilters: { status: ["open"], owner: ["ada"] },
    });

    await chips(wrapper)[0].trigger("click");

    expect(wrapper.emitted("update:selectedFilters")).toEqual([
      [{ owner: ["ada"] }],
    ]);
    wrapper.unmount();
  });
});

describe("NieListControls reset", () => {
  it("stays hidden while nothing is applied", () => {
    const wrapper = mountControls();

    expect(
      wrapper.findAll("button").some((button) => button.text().includes("Reset")),
    ).toBe(false);
    wrapper.unmount();
  });

  it("clears the search term and every filter", async () => {
    const wrapper = mountControls({
      searchTerm: "acme",
      selectedFilters: { status: ["open"] },
    });

    const reset = wrapper
      .findAll("button")
      .find((button) => button.text().includes("Reset"));
    await reset?.trigger("click");

    expect(wrapper.emitted("update:searchTerm")).toEqual([[""]]);
    expect(wrapper.emitted("update:selectedFilters")).toEqual([[{}]]);
    expect(wrapper.emitted("reset")).toHaveLength(1);
    wrapper.unmount();
  });

  it("can be suppressed", () => {
    const wrapper = mountControls({ searchTerm: "acme", showReset: false });

    expect(
      wrapper.findAll("button").some((button) => button.text().includes("Reset")),
    ).toBe(false);
    wrapper.unmount();
  });
});

describe("NieListControls filter dropdown", () => {
  it("hides the dropdown when there are no filter groups with options", () => {
    const wrapper = mountControls({
      filterGroups: [{ key: "status", label: "Status", options: [] }],
    });

    expect(
      wrapper.findAll("button").some((button) => button.text().includes("Filters")),
    ).toBe(false);
    wrapper.unmount();
  });

  it("hides the dropdown when visibility is set to hidden", () => {
    const wrapper = mountControls({ filterDropdownVisibility: "hidden" });

    expect(
      wrapper.findAll("button").some((button) => button.text().includes("Filters")),
    ).toBe(false);
    wrapper.unmount();
  });

  it("shows the desktop trigger for the always mode", () => {
    const wrapper = mountControls();
    const trigger = wrapper
      .findAll("button")
      .find((button) => button.text().includes("Filters"));

    expect(trigger?.classes()).toContain("hidden");
    expect(trigger?.classes()).toContain("md:inline-flex");
    wrapper.unmount();
  });

  it("suppresses the desktop trigger for the mobile-only mode", () => {
    const wrapper = mountControls({ filterDropdownVisibility: "mobile-only" });

    expect(
      wrapper.findAll("button").some((button) => button.text().includes("Filters")),
    ).toBe(false);
    wrapper.unmount();
  });

  it("passes selections through from the dropdown", async () => {
    const wrapper = mountControls({ selectedFilters: { status: ["open"] } });

    await wrapper.setProps({ filterDropdownVisibility: "always" });
    await wrapper.findAllComponents({ name: "NieSmartFilterDropdown" })[0].vm
      .$emit("update:modelValue", { status: ["approved"] });

    expect(wrapper.emitted("update:selectedFilters")).toEqual([
      [{ status: ["approved"] }],
    ]);
    wrapper.unmount();
  });
});

describe("NieListControls mobile toolbar", () => {
  it("is absent on desktop", () => {
    const wrapper = mountControls();

    expect(wrapper.find(".nie-list-mobile-toolbar").exists()).toBe(false);
    wrapper.unmount();
  });

  it("appears on narrow viewports", async () => {
    setViewportWidth(400);
    const wrapper = mountControls();
    await nextTick();

    expect(wrapper.find(".nie-list-mobile-toolbar").exists()).toBe(true);
    wrapper.unmount();
  });

  it("stays hidden when nothing would go in it", async () => {
    setViewportWidth(400);
    const wrapper = mountControls({
      showSearch: false,
      filterGroups: [],
      mobileShowBackButton: false,
    });
    await nextTick();

    expect(wrapper.find(".nie-list-mobile-toolbar").exists()).toBe(false);
    wrapper.unmount();
  });

  it("appears and disappears as the viewport is resized", async () => {
    const wrapper = mountControls();
    expect(wrapper.find(".nie-list-mobile-toolbar").exists()).toBe(false);

    setViewportWidth(400);
    window.dispatchEvent(new Event("resize"));
    await nextTick();
    expect(wrapper.find(".nie-list-mobile-toolbar").exists()).toBe(true);

    setViewportWidth(1200);
    window.dispatchEvent(new Event("resize"));
    await nextTick();
    expect(wrapper.find(".nie-list-mobile-toolbar").exists()).toBe(false);
    wrapper.unmount();
  });

  it("labels its controls for screen readers", async () => {
    setViewportWidth(400);
    const wrapper = mountControls({
      mobileShowBackButton: true,
      mobileBackAriaLabel: "Back to dashboard",
      mobileSearchAriaLabel: "Search purchase orders",
      mobileFilterAriaLabel: "Open purchase order filters",
    });
    await nextTick();

    expect(
      wrapper.find('[aria-label="Back to dashboard"]').exists(),
    ).toBe(true);
    expect(
      wrapper.find('[aria-label="Search purchase orders"]').exists(),
    ).toBe(true);
    expect(
      wrapper.find('[aria-label="Open purchase order filters"]').exists(),
    ).toBe(true);
    wrapper.unmount();
  });

  it("emits back from the back button", async () => {
    setViewportWidth(400);
    const wrapper = mountControls({ mobileShowBackButton: true });
    await nextTick();

    await wrapper.get('[aria-label="Go back"]').trigger("click");

    expect(wrapper.emitted("back")).toHaveLength(1);
    wrapper.unmount();
  });

  it("mirrors the search term and emits edits", async () => {
    setViewportWidth(400);
    const wrapper = mountControls({ searchTerm: "acme" });
    await nextTick();

    const input = wrapper.get(".nie-list-mobile-toolbar__input");
    expect((input.element as HTMLInputElement).value).toBe("acme");

    await input.setValue("acme corp");

    expect(wrapper.emitted("update:searchTerm")).toEqual([["acme corp"]]);
    wrapper.unmount();
  });

  it("offers a clear control only while there is a search term", async () => {
    setViewportWidth(400);
    const empty = mountControls();
    await nextTick();
    expect(empty.find('[aria-label="Clear search"]').exists()).toBe(false);
    empty.unmount();

    const filled = mountControls({ searchTerm: "acme" });
    await nextTick();
    await filled.get('[aria-label="Clear search"]').trigger("click");
    expect(filled.emitted("update:searchTerm")).toEqual([[""]]);
    filled.unmount();
  });

  it("badges the filter button with the number of active filters", async () => {
    setViewportWidth(400);
    const wrapper = mountControls({
      selectedFilters: { status: ["open", "approved"], owner: ["ada"] },
    });
    await nextTick();

    const filter = wrapper.get(".nie-list-mobile-toolbar__filter");
    expect(filter.get(".nie-list-mobile-toolbar__badge").text()).toBe("3");
    expect(filter.classes()).toContain(
      "nie-list-mobile-toolbar__filter--active",
    );
    wrapper.unmount();
  });

  it("opens the filter sheet from the toolbar", async () => {
    setViewportWidth(400);
    const wrapper = mountControls();
    await nextTick();

    await wrapper.get(".nie-list-mobile-toolbar__filter").trigger("click");
    await nextTick();

    expect(document.querySelector(".nie-smart-filter-sheet")).not.toBeNull();
    wrapper.unmount();
  });

  it("closing the reset clears the open filter sheet too", async () => {
    setViewportWidth(400);
    const wrapper = mountControls({ searchTerm: "acme" });
    await nextTick();
    await wrapper.get(".nie-list-mobile-toolbar__filter").trigger("click");
    await nextTick();

    const reset = wrapper
      .findAll("button")
      .find((button) => button.text().includes("Reset"));
    await reset?.trigger("click");
    await nextTick();

    expect(document.querySelector(".nie-smart-filter-sheet")).toBeNull();
    wrapper.unmount();
  });
});

describe("NieListControls slots", () => {
  it("renders the actions and summary slots", () => {
    const wrapper = mountControls({
      summary: "Showing 4 of 12",
      filterGroups: [],
    });
    expect(wrapper.text()).toContain("Showing 4 of 12");
    wrapper.unmount();

    const withSlots = mount(NieListControls, {
      props: { filterGroups: [] } as ControlsProps,
      slots: {
        actions: () => h("button", { type: "button" }, "Export"),
        summary: () => h("span", { class: "extra" }, "Updated just now"),
      },
    });
    expect(withSlots.get("button").text()).toBe("Export");
    expect(withSlots.get(".extra").text()).toBe("Updated just now");
    withSlots.unmount();
  });
});

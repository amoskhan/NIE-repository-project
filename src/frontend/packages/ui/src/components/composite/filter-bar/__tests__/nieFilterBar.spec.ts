import { mount } from "@vue/test-utils";
import { describe, expect, it } from "vitest";
import { h } from "vue";
import NieFilterBar from "../../filter-bar/NieFilterBar.vue";
import type { FilterOption } from "../../filter-bar/NieFilterBar.vue";

const filters: FilterOption[] = [
  { label: "All", value: "all", count: 12 },
  { label: "Open", value: "open", count: 4 },
  { label: "Closed", value: "closed" },
];

describe("NieFilterBar search", () => {
  it("labels the search field with its placeholder", () => {
    const wrapper = mount(NieFilterBar);
    const search = wrapper.get('input[type="search"]');

    expect(search.attributes("aria-label")).toBe(
      "Search records, owners, or keywords",
    );
    expect(search.attributes("placeholder")).toBe(
      "Search records, owners, or keywords",
    );
  });

  it("uses a caller-supplied placeholder for both label and hint", () => {
    const wrapper = mount(NieFilterBar, {
      props: { searchPlaceholder: "Search purchase orders" },
    });

    expect(wrapper.get("input").attributes("aria-label")).toBe(
      "Search purchase orders",
    );
  });

  it("emits every keystroke", async () => {
    const wrapper = mount(NieFilterBar);

    await wrapper.get("input").setValue("acme");

    expect(wrapper.emitted("update:searchTerm")).toEqual([["acme"]]);
  });
});

describe("NieFilterBar filter chips", () => {
  it("renders no chips when no filters are configured", () => {
    expect(mount(NieFilterBar).findAll("button")).toHaveLength(0);
  });

  it("renders one chip per filter with its count", () => {
    const wrapper = mount(NieFilterBar, {
      props: { filters, activeFilter: "all" },
    });
    const chips = wrapper.findAll("button");

    expect(chips).toHaveLength(3);
    expect(chips[0].text()).toContain("All");
    expect(chips[0].text()).toContain("12");
    expect(chips[2].text()).toBe("Closed");
  });

  it("highlights only the active chip", () => {
    const wrapper = mount(NieFilterBar, {
      props: { filters, activeFilter: "open" },
    });
    const chips = wrapper.findAll("button");

    expect(chips[1].classes()).toContain("bg-primary-600");
    expect(chips[0].classes()).not.toContain("bg-primary-600");
  });

  it("emits the chosen filter", async () => {
    const wrapper = mount(NieFilterBar, {
      props: { filters, activeFilter: "all" },
    });

    await wrapper.findAll("button")[1].trigger("click");

    expect(wrapper.emitted("update:activeFilter")).toEqual([["open"]]);
  });
});

describe("NieFilterBar reset", () => {
  it("stays hidden while nothing is filtered", () => {
    const wrapper = mount(NieFilterBar, {
      props: { filters, activeFilter: "all", searchTerm: "" },
    });

    expect(wrapper.findAll("button")).toHaveLength(3);
  });

  it("treats whitespace-only search terms as no search", () => {
    const wrapper = mount(NieFilterBar, {
      props: { filters, activeFilter: "all", searchTerm: "   " },
    });

    expect(wrapper.findAll("button")).toHaveLength(3);
  });

  it("appears once a search term is entered", () => {
    const wrapper = mount(NieFilterBar, {
      props: { filters, activeFilter: "all", searchTerm: "acme" },
    });

    expect(wrapper.findAll("button")).toHaveLength(4);
    expect(wrapper.findAll("button")[3].text()).toContain("Reset");
  });

  it("appears once a non-default filter is chosen", () => {
    const wrapper = mount(NieFilterBar, {
      props: { filters, activeFilter: "open" },
    });

    expect(wrapper.findAll("button")[3].text()).toContain("Reset");
  });

  it("restores the search term and the first filter", async () => {
    const wrapper = mount(NieFilterBar, {
      props: { filters, activeFilter: "open", searchTerm: "acme" },
    });

    await wrapper.findAll("button")[3].trigger("click");

    expect(wrapper.emitted("update:searchTerm")).toEqual([[""]]);
    expect(wrapper.emitted("update:activeFilter")).toEqual([["all"]]);
    expect(wrapper.emitted("reset")).toHaveLength(1);
  });

  it("resets to an empty filter when no filters are configured", async () => {
    const wrapper = mount(NieFilterBar, { props: { searchTerm: "acme" } });

    await wrapper.get("button").trigger("click");

    expect(wrapper.emitted("update:activeFilter")).toEqual([[""]]);
  });

  it("can be suppressed entirely", () => {
    const wrapper = mount(NieFilterBar, {
      props: { filters, activeFilter: "open", showReset: false },
    });

    expect(wrapper.findAll("button")).toHaveLength(3);
  });
});

describe("NieFilterBar summary", () => {
  it("renders no summary row when there is nothing to summarise", () => {
    expect(mount(NieFilterBar).find("span").exists()).toBe(false);
  });

  it("renders the summary text", () => {
    const wrapper = mount(NieFilterBar, {
      props: { summary: "Showing 4 of 12 orders" },
    });

    expect(wrapper.text()).toContain("Showing 4 of 12 orders");
  });

  it("renders the summary and actions slots", () => {
    const wrapper = mount(NieFilterBar, {
      slots: {
        summary: () => h("span", { class: "extra" }, "Updated 2 minutes ago"),
        actions: () => h("button", { type: "button" }, "Export"),
      },
    });

    expect(wrapper.get(".extra").text()).toBe("Updated 2 minutes ago");
    expect(wrapper.get("button").text()).toBe("Export");
  });
});

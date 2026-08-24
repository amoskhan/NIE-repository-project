import { mount } from "@vue/test-utils";
import { afterEach, describe, expect, it, vi } from "vitest";
import { nextTick } from "vue";
import NieColumnFilterMenu from "../NieColumnFilterMenu.vue";
import type { NieDataTableFilterOption } from "../types";

const options: NieDataTableFilterOption[] = [
  { label: "Open", value: "open", count: 4 },
  { label: "Approved", value: "approved", count: 2 },
  { label: "Closed", value: "closed", count: 0 },
];

type MenuProps = InstanceType<typeof NieColumnFilterMenu>["$props"];

function mountMenu(props: Partial<MenuProps> = {}) {
  return mount(NieColumnFilterMenu, {
    attachTo: document.body,
    props: { columnLabel: "Status", options, ...props } as MenuProps,
  });
}

function panel(): HTMLElement | null {
  return document.querySelector('[role="dialog"]');
}

function valueButtons(): HTMLButtonElement[] {
  return [...document.querySelectorAll<HTMLButtonElement>("[data-filter-value]")];
}

afterEach(() => {
  document.body.innerHTML = "";
  vi.useRealTimers();
});

describe("NieColumnFilterMenu trigger", () => {
  it("labels itself with the column it filters", () => {
    const wrapper = mountMenu();
    const trigger = wrapper.get("button");

    expect(trigger.attributes("aria-label")).toBe("Filter Status");
    expect(trigger.attributes("aria-expanded")).toBe("false");
    wrapper.unmount();
  });

  it("shows the number of active selections", () => {
    const none = mountMenu();
    expect(none.get("button").text()).toBe("");
    none.unmount();

    const some = mountMenu({ modelValue: ["open", "approved"] });
    expect(some.get("button").text()).toBe("2");
    expect(some.get("button").classes()).toContain("bg-primary-50");
    some.unmount();
  });

  it("stays inert when there is nothing to filter by", async () => {
    const wrapper = mountMenu({ options: [] });

    await wrapper.get("button").trigger("click");

    expect(panel()).toBeNull();
    wrapper.unmount();
  });

  it("opens for a remote column even with no local options", async () => {
    const wrapper = mountMenu({ options: [], remote: true });

    await wrapper.get("button").trigger("click");
    await nextTick();

    expect(panel()).not.toBeNull();
    wrapper.unmount();
  });
});

describe("NieColumnFilterMenu panel", () => {
  it("opens and closes on the trigger", async () => {
    const wrapper = mountMenu();

    await wrapper.get("button").trigger("click");
    await nextTick();
    expect(panel()?.getAttribute("aria-label")).toBe("Filter Status values");
    expect(wrapper.get("button").attributes("aria-expanded")).toBe("true");

    await wrapper.get("button").trigger("click");
    await nextTick();
    expect(panel()).toBeNull();
    wrapper.unmount();
  });

  it("positions itself as a fixed popup", async () => {
    const wrapper = mountMenu();

    await wrapper.get("button").trigger("click");
    await nextTick();

    expect(panel()?.style.position).toBe("fixed");
    expect(panel()?.style.width).toBe("288px");
    expect(panel()?.style.zIndex).toBe("9999");
    wrapper.unmount();
  });

  it("lists one control per option with its count", async () => {
    const wrapper = mountMenu();
    await wrapper.get("button").trigger("click");
    await nextTick();

    const values = valueButtons();
    expect(values.map((button) => button.dataset.filterValue)).toEqual([
      "open",
      "approved",
      "closed",
    ]);
    expect(values[0].textContent).toContain("4");
    wrapper.unmount();
  });

  it("disables an empty option that is not already selected", async () => {
    const wrapper = mountMenu();
    await wrapper.get("button").trigger("click");
    await nextTick();

    expect(valueButtons()[2].disabled).toBe(true);
    wrapper.unmount();
    document.body.innerHTML = "";

    const selected = mountMenu({ modelValue: ["closed"] });
    await selected.get("button").trigger("click");
    await nextTick();

    expect(valueButtons()[2].disabled).toBe(false);
    selected.unmount();
  });

  it("shows the Clear control only once something is selected", async () => {
    const wrapper = mountMenu();
    await wrapper.get("button").trigger("click");
    await nextTick();
    expect(panel()?.textContent).not.toContain("Clear");
    wrapper.unmount();
    document.body.innerHTML = "";

    const selected = mountMenu({ modelValue: ["open"] });
    await selected.get("button").trigger("click");
    await nextTick();
    expect(panel()?.textContent).toContain("Clear");
    selected.unmount();
  });
});

describe("NieColumnFilterMenu selection", () => {
  it("adds a value that was not selected", async () => {
    const wrapper = mountMenu({ modelValue: ["open"] });
    await wrapper.get("button").trigger("click");
    await nextTick();

    valueButtons()[1].click();

    expect(wrapper.emitted("update:modelValue")).toEqual([
      [["open", "approved"]],
    ]);
    wrapper.unmount();
  });

  it("removes a value that was already selected", async () => {
    const wrapper = mountMenu({ modelValue: ["open", "approved"] });
    await wrapper.get("button").trigger("click");
    await nextTick();

    valueButtons()[0].click();

    expect(wrapper.emitted("update:modelValue")).toEqual([[["approved"]]]);
    wrapper.unmount();
  });

  it("compares values loosely so numeric options still match", async () => {
    const wrapper = mountMenu({
      options: [{ label: "One", value: 1 }],
      modelValue: ["1"],
    });
    await wrapper.get("button").trigger("click");
    await nextTick();

    valueButtons()[0].click();

    expect(wrapper.emitted("update:modelValue")).toEqual([[[]]]);
    wrapper.unmount();
  });

  it("clears every selection at once", async () => {
    const wrapper = mountMenu({ modelValue: ["open", "approved"] });
    await wrapper.get("button").trigger("click");
    await nextTick();

    panel()
      ?.querySelectorAll<HTMLButtonElement>("button")[0]
      .click();

    expect(wrapper.emitted("update:modelValue")).toEqual([[[]]]);
    wrapper.unmount();
  });
});

describe("NieColumnFilterMenu local search", () => {
  it("narrows the options by label or value", async () => {
    const wrapper = mountMenu();
    await wrapper.get("button").trigger("click");
    await nextTick();

    const search = panel()?.querySelector<HTMLInputElement>('input[type="search"]');
    if (search) {
      search.value = "appro";
      search.dispatchEvent(new Event("input"));
    }
    await nextTick();

    expect(valueButtons().map((button) => button.dataset.filterValue)).toEqual([
      "approved",
    ]);
    wrapper.unmount();
  });

  it("tells the user when nothing matches", async () => {
    const wrapper = mountMenu();
    await wrapper.get("button").trigger("click");
    await nextTick();

    const search = panel()?.querySelector<HTMLInputElement>('input[type="search"]');
    if (search) {
      search.value = "zzz";
      search.dispatchEvent(new Event("input"));
    }
    await nextTick();

    expect(panel()?.textContent).toContain("No matching values.");
    wrapper.unmount();
  });

  it("caps the local list and says how many are hidden", async () => {
    const many = Array.from({ length: 60 }, (_, index) => ({
      label: `Option ${index}`,
      value: `option-${index}`,
    }));
    const wrapper = mountMenu({ options: many });

    await wrapper.get("button").trigger("click");
    await nextTick();

    expect(valueButtons()).toHaveLength(50);
    expect(panel()?.textContent).toContain("Showing first 50");
    wrapper.unmount();
  });

  it("forgets the search when the panel is closed", async () => {
    const wrapper = mountMenu();
    await wrapper.get("button").trigger("click");
    await nextTick();
    const search = panel()?.querySelector<HTMLInputElement>('input[type="search"]');
    if (search) {
      search.value = "appro";
      search.dispatchEvent(new Event("input"));
    }
    await nextTick();

    await wrapper.get("button").trigger("click");
    await nextTick();
    await wrapper.get("button").trigger("click");
    await nextTick();

    expect(valueButtons()).toHaveLength(3);
    wrapper.unmount();
  });
});

describe("NieColumnFilterMenu remote options", () => {
  it("asks for the first page as soon as it opens", async () => {
    const wrapper = mountMenu({ remote: true, pageSize: 10 });

    await wrapper.get("button").trigger("click");
    await nextTick();

    expect(wrapper.emitted("request-options")).toEqual([
      [{ page: 1, pageSize: 10, search: "" }],
    ]);
    wrapper.unmount();
  });

  it("debounces the remote search", async () => {
    vi.useFakeTimers();
    const wrapper = mountMenu({ remote: true, pageSize: 10 });
    await wrapper.get("button").trigger("click");
    await nextTick();

    const search = panel()?.querySelector<HTMLInputElement>('input[type="search"]');
    if (search) {
      search.value = "ap";
      search.dispatchEvent(new Event("input"));
    }
    await nextTick();
    if (search) {
      search.value = "appro";
      search.dispatchEvent(new Event("input"));
    }
    await nextTick();

    expect(wrapper.emitted("request-options")).toHaveLength(1);

    vi.advanceTimersByTime(250);

    expect(wrapper.emitted("request-options")).toEqual([
      [{ page: 1, pageSize: 10, search: "" }],
      [{ page: 1, pageSize: 10, search: "appro" }],
    ]);
    wrapper.unmount();
  });

  it("shows the loading and error states", async () => {
    const loading = mountMenu({ remote: true, loading: true });
    await loading.get("button").trigger("click");
    await nextTick();
    expect(panel()?.textContent).toContain("Loading values...");
    loading.unmount();
    document.body.innerHTML = "";

    const failed = mountMenu({ remote: true, error: "Could not load values." });
    await failed.get("button").trigger("click");
    await nextTick();
    expect(panel()?.textContent).toContain("Could not load values.");
    failed.unmount();
  });

  it("pages through remote values", async () => {
    const wrapper = mountMenu({
      remote: true,
      page: 2,
      pageSize: 25,
      totalCount: 60,
      totalPages: 3,
    });
    await wrapper.get("button").trigger("click");
    await nextTick();

    expect(panel()?.textContent).toContain("Page 2 of 3");
    expect(panel()?.textContent).toContain("60 values");

    document
      .querySelector<HTMLButtonElement>(
        '[aria-label="Previous filter values page"]',
      )
      ?.click();
    document
      .querySelector<HTMLButtonElement>('[aria-label="Next filter values page"]')
      ?.click();

    expect(wrapper.emitted("request-options")?.slice(1)).toEqual([
      [{ page: 1, pageSize: 25, search: "" }],
      [{ page: 3, pageSize: 25, search: "" }],
    ]);
    wrapper.unmount();
  });

  it("disables paging at the ends of the range", async () => {
    const wrapper = mountMenu({
      remote: true,
      page: 1,
      totalCount: 10,
      totalPages: 1,
    });
    await wrapper.get("button").trigger("click");
    await nextTick();

    expect(
      document.querySelector<HTMLButtonElement>(
        '[aria-label="Previous filter values page"]',
      )?.disabled,
    ).toBe(true);
    expect(
      document.querySelector<HTMLButtonElement>(
        '[aria-label="Next filter values page"]',
      )?.disabled,
    ).toBe(true);
    wrapper.unmount();
  });
});

describe("NieColumnFilterMenu dismissal", () => {
  it("closes when the user clicks outside", async () => {
    const wrapper = mountMenu();
    await wrapper.get("button").trigger("click");
    await nextTick();

    document.body.dispatchEvent(new MouseEvent("mousedown", { bubbles: true }));
    await nextTick();

    expect(panel()).toBeNull();
    wrapper.unmount();
  });

  it("stays open for clicks inside the panel", async () => {
    const wrapper = mountMenu();
    await wrapper.get("button").trigger("click");
    await nextTick();

    panel()?.dispatchEvent(new MouseEvent("mousedown", { bubbles: true }));
    await nextTick();

    expect(panel()).not.toBeNull();
    wrapper.unmount();
  });

  it("closes on Escape and returns focus to the trigger", async () => {
    const wrapper = mountMenu();
    await wrapper.get("button").trigger("click");
    await nextTick();

    document.dispatchEvent(new KeyboardEvent("keydown", { key: "Escape" }));
    await nextTick();

    expect(panel()).toBeNull();
    expect(document.activeElement).toBe(wrapper.get("button").element);
    wrapper.unmount();
  });

  it("stops listening after unmount", async () => {
    const wrapper = mountMenu();
    await wrapper.get("button").trigger("click");
    await nextTick();
    wrapper.unmount();

    expect(() => {
      document.dispatchEvent(new KeyboardEvent("keydown", { key: "Escape" }));
      window.dispatchEvent(new Event("resize"));
    }).not.toThrow();
  });

  it("repositions itself while the page scrolls or resizes", async () => {
    const wrapper = mountMenu();
    await wrapper.get("button").trigger("click");
    await nextTick();

    expect(() => {
      window.dispatchEvent(new Event("resize"));
      window.dispatchEvent(new Event("scroll"));
    }).not.toThrow();
    expect(panel()?.style.position).toBe("fixed");
    wrapper.unmount();
  });
});

import { mount } from "@vue/test-utils";
import { afterEach, describe, expect, it } from "vitest";
import { nextTick } from "vue";
import NieSmartFilterDropdown from "../../smart-filter-dropdown/NieSmartFilterDropdown.vue";

const groups = [
  {
    key: "status",
    label: "Status",
    options: [
      { label: "Open", value: "open", count: 4 },
      { label: "Approved", value: "approved" },
      { label: "Closed", value: "closed", count: 0 },
    ],
  },
  {
    key: "owner",
    label: "Owner",
    options: [{ label: "Ada", value: "ada" }],
  },
];

type DropdownProps = InstanceType<typeof NieSmartFilterDropdown>["$props"];

function setViewportWidth(width: number): void {
  Object.defineProperty(window, "innerWidth", {
    configurable: true,
    writable: true,
    value: width,
  });
}

function mountDropdown(props: Partial<DropdownProps> = {}) {
  return mount(NieSmartFilterDropdown, {
    attachTo: document.body,
    props: { groups, ...props } as DropdownProps,
  });
}

afterEach(() => {
  setViewportWidth(1024);
  document.body.innerHTML = "";
});

describe("NieSmartFilterDropdown trigger", () => {
  it("renders nothing when no group has options", () => {
    const wrapper = mountDropdown({
      groups: [{ key: "status", label: "Status", options: [] }],
    });

    expect(wrapper.find("button").exists()).toBe(false);
    wrapper.unmount();
  });

  it("skips groups that have no options", () => {
    const wrapper = mountDropdown({
      open: true,
      groups: [...groups, { key: "empty", label: "Empty", options: [] }],
    });

    expect(wrapper.findAll("section")).toHaveLength(2);
    wrapper.unmount();
  });

  it("shows the total number of selected values", async () => {
    const wrapper = mountDropdown({
      modelValue: { status: ["open", "approved"], owner: ["ada"] },
    });

    expect(wrapper.get("button").text()).toContain("3");
    wrapper.unmount();
  });

  it("uses a caller-supplied button label", () => {
    const wrapper = mountDropdown({ buttonLabel: "Refine" });

    expect(wrapper.get("button").text()).toContain("Refine");
    wrapper.unmount();
  });

  it("hides the trigger for the hidden and desktop-only modes", () => {
    const hidden = mountDropdown({ triggerVisibility: "hidden" });
    expect(hidden.find("button").exists()).toBe(false);
    hidden.unmount();

    const desktopOnly = mountDropdown({ triggerVisibility: "desktop-only" });
    expect(desktopOnly.get("button").classes()).toContain("hidden");
    desktopOnly.unmount();
  });
});

describe("NieSmartFilterDropdown open state", () => {
  it("asks the parent to open it rather than opening itself", async () => {
    // `open` is a Boolean prop, so Vue casts an absent value to false: the
    // panel only ever opens when the parent feeds the new state back in.
    const wrapper = mountDropdown();

    await wrapper.get("button").trigger("click");

    expect(wrapper.emitted("update:open")).toEqual([[true]]);
    expect(wrapper.find('input[type="checkbox"]').exists()).toBe(false);
    wrapper.unmount();
  });

  it("opens once the parent hands the new state back", async () => {
    const wrapper = mountDropdown({ open: false });

    await wrapper.get("button").trigger("click");
    await wrapper.setProps({ open: true });
    expect(wrapper.findAll('input[type="checkbox"]')).toHaveLength(4);

    await wrapper.get("button").trigger("click");
    expect(wrapper.emitted("update:open")).toEqual([[true], [false]]);
    wrapper.unmount();
  });

  it("asks to close when the user clicks outside", async () => {
    const wrapper = mountDropdown({ open: true });

    document.body.dispatchEvent(new MouseEvent("mousedown", { bubbles: true }));
    await nextTick();

    expect(wrapper.emitted("update:open")).toEqual([[false]]);
    wrapper.unmount();
  });

  it("stays open for clicks inside the panel", async () => {
    const wrapper = mountDropdown({ open: true });

    wrapper.element.dispatchEvent(
      new MouseEvent("mousedown", { bubbles: true }),
    );
    await nextTick();

    expect(wrapper.emitted("update:open")).toBeUndefined();
    expect(wrapper.findAll('input[type="checkbox"]')).toHaveLength(4);
    wrapper.unmount();
  });

  it("stops listening after unmount", () => {
    const wrapper = mountDropdown({ open: true });
    wrapper.unmount();

    expect(() => {
      document.body.dispatchEvent(
        new MouseEvent("mousedown", { bubbles: true }),
      );
      window.dispatchEvent(new Event("resize"));
    }).not.toThrow();
  });
});

describe("NieSmartFilterDropdown selection", () => {
  it("adds a value to its group", async () => {
    const wrapper = mountDropdown({ open: true });

    await wrapper.findAll('input[type="checkbox"]')[1].trigger("change");

    expect(wrapper.emitted("update:modelValue")).toEqual([
      [{ status: ["approved"] }],
    ]);
    wrapper.unmount();
  });

  it("removes a value and drops the group once it is empty", async () => {
    const wrapper = mountDropdown({
      open: true,
      modelValue: { status: ["open"], owner: ["ada"] },
    });

    await wrapper.findAll('input[type="checkbox"]')[0].trigger("change");

    expect(wrapper.emitted("update:modelValue")).toEqual([
      [{ owner: ["ada"] }],
    ]);
    wrapper.unmount();
  });

  it("keeps the other values in the group", async () => {
    const wrapper = mountDropdown({
      open: true,
      modelValue: { status: ["open", "approved"] },
    });

    await wrapper.findAll('input[type="checkbox"]')[0].trigger("change");

    expect(wrapper.emitted("update:modelValue")).toEqual([
      [{ status: ["approved"] }],
    ]);
    wrapper.unmount();
  });

  it("reflects the current selection in the checkboxes", () => {
    const wrapper = mountDropdown({
      open: true,
      modelValue: { status: ["approved"] },
    });

    expect(
      wrapper
        .findAll('input[type="checkbox"]')
        .map((box) => (box.element as HTMLInputElement).checked),
    ).toEqual([false, true, false, false]);
    wrapper.unmount();
  });

  it("disables an exhausted option that is not already selected", async () => {
    const wrapper = mountDropdown({ open: true });
    const closed = wrapper.findAll('input[type="checkbox"]')[2];

    expect((closed.element as HTMLInputElement).disabled).toBe(true);
    await closed.trigger("change");
    expect(wrapper.emitted("update:modelValue")).toBeUndefined();
    wrapper.unmount();
  });

  it("keeps an exhausted option usable while it is selected", () => {
    const wrapper = mountDropdown({
      open: true,
      modelValue: { status: ["closed"] },
    });

    expect(
      (wrapper.findAll('input[type="checkbox"]')[2].element as HTMLInputElement)
        .disabled,
    ).toBe(false);
    wrapper.unmount();
  });

  it("clears every group at once", async () => {
    const wrapper = mountDropdown({
      open: true,
      modelValue: { status: ["open"], owner: ["ada"] },
    });

    await wrapper.get("section").element.parentElement?.previousElementSibling
      ?.querySelector("button")
      ?.click();

    expect(wrapper.emitted("update:modelValue")).toEqual([[{}]]);
    wrapper.unmount();
  });
});

describe("NieSmartFilterDropdown mobile sheet", () => {
  it("replaces the popover with a bottom sheet on narrow viewports", async () => {
    setViewportWidth(400);
    const wrapper = mountDropdown({ open: true });
    await nextTick();

    expect(wrapper.find('input[type="checkbox"]').exists()).toBe(false);
    expect(document.querySelector(".nie-smart-filter-sheet")).not.toBeNull();
    expect(document.body.textContent).toContain("Refine the current list");
    wrapper.unmount();
  });

  it("reports how many filters are active", async () => {
    setViewportWidth(400);
    const wrapper = mountDropdown({
      open: true,
      modelValue: { status: ["open", "approved"] },
    });
    await nextTick();

    expect(document.body.textContent).toContain("2 selected");
    wrapper.unmount();
  });

  it("disables Clear all until something is selected", async () => {
    setViewportWidth(400);
    const wrapper = mountDropdown({ open: true });
    await nextTick();

    const clear = [
      ...document.querySelectorAll<HTMLButtonElement>(
        ".nie-smart-filter-sheet button",
      ),
    ].find((button) => button.textContent?.includes("Clear all"));
    expect(clear?.disabled).toBe(true);
    wrapper.unmount();
  });

  it("closes from the scrim and from the close button", async () => {
    setViewportWidth(400);
    const wrapper = mountDropdown({ open: true });
    await nextTick();

    const closers = [
      ...document.querySelectorAll<HTMLButtonElement>(
        '[aria-label="Close filters"]',
      ),
    ];
    expect(closers).toHaveLength(2);
    closers[0].click();
    closers[1].click();

    expect(wrapper.emitted("update:open")).toEqual([[false], [false]]);
    wrapper.unmount();
  });

  it("switches back to the popover when the viewport widens", async () => {
    setViewportWidth(400);
    const wrapper = mountDropdown({ open: true });
    await nextTick();
    expect(document.querySelector(".nie-smart-filter-sheet")).not.toBeNull();

    setViewportWidth(1200);
    window.dispatchEvent(new Event("resize"));
    await nextTick();

    expect(document.querySelector(".nie-smart-filter-sheet")).toBeNull();
    expect(wrapper.findAll('input[type="checkbox"]')).toHaveLength(4);
    wrapper.unmount();
  });
});

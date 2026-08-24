import { mount } from "@vue/test-utils";
import { describe, expect, it } from "vitest";
import { nextTick } from "vue";
import NieTabs from "../../tabs/NieTabs.vue";
import type { NieTabItem } from "../../tabs/types";

type TabId = "policies" | "templates" | "delivery";

const items: NieTabItem<TabId>[] = [
  { id: "policies", label: "Policies", icon: "tune", panelId: "policies-panel" },
  { id: "templates", label: "Templates" },
  { id: "delivery", label: "Delivery", count: 4 },
];

function mountTabs(
  overrides: Partial<{
    modelValue: TabId;
    items: NieTabItem<TabId>[];
    idPrefix: string;
  }> = {},
) {
  return mount(NieTabs<TabId>, {
    attachTo: document.body,
    props: {
      modelValue: "policies",
      items,
      ariaLabel: "Notification administration",
      ...overrides,
    },
  });
}

describe("NieTabs structure", () => {
  it("renders an ARIA tablist with one tab per item", () => {
    const wrapper = mountTabs();

    expect(wrapper.get('[role="tablist"]').attributes("aria-label")).toBe(
      "Notification administration",
    );
    expect(wrapper.findAll('[role="tab"]')).toHaveLength(3);
  });

  it("marks only the selected tab as selected and focusable", () => {
    const tabs = mountTabs({ modelValue: "templates" }).findAll('[role="tab"]');

    expect(tabs.map((tab) => tab.attributes("aria-selected"))).toEqual([
      "false",
      "true",
      "false",
    ]);
    expect(tabs.map((tab) => tab.attributes("tabindex"))).toEqual([
      "-1",
      "0",
      "-1",
    ]);
    expect(tabs[1].classes()).toContain("nie-tabs__tab--active");
  });

  it("links a tab to its panel only when a panel id is given", () => {
    const tabs = mountTabs().findAll('[role="tab"]');

    expect(tabs[0].attributes("aria-controls")).toBe("policies-panel");
    expect(tabs[1].attributes("aria-controls")).toBeUndefined();
  });

  it("renders the optional icon and count", () => {
    const tabs = mountTabs().findAll('[role="tab"]');

    expect(tabs[0].get(".nie-tabs__icon").attributes("aria-hidden")).toBe(
      "true",
    );
    expect(tabs[1].find(".nie-tabs__icon").exists()).toBe(false);
    expect(tabs[2].get(".nie-tabs__count").text()).toBe("4");
    expect(tabs[0].find(".nie-tabs__count").exists()).toBe(false);
  });

  it("derives unique tab ids and honours an explicit prefix", () => {
    const generated = mountTabs()
      .findAll('[role="tab"]')
      .map((tab) => tab.attributes("id"));
    expect(new Set(generated).size).toBe(3);

    const prefixed = mountTabs({ idPrefix: "  notifications  " })
      .findAll('[role="tab"]')
      .map((tab) => tab.attributes("id"));
    expect(prefixed).toEqual([
      "notifications-policies",
      "notifications-templates",
      "notifications-delivery",
    ]);
  });
});

describe("NieTabs selection", () => {
  it("updates the model when a tab is clicked", async () => {
    const wrapper = mountTabs();

    await wrapper.findAll('[role="tab"]')[1].trigger("click");

    expect(wrapper.emitted("update:modelValue")).toEqual([["templates"]]);
  });

  it("ignores clicks on a disabled tab", async () => {
    const wrapper = mountTabs({
      items: [items[0], { ...items[1], disabled: true }, items[2]],
    });
    const disabledTab = wrapper.findAll('[role="tab"]')[1];

    expect(disabledTab.attributes("disabled")).toBeDefined();
    await disabledTab.trigger("click");

    expect(wrapper.emitted("update:modelValue")).toBeUndefined();
  });
});

describe("NieTabs keyboard navigation", () => {
  it("moves focus with the arrow keys and wraps around", async () => {
    const wrapper = mountTabs();
    const tabs = wrapper.findAll('[role="tab"]');

    await tabs[0].trigger("keydown", { key: "ArrowRight" });
    expect(document.activeElement).toBe(tabs[1].element);

    await tabs[2].trigger("keydown", { key: "ArrowRight" });
    expect(document.activeElement).toBe(tabs[0].element);

    await tabs[0].trigger("keydown", { key: "ArrowLeft" });
    expect(document.activeElement).toBe(tabs[2].element);

    wrapper.unmount();
  });

  it("jumps to the first and last tab with Home and End", async () => {
    const wrapper = mountTabs();
    const tabs = wrapper.findAll('[role="tab"]');

    await tabs[1].trigger("keydown", { key: "End" });
    expect(document.activeElement).toBe(tabs[2].element);

    await tabs[2].trigger("keydown", { key: "Home" });
    expect(document.activeElement).toBe(tabs[0].element);

    wrapper.unmount();
  });

  it("skips disabled tabs while roving", async () => {
    const wrapper = mountTabs({
      items: [items[0], { ...items[1], disabled: true }, items[2]],
    });
    const tabs = wrapper.findAll('[role="tab"]');

    await tabs[0].trigger("keydown", { key: "ArrowRight" });
    expect(document.activeElement).toBe(tabs[2].element);

    await tabs[2].trigger("keydown", { key: "ArrowLeft" });
    expect(document.activeElement).toBe(tabs[0].element);

    wrapper.unmount();
  });

  it("skips disabled tabs at the boundaries", async () => {
    const wrapper = mountTabs({
      items: [{ ...items[0], disabled: true }, items[1], items[2]],
      modelValue: "templates",
    });
    const tabs = wrapper.findAll('[role="tab"]');

    await tabs[2].trigger("keydown", { key: "Home" });
    expect(document.activeElement).toBe(tabs[1].element);

    wrapper.unmount();
  });

  it("leaves other keys to the browser", async () => {
    const wrapper = mountTabs();
    const tabs = wrapper.findAll<HTMLButtonElement>('[role="tab"]');
    tabs[0].element.focus();

    await tabs[0].trigger("keydown", { key: "ArrowDown" });
    await nextTick();

    expect(document.activeElement).toBe(tabs[0].element);

    wrapper.unmount();
  });
});

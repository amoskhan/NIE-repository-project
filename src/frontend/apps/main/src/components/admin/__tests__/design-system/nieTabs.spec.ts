import { mount } from "@vue/test-utils";
import { describe, expect, it } from "vitest";
import { nextTick } from "vue";
import { NieTabs, type NieTabItem } from "@nie/ui";
import { readFileSync } from "node:fs";
import { resolve } from "node:path";

type TabId = "policies" | "templates" | "delivery";

const items: NieTabItem<TabId>[] = [
  {
    id: "policies",
    label: "Policies",
    icon: "tune",
    panelId: "policies-panel",
  },
  { id: "templates", label: "Email templates", icon: "mail" },
  { id: "delivery", label: "Delivery", icon: "outbox", count: 4 },
];

describe("NieTabs", () => {
  it("keeps overflow swipeable without exposing a native scrollbar", () => {
    const source = readFileSync(
      resolve(
        process.cwd(),
        "../../packages/ui/src/components/ui/tabs/NieTabs.vue",
      ),
      "utf8",
    );

    expect(source).toMatch(/\.nie-tabs\s*\{[\s\S]*scrollbar-width:\s*none/);
    expect(source).toContain(".nie-tabs::-webkit-scrollbar");
  });

  it("exposes the shared tab UI and updates the typed model", async () => {
    const wrapper = mount(NieTabs, {
      attachTo: document.body,
      props: {
        modelValue: "policies",
        items,
        ariaLabel: "Notification administration",
        "onUpdate:modelValue": (value: TabId) =>
          wrapper.setProps({ modelValue: value }),
      },
    });

    expect(wrapper.get('[role="tablist"]').attributes("aria-label")).toBe(
      "Notification administration",
    );

    const tabs = wrapper.findAll('[role="tab"]');
    expect(tabs).toHaveLength(3);
    expect(tabs[0].attributes("aria-selected")).toBe("true");
    expect(tabs[0].attributes("tabindex")).toBe("0");
    expect(tabs[0].attributes("aria-controls")).toBe("policies-panel");
    expect(tabs[2].text()).toContain("4");

    await tabs[1].trigger("click");
    await nextTick();

    expect(wrapper.emitted("update:modelValue")?.at(-1)).toEqual(["templates"]);
    expect(wrapper.findAll('[role="tab"]')[1].attributes("aria-selected")).toBe(
      "true",
    );

    wrapper.unmount();
  });

  it("supports arrow, Home, and End keyboard navigation", async () => {
    const wrapper = mount(NieTabs, {
      attachTo: document.body,
      props: {
        modelValue: "policies",
        items,
        ariaLabel: "Notification administration",
      },
    });
    const tabs = wrapper.findAll<HTMLButtonElement>('[role="tab"]');

    tabs[0].element.focus();
    await tabs[0].trigger("keydown", { key: "ArrowRight" });
    expect(document.activeElement).toBe(tabs[1].element);

    await tabs[1].trigger("keydown", { key: "End" });
    expect(document.activeElement).toBe(tabs[2].element);

    await tabs[2].trigger("keydown", { key: "Home" });
    expect(document.activeElement).toBe(tabs[0].element);

    wrapper.unmount();
  });
});

import { mount } from "@vue/test-utils";
import { describe, expect, it } from "vitest";
import NieButton from "../../button/NieButton.vue";

describe("NieButton rendering", () => {
  it("renders a non-submitting button by default", () => {
    const wrapper = mount(NieButton, { slots: { default: "Save" } });

    expect(wrapper.element.tagName).toBe("BUTTON");
    expect(wrapper.attributes("type")).toBe("button");
    expect(wrapper.attributes("data-nie-control")).toBe("button");
    expect(wrapper.text()).toBe("Save");
  });

  it("honours an explicit button type", () => {
    const wrapper = mount(NieButton, { props: { type: "submit" } });

    expect(wrapper.attributes("type")).toBe("submit");
  });

  it("applies the variant and size classes", () => {
    const primary = mount(NieButton);
    expect(primary.classes()).toContain("bg-primary-600");

    const danger = mount(NieButton, { props: { variant: "danger", size: "lg" } });
    expect(danger.classes()).toContain("bg-status-danger");
    expect(danger.classes()).toContain("min-h-12");

    const outline = mount(NieButton, { props: { variant: "outline", size: "sm" } });
    expect(outline.classes()).toContain("border");
    expect(outline.classes()).toContain("min-h-10");
  });

  it("keeps every size at or above the 40px touch target", () => {
    for (const [size, expected] of [
      ["sm", "min-h-10"],
      ["md", "min-h-11"],
      ["lg", "min-h-12"],
    ] as const) {
      expect(mount(NieButton, { props: { size } }).classes()).toContain(
        expected,
      );
    }
  });

  it("merges a caller-supplied class", () => {
    const wrapper = mount(NieButton, { props: { class: "w-full" } });

    expect(wrapper.classes()).toContain("w-full");
  });
});

describe("NieButton loading state", () => {
  it("shows a labelled loading indicator and disables the control", () => {
    const wrapper = mount(NieButton, {
      props: { loading: true },
      slots: { default: "Save" },
    });

    const loader = wrapper.get('[data-testid="nie-loader-symbol"]');
    expect(loader.attributes("role")).toBe("status");
    expect(loader.attributes("aria-label")).toBe("Loading");
    expect(wrapper.attributes("disabled")).toBeDefined();
  });

  it("shows no indicator when idle", () => {
    const wrapper = mount(NieButton, { slots: { default: "Save" } });

    expect(wrapper.find('[data-testid="nie-loader-symbol"]').exists()).toBe(
      false,
    );
  });
});

describe("NieButton click handling", () => {
  it("emits the native event when enabled", async () => {
    const wrapper = mount(NieButton);

    await wrapper.trigger("click");

    expect(wrapper.emitted("click")).toHaveLength(1);
  });

  it("does not emit while disabled", async () => {
    const wrapper = mount(NieButton, { props: { disabled: true } });

    await wrapper.trigger("click");

    expect(wrapper.emitted("click")).toBeUndefined();
    expect(wrapper.attributes("disabled")).toBeDefined();
  });

  it("does not emit while loading", async () => {
    const wrapper = mount(NieButton, { props: { loading: true } });

    await wrapper.trigger("click");

    expect(wrapper.emitted("click")).toBeUndefined();
  });
});

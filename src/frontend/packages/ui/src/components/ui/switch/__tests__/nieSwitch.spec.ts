import { mount } from "@vue/test-utils";
import { describe, expect, it } from "vitest";
import NieSwitch from "../../switch/NieSwitch.vue";

describe("NieSwitch accessibility", () => {
  it("exposes the ARIA switch role and state", () => {
    const off = mount(NieSwitch, { props: { modelValue: false } });
    const on = mount(NieSwitch, { props: { modelValue: true } });

    expect(off.get("button").attributes("role")).toBe("switch");
    expect(off.get("button").attributes("aria-checked")).toBe("false");
    expect(on.get("button").attributes("aria-checked")).toBe("true");
  });

  it("falls back to a generic aria-label when nothing is provided", () => {
    const wrapper = mount(NieSwitch, { props: { modelValue: false } });

    expect(wrapper.get("button").attributes("aria-label")).toBe(
      "Toggle setting",
    );
  });

  it("uses an explicit aria-label over the fallback", () => {
    const wrapper = mount(NieSwitch, {
      props: { modelValue: false, ariaLabel: "Enable notifications" },
    });

    expect(wrapper.get("button").attributes("aria-label")).toBe(
      "Enable notifications",
    );
  });

  it("drops aria-label when a visible label already names the control", () => {
    const wrapper = mount(NieSwitch, {
      props: { modelValue: false, label: "Enable notifications" },
    });

    expect(wrapper.get("button").attributes("aria-label")).toBeUndefined();
    expect(wrapper.get("label").text()).toBe("Enable notifications");
  });

  it("keeps the whole control at least 44px tall", () => {
    const wrapper = mount(NieSwitch, { props: { modelValue: false } });

    expect(wrapper.get("label").classes()).toContain("min-h-11");
  });
});

describe("NieSwitch toggling", () => {
  it("emits the inverted value on click", async () => {
    const wrapper = mount(NieSwitch, { props: { modelValue: false } });

    await wrapper.get("button").trigger("click");

    expect(wrapper.emitted("update:modelValue")).toEqual([[true]]);
  });

  it("emits false when switching off", async () => {
    const wrapper = mount(NieSwitch, { props: { modelValue: true } });

    await wrapper.get("button").trigger("click");

    expect(wrapper.emitted("update:modelValue")).toEqual([[false]]);
  });

  it("stays put while disabled", async () => {
    const wrapper = mount(NieSwitch, {
      props: { modelValue: false, disabled: true },
    });

    await wrapper.get("button").trigger("click");

    expect(wrapper.emitted("update:modelValue")).toBeUndefined();
    expect(wrapper.get("button").attributes("disabled")).toBeDefined();
    expect(wrapper.get("button").classes()).toContain("cursor-not-allowed");
  });
});

describe("NieSwitch appearance", () => {
  it("tints the track from the state", () => {
    expect(
      mount(NieSwitch, { props: { modelValue: true } }).get("button").classes(),
    ).toContain("bg-primary-600");
    expect(
      mount(NieSwitch, { props: { modelValue: false } }).get("button").classes(),
    ).toContain("bg-secondary-200");
  });

  it("slides the knob only when switched on", () => {
    const on = mount(NieSwitch, { props: { modelValue: true, size: "lg" } });
    const off = mount(NieSwitch, { props: { modelValue: false, size: "lg" } });

    expect(on.get("button span").classes()).toContain("translate-x-8");
    expect(off.get("button span").classes()).toContain("translate-x-0");
  });

  it("sizes the track and knob together", () => {
    for (const [size, track, knob] of [
      ["sm", "h-5", "h-4"],
      ["md", "h-6", "h-5"],
      ["lg", "h-11", "h-7"],
    ] as const) {
      const wrapper = mount(NieSwitch, { props: { modelValue: false, size } });

      expect(wrapper.get("button").classes()).toContain(track);
      expect(wrapper.get("button span").classes()).toContain(knob);
    }
  });
});

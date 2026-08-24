import { mount } from "@vue/test-utils";
import { describe, expect, it } from "vitest";
import NieBadge from "../../badge/NieBadge.vue";
import type { NieBadgeVariant } from "../../badge/NieBadge.vue";

const variants: NieBadgeVariant[] = [
  "default",
  "primary",
  "success",
  "warning",
  "danger",
  "info",
];

describe("NieBadge", () => {
  it("renders its slot content inside a span", () => {
    const wrapper = mount(NieBadge, { slots: { default: "Approved" } });

    expect(wrapper.element.tagName).toBe("SPAN");
    expect(wrapper.text()).toBe("Approved");
    expect(wrapper.classes()).toContain("nie-badge");
    expect(wrapper.classes()).toContain("nie-badge--default");
  });

  it("carries a modifier class for every variant", () => {
    for (const variant of variants) {
      const wrapper = mount(NieBadge, { props: { variant } });

      expect(wrapper.classes()).toContain(`nie-badge--${variant}`);
    }
  });

  it("switches between pill and rounded-rectangle shapes", () => {
    expect(mount(NieBadge).classes()).toContain("rounded-md");
    expect(mount(NieBadge, { props: { rounded: true } }).classes()).toContain(
      "rounded-full",
    );
  });

  it("applies the requested size", () => {
    expect(mount(NieBadge, { props: { size: "sm" } }).classes()).toContain(
      "text-xs",
    );
    expect(mount(NieBadge, { props: { size: "lg" } }).classes()).toContain(
      "px-3",
    );
  });

  it("merges a caller-supplied class", () => {
    expect(mount(NieBadge, { props: { class: "ml-2" } }).classes()).toContain(
      "ml-2",
    );
  });
});

describe("NieBadge status dot", () => {
  it("is hidden by default", () => {
    const wrapper = mount(NieBadge, { slots: { default: "Draft" } });

    expect(wrapper.find('[data-testid="nie-badge-dot"]').exists()).toBe(false);
  });

  it("is decorative, so it is hidden from assistive technology", () => {
    const wrapper = mount(NieBadge, {
      props: { dot: true, variant: "success" },
      slots: { default: "Approved" },
    });

    const dot = wrapper.get('[data-testid="nie-badge-dot"]');
    expect(dot.attributes("aria-hidden")).toBe("true");
    expect(dot.classes()).toContain("bg-success-500");
    expect(wrapper.text()).toBe("Approved");
  });

  it("tints the dot per variant", () => {
    for (const variant of variants) {
      const wrapper = mount(NieBadge, { props: { dot: true, variant } });

      expect(
        wrapper.get('[data-testid="nie-badge-dot"]').classes().join(" "),
      ).toMatch(/bg-[a-z]+-\d00/);
    }
  });
});

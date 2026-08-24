import { mount } from "@vue/test-utils";
import { describe, expect, it } from "vitest";
import { h } from "vue";
import NieStatePanel from "../../state-panel/NieStatePanel.vue";
import type { StatePanelVariant } from "../../state-panel/NieStatePanel.vue";

const variants: StatePanelVariant[] = [
  "info",
  "success",
  "warning",
  "error",
  "empty",
  "loading",
];

describe("NieStatePanel", () => {
  it("renders the title as a heading and omits an empty description", () => {
    const wrapper = mount(NieStatePanel, { props: { title: "No orders yet" } });

    expect(wrapper.get("h3").text()).toBe("No orders yet");
    expect(wrapper.findAll("p")).toHaveLength(0);
  });

  it("renders the description when supplied", () => {
    const wrapper = mount(NieStatePanel, {
      props: { title: "No orders yet", description: "Create one to begin." },
    });

    expect(wrapper.get("p").text()).toBe("Create one to begin.");
  });

  it("shows the spinner only for the loading variant", () => {
    const loading = mount(NieStatePanel, {
      props: { title: "Loading", variant: "loading" },
    });
    expect(
      loading.get('[data-testid="nie-loader-symbol"]').attributes("aria-label"),
    ).toBe("Loading");
    expect(loading.find("svg.h-7").exists()).toBe(false);

    const empty = mount(NieStatePanel, {
      props: { title: "Nothing here", variant: "empty" },
    });
    expect(empty.find('[data-testid="nie-loader-symbol"]').exists()).toBe(false);
  });

  it("renders a distinct icon per non-loading variant", () => {
    const icons = variants
      .filter((variant) => variant !== "loading")
      .map((variant) =>
        mount(NieStatePanel, { props: { title: "x", variant } })
          .get("svg")
          .html(),
      );

    // warning and error deliberately share the same triangle icon.
    expect(new Set(icons).size).toBe(4);
  });

  it("tints the icon badge per variant", () => {
    const badges = variants.map(
      (variant) =>
        mount(NieStatePanel, { props: { title: "x", variant } })
          .get(".rounded-2xl.h-14")
          .classes()
          .find((entry) => entry.startsWith("bg-")) ?? "",
    );

    expect(badges).toContain("bg-success-100");
    expect(badges).toContain("bg-warning-100");
    expect(badges).toContain("bg-danger-100");
    expect(badges.filter((entry) => entry === "bg-primary-100")).toHaveLength(3);
  });

  it("gives each variant its own border treatment", () => {
    expect(
      mount(NieStatePanel, { props: { title: "x", variant: "error" } }).classes(),
    ).toContain("border-danger-200");
    expect(
      mount(NieStatePanel, { props: { title: "x", variant: "warning" } }).classes(),
    ).toContain("border-warning-200");
    expect(
      mount(NieStatePanel, { props: { title: "x", variant: "success" } }).classes(),
    ).toContain("border-success-200");
    expect(
      mount(NieStatePanel, { props: { title: "x", variant: "empty" } }).classes(),
    ).toContain("border-secondary-200");
  });

  it("switches to compact padding when asked", () => {
    expect(mount(NieStatePanel, { props: { title: "x" } }).classes()).toContain(
      "py-10",
    );
    expect(
      mount(NieStatePanel, { props: { title: "x", compact: true } }).classes(),
    ).toContain("py-6");
  });

  it("renders the actions region only when the slot is filled", () => {
    const bare = mount(NieStatePanel, { props: { title: "x" } });
    expect(bare.find("button").exists()).toBe(false);

    const withActions = mount(NieStatePanel, {
      props: { title: "x" },
      slots: { actions: () => h("button", { type: "button" }, "Retry") },
    });
    expect(withActions.get("button").text()).toBe("Retry");
  });

  it("merges a caller-supplied class", () => {
    expect(
      mount(NieStatePanel, { props: { title: "x", class: "mt-8" } }).classes(),
    ).toContain("mt-8");
  });
});

import { mount } from "@vue/test-utils";
import { describe, expect, it } from "vitest";
import NieAlert from "../../alert/NieAlert.vue";

describe("NieAlert", () => {
  it("always exposes the alert role", () => {
    const wrapper = mount(NieAlert, { slots: { default: "Saved" } });

    expect(wrapper.attributes("role")).toBe("alert");
    expect(wrapper.text()).toContain("Saved");
  });

  it("defaults to the informational variant", () => {
    const wrapper = mount(NieAlert);

    expect(wrapper.classes()).toContain("bg-info-50");
  });

  it("tints itself per variant", () => {
    for (const [variant, expected] of [
      ["info", "bg-info-50"],
      ["success", "bg-success-50"],
      ["warning", "bg-warning-50"],
      ["danger", "bg-danger-50"],
    ] as const) {
      expect(mount(NieAlert, { props: { variant } }).classes()).toContain(
        expected,
      );
    }
  });

  it("renders a distinct icon per variant", () => {
    const paths = (["info", "success", "warning", "danger"] as const).map(
      (variant) => mount(NieAlert, { props: { variant } }).get("svg").html(),
    );

    expect(new Set(paths).size).toBe(4);
  });

  it("renders the title only when supplied", () => {
    expect(mount(NieAlert).find("h3").exists()).toBe(false);

    const titled = mount(NieAlert, {
      props: { title: "Heads up" },
      slots: { default: "Body" },
    });
    expect(titled.get("h3").text()).toBe("Heads up");
  });

  it("merges a caller-supplied class", () => {
    expect(mount(NieAlert, { props: { class: "mb-4" } }).classes()).toContain(
      "mb-4",
    );
  });
});

describe("NieAlert dismissal", () => {
  it("has no dismiss control by default", () => {
    expect(mount(NieAlert).find("button").exists()).toBe(false);
  });

  it("offers a screen-reader-labelled dismiss control", async () => {
    const wrapper = mount(NieAlert, { props: { dismissible: true } });
    const button = wrapper.get("button");

    expect(button.attributes("type")).toBe("button");
    expect(button.get(".sr-only").text()).toBe("Dismiss");

    await button.trigger("click");

    expect(wrapper.emitted("dismiss")).toHaveLength(1);
  });
});

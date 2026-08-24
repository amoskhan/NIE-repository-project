import { mount } from "@vue/test-utils";
import { describe, expect, it } from "vitest";
import { NieLoaderSymbol } from "@nie/ui";

describe("branded NIE loader", () => {
  it("renders one orbit and the NIE-only monogram", () => {
    const wrapper = mount(NieLoaderSymbol, {
      props: {
        variant: "brand",
        size: "lg",
        label: "Loading procurement",
      },
    });

    expect(wrapper.get('[role="status"]').attributes("aria-label")).toBe(
      "Loading procurement",
    );
    expect(wrapper.findAll("[data-loader-orbit]")).toHaveLength(1);
    expect(wrapper.find("[data-loader-lion]").exists()).toBe(false);
    expect(
      wrapper.findAll("[data-loader-letter]").map((letter) => letter.text()),
    ).toEqual(["N", "I", "E"]);
    expect(wrapper.get("svg").attributes("aria-hidden")).toBe("true");
  });

  it("keeps the compact orbit loader as the default variant", () => {
    const wrapper = mount(NieLoaderSymbol, {
      props: { size: "sm" },
    });

    expect(wrapper.find("[data-loader-lion]").exists()).toBe(false);
    expect(wrapper.find(".nie-loader-symbol__compact").exists()).toBe(true);
  });
});

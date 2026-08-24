import { mount } from "@vue/test-utils";
import { describe, expect, it } from "vitest";
import NieLoaderSymbol from "../../loading/NieLoaderSymbol.vue";
import NieLoadingOverlay from "../../loading/NieLoadingOverlay.vue";
import type { NieLoaderSymbolTone, NieLoaderSymbolSize } from "../../loading/NieLoaderSymbol.vue";

describe("NieLoaderSymbol", () => {
  it("announces itself as a labelled status region", () => {
    const wrapper = mount(NieLoaderSymbol);

    expect(wrapper.attributes("role")).toBe("status");
    expect(wrapper.attributes("aria-label")).toBe("Loading");
    expect(wrapper.attributes("data-testid")).toBe("nie-loader-symbol");
  });

  it("uses a caller-supplied label", () => {
    const wrapper = mount(NieLoaderSymbol, {
      props: { label: "Loading purchase orders" },
    });

    expect(wrapper.attributes("aria-label")).toBe("Loading purchase orders");
  });

  it("renders the compact orbit artwork by default", () => {
    const wrapper = mount(NieLoaderSymbol);

    expect(wrapper.attributes("data-loader-variant")).toBe("orbit");
    expect(wrapper.find(".nie-loader-symbol__compact").exists()).toBe(true);
    expect(wrapper.find(".nie-loader-symbol__brand").exists()).toBe(false);
  });

  it("renders the animated NIE monogram for the brand variant", () => {
    const wrapper = mount(NieLoaderSymbol, { props: { variant: "brand" } });

    expect(wrapper.attributes("data-loader-variant")).toBe("brand");
    expect(wrapper.find(".nie-loader-symbol__brand").exists()).toBe(true);
    expect(wrapper.findAll("[data-loader-letter]").map((n) => n.text())).toEqual(
      ["N", "I", "E"],
    );
    expect(wrapper.find("[data-loader-orbit]").exists()).toBe(true);
  });

  it("keeps the artwork hidden from assistive technology", () => {
    for (const variant of ["orbit", "brand"] as const) {
      expect(
        mount(NieLoaderSymbol, { props: { variant } })
          .get("svg")
          .attributes("aria-hidden"),
      ).toBe("true");
    }
  });

  it("applies a size class for every size", () => {
    const sizes: Record<NieLoaderSymbolSize, string> = {
      xs: "h-4",
      sm: "h-5",
      md: "h-8",
      lg: "h-12",
      xl: "h-16",
    };

    for (const [size, expected] of Object.entries(sizes)) {
      expect(
        mount(NieLoaderSymbol, {
          props: { size: size as NieLoaderSymbolSize },
        }).classes(),
      ).toContain(expected);
    }
  });

  it("applies a tone class for every tone", () => {
    const tones: NieLoaderSymbolTone[] = [
      "primary",
      "secondary",
      "success",
      "warning",
      "error",
      "white",
      "current",
    ];

    for (const tone of tones) {
      expect(
        mount(NieLoaderSymbol, { props: { tone } }).classes().join(" "),
      ).toMatch(/text-/);
    }
  });

  it("merges a caller-supplied class", () => {
    expect(
      mount(NieLoaderSymbol, { props: { class: "-ml-1" } }).classes(),
    ).toContain("-ml-1");
  });
});

describe("NieLoadingOverlay", () => {
  it("is visible with a default message", () => {
    const wrapper = mount(NieLoadingOverlay);

    expect(wrapper.text()).toContain("Loading...");
    expect(
      wrapper.get('[data-testid="nie-loader-symbol"]').attributes("aria-label"),
    ).toBe("Loading...");
  });

  it("renders nothing when hidden", () => {
    const wrapper = mount(NieLoadingOverlay, { props: { show: false } });

    expect(wrapper.find('[data-testid="nie-loader-symbol"]').exists()).toBe(
      false,
    );
  });

  it("covers only its container by default and the viewport when fullscreen", () => {
    expect(mount(NieLoadingOverlay).get("div").classes()).toContain("absolute");
    expect(
      mount(NieLoadingOverlay, { props: { fullscreen: true } })
        .get("div")
        .classes(),
    ).toContain("fixed");
  });

  it("keeps an accessible label even with no visible message", () => {
    const wrapper = mount(NieLoadingOverlay, { props: { message: "" } });

    expect(wrapper.findAll("p")).toHaveLength(0);
    expect(
      wrapper.get('[data-testid="nie-loader-symbol"]').attributes("aria-label"),
    ).toBe("Loading");
  });

  it("uses the brand loader so long waits stay on-brand", () => {
    const wrapper = mount(NieLoadingOverlay);

    expect(
      wrapper
        .get('[data-testid="nie-loader-symbol"]')
        .attributes("data-loader-variant"),
    ).toBe("brand");
  });
});

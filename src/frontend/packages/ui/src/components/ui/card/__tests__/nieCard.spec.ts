import { mount } from "@vue/test-utils";
import { describe, expect, it } from "vitest";
import NieCard from "../../card/NieCard.vue";

describe("NieCard", () => {
  it("renders only the body region when no optional slot is filled", () => {
    const wrapper = mount(NieCard, { slots: { default: "Body" } });

    expect(wrapper.findAll("div")).toHaveLength(2);
    expect(wrapper.text()).toBe("Body");
  });

  it("renders the header and footer regions when their slots are filled", () => {
    const wrapper = mount(NieCard, {
      slots: { header: "Header", default: "Body", footer: "Footer" },
    });

    const regions = wrapper.findAll(":scope > div");
    expect(regions).toHaveLength(3);
    expect(regions[0].text()).toBe("Header");
    expect(regions[0].classes()).toContain("border-b");
    expect(regions[2].text()).toBe("Footer");
    expect(regions[2].classes()).toContain("border-t");
  });

  it("pads the body by default and can be un-padded", () => {
    const padded = mount(NieCard, { slots: { default: "Body" } });
    expect(padded.get(":scope > div").classes()).toContain("p-6");

    const flush = mount(NieCard, {
      props: { padding: false },
      slots: { default: "Body" },
    });
    expect(flush.get(":scope > div").classes()).not.toContain("p-6");
  });

  it("merges a caller-supplied class", () => {
    expect(mount(NieCard, { props: { class: "h-full" } }).classes()).toContain(
      "h-full",
    );
  });
});

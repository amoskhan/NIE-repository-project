import { mount } from "@vue/test-utils";
import { describe, expect, it } from "vitest";
import { h } from "vue";
import NiePageHeader from "../../page-header/NiePageHeader.vue";

describe("NiePageHeader", () => {
  it("renders the title as the page-level heading", () => {
    const wrapper = mount(NiePageHeader, { props: { title: "Purchase orders" } });

    expect(wrapper.get("h1").text()).toBe("Purchase orders");
    expect(wrapper.element.tagName).toBe("SECTION");
  });

  it("omits the optional eyebrow and subtitle", () => {
    const wrapper = mount(NiePageHeader, { props: { title: "Purchase orders" } });

    expect(wrapper.findAll("p")).toHaveLength(0);
  });

  it("renders the eyebrow and subtitle when supplied", () => {
    const wrapper = mount(NiePageHeader, {
      props: {
        eyebrow: "Procurement",
        title: "Purchase orders",
        subtitle: "Review and approve requests.",
      },
    });

    expect(wrapper.findAll("p").map((p) => p.text())).toEqual([
      "Procurement",
      "Review and approve requests.",
    ]);
  });

  it("switches to compact padding when asked", () => {
    expect(
      mount(NiePageHeader, { props: { title: "x" } }).classes(),
    ).toContain("p-6");
    expect(
      mount(NiePageHeader, { props: { title: "x", compact: true } }).classes(),
    ).toContain("p-5");
  });

  it("merges a caller-supplied class", () => {
    expect(
      mount(NiePageHeader, { props: { title: "x", class: "mb-6" } }).classes(),
    ).toContain("mb-6");
  });
});

describe("NiePageHeader slots", () => {
  it("renders no actions region unless the slot is filled", () => {
    const wrapper = mount(NiePageHeader, { props: { title: "x" } });

    expect(wrapper.find(".lg\\:justify-end").exists()).toBe(false);
  });

  it("renders the actions and meta slots", () => {
    const wrapper = mount(NiePageHeader, {
      props: { title: "Purchase orders" },
      slots: {
        actions: () => h("button", { type: "button" }, "New order"),
        meta: () => h("span", { class: "meta" }, "12 open"),
      },
    });

    expect(wrapper.get("button").text()).toBe("New order");
    expect(wrapper.get(".meta").text()).toBe("12 open");
  });
});

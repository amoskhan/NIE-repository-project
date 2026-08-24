import { mount } from "@vue/test-utils";
import { describe, expect, it } from "vitest";
import { defineComponent, h } from "vue";
import NieTextarea from "../../textarea/NieTextarea.vue";

describe("NieTextarea rendering", () => {
  it("defaults to four rows and an empty value", () => {
    const wrapper = mount(NieTextarea);
    const textarea = wrapper.get("textarea");

    expect(textarea.attributes("rows")).toBe("4");
    expect(textarea.attributes("data-nie-control")).toBe("textarea");
    expect(textarea.element.value).toBe("");
  });

  it("forwards the native attributes", () => {
    const wrapper = mount(NieTextarea, {
      props: {
        rows: 8,
        maxlength: 200,
        placeholder: "Justification",
        spellcheck: false,
        required: true,
        readonly: true,
        disabled: true,
      },
    });
    const textarea = wrapper.get("textarea");

    expect(textarea.attributes("rows")).toBe("8");
    expect(textarea.attributes("maxlength")).toBe("200");
    expect(textarea.attributes("placeholder")).toBe("Justification");
    expect(textarea.attributes("spellcheck")).toBe("false");
    expect(textarea.attributes("required")).toBeDefined();
    expect(textarea.attributes("readonly")).toBeDefined();
    expect(textarea.attributes("disabled")).toBeDefined();
  });

  it("merges a caller-supplied class", () => {
    const wrapper = mount(NieTextarea, { props: { class: "font-mono" } });

    expect(wrapper.get("textarea").classes()).toContain("font-mono");
  });
});

describe("NieTextarea labelling", () => {
  it("renders no label element when no label is given", () => {
    expect(mount(NieTextarea).find("label").exists()).toBe(false);
  });

  it("generates a stable id shared by the label and the control", () => {
    const wrapper = mount(NieTextarea, { props: { label: "Justification" } });

    const id = wrapper.get("textarea").attributes("id");
    expect(id).toMatch(/^textarea-/);
    expect(wrapper.get("label").attributes("for")).toBe(id);
  });

  it("gives each instance on a page its own generated id", () => {
    const wrapper = mount(
      defineComponent({
        setup: () => () => h("form", [h(NieTextarea), h(NieTextarea)]),
      }),
    );

    const ids = wrapper
      .findAll("textarea")
      .map((textarea) => textarea.attributes("id"));

    expect(ids).toHaveLength(2);
    expect(new Set(ids).size).toBe(2);
  });

  it("prefers an explicit id", () => {
    const wrapper = mount(NieTextarea, {
      props: { id: "justification", label: "Justification" },
    });

    expect(wrapper.get("textarea").attributes("id")).toBe("justification");
  });
});

describe("NieTextarea hint and error", () => {
  it("describes the control with the hint when valid", () => {
    const wrapper = mount(NieTextarea, {
      props: { id: "justification", hint: "Up to 200 characters." },
    });

    expect(wrapper.get("textarea").attributes("aria-describedby")).toBe(
      "justification-hint",
    );
    expect(wrapper.get("#justification-hint").text()).toBe(
      "Up to 200 characters.",
    );
    expect(wrapper.get("textarea").attributes("aria-invalid")).toBeUndefined();
  });

  it("replaces the hint with the error and marks the field invalid", () => {
    const wrapper = mount(NieTextarea, {
      props: {
        id: "justification",
        hint: "Up to 200 characters.",
        error: "Justification is required.",
      },
    });
    const textarea = wrapper.get("textarea");

    expect(textarea.attributes("aria-invalid")).toBe("true");
    expect(textarea.attributes("aria-describedby")).toBe(
      "justification-error",
    );
    expect(wrapper.get('[role="alert"]').text()).toBe(
      "Justification is required.",
    );
    expect(wrapper.find("#justification-hint").exists()).toBe(false);
    expect(textarea.classes()).toContain("border-danger-300");
  });

  it("describes nothing when there is neither hint nor error", () => {
    const wrapper = mount(NieTextarea, { props: { id: "justification" } });

    expect(
      wrapper.get("textarea").attributes("aria-describedby"),
    ).toBeUndefined();
  });
});

describe("NieTextarea model", () => {
  it("renders the bound value and emits edits", async () => {
    const wrapper = mount(NieTextarea, {
      props: { modelValue: "Existing" },
    });

    expect(wrapper.get("textarea").element.value).toBe("Existing");

    await wrapper.get("textarea").setValue("Updated");

    expect(wrapper.emitted("update:modelValue")).toEqual([["Updated"]]);
  });
});

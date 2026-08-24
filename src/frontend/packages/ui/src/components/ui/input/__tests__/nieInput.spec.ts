import { mount } from "@vue/test-utils";
import { describe, expect, it } from "vitest";
import NieInput from "../../input/NieInput.vue";

describe("NieInput rendering", () => {
  it("renders a text input by default", () => {
    const wrapper = mount(NieInput);
    const input = wrapper.get("input");

    expect(input.attributes("type")).toBe("text");
    expect(input.attributes("data-nie-control")).toBe("input");
    expect((input.element as HTMLInputElement).value).toBe("");
  });

  it("renders null and undefined models as an empty field", () => {
    expect(
      mount(NieInput, { props: { modelValue: null } }).get("input").element.value,
    ).toBe("");
    expect(mount(NieInput).get("input").element.value).toBe("");
  });

  it("forwards the native constraint attributes", () => {
    const wrapper = mount(NieInput, {
      props: {
        type: "number",
        placeholder: "Amount",
        min: 0,
        max: 10,
        step: 0.5,
        maxlength: 4,
        autocomplete: "off",
        readonly: true,
        disabled: true,
      },
    });
    const input = wrapper.get("input");

    expect(input.attributes("type")).toBe("number");
    expect(input.attributes("placeholder")).toBe("Amount");
    expect(input.attributes("min")).toBe("0");
    expect(input.attributes("max")).toBe("10");
    expect(input.attributes("step")).toBe("0.5");
    expect(input.attributes("maxlength")).toBe("4");
    expect(input.attributes("autocomplete")).toBe("off");
    expect(input.attributes("readonly")).toBeDefined();
    expect(input.attributes("disabled")).toBeDefined();
  });
});

describe("NieInput label and error wiring", () => {
  it("renders no label element when no label is given", () => {
    expect(mount(NieInput).find("label").exists()).toBe(false);
  });

  it("points the label at the input via a generated id", () => {
    const wrapper = mount(NieInput, { props: { label: "Vendor name" } });

    const id = wrapper.get("input").attributes("id");
    expect(id).toMatch(/^input-/);
    expect(wrapper.get("label").attributes("for")).toBe(id);
    expect(wrapper.get("label").text()).toBe("Vendor name");
  });

  it("prefers an explicit id", () => {
    const wrapper = mount(NieInput, {
      props: { id: "vendor-name", label: "Vendor name" },
    });

    expect(wrapper.get("input").attributes("id")).toBe("vendor-name");
    expect(wrapper.get("label").attributes("for")).toBe("vendor-name");
  });

  it("exposes no error affordances when valid", () => {
    const wrapper = mount(NieInput, { props: { id: "vendor-name" } });

    expect(wrapper.find('[role="alert"]').exists()).toBe(false);
    expect(wrapper.get("input").attributes("aria-invalid")).toBeUndefined();
    expect(wrapper.get("input").attributes("aria-describedby")).toBeUndefined();
    expect(wrapper.get("input").classes()).toContain("border-secondary-300");
  });

  it("announces the error and links it with aria-describedby", () => {
    const wrapper = mount(NieInput, {
      props: { id: "vendor-name", error: "Vendor name is required." },
    });
    const input = wrapper.get("input");
    const error = wrapper.get('[role="alert"]');

    expect(input.attributes("aria-invalid")).toBe("true");
    expect(input.attributes("aria-describedby")).toBe("vendor-name-error");
    expect(error.attributes("id")).toBe("vendor-name-error");
    expect(error.text()).toBe("Vendor name is required.");
    expect(input.classes()).toContain("border-danger-300");
  });
});

describe("NieInput model updates", () => {
  it("emits the raw string for text inputs", async () => {
    const wrapper = mount(NieInput);

    await wrapper.get("input").setValue("Acme");

    expect(wrapper.emitted("update:modelValue")).toEqual([["Acme"]]);
  });

  it("emits a number for number inputs", async () => {
    const wrapper = mount(NieInput, { props: { type: "number" } });

    await wrapper.get("input").setValue("42");

    expect(wrapper.emitted("update:modelValue")).toEqual([[42]]);
  });

  it("emits null when a number input is cleared", async () => {
    const wrapper = mount(NieInput, {
      props: { type: "number", modelValue: 42 },
    });

    await wrapper.get("input").setValue("");

    expect(wrapper.emitted("update:modelValue")).toEqual([[null]]);
  });

  it("forwards focus and blur", async () => {
    const wrapper = mount(NieInput);

    await wrapper.get("input").trigger("focus");
    await wrapper.get("input").trigger("blur");

    expect(wrapper.emitted("focus")).toHaveLength(1);
    expect(wrapper.emitted("blur")).toHaveLength(1);
  });
});

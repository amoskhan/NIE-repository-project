import { mount } from "@vue/test-utils";
import { describe, expect, it } from "vitest";
import {
  NieButton,
  NieCard,
  NieInput,
  NieSelect,
  NieSwitch,
  NieTextarea,
} from "@nie/ui";

describe("shared visual primitives", () => {
  it("uses semantic geometry for controls and panels", () => {
    expect(mount(NieButton).get("button").classes()).toContain(
      "rounded-[var(--theme-radius-control)]",
    );
    expect(mount(NieInput).get("input").classes()).toContain(
      "rounded-[var(--theme-radius-control)]",
    );
    expect(mount(NieInput).get("input").classes()).toContain(
      "nie-input-control",
    );
    expect(
      mount(NieSelect, { props: { options: [] } })
        .get('[role="combobox"]')
        .classes(),
    ).toContain("nie-select-trigger");
    expect(mount(NieCard).get("div").classes()).toContain(
      "rounded-[var(--theme-radius-panel)]",
    );
    expect(
      mount(NieSwitch, { props: { modelValue: false } })
        .get("label")
        .classes(),
    ).toContain("min-h-11");
  });

  it("represents an empty numeric field as null", async () => {
    const wrapper = mount(NieInput, {
      props: { modelValue: 12, type: "number" },
    });

    await wrapper.get("input").setValue("");

    expect(wrapper.emitted("update:modelValue")?.at(-1)).toEqual([null]);
  });

  it("provides a labelled multiline field with shared error semantics", () => {
    const wrapper = mount(NieTextarea, {
      props: {
        label: "Description",
        error: "Description is required",
      },
    });

    const textarea = wrapper.get("textarea");
    expect(textarea.classes()).toContain("nie-textarea-control");
    expect(wrapper.get("label").attributes("for")).toBe(
      textarea.attributes("id"),
    );
    expect(textarea.attributes("aria-invalid")).toBe("true");
    expect(wrapper.get('[role="alert"]').text()).toBe(
      "Description is required",
    );
  });
});

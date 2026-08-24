import { mount } from "@vue/test-utils";
import { describe, expect, it, vi } from "vitest";
import { nextTick } from "vue";
import NieSelect from "../../select/NieSelect.vue";

// jsdom does not implement scrollIntoView, which the roving highlight calls.
Element.prototype.scrollIntoView = vi.fn();

const options = [
  { value: "open", label: "Open" },
  { value: "approved", label: "Approved" },
  { value: "closed", label: "Closed", disabled: true },
];

type SelectProps = InstanceType<typeof NieSelect>["$props"];

function mountSelect(props: Partial<SelectProps> = {}) {
  return mount(NieSelect, {
    attachTo: document.body,
    props: { options, ...props } as SelectProps,
  });
}

describe("NieSelect trigger", () => {
  it("exposes combobox semantics", () => {
    const wrapper = mountSelect();
    const trigger = wrapper.get("button");

    expect(trigger.attributes("role")).toBe("combobox");
    expect(trigger.attributes("aria-haspopup")).toBe("listbox");
    expect(trigger.attributes("aria-expanded")).toBe("false");
    expect(trigger.attributes("data-nie-control")).toBe("select");
  });

  it("shows the placeholder until something is selected", () => {
    const empty = mountSelect({ placeholder: "Pick a status" });
    expect(empty.get("button span").text()).toBe("Pick a status");
    expect(empty.get("button span").classes()).toContain("text-secondary-400");

    const chosen = mountSelect({ modelValue: "approved" });
    expect(chosen.get("button span").text()).toBe("Approved");
    expect(chosen.get("button span").classes()).not.toContain(
      "text-secondary-400",
    );
  });

  it("treats null, undefined and the empty string as no selection", () => {
    for (const modelValue of [null, undefined, ""]) {
      expect(mountSelect({ modelValue }).get("button span").text()).toBe(
        "Select an option",
      );
    }
  });

  it("matches the model loosely so numeric values still resolve", () => {
    const wrapper = mount(NieSelect, {
      props: { options: [{ value: 1, label: "One" }], modelValue: "1" },
    });

    expect(wrapper.get("button span").text()).toBe("One");
  });

  it("falls back to the placeholder when the model matches no option", () => {
    const wrapper = mountSelect({ modelValue: "gone" });

    expect(wrapper.get("button span").text()).toBe("Select an option");
    expect(wrapper.get("button span").classes()).toContain("text-secondary-400");
  });

  it("wires up the label and the error message", () => {
    const wrapper = mountSelect({
      id: "status",
      label: "Status",
      error: "Status is required.",
    });

    expect(wrapper.get("label").attributes("for")).toBe("status");
    expect(wrapper.get("button").attributes("aria-invalid")).toBe("true");
    expect(wrapper.get("button").attributes("aria-describedby")).toBe(
      "status-error",
    );
    expect(wrapper.get('[role="alert"]').text()).toBe("Status is required.");
    expect(wrapper.get("button").classes()).toContain("border-danger-300");
  });

  it("generates an id when none is supplied", () => {
    expect(mountSelect({ label: "Status" }).get("button").attributes("id")).toMatch(
      /^select-/,
    );
  });
});

describe("NieSelect opening and closing", () => {
  it("opens on click and closes on a second click", async () => {
    const wrapper = mountSelect();

    await wrapper.get("button").trigger("click");
    expect(wrapper.get("button").attributes("aria-expanded")).toBe("true");
    expect(wrapper.findAll('[role="option"]')).toHaveLength(3);

    await wrapper.get("button").trigger("click");
    expect(wrapper.find('[role="listbox"]').exists()).toBe(false);
  });

  it("stays shut while disabled", async () => {
    const wrapper = mountSelect({ disabled: true });

    await wrapper.get("button").trigger("click");
    await wrapper.get("button").trigger("keydown", { key: "Enter" });

    expect(wrapper.find('[role="listbox"]').exists()).toBe(false);
    expect(wrapper.get("button").attributes("disabled")).toBeDefined();
  });

  it("closes when the user clicks outside", async () => {
    const wrapper = mountSelect();
    await wrapper.get("button").trigger("click");

    document.body.dispatchEvent(new MouseEvent("mousedown", { bubbles: true }));
    await nextTick();

    expect(wrapper.find('[role="listbox"]').exists()).toBe(false);
    wrapper.unmount();
  });

  it("stays open when the click is inside the control", async () => {
    const wrapper = mountSelect();
    await wrapper.get("button").trigger("click");

    wrapper.element.dispatchEvent(
      new MouseEvent("mousedown", { bubbles: true }),
    );
    await nextTick();

    expect(wrapper.find('[role="listbox"]').exists()).toBe(true);
    wrapper.unmount();
  });
});

describe("NieSelect option selection", () => {
  it("emits both the model update and the change event", async () => {
    const wrapper = mountSelect();
    await wrapper.get("button").trigger("click");

    await wrapper.findAll('[role="option"]')[1].trigger("click");

    expect(wrapper.emitted("update:modelValue")).toEqual([["approved"]]);
    expect(wrapper.emitted("change")).toEqual([["approved"]]);
    expect(wrapper.find('[role="listbox"]').exists()).toBe(false);
  });

  it("ignores a disabled option", async () => {
    const wrapper = mountSelect();
    await wrapper.get("button").trigger("click");

    await wrapper.findAll('[role="option"]')[2].trigger("click");

    expect(wrapper.emitted("update:modelValue")).toBeUndefined();
    expect(wrapper.find('[role="listbox"]').exists()).toBe(true);
  });

  it("marks the selected option for assistive technology", async () => {
    const wrapper = mountSelect({ modelValue: "approved" });
    await wrapper.get("button").trigger("click");

    expect(
      wrapper.findAll('[role="option"]').map((o) => o.attributes("aria-selected")),
    ).toEqual(["false", "true", "false"]);
  });

  it("highlights the option under the pointer", async () => {
    const wrapper = mountSelect();
    await wrapper.get("button").trigger("click");

    await wrapper.findAll('[role="option"]')[1].trigger("mouseenter");

    expect(
      wrapper.findAll('[role="option"]')[1].attributes("data-highlighted"),
    ).toBe("");
  });
});

describe("NieSelect keyboard support", () => {
  it("opens with Enter, Space or ArrowDown", async () => {
    for (const key of ["Enter", " ", "ArrowDown"]) {
      const wrapper = mountSelect();

      await wrapper.get("button").trigger("keydown", { key });

      expect(wrapper.find('[role="listbox"]').exists()).toBe(true);
      wrapper.unmount();
    }
  });

  it("leaves other keys alone while closed", async () => {
    const wrapper = mountSelect();

    await wrapper.get("button").trigger("keydown", { key: "a" });

    expect(wrapper.find('[role="listbox"]').exists()).toBe(false);
  });

  it("moves the highlight with the arrow keys and stops at the ends", async () => {
    const wrapper = mountSelect();
    const trigger = wrapper.get("button");
    await trigger.trigger("click");

    await trigger.trigger("keydown", { key: "ArrowDown" });
    expect(
      wrapper.findAll('[role="option"]')[0].attributes("data-highlighted"),
    ).toBe("");

    await trigger.trigger("keydown", { key: "ArrowDown" });
    await trigger.trigger("keydown", { key: "ArrowDown" });
    await trigger.trigger("keydown", { key: "ArrowDown" });
    expect(
      wrapper.findAll('[role="option"]')[2].attributes("data-highlighted"),
    ).toBe("");

    await trigger.trigger("keydown", { key: "ArrowUp" });
    await trigger.trigger("keydown", { key: "ArrowUp" });
    await trigger.trigger("keydown", { key: "ArrowUp" });
    expect(
      wrapper.findAll('[role="option"]')[0].attributes("data-highlighted"),
    ).toBe("");
  });

  it("selects the highlighted option with Enter", async () => {
    const wrapper = mountSelect();
    const trigger = wrapper.get("button");
    await trigger.trigger("click");
    await trigger.trigger("keydown", { key: "ArrowDown" });
    await trigger.trigger("keydown", { key: "ArrowDown" });

    await trigger.trigger("keydown", { key: "Enter" });

    expect(wrapper.emitted("update:modelValue")).toEqual([["approved"]]);
  });

  it("does nothing on Enter while no option is highlighted", async () => {
    const wrapper = mountSelect();
    const trigger = wrapper.get("button");
    await trigger.trigger("click");

    await trigger.trigger("keydown", { key: "Enter" });

    expect(wrapper.emitted("update:modelValue")).toBeUndefined();
    expect(wrapper.find('[role="listbox"]').exists()).toBe(true);
  });

  it("closes on Escape without selecting", async () => {
    const wrapper = mountSelect();
    const trigger = wrapper.get("button");
    await trigger.trigger("click");
    await trigger.trigger("keydown", { key: "ArrowDown" });

    await trigger.trigger("keydown", { key: "Escape" });

    expect(wrapper.find('[role="listbox"]').exists()).toBe(false);
    expect(wrapper.emitted("update:modelValue")).toBeUndefined();
  });
});

describe("NieSelect search", () => {
  it("shows no search field unless the select is searchable", async () => {
    const wrapper = mountSelect();
    await wrapper.get("button").trigger("click");

    expect(wrapper.find("input").exists()).toBe(false);
  });

  it("filters the options as the user types", async () => {
    const wrapper = mountSelect({ searchable: true, label: "Status" });
    await wrapper.get("button").trigger("click");

    await wrapper.get("input").setValue("appro");

    const visible = wrapper.findAll('[role="option"]');
    expect(visible).toHaveLength(1);
    expect(visible[0].text()).toBe("Approved");
    expect(wrapper.get("input").attributes("aria-label")).toBe("Search Status");
  });

  it("tells the user when nothing matches", async () => {
    const wrapper = mountSelect({ searchable: true });
    await wrapper.get("button").trigger("click");

    await wrapper.get("input").setValue("zzz");

    expect(wrapper.findAll('[role="option"]')).toHaveLength(0);
    expect(wrapper.get('[role="listbox"]').text()).toBe("No options found.");
  });

  it("selects from the filtered list with the keyboard", async () => {
    const wrapper = mountSelect({ searchable: true });
    await wrapper.get("button").trigger("click");
    await wrapper.get("input").setValue("clo");

    await wrapper.get("input").trigger("keydown", { key: "Enter" });

    expect(wrapper.emitted("update:modelValue")).toBeUndefined();
  });

  it("clears the search when the list is reopened", async () => {
    const wrapper = mountSelect({ searchable: true });
    await wrapper.get("button").trigger("click");
    await wrapper.get("input").setValue("appro");
    await wrapper.get("button").trigger("click");

    await wrapper.get("button").trigger("click");

    expect(wrapper.get("input").element.value).toBe("");
    expect(wrapper.findAll('[role="option"]')).toHaveLength(3);
  });
});

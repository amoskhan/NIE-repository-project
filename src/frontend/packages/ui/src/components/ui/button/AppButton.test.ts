// EXAMPLE UNIT TEST — a Vue component, mounted with @vue/test-utils.
//
// Component tests should assert BEHAVIOUR the user can observe (what renders, what is
// emitted, what is disabled), not implementation details like internal variable names.
// Assert on classes only where the class IS the behaviour, as with `disabled`.

import { describe, expect, it } from "vitest";
import { mount } from "@vue/test-utils";
import AppButton from "./AppButton.vue";

describe("AppButton", () => {
  it("renders its slot content", () => {
    const wrapper = mount(AppButton, {
      slots: { default: "Save changes" },
    });

    expect(wrapper.text()).toContain("Save changes");
    // `type` defaults to "button" so a button inside a <form> never submits by accident.
    expect(wrapper.attributes("type")).toBe("button");
  });

  it("emits click when enabled", async () => {
    const wrapper = mount(AppButton, { slots: { default: "Go" } });

    await wrapper.trigger("click");

    expect(wrapper.emitted("click")).toHaveLength(1);
  });

  it("does not emit click while loading", async () => {
    const wrapper = mount(AppButton, {
      props: { loading: true },
      slots: { default: "Go" },
    });

    await wrapper.trigger("click");

    expect(wrapper.emitted("click")).toBeUndefined();
    // The element is disabled too, so keyboard activation is blocked as well.
    expect(wrapper.attributes("disabled")).toBeDefined();
  });

  it("shows the shared loader symbol only while loading", async () => {
    const wrapper = mount(AppButton, { slots: { default: "Go" } });
    expect(wrapper.find('[data-testid="app-loader-symbol"]').exists()).toBe(
      false,
    );

    await wrapper.setProps({ loading: true });
    expect(wrapper.find('[data-testid="app-loader-symbol"]').exists()).toBe(
      true,
    );
  });

  it("applies the requested variant and size", () => {
    const wrapper = mount(AppButton, {
      props: { variant: "danger", size: "lg" },
      slots: { default: "Delete" },
    });

    expect(wrapper.classes().join(" ")).toContain("bg-red-600");
    expect(wrapper.classes().join(" ")).toContain("min-h-12");
  });
});

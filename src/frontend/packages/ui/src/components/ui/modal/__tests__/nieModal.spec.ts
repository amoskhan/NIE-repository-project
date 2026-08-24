import { mount } from "@vue/test-utils";
import { afterEach, describe, expect, it } from "vitest";
import { h, nextTick } from "vue";
import NieModal from "../../modal/NieModal.vue";

type ModalProps = InstanceType<typeof NieModal>["$props"];

function mountModal(props: Partial<ModalProps> = {}, slots = {}) {
  return mount(NieModal, {
    attachTo: document.body,
    props: { modelValue: true, ...props } as ModalProps,
    slots,
  });
}

function pressEscape(): void {
  document.dispatchEvent(
    new KeyboardEvent("keydown", { key: "Escape", cancelable: true }),
  );
}

afterEach(() => {
  document.body.innerHTML = "";
  document.body.style.overflow = "";
});

describe("NieModal visibility", () => {
  it("renders nothing while closed", () => {
    mountModal({ modelValue: false });

    expect(document.querySelector('[role="dialog"]')).toBeNull();
    expect(document.body.style.overflow).toBe("");
  });

  it("teleports an ARIA dialog to the body when open", () => {
    const wrapper = mountModal();
    const dialog = document.querySelector('[role="dialog"]');

    expect(dialog).not.toBeNull();
    expect(dialog?.getAttribute("aria-modal")).toBe("true");
    expect(dialog?.getAttribute("tabindex")).toBe("-1");
    expect(document.body.style.overflow).toBe("hidden");

    wrapper.unmount();
  });

  it("restores page scrolling once it closes", async () => {
    const wrapper = mountModal();
    expect(document.body.style.overflow).toBe("hidden");

    await wrapper.setProps({ modelValue: false });

    expect(document.body.style.overflow).toBe("");
    wrapper.unmount();
  });
});

describe("NieModal labelling", () => {
  it("labels itself with the heading when a title is given", () => {
    const wrapper = mountModal({ title: "Delete order" });
    const dialog = document.querySelector('[role="dialog"]');
    const heading = document.querySelector("h3");

    expect(dialog?.getAttribute("aria-labelledby")).toBe(heading?.id);
    expect(dialog?.getAttribute("aria-label")).toBeNull();
    expect(heading?.textContent).toBe("Delete order");

    wrapper.unmount();
  });

  it("falls back to aria-label when there is no title", () => {
    const wrapper = mountModal({ ariaLabel: "Review saved filters" });
    const dialog = document.querySelector('[role="dialog"]');

    expect(dialog?.getAttribute("aria-label")).toBe("Review saved filters");
    expect(dialog?.getAttribute("aria-labelledby")).toBeNull();

    wrapper.unmount();
  });

  it("uses a generic label when an empty one is supplied", () => {
    const wrapper = mountModal({ ariaLabel: "" });

    expect(
      document.querySelector('[role="dialog"]')?.getAttribute("aria-label"),
    ).toBe("Dialog");

    wrapper.unmount();
  });

  it("renders no chrome at all when both title and close button are suppressed", () => {
    const wrapper = mountModal({ showClose: false });

    expect(document.querySelector('[aria-label="Close dialog"]')).toBeNull();
    expect(document.querySelector("h3")).toBeNull();

    wrapper.unmount();
  });
});

describe("NieModal closing", () => {
  it("closes from the close button", async () => {
    const wrapper = mountModal({ title: "Delete order" });

    document
      .querySelector<HTMLButtonElement>('[aria-label="Close dialog"]')
      ?.click();
    await nextTick();

    expect(wrapper.emitted("update:modelValue")).toEqual([[false]]);
    expect(wrapper.emitted("close")).toHaveLength(1);
    wrapper.unmount();
  });

  it("closes when the overlay is clicked", async () => {
    const wrapper = mountModal();

    document
      .querySelector<HTMLElement>('[aria-hidden="true"].fixed')
      ?.dispatchEvent(new MouseEvent("click", { bubbles: true }));
    await nextTick();

    expect(wrapper.emitted("update:modelValue")).toEqual([[false]]);
    wrapper.unmount();
  });

  it("ignores overlay clicks when closeOnOverlay is off", async () => {
    const wrapper = mountModal({ closeOnOverlay: false });

    document
      .querySelector<HTMLElement>('[aria-hidden="true"].fixed')
      ?.dispatchEvent(new MouseEvent("click", { bubbles: true }));
    await nextTick();

    expect(wrapper.emitted("update:modelValue")).toBeUndefined();
    wrapper.unmount();
  });

  it("closes on Escape", async () => {
    const wrapper = mountModal();

    pressEscape();
    await nextTick();

    expect(wrapper.emitted("update:modelValue")).toEqual([[false]]);
    wrapper.unmount();
  });

  it("ignores Escape when closeOnEscape is off", async () => {
    const wrapper = mountModal({ closeOnEscape: false });

    pressEscape();
    await nextTick();

    expect(wrapper.emitted("update:modelValue")).toBeUndefined();
    wrapper.unmount();
  });

  it("stops listening for Escape after unmount", async () => {
    const wrapper = mountModal();
    wrapper.unmount();

    expect(() => {
      pressEscape();
    }).not.toThrow();
    await nextTick();
  });
});

describe("NieModal stacking", () => {
  it("routes Escape and overlay clicks to the topmost dialog only", async () => {
    const outer = mountModal({ ariaLabel: "Outer" });
    const inner = mountModal({ ariaLabel: "Inner" });

    pressEscape();
    await nextTick();

    expect(inner.emitted("update:modelValue")).toEqual([[false]]);
    expect(outer.emitted("update:modelValue")).toBeUndefined();

    inner.unmount();
    outer.unmount();
  });

  it("keeps the page locked until the last dialog closes", async () => {
    const outer = mountModal({ ariaLabel: "Outer" });
    const inner = mountModal({ ariaLabel: "Inner" });

    await inner.setProps({ modelValue: false });
    expect(document.body.style.overflow).toBe("hidden");

    await outer.setProps({ modelValue: false });
    expect(document.body.style.overflow).toBe("");

    inner.unmount();
    outer.unmount();
  });
});

describe("NieModal focus management", () => {
  it("moves focus to the first focusable control when it opens", async () => {
    const wrapper = mountModal({ title: "Delete order" });
    await nextTick();
    await nextTick();

    expect(document.activeElement?.getAttribute("aria-label")).toBe(
      "Close dialog",
    );
    wrapper.unmount();
  });

  it("honours the initialFocus selector", async () => {
    const wrapper = mountModal(
      { title: "Delete order", initialFocus: "[data-confirm]" },
      {
        default: () =>
          h("button", { type: "button", "data-confirm": "" }, "Confirm"),
      },
    );
    await nextTick();
    await nextTick();

    expect(document.activeElement?.textContent).toBe("Confirm");
    wrapper.unmount();
  });

  it("returns focus to the trigger when it closes", async () => {
    const trigger = document.createElement("button");
    document.body.append(trigger);
    trigger.focus();

    const wrapper = mountModal({ title: "Delete order" });
    await nextTick();
    await wrapper.setProps({ modelValue: false });
    await nextTick();
    await nextTick();

    expect(document.activeElement).toBe(trigger);
    wrapper.unmount();
  });

  it("wraps Tab from the last control back to the first", async () => {
    const wrapper = mountModal(
      { title: "Delete order" },
      {
        default: () => h("button", { type: "button" }, "Confirm"),
      },
    );
    await nextTick();

    const dialog = document.querySelector<HTMLElement>('[role="dialog"]');
    const controls = [...(dialog?.querySelectorAll("button") ?? [])];
    controls[controls.length - 1].focus();

    dialog?.dispatchEvent(
      new KeyboardEvent("keydown", { key: "Tab", bubbles: true, cancelable: true }),
    );
    await nextTick();

    expect(document.activeElement).toBe(controls[0]);
    wrapper.unmount();
  });

  it("wraps Shift+Tab from the first control back to the last", async () => {
    const wrapper = mountModal(
      { title: "Delete order" },
      {
        default: () => h("button", { type: "button" }, "Confirm"),
      },
    );
    await nextTick();

    const dialog = document.querySelector<HTMLElement>('[role="dialog"]');
    const controls = [...(dialog?.querySelectorAll("button") ?? [])];
    controls[0].focus();

    dialog?.dispatchEvent(
      new KeyboardEvent("keydown", {
        key: "Tab",
        shiftKey: true,
        bubbles: true,
        cancelable: true,
      }),
    );
    await nextTick();

    expect(document.activeElement).toBe(controls[controls.length - 1]);
    wrapper.unmount();
  });

  it("keeps focus on the dialog when it holds no focusable control", async () => {
    const wrapper = mountModal({ showClose: false });
    await nextTick();

    const dialog = document.querySelector<HTMLElement>('[role="dialog"]');
    dialog?.dispatchEvent(
      new KeyboardEvent("keydown", { key: "Tab", bubbles: true, cancelable: true }),
    );
    await nextTick();

    expect(document.activeElement).toBe(dialog);
    wrapper.unmount();
  });
});

describe("NieModal layout", () => {
  it("applies the requested width", () => {
    for (const [size, expected] of [
      ["sm", "max-w-sm"],
      ["md", "max-w-md"],
      ["lg", "max-w-lg"],
      ["xl", "max-w-xl"],
      ["full", "max-w-full"],
    ] as const) {
      const wrapper = mountModal({ size });

      expect(
        document.querySelector('[role="dialog"]')?.classList.contains(expected),
      ).toBe(true);
      wrapper.unmount();
      document.body.innerHTML = "";
    }
  });

  it("bottom-sheets on small screens when asked", () => {
    const wrapper = mountModal({ placement: "mobile-sheet" });

    expect(
      document
        .querySelector('[role="dialog"]')
        ?.classList.contains("max-sm:rounded-b-none"),
    ).toBe(true);
    wrapper.unmount();
  });

  it("renders the footer region only when the slot is filled", () => {
    const bare = mountModal();
    expect(document.querySelector(".border-t")).toBeNull();
    bare.unmount();
    document.body.innerHTML = "";

    const withFooter = mountModal(
      {},
      { footer: () => h("button", { type: "button" }, "Save") },
    );
    expect(document.querySelector(".border-t")).not.toBeNull();
    withFooter.unmount();
  });
});

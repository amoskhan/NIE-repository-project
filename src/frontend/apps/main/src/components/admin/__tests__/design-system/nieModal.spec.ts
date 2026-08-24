import { mount } from "@vue/test-utils";
import { nextTick } from "vue";
import { afterEach, describe, expect, it } from "vitest";
import { NieModal } from "@nie/ui";

afterEach(() => {
  document.body.innerHTML = "";
  document.body.style.overflow = "";
});

async function settleFocus() {
  await nextTick();
  await Promise.resolve();
}

describe("NieModal accessibility behavior", () => {
  it("labels the dialog, focuses the first control, and contains Tab focus", async () => {
    const wrapper = mount(NieModal, {
      attachTo: document.body,
      props: { modelValue: true, title: "Edit vendor" },
      slots: {
        default:
          '<button data-testid="first">First</button><button data-testid="last">Last</button>',
      },
    });

    await settleFocus();
    const dialog = document.querySelector<HTMLElement>('[role="dialog"]')!;
    const title = document.querySelector<HTMLElement>("[id^='nie-modal-title-']")!;
    const close = document.querySelector<HTMLButtonElement>(
      'button[aria-label="Close dialog"]',
    )!;

    expect(dialog.getAttribute("aria-modal")).toBe("true");
    expect(dialog.getAttribute("aria-labelledby")).toBe(title.id);
    expect(document.activeElement).toBe(close);

    const last = document.querySelector<HTMLButtonElement>(
      '[data-testid="last"]',
    )!;
    last.focus();
    dialog.dispatchEvent(
      new KeyboardEvent("keydown", { key: "Tab", bubbles: true }),
    );
    expect(document.activeElement).toBe(close);

    wrapper.unmount();
  });

  it("closes on Escape, restores invoking focus, and restores body scroll", async () => {
    const opener = document.createElement("button");
    opener.textContent = "Open";
    document.body.append(opener);
    opener.focus();

    const wrapper = mount(NieModal, {
      attachTo: document.body,
      props: {
        modelValue: true,
        ariaLabel: "Filters",
        "onUpdate:modelValue": (value: boolean) =>
          wrapper.setProps({ modelValue: value }),
      },
      slots: { default: "Filter content" },
    });
    await settleFocus();

    expect(document.body.style.overflow).toBe("hidden");
    document.dispatchEvent(
      new KeyboardEvent("keydown", { key: "Escape", bubbles: true }),
    );
    await settleFocus();

    expect(wrapper.emitted("close")).toHaveLength(1);
    expect(document.querySelector('[role="dialog"]')).toBeNull();
    expect(document.body.style.overflow).toBe("");
    expect(document.activeElement).toBe(opener);

    wrapper.unmount();
  });

  it("renders above fixed application toolbars", async () => {
    const wrapper = mount(NieModal, {
      attachTo: document.body,
      props: { modelValue: true, ariaLabel: "Preferences" },
      slots: { default: "Preference content" },
    });

    await settleFocus();
    expect(document.querySelector(".fixed.inset-0")?.classList).toContain(
      "z-[200]",
    );

    wrapper.unmount();
  });
});

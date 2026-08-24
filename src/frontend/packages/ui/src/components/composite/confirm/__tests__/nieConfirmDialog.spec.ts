import { mount } from "@vue/test-utils";
import { afterEach, describe, expect, it } from "vitest";
import { nextTick } from "vue";
import NieConfirmDialog from "../../confirm/NieConfirmDialog.vue";
import type { ConfirmOptions } from "../../confirm/NieConfirmDialog.vue";

const deleteOrder: ConfirmOptions = {
  title: "Delete order",
  message: "This cannot be undone.",
  confirmText: "Delete",
  cancelText: "Keep",
  variant: "danger",
};

function mountDialog(options: ConfirmOptions | null, loading = false) {
  return mount(NieConfirmDialog, {
    attachTo: document.body,
    props: { options, loading },
  });
}

function footerButtons(): HTMLButtonElement[] {
  return [
    ...document.querySelectorAll<HTMLButtonElement>(".border-t button"),
  ];
}

afterEach(() => {
  document.body.innerHTML = "";
  document.body.style.overflow = "";
});

describe("NieConfirmDialog visibility", () => {
  it("stays closed while there are no options", () => {
    mountDialog(null);

    expect(document.querySelector('[role="dialog"]')).toBeNull();
  });

  it("opens as soon as options arrive", async () => {
    const wrapper = mountDialog(null);

    await wrapper.setProps({ options: deleteOrder });

    expect(document.querySelector('[role="dialog"]')).not.toBeNull();
    wrapper.unmount();
  });
});

describe("NieConfirmDialog content", () => {
  it("shows the supplied title, message and button labels", () => {
    const wrapper = mountDialog(deleteOrder);

    expect(document.querySelector("h3")?.textContent).toBe("Delete order");
    expect(document.body.textContent).toContain("This cannot be undone.");
    expect(footerButtons().map((button) => button.textContent?.trim())).toEqual([
      "Keep",
      "Delete",
    ]);
    wrapper.unmount();
  });

  it("falls back to generic wording", () => {
    const wrapper = mountDialog({ message: "Continue?" });

    expect(document.querySelector("h3")?.textContent).toBe("Confirm");
    expect(footerButtons().map((button) => button.textContent?.trim())).toEqual([
      "Cancel",
      "Confirm",
    ]);
    wrapper.unmount();
  });

  it("uses the danger styling only when asked", () => {
    const dangerous = mountDialog(deleteOrder);
    expect(footerButtons()[1].className).toContain("bg-status-danger");
    dangerous.unmount();
    document.body.innerHTML = "";

    const neutral = mountDialog({ message: "Continue?" });
    expect(footerButtons()[1].className).toContain("bg-primary-600");
    neutral.unmount();
  });
});

describe("NieConfirmDialog answers", () => {
  it("emits confirm from the confirm button", async () => {
    const wrapper = mountDialog(deleteOrder);

    footerButtons()[1].click();
    await nextTick();

    expect(wrapper.emitted("confirm")).toHaveLength(1);
    expect(wrapper.emitted("cancel")).toBeUndefined();
    wrapper.unmount();
  });

  it("emits cancel from the cancel button", async () => {
    const wrapper = mountDialog(deleteOrder);

    footerButtons()[0].click();
    await nextTick();

    expect(wrapper.emitted("cancel")).toHaveLength(1);
    wrapper.unmount();
  });

  it("treats dismissing the dialog as a cancellation", async () => {
    const wrapper = mountDialog(deleteOrder);

    document
      .querySelector<HTMLButtonElement>('[aria-label="Close dialog"]')
      ?.click();
    await nextTick();

    expect(wrapper.emitted("cancel")).toHaveLength(1);
    wrapper.unmount();
  });
});

describe("NieConfirmDialog while the answer is in flight", () => {
  it("locks the dialog so the action cannot be abandoned midway", () => {
    const wrapper = mountDialog(deleteOrder, true);

    expect(document.querySelector('[aria-label="Close dialog"]')).toBeNull();
    expect(footerButtons()[0].disabled).toBe(true);
    expect(footerButtons()[1].disabled).toBe(true);
    wrapper.unmount();
  });

  it("shows progress on the confirm button", () => {
    const wrapper = mountDialog(deleteOrder, true);

    expect(
      footerButtons()[1].querySelector('[data-testid="nie-loader-symbol"]'),
    ).not.toBeNull();
    wrapper.unmount();
  });

  it("ignores Escape while busy", async () => {
    const wrapper = mountDialog(deleteOrder, true);

    document.dispatchEvent(
      new KeyboardEvent("keydown", { key: "Escape", cancelable: true }),
    );
    await nextTick();

    expect(wrapper.emitted("cancel")).toBeUndefined();
    wrapper.unmount();
  });
});

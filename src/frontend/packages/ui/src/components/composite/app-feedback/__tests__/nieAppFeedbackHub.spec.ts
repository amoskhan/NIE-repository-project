import { mount } from "@vue/test-utils";
import { afterEach, describe, expect, it } from "vitest";
import { nextTick } from "vue";
import { useConfirm } from "../../../../composables/useConfirm";
import { useToast } from "../../../../composables/useToast";
import NieAppFeedbackHub from "../../app-feedback/NieAppFeedbackHub.vue";

afterEach(() => {
  useToast().clear();
  useConfirm().handleCancel();
  document.body.innerHTML = "";
  document.body.style.overflow = "";
});

describe("NieAppFeedbackHub toasts", () => {
  it("renders nothing while the queues are empty", () => {
    const wrapper = mount(NieAppFeedbackHub, { attachTo: document.body });

    expect(wrapper.find("[role]").exists()).toBe(false);
    expect(document.querySelector('[role="dialog"]')).toBeNull();
    wrapper.unmount();
  });

  it("shows toasts pushed through the composable", async () => {
    const wrapper = mount(NieAppFeedbackHub, { attachTo: document.body });

    useToast().success("Order created", "Saved");
    await nextTick();

    expect(wrapper.get('[role="status"]').text()).toContain("Order created");
    wrapper.unmount();
  });

  it("dismissing a toast removes it from the shared queue", async () => {
    const wrapper = mount(NieAppFeedbackHub, { attachTo: document.body });
    const toast = useToast();
    toast.info("Queued", undefined, 0);
    await nextTick();

    await wrapper.get('[role="status"] button').trigger("click");

    expect(toast.toasts.value).toHaveLength(0);
    wrapper.unmount();
  });
});

describe("NieAppFeedbackHub confirmations", () => {
  it("opens the confirm dialog for a pending confirmation", async () => {
    const wrapper = mount(NieAppFeedbackHub, { attachTo: document.body });

    void useConfirm().confirm({ title: "Delete order", message: "Sure?" });
    await nextTick();

    expect(document.querySelector("h3")?.textContent).toBe("Delete order");
    wrapper.unmount();
  });

  it("resolves the pending promise when the user confirms", async () => {
    const wrapper = mount(NieAppFeedbackHub, { attachTo: document.body });
    const pending = useConfirm().confirm("Delete this order?");
    await nextTick();

    const buttons = [
      ...document.querySelectorAll<HTMLButtonElement>(".border-t button"),
    ];
    buttons[1].click();
    await nextTick();

    await expect(pending).resolves.toBe(true);
    expect(document.querySelector('[role="dialog"]')).toBeNull();
    wrapper.unmount();
  });

  it("resolves false when the user cancels", async () => {
    const wrapper = mount(NieAppFeedbackHub, { attachTo: document.body });
    const pending = useConfirm().confirm("Delete this order?");
    await nextTick();

    const buttons = [
      ...document.querySelectorAll<HTMLButtonElement>(".border-t button"),
    ];
    buttons[0].click();
    await nextTick();

    await expect(pending).resolves.toBe(false);
    wrapper.unmount();
  });
});

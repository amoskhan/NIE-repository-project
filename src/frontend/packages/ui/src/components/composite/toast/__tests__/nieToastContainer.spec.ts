import { mount } from "@vue/test-utils";
import { describe, expect, it } from "vitest";
import NieToastContainer from "../../toast/NieToastContainer.vue";
import type { Toast, ToastType } from "../../toast/NieToastContainer.vue";

const types: ToastType[] = ["success", "error", "warning", "info"];

function toast(overrides: Partial<Toast> = {}): Toast {
  return { id: "toast-1", type: "info", message: "Saved", ...overrides };
}

describe("NieToastContainer", () => {
  it("renders nothing when the queue is empty", () => {
    const wrapper = mount(NieToastContainer, { props: { toasts: [] } });

    expect(wrapper.findAll("[role]")).toHaveLength(0);
  });

  it("renders one entry per toast, in order", () => {
    const wrapper = mount(NieToastContainer, {
      props: {
        toasts: [
          toast({ id: "a", message: "First" }),
          toast({ id: "b", message: "Second" }),
        ],
      },
    });

    const entries = wrapper.findAll('[role="status"]');
    expect(entries).toHaveLength(2);
    expect(entries[0].text()).toContain("First");
    expect(entries[1].text()).toContain("Second");
  });

  it("renders the optional title above the message", () => {
    const withTitle = mount(NieToastContainer, {
      props: { toasts: [toast({ title: "Saved", message: "Order created" })] },
    });
    expect(withTitle.findAll("p").map((p) => p.text())).toEqual([
      "Saved",
      "Order created",
    ]);

    const withoutTitle = mount(NieToastContainer, {
      props: { toasts: [toast({ message: "Order created" })] },
    });
    expect(withoutTitle.findAll("p")).toHaveLength(1);
  });
});

describe("NieToastContainer live-region politeness", () => {
  it("interrupts the user only for errors", () => {
    const wrapper = mount(NieToastContainer, {
      props: {
        toasts: [
          toast({ id: "a", type: "error", message: "Failed" }),
          toast({ id: "b", type: "success", message: "Saved" }),
        ],
      },
    });

    const error = wrapper.get('[role="alert"]');
    expect(error.attributes("aria-live")).toBe("assertive");

    const success = wrapper.get('[role="status"]');
    expect(success.attributes("aria-live")).toBe("polite");
  });

  it("uses the status role for every non-error type", () => {
    for (const type of types.filter((entry) => entry !== "error")) {
      const wrapper = mount(NieToastContainer, {
        props: { toasts: [toast({ type })] },
      });

      expect(wrapper.find('[role="status"]').exists()).toBe(true);
    }
  });
});

describe("NieToastContainer appearance", () => {
  it("tints each type differently", () => {
    const backgrounds = types.map((type) =>
      mount(NieToastContainer, { props: { toasts: [toast({ type })] } })
        .get("[role]")
        .classes()
        .find((entry) => entry.startsWith("bg-")),
    );

    expect(new Set(backgrounds).size).toBe(4);
    expect(backgrounds).not.toContain(undefined);
  });

  it("renders a distinct icon per type", () => {
    const icons = types.map((type) =>
      mount(NieToastContainer, { props: { toasts: [toast({ type })] } })
        .get("svg")
        .html(),
    );

    expect(new Set(icons).size).toBe(4);
  });
});

describe("NieToastContainer dismissal", () => {
  it("labels the dismiss control with the toast type", () => {
    const wrapper = mount(NieToastContainer, {
      props: { toasts: [toast({ type: "warning" })] },
    });

    expect(wrapper.get("button").attributes("aria-label")).toBe(
      "Dismiss warning notification",
    );
  });

  it("emits the id of the toast being dismissed", async () => {
    const wrapper = mount(NieToastContainer, {
      props: {
        toasts: [toast({ id: "a" }), toast({ id: "b" })],
      },
    });

    await wrapper.findAll("button")[1].trigger("click");

    expect(wrapper.emitted("dismiss")).toEqual([["b"]]);
  });
});

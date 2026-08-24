import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { useToast } from "../useToast";

beforeEach(() => {
  useToast().clear();
});

afterEach(() => {
  useToast().clear();
  vi.useRealTimers();
});

describe("useToast queue", () => {
  it("is shared between call sites", () => {
    const publisher = useToast();
    const reader = useToast();

    publisher.info("Saved");

    expect(reader.toasts.value).toHaveLength(1);
  });

  it("records the type, message and title of each toast", () => {
    const toast = useToast();

    toast.success("Order created", "Success");
    toast.error("Order rejected", "Failed");
    toast.warning("Order expires soon");
    toast.info("Order queued");

    expect(
      toast.toasts.value.map(({ type, message, title }) => ({
        type,
        message,
        title,
      })),
    ).toEqual([
      { type: "success", message: "Order created", title: "Success" },
      { type: "error", message: "Order rejected", title: "Failed" },
      { type: "warning", message: "Order expires soon", title: undefined },
      { type: "info", message: "Order queued", title: undefined },
    ]);
  });

  it("hands back a unique id for every toast", () => {
    const toast = useToast();

    const first = toast.info("One");
    const second = toast.info("Two");

    expect(first).not.toBe(second);
    expect(toast.toasts.value.map((entry) => entry.id)).toEqual([
      first,
      second,
    ]);
  });
});

describe("useToast auto-dismiss", () => {
  it("removes a toast once its duration has elapsed", () => {
    vi.useFakeTimers();
    const toast = useToast();

    toast.success("Saved");
    expect(toast.toasts.value).toHaveLength(1);

    vi.advanceTimersByTime(4999);
    expect(toast.toasts.value).toHaveLength(1);

    vi.advanceTimersByTime(1);
    expect(toast.toasts.value).toHaveLength(0);
  });

  it("honours a custom duration", () => {
    vi.useFakeTimers();
    const toast = useToast();

    toast.error("Boom", undefined, 100);
    vi.advanceTimersByTime(100);

    expect(toast.toasts.value).toHaveLength(0);
  });

  it("keeps a toast forever when the duration is zero", () => {
    vi.useFakeTimers();
    const toast = useToast();

    toast.warning("Read me", undefined, 0);
    vi.advanceTimersByTime(60_000);

    expect(toast.toasts.value).toHaveLength(1);
  });

  it("only removes the toast that expired", () => {
    vi.useFakeTimers();
    const toast = useToast();

    toast.info("Short", undefined, 100);
    const longId = toast.info("Long", undefined, 10_000);
    vi.advanceTimersByTime(100);

    expect(toast.toasts.value.map((entry) => entry.id)).toEqual([longId]);
  });
});

describe("useToast removal", () => {
  it("removes a toast by id", () => {
    const toast = useToast();
    const first = toast.info("One", undefined, 0);
    const second = toast.info("Two", undefined, 0);

    toast.remove(first);

    expect(toast.toasts.value.map((entry) => entry.id)).toEqual([second]);
  });

  it("ignores an unknown id", () => {
    const toast = useToast();
    toast.info("One", undefined, 0);

    toast.remove("toast-does-not-exist");

    expect(toast.toasts.value).toHaveLength(1);
  });

  it("does not throw when a dismissed toast later times out", () => {
    vi.useFakeTimers();
    const toast = useToast();
    const id = toast.info("One", undefined, 100);

    toast.remove(id);
    expect(() => {
      vi.advanceTimersByTime(100);
    }).not.toThrow();
    expect(toast.toasts.value).toHaveLength(0);
  });

  it("clears every toast at once", () => {
    const toast = useToast();
    toast.info("One", undefined, 0);
    toast.info("Two", undefined, 0);

    toast.clear();

    expect(toast.toasts.value).toHaveLength(0);
  });
});

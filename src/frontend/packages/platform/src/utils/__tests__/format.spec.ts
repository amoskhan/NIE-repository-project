import { afterEach, describe, expect, it, vi } from "vitest";
import {
  debounce,
  formatCurrency,
  formatDate,
  formatDateTime,
  throttle,
} from "../format";

afterEach(() => {
  vi.useRealTimers();
});

describe("formatDate", () => {
  it("returns an empty string for every empty input", () => {
    expect(formatDate(null)).toBe("");
    expect(formatDate(undefined)).toBe("");
    expect(formatDate("")).toBe("");
  });

  it("formats a Date and the equivalent ISO string identically", () => {
    const date = new Date(2026, 7, 7, 9, 30, 0);

    expect(formatDate(date)).toBe(date.toLocaleDateString());
    expect(formatDate(date.toISOString())).toBe(date.toLocaleDateString());
  });
});

describe("formatDateTime", () => {
  it("returns an empty string for every empty input", () => {
    expect(formatDateTime(null)).toBe("");
    expect(formatDateTime(undefined)).toBe("");
    expect(formatDateTime("")).toBe("");
  });

  it("includes the time component, unlike formatDate", () => {
    const date = new Date(2026, 7, 7, 9, 30, 0);

    expect(formatDateTime(date)).toBe(date.toLocaleString());
    expect(formatDateTime(date.toISOString())).toBe(date.toLocaleString());
    expect(formatDateTime(date).length).toBeGreaterThan(
      formatDate(date).length,
    );
  });
});

describe("formatCurrency", () => {
  it("defaults to Singapore dollars", () => {
    expect(formatCurrency(1234.5)).toBe("$1,234.50");
  });

  it("honours an explicit currency and negative amounts", () => {
    expect(formatCurrency(1234.5, "USD")).toBe("US$1,234.50");
    expect(formatCurrency(-12)).toBe("-$12.00");
    expect(formatCurrency(0)).toBe("$0.00");
  });
});

describe("debounce", () => {
  it("invokes the callback once, with the newest arguments", () => {
    vi.useFakeTimers();
    const spy = vi.fn();
    const debounced = debounce(spy, 200);

    debounced("first");
    debounced("second");
    vi.advanceTimersByTime(199);
    expect(spy).not.toHaveBeenCalled();

    vi.advanceTimersByTime(1);
    expect(spy).toHaveBeenCalledTimes(1);
    expect(spy).toHaveBeenCalledWith("second");
  });

  it("restarts the delay for calls placed after the callback ran", () => {
    vi.useFakeTimers();
    const spy = vi.fn();
    const debounced = debounce(spy, 50);

    debounced("a");
    vi.advanceTimersByTime(50);
    debounced("b");
    vi.advanceTimersByTime(50);

    expect(spy.mock.calls).toEqual([["a"], ["b"]]);
  });
});

describe("throttle", () => {
  it("runs the first call immediately and swallows calls inside the window", () => {
    vi.useFakeTimers();
    const spy = vi.fn();
    const throttled = throttle(spy, 100);

    throttled("first");
    throttled("second");
    throttled("third");

    expect(spy).toHaveBeenCalledTimes(1);
    expect(spy).toHaveBeenCalledWith("first");
  });

  it("allows the next call once the window has elapsed", () => {
    vi.useFakeTimers();
    const spy = vi.fn();
    const throttled = throttle(spy, 100);

    throttled("first");
    vi.advanceTimersByTime(100);
    throttled("second");

    expect(spy.mock.calls).toEqual([["first"], ["second"]]);
  });
});

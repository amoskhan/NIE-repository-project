// EXAMPLE UNIT TEST — copy this file's shape when you write your own.
//
// Covers the pure helpers in ./format.ts. Pure functions are the cheapest thing in the
// codebase to test: no server, no component, no browser — just input in, output out.

import { describe, expect, it, vi } from "vitest";
import { debounce, formatCurrency, formatDate, throttle } from "./format";

describe("formatDate", () => {
  it("returns an empty string for missing dates", () => {
    // Guard clauses deserve tests: they are the branch real data hits first.
    expect(formatDate(null)).toBe("");
    expect(formatDate(undefined)).toBe("");
    expect(formatDate("")).toBe("");
  });

  it("accepts both Date objects and ISO strings", () => {
    const fromDate = formatDate(new Date(2026, 0, 31));
    const fromString = formatDate("2026-01-31T00:00:00");

    expect(fromDate).not.toBe("");
    expect(fromString).toBe(fromDate);
  });
});

describe("formatCurrency", () => {
  it("formats an amount with two decimal places", () => {
    // Asserting on the digits rather than the exact symbol keeps the test stable
    // across Node builds that ship different Intl data.
    expect(formatCurrency(1234.5)).toContain("1,234.50");
  });

  it("honours a caller-supplied currency", () => {
    expect(formatCurrency(10, "USD")).toContain("10.00");
  });
});

describe("debounce", () => {
  it("runs the callback once, after the delay, with the last arguments", () => {
    vi.useFakeTimers();
    const spy = vi.fn();
    const debounced = debounce(spy as (...args: unknown[]) => void, 100);

    debounced("first");
    debounced("second");
    debounced("third");
    expect(spy).not.toHaveBeenCalled();

    vi.advanceTimersByTime(100);

    expect(spy).toHaveBeenCalledTimes(1);
    expect(spy).toHaveBeenCalledWith("third");

    vi.useRealTimers();
  });
});

describe("throttle", () => {
  it("runs immediately, then ignores calls until the window has passed", () => {
    vi.useFakeTimers();
    const spy = vi.fn();
    const throttled = throttle(spy as (...args: unknown[]) => void, 100);

    throttled("a");
    throttled("b");
    expect(spy).toHaveBeenCalledTimes(1);
    expect(spy).toHaveBeenCalledWith("a");

    vi.advanceTimersByTime(100);
    throttled("c");

    expect(spy).toHaveBeenCalledTimes(2);
    expect(spy).toHaveBeenLastCalledWith("c");

    vi.useRealTimers();
  });
});

import { afterEach, describe, expect, it, vi } from "vitest";
import {
  capitalize,
  cn,
  debounce,
  formatDate,
  formatDateTime,
  generateId,
  sleep,
  truncate,
} from "../utils";

afterEach(() => {
  vi.useRealTimers();
});

describe("cn", () => {
  it("joins conditional class values", () => {
    expect(cn("a", false && "b", undefined, ["c", null], { d: true, e: false })).toBe(
      "a c d",
    );
  });

  it("lets a later Tailwind utility win over an earlier conflicting one", () => {
    expect(cn("px-2 py-1", "px-4")).toBe("py-1 px-4");
    expect(cn("text-sm", "text-lg")).toBe("text-lg");
  });

  it("returns an empty string when nothing is supplied", () => {
    expect(cn()).toBe("");
    expect(cn(undefined, null, false)).toBe("");
  });
});

describe("formatDate", () => {
  it("returns an empty string for empty input", () => {
    expect(formatDate(null)).toBe("");
    expect(formatDate(undefined)).toBe("");
    expect(formatDate("")).toBe("");
    expect(formatDate(0)).toBe("");
  });

  it("returns an empty string for unparseable input", () => {
    expect(formatDate("not-a-date")).toBe("");
    expect(formatDate(Number.NaN)).toBe("");
  });

  it("uses the Singapore locale for Dates, strings and timestamps", () => {
    const date = new Date(2026, 7, 7);
    const expected = date.toLocaleDateString("en-SG");

    expect(formatDate(date)).toBe(expected);
    expect(formatDate(date.toISOString())).toBe(expected);
    expect(formatDate(date.getTime())).toBe(expected);
  });
});

describe("formatDateTime", () => {
  it("returns an empty string for empty or unparseable input", () => {
    expect(formatDateTime(null)).toBe("");
    expect(formatDateTime(undefined)).toBe("");
    expect(formatDateTime("")).toBe("");
    expect(formatDateTime("not-a-date")).toBe("");
  });

  it("adds the time to what formatDate produces", () => {
    const date = new Date(2026, 7, 7, 14, 5);

    expect(formatDateTime(date)).toBe(date.toLocaleString("en-SG"));
    expect(formatDateTime(date).length).toBeGreaterThan(
      formatDate(date).length,
    );
  });
});

describe("truncate", () => {
  it("returns an empty string for empty input", () => {
    expect(truncate(null)).toBe("");
    expect(truncate(undefined)).toBe("");
    expect(truncate("")).toBe("");
  });

  it("leaves values at or below the limit untouched", () => {
    expect(truncate("abc", 3)).toBe("abc");
    expect(truncate("ab", 3)).toBe("ab");
  });

  it("keeps the ellipsis inside the requested length", () => {
    expect(truncate("abcdef", 5)).toBe("ab...");
    expect(truncate("abcdef", 5)).toHaveLength(5);
  });

  it("defaults to 80 characters", () => {
    const long = "x".repeat(100);

    expect(truncate(long)).toHaveLength(80);
    expect(truncate(long).endsWith("...")).toBe(true);
  });

  it("never slices past the start of the string", () => {
    expect(truncate("abcdef", 2)).toBe("...");
  });
});

describe("capitalize", () => {
  it("returns an empty string for empty input", () => {
    expect(capitalize(null)).toBe("");
    expect(capitalize(undefined)).toBe("");
    expect(capitalize("")).toBe("");
  });

  it("upper-cases only the first character", () => {
    expect(capitalize("purchase order")).toBe("Purchase order");
    expect(capitalize("ALREADY")).toBe("ALREADY");
    expect(capitalize("a")).toBe("A");
    expect(capitalize("1st")).toBe("1st");
  });
});

describe("generateId", () => {
  it("prefixes with 'id' by default", () => {
    expect(generateId()).toMatch(/^id-[0-9a-z]+$/);
  });

  it("honours a custom prefix and stays unique", () => {
    const first = generateId("row");
    const second = generateId("row");

    expect(first).toMatch(/^row-[0-9a-z]+$/);
    expect(first).not.toBe(second);
  });
});

describe("sleep", () => {
  it("resolves only after the requested delay", async () => {
    vi.useFakeTimers();
    const settled = vi.fn();

    const pending = sleep(500).then(settled);
    await vi.advanceTimersByTimeAsync(499);
    expect(settled).not.toHaveBeenCalled();

    await vi.advanceTimersByTimeAsync(1);
    await pending;
    expect(settled).toHaveBeenCalledTimes(1);
  });
});

describe("debounce", () => {
  it("collapses a burst into a single trailing call", () => {
    vi.useFakeTimers();
    const spy = vi.fn();
    const debounced = debounce(spy, 300);

    debounced("a");
    debounced("b");
    debounced("c");
    vi.advanceTimersByTime(299);
    expect(spy).not.toHaveBeenCalled();

    vi.advanceTimersByTime(1);
    expect(spy).toHaveBeenCalledTimes(1);
    expect(spy).toHaveBeenCalledWith("c");
  });

  it("starts a fresh timer after the callback has run", () => {
    vi.useFakeTimers();
    const spy = vi.fn();
    const debounced = debounce(spy, 100);

    debounced("first");
    vi.advanceTimersByTime(100);
    debounced("second");
    vi.advanceTimersByTime(100);

    expect(spy.mock.calls).toEqual([["first"], ["second"]]);
  });
});

import { afterEach, describe, expect, it } from "vitest";
import { i18n, setLocale, type SupportedLocale } from "../index";

function activeLocale(): string {
  return (i18n.global.locale as unknown as { value: string }).value;
}

afterEach(() => {
  setLocale("en");
  localStorage.clear();
});

describe("i18n", () => {
  it("starts on English with English as the fallback", () => {
    expect(activeLocale()).toBe("en");
    expect(i18n.global.fallbackLocale.value).toBe("en");
  });

  it("registers both supported locales", () => {
    expect(Object.keys(i18n.global.messages.value).sort()).toEqual(["en", "zh"]);
  });
});

describe("setLocale", () => {
  it("switches the active locale", () => {
    setLocale("zh");

    expect(activeLocale()).toBe("zh");
  });

  it("persists the choice so the next visit restores it", () => {
    setLocale("zh");

    expect(localStorage.getItem("locale")).toBe("zh");
  });

  it("keeps the document language in sync for assistive technology", () => {
    setLocale("zh");
    expect(document.documentElement.getAttribute("lang")).toBe("zh");

    setLocale("en");
    expect(document.documentElement.getAttribute("lang")).toBe("en");
  });

  it("accepts every supported locale", () => {
    const locales: SupportedLocale[] = ["en", "zh"];

    for (const locale of locales) {
      setLocale(locale);
      expect(activeLocale()).toBe(locale);
    }
  });
});

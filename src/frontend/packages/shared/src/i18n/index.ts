import { createI18n } from "vue-i18n";
import en from "./locales/en.json";
import zh from "./locales/zh.json";

export type SupportedLocale = "en" | "zh";

export const i18n = createI18n({
  legacy: false,
  locale: (localStorage.getItem("locale") as SupportedLocale) || "en",
  fallbackLocale: "en",
  messages: { en, zh },
});

/** Switch the active locale and persist the choice */
export function setLocale(locale: SupportedLocale) {
  // vue-i18n composition API exposes .global.locale as a ref
  (i18n.global.locale as unknown as { value: string }).value = locale;
  localStorage.setItem("locale", locale);
  document.documentElement.setAttribute("lang", locale);
}

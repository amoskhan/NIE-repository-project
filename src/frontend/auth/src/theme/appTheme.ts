import type { AppThemeConfig } from "@apptemplate/ui";

export const authThemeConfig: AppThemeConfig = {
  defaultPreset: "cobalt",
  allowedPresets: ["cobalt", "ocean", "emerald", "rose", "amber", "violet"],
  allowedScenarios: ["auth"],
  defaultScenario: "auth",
  defaultLayoutVariant: "split-auth",
  runtimeSwitching: false,
  defaultPreference: "light",
  defaultDensity: "comfortable",
  brandLabel: "App Template",
};

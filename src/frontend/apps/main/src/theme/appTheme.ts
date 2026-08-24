import type { AppThemeConfig } from "@nie/ui";

export const mainThemeConfig: AppThemeConfig = {
  defaultPreset: "cobalt",
  allowedPresets: ["cobalt", "ocean", "emerald", "rose", "amber", "violet"],
  allowedScenarios: [
    "admin",
    "crud",
    "wizard",
    "reporting",
    "auth",
    "applicant",
    "public",
  ],
  defaultScenario: "admin",
  defaultLayoutVariant: "sidebar-admin",
  runtimeSwitching: true,
  defaultPreference: "light",
  defaultDensity: "comfortable",
  brandLabel: "NIE Template",
};

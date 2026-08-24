import { computed } from "vue";
import {
  useTheme as useUiTheme,
  type ThemePreference,
  type ThemePresetId,
} from "@nie/ui";

export type ThemeMode = ThemePreference;
export type ColorPalette =
  | "default"
  | "ocean"
  | "emerald"
  | "rose"
  | "amber"
  | "violet";

export const PALETTES: { id: ColorPalette; name: string; swatch: string }[] = [
  { id: "default", name: "Electric Blue", swatch: "#1500f8" },
  { id: "ocean", name: "Ocean", swatch: "#0891b2" },
  { id: "emerald", name: "Emerald", swatch: "#059669" },
  { id: "rose", name: "Rose", swatch: "#e11d48" },
  { id: "amber", name: "Amber", swatch: "#d97706" },
  { id: "violet", name: "Violet", swatch: "#7c3aed" },
];

const PALETTE_TO_PRESET: Record<ColorPalette, ThemePresetId> = {
  default: "cobalt",
  ocean: "ocean",
  emerald: "emerald",
  rose: "rose",
  amber: "amber",
  violet: "violet",
};

const PRESET_TO_PALETTE: Record<ThemePresetId, ColorPalette> = {
  cobalt: "default",
  ocean: "ocean",
  emerald: "emerald",
  rose: "rose",
  amber: "amber",
  violet: "violet",
};

export function useTheme() {
  const uiTheme = useUiTheme();

  const mode = computed<ThemeMode>({
    get: () => uiTheme.themePreference.value,
    set: (value) => {
      if (value === "system") {
        uiTheme.setThemePreference("system");
        return;
      }

      uiTheme.setMode(value);
    },
  });

  const resolvedMode = computed<Exclude<ThemeMode, "system">>(
    () => uiTheme.mode.value,
  );

  const palette = computed<ColorPalette>({
    get: () => PRESET_TO_PALETTE[uiTheme.preset.value] ?? "default",
    set: (value) => {
      uiTheme.setPreset(PALETTE_TO_PRESET[value] ?? "cobalt");
    },
  });

  function toggleMode() {
    uiTheme.setMode(uiTheme.mode.value === "dark" ? "light" : "dark");
  }

  function setMode(value: ThemeMode) {
    mode.value = value;
  }

  function setPalette(value: ColorPalette) {
    palette.value = value;
  }

  return {
    mode,
    resolvedMode,
    palette,
    // Reactive product name from the active theme (theme/appTheme.ts → brandLabel).
    brandLabel: uiTheme.brandLabel,
    toggleMode,
    setMode,
    setPalette,
    PALETTES,
  };
}


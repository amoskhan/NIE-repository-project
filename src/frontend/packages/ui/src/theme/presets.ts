import type {
  AppScenario,
  LayoutVariant,
  ThemeManifest,
  ThemeMode,
  ThemePresetId,
  ThemeScale,
  ThemeTokenBundle,
  ThemeTypographyTokens,
} from "./types";

const typography: ThemeTypographyTokens = {
  families: {
    display: '"Plus Jakarta Sans", "Inter", system-ui, sans-serif',
    body: '"Plus Jakarta Sans", "Inter", system-ui, sans-serif',
    mono: '"IBM Plex Mono", "SFMono-Regular", Consolas, monospace',
  },
  sizes: {
    xs: "0.75rem",
    sm: "0.875rem",
    md: "1rem",
    lg: "1.125rem",
    xl: "1.25rem",
    "2xl": "1.5rem",
    "3xl": "1.875rem",
    "4xl": "2.5rem",
  },
  weights: {
    regular: "400",
    medium: "500",
    semibold: "600",
    bold: "700",
    black: "800",
  },
  tracking: {
    tight: "-0.03em",
    normal: "0",
    wide: "0.22em",
  },
};

const neutralLight: ThemeScale = {
  50: "#f8fafc",
  100: "#f1f5f9",
  200: "#e2e8f0",
  300: "#cbd5e1",
  400: "#94a3b8",
  500: "#64748b",
  600: "#475569",
  700: "#334155",
  800: "#1e293b",
  900: "#0f172a",
  950: "#020617",
};

const neutralDark: ThemeScale = {
  50: "#eef2ff",
  100: "#e2e8f0",
  200: "#cbd5e1",
  300: "#94a3b8",
  400: "#64748b",
  500: "#475569",
  600: "#334155",
  700: "#1e293b",
  800: "#172033",
  900: "#0f172a",
  950: "#020617",
};

const successScale: ThemeScale = {
  50: "#ecfdf5",
  100: "#d1fae5",
  200: "#a7f3d0",
  300: "#6ee7b7",
  400: "#34d399",
  500: "#10b981",
  600: "#059669",
  700: "#047857",
  800: "#065f46",
  900: "#064e3b",
  950: "#022c22",
};

const warningScale: ThemeScale = {
  50: "#fffbeb",
  100: "#fef3c7",
  200: "#fde68a",
  300: "#fcd34d",
  400: "#fbbf24",
  500: "#f59e0b",
  600: "#d97706",
  700: "#b45309",
  800: "#92400e",
  900: "#78350f",
  950: "#451a03",
};

const dangerScale: ThemeScale = {
  50: "#fff1f2",
  100: "#ffe4e6",
  200: "#fecdd3",
  300: "#fda4af",
  400: "#fb7185",
  500: "#f43f5e",
  600: "#e11d48",
  700: "#be123c",
  800: "#9f1239",
  900: "#881337",
  950: "#4c0519",
};

const infoScale: ThemeScale = {
  50: "#eff6ff",
  100: "#dbeafe",
  200: "#bfdbfe",
  300: "#93c5fd",
  400: "#60a5fa",
  500: "#3b82f6",
  600: "#2563eb",
  700: "#1d4ed8",
  800: "#1e40af",
  900: "#1e3a8a",
  950: "#172554",
};

const layoutVariants: Partial<Record<AppScenario, LayoutVariant[]>> = {
  auth: ["split-auth", "bare-content"],
  admin: ["sidebar-admin", "topbar-admin"],
  crud: ["sidebar-admin", "topbar-admin", "bare-content"],
  wizard: ["wizard-shell", "bare-content"],
  reporting: ["sidebar-admin", "topbar-admin", "bare-content"],
  applicant: ["portal-shell", "bare-content"],
  public: ["public-topnav", "bare-content"],
};

function hexToRgba(hex: string, alpha: number): string {
  const r = parseInt(hex.slice(1, 3), 16);
  const g = parseInt(hex.slice(3, 5), 16);
  const b = parseInt(hex.slice(5, 7), 16);
  return `rgba(${r}, ${g}, ${b}, ${alpha})`;
}

function relativeLuminance(hex: string): number {
  const channels = [1, 3, 5].map((start) => {
    const channel = parseInt(hex.slice(start, start + 2), 16) / 255;
    return channel <= 0.04045
      ? channel / 12.92
      : ((channel + 0.055) / 1.055) ** 2.4;
  });
  return (
    0.2126 * (channels[0] ?? 0) +
    0.7152 * (channels[1] ?? 0) +
    0.0722 * (channels[2] ?? 0)
  );
}

function contrastRatio(first: string, second: string): number {
  const lighter = Math.max(relativeLuminance(first), relativeLuminance(second));
  const darker = Math.min(relativeLuminance(first), relativeLuminance(second));
  return (lighter + 0.05) / (darker + 0.05);
}

function accessibleForeground(background: string): string {
  const light = "#ffffff";
  const dark = "#020617";
  return contrastRatio(background, light) >= contrastRatio(background, dark)
    ? light
    : dark;
}

function makeBundle(
  brand: ThemeScale,
  neutral: ThemeScale,
  mode: ThemeMode,
): ThemeTokenBundle {
  const dark = mode === "dark";

  return {
    colors: {
      brand,
      brandContrast: accessibleForeground(brand[600]),
      neutral,
      surface: dark
        ? {
            canvas: "#0f172a",
            subtle: "#111c2f",
            panel: "#182336",
            elevated: "#1e293b",
            sidebar: "#132033",
            sidebarActive: hexToRgba(brand[400], 0.16),
            overlay: "rgba(2, 6, 23, 0.78)",
          }
        : {
            canvas: "#f6f5f8",
            subtle: "#eef3fb",
            panel: "#ffffff",
            elevated: "#ffffff",
            sidebar: "#ffffff",
            sidebarActive: hexToRgba(brand[600], 0.08),
            overlay: "rgba(15, 23, 42, 0.5)",
          },
      text: dark
        ? {
            strong: "#f8fafc",
            muted: "#94a3b8",
            soft: "#cbd5e1",
            inverse: "#020617",
          }
        : {
            strong: "#0f172a",
            muted: "#475569",
            soft: "#334155",
            inverse: "#ffffff",
          },
      border: dark
        ? {
            subtle: "rgba(148, 163, 184, 0.12)",
            default: "#334155",
            strong: "#475569",
          }
        : {
            subtle: "rgba(148, 163, 184, 0.2)",
            default: "#e2e8f0",
            strong: "#cbd5e1",
          },
      success: {
        scale: successScale,
        solid: dark ? successScale[400] : successScale[600],
        contrast: accessibleForeground(
          dark ? successScale[400] : successScale[600],
        ),
      },
      warning: {
        scale: warningScale,
        solid: dark ? warningScale[400] : warningScale[600],
        contrast: accessibleForeground(
          dark ? warningScale[400] : warningScale[600],
        ),
      },
      danger: {
        scale: dangerScale,
        solid: dark ? dangerScale[400] : dangerScale[600],
        contrast: accessibleForeground(
          dark ? dangerScale[400] : dangerScale[600],
        ),
      },
      info: {
        scale: infoScale,
        solid: dark ? infoScale[400] : infoScale[600],
        contrast: accessibleForeground(
          dark ? infoScale[400] : infoScale[600],
        ),
      },
    },
    typography,
    elevation: dark
      ? {
          soft: "0 20px 50px -28px rgba(15, 23, 42, 0.72)",
          card: "0 18px 40px -28px rgba(2, 6, 23, 0.82)",
          float: "0 28px 80px -36px rgba(2, 6, 23, 0.9)",
          inset: "inset 0 1px 0 rgba(255,255,255,0.04)",
        }
      : {
          soft: "0 10px 15px -3px rgba(15, 23, 42, 0.08), 0 4px 6px -4px rgba(15, 23, 42, 0.04)",
          card: "0 20px 50px -24px rgba(21, 0, 248, 0.16)",
          float: "0 24px 80px -30px rgba(15, 23, 42, 0.24)",
          inset: "inset 0 1px 0 rgba(255,255,255,0.8)",
        },
    layout: {
      sidebarWidth: "18.5rem",
      sidebarCollapsedWidth: "5.5rem",
      contentMaxWidth: "1600px",
      pageGutter: "1.5rem",
      panelGap: "1.5rem",
    },
  };
}

function preset(
  id: ThemePresetId,
  name: string,
  lightBrand: ThemeScale,
  darkBrand: ThemeScale,
): ThemeManifest {
  return {
    id,
    name,
    modeSupport: ["light", "dark"],
    density: "comfortable",
    radius: "soft",
    motion: "expressive",
    tokens: {
      light: makeBundle(lightBrand, neutralLight, "light"),
      dark: makeBundle(darkBrand, neutralDark, "dark"),
    },
    assets: {},
    layoutVariants,
  };
}

export const themePresets: Record<ThemePresetId, ThemeManifest> = {
  cobalt: preset(
    "cobalt",
    "Cobalt",
    {
      50: "#eceaff",
      100: "#d6d0ff",
      200: "#b3a6ff",
      300: "#8d79ff",
      400: "#694cff",
      500: "#3f26ff",
      600: "#1500f8",
      700: "#1100c8",
      800: "#0d0097",
      900: "#090066",
      950: "#040033",
    },
    {
      50: "#eef2ff",
      100: "#e0e7ff",
      200: "#c7d2fe",
      300: "#a5b4fc",
      400: "#818cf8",
      500: "#6366f1",
      600: "#4f46e5",
      700: "#4338ca",
      800: "#3730a3",
      900: "#312e81",
      950: "#1e1b4b",
    },
  ),
  ocean: preset(
    "ocean",
    "Ocean",
    {
      50: "#ecfeff",
      100: "#cffafe",
      200: "#a5f3fc",
      300: "#67e8f9",
      400: "#22d3ee",
      500: "#06b6d4",
      600: "#0891b2",
      700: "#0e7490",
      800: "#155e75",
      900: "#164e63",
      950: "#083344",
    },
    {
      50: "#ecfeff",
      100: "#cffafe",
      200: "#a5f3fc",
      300: "#67e8f9",
      400: "#22d3ee",
      500: "#06b6d4",
      600: "#0891b2",
      700: "#0e7490",
      800: "#155e75",
      900: "#164e63",
      950: "#083344",
    },
  ),
  emerald: preset("emerald", "Emerald", successScale, successScale),
  rose: preset("rose", "Rose", dangerScale, dangerScale),
  amber: preset("amber", "Amber", warningScale, warningScale),
  violet: preset(
    "violet",
    "Violet",
    {
      50: "#f5f3ff",
      100: "#ede9fe",
      200: "#ddd6fe",
      300: "#c4b5fd",
      400: "#a78bfa",
      500: "#8b5cf6",
      600: "#7c3aed",
      700: "#6d28d9",
      800: "#5b21b6",
      900: "#4c1d95",
      950: "#2e1065",
    },
    {
      50: "#f5f3ff",
      100: "#ede9fe",
      200: "#ddd6fe",
      300: "#c4b5fd",
      400: "#a78bfa",
      500: "#8b5cf6",
      600: "#7c3aed",
      700: "#6d28d9",
      800: "#5b21b6",
      900: "#4c1d95",
      950: "#2e1065",
    },
  ),
};

export const THEME_PRESETS = Object.values(themePresets).map((preset) => ({
  id: preset.id,
  name: preset.name,
  swatch: preset.tokens.light.colors.brand[600],
}));

export function getThemeManifest(preset: ThemePresetId): ThemeManifest {
  return themePresets[preset] ?? themePresets.cobalt;
}

export type ThemeMode = "light" | "dark";
export type ThemePreference = "system" | "light" | "dark";
export type ThemePresetId =
  | "cobalt"
  | "ocean"
  | "emerald"
  | "rose"
  | "amber"
  | "violet";
export type ThemeDensity = "comfortable" | "compact";
export type ThemeRadius = "soft" | "rounded" | "sharp";
export type ThemeMotion = "expressive" | "reduced";

export type AppScenario =
  | "auth"
  | "admin"
  | "crud"
  | "wizard"
  | "reporting"
  | "applicant"
  | "public";

export type LayoutVariant =
  | "split-auth"
  | "sidebar-admin"
  | "topbar-admin"
  | "wizard-shell"
  | "portal-shell"
  | "public-topnav"
  | "bare-content";

export interface ThemeScale {
  50: string;
  100: string;
  200: string;
  300: string;
  400: string;
  500: string;
  600: string;
  700: string;
  800: string;
  900: string;
  950: string;
}

export interface ThemeSurfaceTokens {
  canvas: string;
  subtle: string;
  panel: string;
  elevated: string;
  sidebar: string;
  sidebarActive: string;
  overlay: string;
}

export interface ThemeTextTokens {
  strong: string;
  muted: string;
  soft: string;
  inverse: string;
}

export interface ThemeBorderTokens {
  subtle: string;
  default: string;
  strong: string;
}

export interface ThemeStatusTokens {
  scale: ThemeScale;
  solid: string;
  contrast: string;
}

export interface ThemeColorTokens {
  brand: ThemeScale;
  brandContrast: string;
  neutral: ThemeScale;
  surface: ThemeSurfaceTokens;
  text: ThemeTextTokens;
  border: ThemeBorderTokens;
  success: ThemeStatusTokens;
  warning: ThemeStatusTokens;
  danger: ThemeStatusTokens;
  info: ThemeStatusTokens;
}

export interface ThemeTypographyTokens {
  families: {
    display: string;
    body: string;
    mono: string;
  };
  sizes: Record<
    "xs" | "sm" | "md" | "lg" | "xl" | "2xl" | "3xl" | "4xl",
    string
  >;
  weights: Record<
    "regular" | "medium" | "semibold" | "bold" | "black",
    string
  >;
  tracking: Record<"tight" | "normal" | "wide", string>;
}

export interface ThemeElevationTokens {
  soft: string;
  card: string;
  float: string;
  inset: string;
}

export interface ThemeLayoutTokens {
  sidebarWidth: string;
  sidebarCollapsedWidth: string;
  contentMaxWidth: string;
  pageGutter: string;
  panelGap: string;
}

export interface ThemeTokenBundle {
  colors: ThemeColorTokens;
  typography: ThemeTypographyTokens;
  elevation: ThemeElevationTokens;
  layout: ThemeLayoutTokens;
}

export interface ThemeAssets {
  logo?: string;
  heroArt?: string;
  emptyState?: string;
  favicon?: string;
  illustrations?: Record<string, string>;
}

export interface ThemeManifest {
  id: ThemePresetId;
  name: string;
  modeSupport: ThemeMode[];
  density: ThemeDensity;
  radius: ThemeRadius;
  motion: ThemeMotion;
  tokens: Record<ThemeMode, ThemeTokenBundle>;
  assets: ThemeAssets;
  layoutVariants: Partial<Record<AppScenario, LayoutVariant[]>>;
}

export interface AppThemeConfig {
  defaultPreset: ThemePresetId;
  allowedPresets: ThemePresetId[];
  allowedScenarios: AppScenario[];
  defaultScenario?: AppScenario;
  defaultLayoutVariant: LayoutVariant;
  runtimeSwitching: boolean;
  defaultPreference?: ThemePreference;
  defaultDensity?: ThemeDensity;
  defaultRadius?: ThemeRadius;
  defaultMotion?: ThemeMotion;
  brandLabel?: string;
}

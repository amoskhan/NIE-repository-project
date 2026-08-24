import { computed, ref, watch } from "vue";
import { getThemeManifest, THEME_PRESETS, themePresets } from "./presets";
import type {
  AppScenario,
  AppThemeConfig,
  LayoutVariant,
  ThemeDensity,
  ThemeManifest,
  ThemeMode,
  ThemeMotion,
  ThemePreference,
  ThemePresetId,
  ThemeRadius,
  ThemeScale,
  ThemeTokenBundle,
} from "./types";

const STORAGE_KEYS = {
  mode: "nie_template_theme_mode",
  preset: "nie_template_theme_preset",
  density: "nie_template_theme_density",
  scenario: "nie_template_theme_scenario",
  layout: "nie_template_theme_layout",
  preference: "nie_template_theme_preference",
  radius: "nie_template_theme_radius",
  motion: "nie_template_theme_motion",
};

const defaultConfig: AppThemeConfig = {
  defaultPreset: "cobalt",
  allowedPresets: THEME_PRESETS.map((preset) => preset.id),
  allowedScenarios: ["admin"],
  defaultScenario: "admin",
  defaultLayoutVariant: "sidebar-admin",
  runtimeSwitching: true,
  defaultPreference: "light",
  defaultDensity: "comfortable",
  defaultRadius: "soft",
  defaultMotion: "expressive",
  brandLabel: "NIE Template",
};

const mode = ref<ThemeMode>("light");
const preset = ref<ThemePresetId>("cobalt");
const density = ref<ThemeDensity>("comfortable");
const themePreference = ref<ThemePreference>("light");
const scenario = ref<AppScenario>("admin");
const layoutVariant = ref<LayoutVariant>("sidebar-admin");
const radius = ref<ThemeRadius>("soft");
const motion = ref<ThemeMotion>("expressive");
const brandLabel = ref("NIE Template");

const appConfig = ref<AppThemeConfig>(defaultConfig);
const initialized = ref(false);

let systemThemeQuery: MediaQueryList | null = null;
let systemThemeListener: ((event: MediaQueryListEvent) => void) | null = null;
let syncingFromSystem = false;

function canUseDom(): boolean {
  return typeof window !== "undefined" && typeof document !== "undefined";
}

function getSystemTheme(): ThemeMode {
  if (!canUseDom()) {
    return "light";
  }

  return window.matchMedia("(prefers-color-scheme: dark)").matches
    ? "dark"
    : "light";
}

function clampPreset(value: unknown): ThemePresetId {
  if (typeof value === "string" && value in themePresets) {
    return value as ThemePresetId;
  }

  return appConfig.value.defaultPreset;
}

function clampDensity(value: unknown): ThemeDensity {
  return value === "compact" ? "compact" : "comfortable";
}

function clampPreference(value: unknown): ThemePreference {
  return value === "system" || value === "dark" || value === "light"
    ? value
    : appConfig.value.defaultPreference ?? "light";
}

function clampRadius(value: unknown): ThemeRadius {
  return value === "rounded" || value === "sharp" || value === "soft"
    ? value
    : appConfig.value.defaultRadius ?? "soft";
}

function clampMotion(value: unknown): ThemeMotion {
  return value === "reduced" || value === "expressive"
    ? value
    : appConfig.value.defaultMotion ?? "expressive";
}

function clampScenario(value: unknown): AppScenario {
  if (
    typeof value === "string" &&
    appConfig.value.allowedScenarios.includes(value as AppScenario)
  ) {
    return value as AppScenario;
  }

  return appConfig.value.defaultScenario ?? appConfig.value.allowedScenarios[0];
}

function clampLayout(
  nextLayout: unknown,
  nextScenario: AppScenario,
  nextPreset: ThemePresetId,
): LayoutVariant {
  const manifest = getThemeManifest(nextPreset);
  const scenarioLayouts =
    manifest.layoutVariants[nextScenario] ??
    manifest.layoutVariants[appConfig.value.defaultScenario ?? nextScenario] ??
    [appConfig.value.defaultLayoutVariant];

  if (
    typeof nextLayout === "string" &&
    scenarioLayouts.includes(nextLayout as LayoutVariant)
  ) {
    return nextLayout as LayoutVariant;
  }

  return scenarioLayouts[0] ?? appConfig.value.defaultLayoutVariant;
}

function readStorage(key: string): string | null {
  if (!canUseDom()) {
    return null;
  }

  try {
    return localStorage.getItem(key);
  } catch {
    return null;
  }
}

function persistState(): void {
  if (!canUseDom()) {
    return;
  }

  try {
    localStorage.setItem(STORAGE_KEYS.mode, mode.value);
    localStorage.setItem(STORAGE_KEYS.preset, preset.value);
    localStorage.setItem(STORAGE_KEYS.density, density.value);
    localStorage.setItem(STORAGE_KEYS.scenario, scenario.value);
    localStorage.setItem(STORAGE_KEYS.layout, layoutVariant.value);
    localStorage.setItem(STORAGE_KEYS.preference, themePreference.value);
    localStorage.setItem(STORAGE_KEYS.radius, radius.value);
    localStorage.setItem(STORAGE_KEYS.motion, motion.value);
  } catch {
    // Ignore storage write failures.
  }
}

function setScaleVariables(prefix: string, scale: ThemeScale): void {
  const root = document.documentElement;
  const keys: Array<keyof ThemeScale> = [
    50, 100, 200, 300, 400, 500, 600, 700, 800, 900, 950,
  ];

  keys.forEach((key) => {
    root.style.setProperty(`${prefix}-${key}`, scale[key]);
  });
}

function applyTokenBundle(bundle: ThemeTokenBundle): void {
  const root = document.documentElement;
  const { colors, typography, elevation, layout } = bundle;

  setScaleVariables("--theme-color-brand", colors.brand);
  setScaleVariables("--theme-color-neutral", colors.neutral);
  setScaleVariables("--theme-color-success", colors.success.scale);
  setScaleVariables("--theme-color-warning", colors.warning.scale);
  setScaleVariables("--theme-color-danger", colors.danger.scale);
  setScaleVariables("--theme-color-info", colors.info.scale);

  root.style.setProperty("--theme-color-surface-canvas", colors.surface.canvas);
  root.style.setProperty("--theme-color-surface-subtle", colors.surface.subtle);
  root.style.setProperty("--theme-color-surface-panel", colors.surface.panel);
  root.style.setProperty(
    "--theme-color-surface-elevated",
    colors.surface.elevated,
  );
  root.style.setProperty("--theme-color-surface-sidebar", colors.surface.sidebar);
  root.style.setProperty(
    "--theme-color-surface-sidebar-active",
    colors.surface.sidebarActive,
  );
  root.style.setProperty("--theme-color-surface-overlay", colors.surface.overlay);

  root.style.setProperty("--theme-color-text-strong", colors.text.strong);
  root.style.setProperty("--theme-color-text-muted", colors.text.muted);
  root.style.setProperty("--theme-color-text-soft", colors.text.soft);
  root.style.setProperty("--theme-color-text-inverse", colors.text.inverse);
  root.style.setProperty("--theme-color-on-brand", colors.brandContrast);

  root.style.setProperty("--theme-color-border-subtle", colors.border.subtle);
  root.style.setProperty("--theme-color-border-default", colors.border.default);
  root.style.setProperty("--theme-color-border-strong", colors.border.strong);

  root.style.setProperty("--theme-color-success-solid", colors.success.solid);
  root.style.setProperty(
    "--theme-color-success-contrast",
    colors.success.contrast,
  );
  root.style.setProperty("--theme-color-warning-solid", colors.warning.solid);
  root.style.setProperty(
    "--theme-color-warning-contrast",
    colors.warning.contrast,
  );
  root.style.setProperty("--theme-color-danger-solid", colors.danger.solid);
  root.style.setProperty("--theme-color-danger-contrast", colors.danger.contrast);
  root.style.setProperty("--theme-color-info-solid", colors.info.solid);
  root.style.setProperty("--theme-color-info-contrast", colors.info.contrast);

  root.style.setProperty("--theme-font-display", typography.families.display);
  root.style.setProperty("--theme-font-body", typography.families.body);
  root.style.setProperty("--theme-font-mono", typography.families.mono);
  root.style.setProperty("--theme-font-size-xs", typography.sizes.xs);
  root.style.setProperty("--theme-font-size-sm", typography.sizes.sm);
  root.style.setProperty("--theme-font-size-md", typography.sizes.md);
  root.style.setProperty("--theme-font-size-lg", typography.sizes.lg);
  root.style.setProperty("--theme-font-size-xl", typography.sizes.xl);
  root.style.setProperty("--theme-font-size-2xl", typography.sizes["2xl"]);
  root.style.setProperty("--theme-font-size-3xl", typography.sizes["3xl"]);
  root.style.setProperty("--theme-font-size-4xl", typography.sizes["4xl"]);
  root.style.setProperty(
    "--theme-font-weight-regular",
    typography.weights.regular,
  );
  root.style.setProperty(
    "--theme-font-weight-medium",
    typography.weights.medium,
  );
  root.style.setProperty(
    "--theme-font-weight-semibold",
    typography.weights.semibold,
  );
  root.style.setProperty("--theme-font-weight-bold", typography.weights.bold);
  root.style.setProperty("--theme-font-weight-black", typography.weights.black);
  root.style.setProperty(
    "--theme-letter-spacing-tight",
    typography.tracking.tight,
  );
  root.style.setProperty(
    "--theme-letter-spacing-normal",
    typography.tracking.normal,
  );
  root.style.setProperty("--theme-letter-spacing-wide", typography.tracking.wide);

  root.style.setProperty("--theme-shadow-soft", elevation.soft);
  root.style.setProperty("--theme-shadow-card", elevation.card);
  root.style.setProperty("--theme-shadow-float", elevation.float);
  root.style.setProperty("--theme-shadow-inset", elevation.inset);

  root.style.setProperty("--theme-shell-sidebar-width", layout.sidebarWidth);
  root.style.setProperty(
    "--theme-shell-sidebar-collapsed-width",
    layout.sidebarCollapsedWidth,
  );
  root.style.setProperty("--theme-shell-content-max-width", layout.contentMaxWidth);
  root.style.setProperty("--theme-shell-page-gutter", layout.pageGutter);
  root.style.setProperty("--theme-shell-panel-gap", layout.panelGap);

  root.style.setProperty("--color-primary", colors.brand[600]);
  root.style.setProperty("--color-primary-dark", colors.brand[700]);
  root.style.setProperty("--color-accent", colors.brand[600]);
  root.style.setProperty("--color-accent-light", colors.brand[100]);
  root.style.setProperty("--color-bg-light", colors.surface.canvas);
  root.style.setProperty("--color-bg-dark", colors.neutral[950]);
  root.style.setProperty("--color-surface", colors.surface.panel);
  root.style.setProperty("--color-surface-alt", colors.surface.subtle);
  root.style.setProperty("--color-sidebar", colors.surface.sidebar);
  root.style.setProperty("--color-sidebar-active", colors.surface.sidebarActive);
  root.style.setProperty("--color-text", colors.text.strong);
  root.style.setProperty("--color-text-muted", colors.text.muted);
  root.style.setProperty("--color-border", colors.border.default);

  setScaleVariables("--primary", colors.brand);
  setScaleVariables("--surface", colors.neutral);
}

function applyTheme(): void {
  if (!canUseDom()) {
    return;
  }

  const root = document.documentElement;
  const body = document.body;
  const manifest = getThemeManifest(preset.value);
  const bundle = manifest.tokens[mode.value];

  applyTokenBundle(bundle);

  root.classList.toggle("dark", mode.value === "dark");
  body.classList.toggle("dark", mode.value === "dark");

  root.setAttribute("data-theme-mode", mode.value);
  root.setAttribute("data-theme-preset", preset.value);
  root.setAttribute("data-theme-density", density.value);
  root.setAttribute("data-theme-scenario", scenario.value);
  root.setAttribute("data-layout-variant", layoutVariant.value);
  root.setAttribute("data-theme-radius", radius.value);
  root.setAttribute("data-theme-motion", motion.value);
  body.setAttribute("data-theme-mode", mode.value);
  body.setAttribute("data-theme-preset", preset.value);
  body.setAttribute("data-theme-scenario", scenario.value);
  body.setAttribute("data-layout-variant", layoutVariant.value);

  const metaThemeColor = document.querySelector('meta[name="theme-color"]');
  if (metaThemeColor) {
    metaThemeColor.setAttribute(
      "content",
      mode.value === "dark" ? bundle.colors.surface.canvas : bundle.colors.brand[600],
    );
  }

  window.dispatchEvent(
    new CustomEvent("theme-changed", {
      detail: {
        mode: mode.value,
        preset: preset.value,
        density: density.value,
        scenario: scenario.value,
        layoutVariant: layoutVariant.value,
        preference: themePreference.value,
        radius: radius.value,
        motion: motion.value,
      },
    }),
  );
}

function readStoredState(): void {
  preset.value = clampPreset(readStorage(STORAGE_KEYS.preset));
  density.value = clampDensity(readStorage(STORAGE_KEYS.density));
  scenario.value = clampScenario(readStorage(STORAGE_KEYS.scenario));
  layoutVariant.value = clampLayout(
    readStorage(STORAGE_KEYS.layout),
    scenario.value,
    preset.value,
  );
  themePreference.value = clampPreference(readStorage(STORAGE_KEYS.preference));
  radius.value = clampRadius(readStorage(STORAGE_KEYS.radius));
  motion.value = clampMotion(readStorage(STORAGE_KEYS.motion));
  mode.value =
    themePreference.value === "system"
      ? getSystemTheme()
      : (themePreference.value as ThemeMode);
}

function ensureSystemListener(): void {
  if (!canUseDom() || systemThemeListener) {
    return;
  }

  systemThemeQuery = window.matchMedia("(prefers-color-scheme: dark)");
  systemThemeListener = (event: MediaQueryListEvent) => {
    if (themePreference.value !== "system") {
      return;
    }

    syncingFromSystem = true;
    mode.value = event.matches ? "dark" : "light";
    syncingFromSystem = false;
    persistState();
    applyTheme();
  };

  if (systemThemeQuery.addEventListener) {
    systemThemeQuery.addEventListener("change", systemThemeListener);
    return;
  }

  systemThemeQuery.addListener(systemThemeListener);
}

function setMode(nextMode: ThemeMode): void {
  mode.value = nextMode;
  themePreference.value = nextMode;
}

function toggleMode(): void {
  setMode(mode.value === "dark" ? "light" : "dark");
}

function setThemePreference(preference: ThemePreference): void {
  themePreference.value = preference;
  mode.value = preference === "system" ? getSystemTheme() : preference;
}

function setPreset(nextPreset: ThemePresetId): void {
  preset.value = clampPreset(nextPreset);
  layoutVariant.value = clampLayout(
    layoutVariant.value,
    scenario.value,
    preset.value,
  );
}

function setDensity(nextDensity: ThemeDensity): void {
  density.value = clampDensity(nextDensity);
}

function setScenario(nextScenario: AppScenario): void {
  scenario.value = clampScenario(nextScenario);
  layoutVariant.value = clampLayout(
    layoutVariant.value,
    scenario.value,
    preset.value,
  );
}

function setLayoutVariant(nextLayout: LayoutVariant): void {
  layoutVariant.value = clampLayout(nextLayout, scenario.value, preset.value);
}

export function initTheme(config?: AppThemeConfig): void {
  appConfig.value = {
    ...defaultConfig,
    ...config,
    allowedPresets: config?.allowedPresets?.length
      ? config.allowedPresets
      : defaultConfig.allowedPresets,
    allowedScenarios: config?.allowedScenarios?.length
      ? config.allowedScenarios
      : defaultConfig.allowedScenarios,
  };

  brandLabel.value = appConfig.value.brandLabel ?? defaultConfig.brandLabel!;
  radius.value = appConfig.value.defaultRadius ?? "soft";
  motion.value = appConfig.value.defaultMotion ?? "expressive";

  if (!initialized.value) {
    readStoredState();
    ensureSystemListener();
    initialized.value = true;
  }

  preset.value = clampPreset(preset.value);
  scenario.value = clampScenario(scenario.value);
  density.value = clampDensity(density.value);
  radius.value = clampRadius(radius.value);
  motion.value = clampMotion(motion.value);
  layoutVariant.value = clampLayout(layoutVariant.value, scenario.value, preset.value);

  if (!appConfig.value.allowedPresets.includes(preset.value)) {
    preset.value = appConfig.value.defaultPreset;
  }

  applyTheme();
}

watch(
  [
    mode,
    preset,
    density,
    scenario,
    layoutVariant,
    themePreference,
    radius,
    motion,
  ],
  () => {
    if (syncingFromSystem) {
      return;
    }

    persistState();
    applyTheme();
  },
);

export const activeManifest = computed<ThemeManifest>(() =>
  getThemeManifest(preset.value),
);

export const availablePresets = computed(() =>
  THEME_PRESETS.filter((option) =>
    appConfig.value.allowedPresets.includes(option.id),
  ),
);

export const availableScenarios = computed(() => appConfig.value.allowedScenarios);

export const availableLayoutVariants = computed(() => {
  return (
    activeManifest.value.layoutVariants[scenario.value] ??
    activeManifest.value.layoutVariants[
      appConfig.value.defaultScenario ?? scenario.value
    ] ??
    []
  );
});

export const isDarkMode = computed(() => mode.value === "dark");

export function useTheme() {
  return {
    mode,
    preset,
    density,
    themePreference,
    scenario,
    layoutVariant,
    radius,
    motion,
    brandLabel,
    activeManifest,
    availablePresets,
    availableScenarios,
    availableLayoutVariants,
    isDarkMode,
    setMode,
    toggleMode,
    toggleTheme: toggleMode,
    setPreset,
    setDensity,
    setScenario,
    setLayoutVariant,
    setThemePreference,
    initTheme,
  };
}

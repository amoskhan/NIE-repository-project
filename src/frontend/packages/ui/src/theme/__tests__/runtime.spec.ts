import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { nextTick } from "vue";
import type { AppThemeConfig } from "../types";

type SystemThemeListener = (event: MediaQueryListEvent) => void;

const system = {
  prefersDark: false,
  listeners: [] as SystemThemeListener[],
  legacy: false,
};

function installMatchMedia(): void {
  Object.defineProperty(window, "matchMedia", {
    configurable: true,
    writable: true,
    value: (query: string) => {
      const list = {
        matches: system.prefersDark,
        media: query,
        onchange: null,
        addListener: (listener: SystemThemeListener) => {
          system.listeners.push(listener);
        },
        removeListener: () => {},
        dispatchEvent: () => false,
      };

      if (system.legacy) return list;

      return {
        ...list,
        addEventListener: (_type: string, listener: SystemThemeListener) => {
          system.listeners.push(listener);
        },
        removeEventListener: () => {},
      };
    },
  });
}

function emitSystemChange(prefersDark: boolean): void {
  system.prefersDark = prefersDark;
  for (const listener of system.listeners) {
    listener({ matches: prefersDark } as MediaQueryListEvent);
  }
}

async function loadRuntime() {
  vi.resetModules();
  return import("../runtime");
}

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

const baseConfig: AppThemeConfig = {
  defaultPreset: "cobalt",
  allowedPresets: ["cobalt", "violet"],
  allowedScenarios: ["admin", "wizard"],
  defaultScenario: "admin",
  defaultLayoutVariant: "sidebar-admin",
  runtimeSwitching: true,
};

beforeEach(() => {
  system.prefersDark = false;
  system.legacy = false;
  system.listeners.length = 0;
  installMatchMedia();
  localStorage.clear();
  document.documentElement.removeAttribute("style");
  document.documentElement.className = "";
  document.body.className = "";
  document.head.innerHTML = "";
});

afterEach(() => {
  document.head.innerHTML = "";
});

describe("initTheme defaults", () => {
  it("applies the cobalt light theme when nothing is stored", async () => {
    const { initTheme } = await loadRuntime();

    initTheme();

    const root = document.documentElement;
    expect(root.getAttribute("data-theme-mode")).toBe("light");
    expect(root.getAttribute("data-theme-preset")).toBe("cobalt");
    expect(root.getAttribute("data-theme-density")).toBe("comfortable");
    expect(root.getAttribute("data-theme-scenario")).toBe("admin");
    expect(root.getAttribute("data-layout-variant")).toBe("sidebar-admin");
    expect(root.getAttribute("data-theme-radius")).toBe("soft");
    expect(root.getAttribute("data-theme-motion")).toBe("expressive");
    expect(root.classList.contains("dark")).toBe(false);
    expect(document.body.classList.contains("dark")).toBe(false);
  });

  it("publishes the preset colour ramp as CSS custom properties", async () => {
    const { initTheme, activeManifest } = await loadRuntime();

    initTheme();

    const style = document.documentElement.style;
    const brand = activeManifest.value.tokens.light.colors.brand;
    expect(style.getPropertyValue("--theme-color-brand-600")).toBe(brand[600]);
    expect(style.getPropertyValue("--color-primary")).toBe(brand[600]);
    expect(style.getPropertyValue("--color-primary-dark")).toBe(brand[700]);
    expect(style.getPropertyValue("--theme-font-body")).not.toBe("");
    expect(style.getPropertyValue("--theme-shell-sidebar-width")).toBe(
      "18.5rem",
    );
  });

  it("writes nothing to storage until the theme actually changes", async () => {
    const { initTheme, useTheme } = await loadRuntime();

    initTheme();
    expect(localStorage.getItem(STORAGE_KEYS.preset)).toBeNull();

    useTheme().setPreset("violet");
    await nextTick();

    expect(localStorage.getItem(STORAGE_KEYS.preset)).toBe("violet");
    expect(localStorage.getItem(STORAGE_KEYS.mode)).toBe("light");
    expect(localStorage.getItem(STORAGE_KEYS.preference)).toBe("light");
    expect(localStorage.getItem(STORAGE_KEYS.density)).toBe("comfortable");
    expect(localStorage.getItem(STORAGE_KEYS.layout)).toBe("sidebar-admin");
  });

  it("uses the brand label from the application config", async () => {
    const { initTheme, useTheme } = await loadRuntime();

    initTheme({ ...baseConfig, brandLabel: "Procurement" });

    expect(useTheme().brandLabel.value).toBe("Procurement");
  });
});

describe("initTheme stored state", () => {
  it("restores every stored preference", async () => {
    localStorage.setItem(STORAGE_KEYS.preset, "emerald");
    localStorage.setItem(STORAGE_KEYS.density, "compact");
    localStorage.setItem(STORAGE_KEYS.preference, "dark");
    localStorage.setItem(STORAGE_KEYS.radius, "sharp");
    localStorage.setItem(STORAGE_KEYS.motion, "reduced");
    localStorage.setItem(STORAGE_KEYS.layout, "topbar-admin");
    const { initTheme, isDarkMode } = await loadRuntime();

    initTheme();

    const root = document.documentElement;
    expect(root.getAttribute("data-theme-preset")).toBe("emerald");
    expect(root.getAttribute("data-theme-density")).toBe("compact");
    expect(root.getAttribute("data-theme-mode")).toBe("dark");
    expect(root.getAttribute("data-theme-radius")).toBe("sharp");
    expect(root.getAttribute("data-theme-motion")).toBe("reduced");
    expect(root.getAttribute("data-layout-variant")).toBe("topbar-admin");
    expect(root.classList.contains("dark")).toBe(true);
    expect(document.body.classList.contains("dark")).toBe(true);
    expect(isDarkMode.value).toBe(true);
  });

  it("clamps unrecognised stored values back to the defaults", async () => {
    localStorage.setItem(STORAGE_KEYS.preset, "chartreuse");
    localStorage.setItem(STORAGE_KEYS.density, "cozy");
    localStorage.setItem(STORAGE_KEYS.preference, "auto");
    localStorage.setItem(STORAGE_KEYS.radius, "circular");
    localStorage.setItem(STORAGE_KEYS.motion, "wild");
    localStorage.setItem(STORAGE_KEYS.layout, "space-station");
    localStorage.setItem(STORAGE_KEYS.scenario, "intergalactic");
    const { initTheme } = await loadRuntime();

    initTheme();

    const root = document.documentElement;
    expect(root.getAttribute("data-theme-preset")).toBe("cobalt");
    expect(root.getAttribute("data-theme-density")).toBe("comfortable");
    expect(root.getAttribute("data-theme-mode")).toBe("light");
    expect(root.getAttribute("data-theme-radius")).toBe("soft");
    expect(root.getAttribute("data-theme-motion")).toBe("expressive");
    expect(root.getAttribute("data-theme-scenario")).toBe("admin");
    expect(root.getAttribute("data-layout-variant")).toBe("sidebar-admin");
  });

  it("drops a stored preset the application no longer allows", async () => {
    localStorage.setItem(STORAGE_KEYS.preset, "emerald");
    const { initTheme, availablePresets } = await loadRuntime();

    initTheme(baseConfig);

    expect(document.documentElement.getAttribute("data-theme-preset")).toBe(
      "cobalt",
    );
    expect(availablePresets.value.map((option) => option.id)).toEqual([
      "cobalt",
      "violet",
    ]);
  });

  it("survives a browser that refuses storage access", async () => {
    vi.spyOn(Storage.prototype, "getItem").mockImplementation(() => {
      throw new Error("storage disabled");
    });
    vi.spyOn(Storage.prototype, "setItem").mockImplementation(() => {
      throw new Error("storage disabled");
    });
    const { initTheme } = await loadRuntime();

    expect(() => {
      initTheme();
    }).not.toThrow();
    expect(document.documentElement.getAttribute("data-theme-preset")).toBe(
      "cobalt",
    );
  });

  it("reads the theme only once, so a second init keeps the live state", async () => {
    const { initTheme, useTheme } = await loadRuntime();
    initTheme();
    const theme = useTheme();

    theme.setPreset("violet");
    localStorage.setItem(STORAGE_KEYS.preset, "emerald");
    initTheme(baseConfig);
    await nextTick();

    expect(theme.preset.value).toBe("violet");
  });
});

describe("system colour scheme", () => {
  it("resolves the system preference on load", async () => {
    system.prefersDark = true;
    localStorage.setItem(STORAGE_KEYS.preference, "system");
    const { initTheme, isDarkMode } = await loadRuntime();

    initTheme();

    expect(isDarkMode.value).toBe(true);
    expect(document.documentElement.getAttribute("data-theme-mode")).toBe(
      "dark",
    );
  });

  it("follows later system changes while the preference is 'system'", async () => {
    localStorage.setItem(STORAGE_KEYS.preference, "system");
    const { initTheme, isDarkMode } = await loadRuntime();
    initTheme();
    expect(isDarkMode.value).toBe(false);

    emitSystemChange(true);
    await nextTick();

    expect(isDarkMode.value).toBe(true);
    expect(document.documentElement.getAttribute("data-theme-mode")).toBe(
      "dark",
    );
    expect(localStorage.getItem(STORAGE_KEYS.mode)).toBe("dark");
  });

  it("ignores system changes once the user has chosen a mode", async () => {
    const { initTheme, useTheme, isDarkMode } = await loadRuntime();
    initTheme();
    useTheme().setThemePreference("light");
    await nextTick();

    emitSystemChange(true);
    await nextTick();

    expect(isDarkMode.value).toBe(false);
  });

  it("falls back to the legacy addListener API", async () => {
    system.legacy = true;
    localStorage.setItem(STORAGE_KEYS.preference, "system");
    const { initTheme, isDarkMode } = await loadRuntime();
    initTheme();

    emitSystemChange(true);
    await nextTick();

    expect(isDarkMode.value).toBe(true);
  });
});

describe("useTheme mutations", () => {
  it("setMode also pins the preference so the system stops overriding it", async () => {
    const { initTheme, useTheme } = await loadRuntime();
    initTheme();
    const theme = useTheme();

    theme.setMode("dark");
    await nextTick();

    expect(theme.mode.value).toBe("dark");
    expect(theme.themePreference.value).toBe("dark");
    expect(document.documentElement.classList.contains("dark")).toBe(true);
  });

  it("toggleMode flips between light and dark", async () => {
    const { initTheme, useTheme } = await loadRuntime();
    initTheme();
    const theme = useTheme();

    theme.toggleMode();
    await nextTick();
    expect(theme.mode.value).toBe("dark");

    theme.toggleTheme();
    await nextTick();
    expect(theme.mode.value).toBe("light");
    expect(document.documentElement.classList.contains("dark")).toBe(false);
  });

  it("setThemePreference('system') hands control back to the OS", async () => {
    system.prefersDark = true;
    const { initTheme, useTheme } = await loadRuntime();
    initTheme();
    const theme = useTheme();

    theme.setThemePreference("system");
    await nextTick();

    expect(theme.themePreference.value).toBe("system");
    expect(theme.mode.value).toBe("dark");
  });

  it("setPreset ignores an unknown preset", async () => {
    const { initTheme, useTheme } = await loadRuntime();
    initTheme();
    const theme = useTheme();

    theme.setPreset("violet");
    await nextTick();
    expect(theme.preset.value).toBe("violet");

    theme.setPreset("chartreuse" as never);
    await nextTick();
    expect(theme.preset.value).toBe("cobalt");
  });

  it("setDensity only accepts the two supported densities", async () => {
    const { initTheme, useTheme } = await loadRuntime();
    initTheme();
    const theme = useTheme();

    theme.setDensity("compact");
    await nextTick();
    expect(theme.density.value).toBe("compact");

    theme.setDensity("roomy" as never);
    await nextTick();
    expect(theme.density.value).toBe("comfortable");
  });

  it("setScenario re-clamps the layout to one the scenario offers", async () => {
    const { initTheme, useTheme, availableLayoutVariants } =
      await loadRuntime();
    initTheme(baseConfig);
    const theme = useTheme();

    theme.setScenario("wizard");
    await nextTick();

    expect(theme.scenario.value).toBe("wizard");
    expect(theme.layoutVariant.value).toBe("wizard-shell");
    expect(availableLayoutVariants.value).toEqual([
      "wizard-shell",
      "bare-content",
    ]);
  });

  it("setScenario rejects a scenario the application did not allow", async () => {
    const { initTheme, useTheme, availableScenarios } = await loadRuntime();
    initTheme(baseConfig);
    const theme = useTheme();

    theme.setScenario("public");
    await nextTick();

    expect(theme.scenario.value).toBe("admin");
    expect(availableScenarios.value).toEqual(["admin", "wizard"]);
  });

  it("setLayoutVariant keeps the layout within the active scenario", async () => {
    const { initTheme, useTheme } = await loadRuntime();
    initTheme(baseConfig);
    const theme = useTheme();

    theme.setLayoutVariant("topbar-admin");
    await nextTick();
    expect(theme.layoutVariant.value).toBe("topbar-admin");

    theme.setLayoutVariant("split-auth");
    await nextTick();
    expect(theme.layoutVariant.value).toBe("sidebar-admin");
  });
});

describe("applyTheme side effects", () => {
  it("announces every change on the theme-changed event", async () => {
    const { initTheme, useTheme } = await loadRuntime();
    initTheme();
    const theme = useTheme();
    const detail = vi.fn();
    window.addEventListener("theme-changed", (event) => {
      detail((event as CustomEvent).detail);
    });

    theme.setMode("dark");
    theme.setDensity("compact");
    await nextTick();

    expect(detail).toHaveBeenCalledWith(
      expect.objectContaining({
        mode: "dark",
        density: "compact",
        preset: "cobalt",
        scenario: "admin",
        layoutVariant: "sidebar-admin",
        preference: "dark",
        radius: "soft",
        motion: "expressive",
      }),
    );
  });

  it("keeps the browser theme-color meta tag in step with the mode", async () => {
    document.head.innerHTML = `<meta name="theme-color" content="#ffffff" />`;
    const { initTheme, useTheme, activeManifest } = await loadRuntime();
    initTheme();
    const meta = document.querySelector('meta[name="theme-color"]');

    expect(meta?.getAttribute("content")).toBe(
      activeManifest.value.tokens.light.colors.brand[600],
    );

    useTheme().setMode("dark");
    await nextTick();

    expect(meta?.getAttribute("content")).toBe(
      activeManifest.value.tokens.dark.colors.surface.canvas,
    );
  });

  it("swaps the whole token bundle when the mode changes", async () => {
    const { initTheme, useTheme, activeManifest } = await loadRuntime();
    initTheme();
    const style = document.documentElement.style;
    const lightCanvas = style.getPropertyValue("--theme-color-surface-canvas");

    useTheme().setMode("dark");
    await nextTick();

    expect(style.getPropertyValue("--theme-color-surface-canvas")).not.toBe(
      lightCanvas,
    );
    expect(style.getPropertyValue("--theme-color-surface-canvas")).toBe(
      activeManifest.value.tokens.dark.colors.surface.canvas,
    );
  });

  it("exposes the manifest of the active preset", async () => {
    const { initTheme, useTheme, activeManifest } = await loadRuntime();
    initTheme();

    useTheme().setPreset("violet");
    await nextTick();

    expect(activeManifest.value.id).toBe("violet");
    expect(activeManifest.value.name).toBe("Violet");
  });
});

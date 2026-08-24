import { describe, expect, it } from "vitest";
import { THEME_PRESETS, getThemeManifest, themePresets } from "../presets";
import type { ThemeMode, ThemePresetId, ThemeScale } from "../types";

const presetIds = Object.keys(themePresets) as ThemePresetId[];
const modes: ThemeMode[] = ["light", "dark"];
const scaleKeys: Array<keyof ThemeScale> = [
  50, 100, 200, 300, 400, 500, 600, 700, 800, 900, 950,
];

function relativeLuminance(color: string): number {
  const channels = [1, 3, 5].map((start) => {
    const channel = Number.parseInt(color.slice(start, start + 2), 16) / 255;
    return channel <= 0.04045
      ? channel / 12.92
      : ((channel + 0.055) / 1.055) ** 2.4;
  });
  return 0.2126 * channels[0] + 0.7152 * channels[1] + 0.0722 * channels[2];
}

function contrastRatio(first: string, second: string): number {
  const lighter = Math.max(relativeLuminance(first), relativeLuminance(second));
  const darker = Math.min(relativeLuminance(first), relativeLuminance(second));
  return (lighter + 0.05) / (darker + 0.05);
}

describe("getThemeManifest", () => {
  it("returns the manifest matching the requested preset", () => {
    for (const id of presetIds) {
      expect(getThemeManifest(id).id).toBe(id);
    }
  });

  it("falls back to cobalt for an unknown preset", () => {
    expect(getThemeManifest("does-not-exist" as ThemePresetId).id).toBe(
      "cobalt",
    );
  });
});

describe("THEME_PRESETS", () => {
  it("lists every preset with a swatch taken from its light brand ramp", () => {
    expect(THEME_PRESETS.map((option) => option.id)).toEqual(presetIds);

    for (const option of THEME_PRESETS) {
      expect(option.name).not.toBe("");
      expect(option.swatch).toBe(
        themePresets[option.id].tokens.light.colors.brand[600],
      );
    }
  });
});

describe("theme manifests", () => {
  it("supports both modes with a full colour ramp", () => {
    for (const id of presetIds) {
      const manifest = getThemeManifest(id);
      expect(manifest.modeSupport).toEqual(["light", "dark"]);

      for (const mode of modes) {
        const { brand, neutral } = manifest.tokens[mode].colors;
        for (const key of scaleKeys) {
          expect(brand[key]).toMatch(/^#[0-9a-f]{6}$/i);
          expect(neutral[key]).toMatch(/^#[0-9a-f]{6}$/i);
        }
      }
    }
  });

  it("picks a brand foreground that clears the WCAG AA 4.5:1 ratio", () => {
    for (const id of presetIds) {
      for (const mode of modes) {
        const { brand, brandContrast } = getThemeManifest(id).tokens[mode].colors;
        expect(contrastRatio(brand[600], brandContrast)).toBeGreaterThanOrEqual(
          4.5,
        );
      }
    }
  });

  it("picks status foregrounds that clear the WCAG AA 4.5:1 ratio", () => {
    for (const id of presetIds) {
      for (const mode of modes) {
        const { success, warning, danger, info } =
          getThemeManifest(id).tokens[mode].colors;
        for (const status of [success, warning, danger, info]) {
          expect(
            contrastRatio(status.solid, status.contrast),
          ).toBeGreaterThanOrEqual(4.5);
        }
      }
    }
  });

  it("darkens the canvas and lightens the text in dark mode", () => {
    for (const id of presetIds) {
      const light = getThemeManifest(id).tokens.light.colors;
      const dark = getThemeManifest(id).tokens.dark.colors;

      expect(relativeLuminance(dark.surface.canvas)).toBeLessThan(
        relativeLuminance(light.surface.canvas),
      );
      expect(relativeLuminance(dark.text.strong)).toBeGreaterThan(
        relativeLuminance(light.text.strong),
      );
    }
  });

  it("offers a layout variant for every scenario the shell supports", () => {
    const manifest = getThemeManifest("cobalt");

    expect(manifest.layoutVariants.auth).toContain("split-auth");
    expect(manifest.layoutVariants.admin).toEqual([
      "sidebar-admin",
      "topbar-admin",
    ]);
    expect(manifest.layoutVariants.wizard).toContain("wizard-shell");
    expect(manifest.layoutVariants.applicant).toContain("portal-shell");
    expect(manifest.layoutVariants.public).toContain("public-topnav");

    for (const variants of Object.values(manifest.layoutVariants)) {
      expect(variants.length).toBeGreaterThan(0);
    }
  });

  it("shares one typography scale across every preset", () => {
    const cobalt = getThemeManifest("cobalt").tokens.light.typography;

    for (const id of presetIds) {
      expect(getThemeManifest(id).tokens.dark.typography).toBe(cobalt);
    }
    expect(cobalt.sizes.xs).toBe("0.75rem");
    expect(cobalt.weights.bold).toBe("700");
  });
});

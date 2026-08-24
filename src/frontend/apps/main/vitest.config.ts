import { defineConfig, mergeConfig } from "vitest/config";
import viteConfig from "./vite.config.ts";

export default mergeConfig(
  viteConfig,
  defineConfig({
    test: {
      environment: "jsdom",
      globals: true,
      restoreMocks: true,
      coverage: {
        provider: "v8",
        reporter: ["text", "html", "lcov", "cobertura"],
        reportsDirectory: "coverage",
        // Without an explicit include, only files that a test happens to import
        // are instrumented, so the percentages describe a subset instead of the
        // application.
        include: ["src/**/*.{ts,vue}"],
        exclude: [
          "coverage/**",
          "dist/**",
          "**/*.d.ts",
          "**/*.config.*",
          "**/types/**",
          "**/*.test.ts",
          "**/__tests__/**",
        ],
        // Measured 2026-08-07: 13.52 / 11.09 / 10.82 / 13.57, rounded down.
        // These are low because they are honest: before coverage.include was
        // set, the same suite reported 70.65% over 12 of 84 files. Ratchet up,
        // never down (NIE-TEST-002).
        thresholds: {
          statements: 13,
          branches: 11,
          functions: 10,
          lines: 13,
        },
      },
    },
  }),
);

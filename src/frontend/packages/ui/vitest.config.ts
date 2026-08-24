import vue from "@vitejs/plugin-vue";
import { resolve } from "node:path";
import { defineConfig } from "vitest/config";

export default defineConfig({
  plugins: [vue()],
  resolve: {
    alias: {
      "@": resolve(import.meta.dirname, "src"),
    },
  },
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
      // package. Keep this aligned with the sources shipped by the library.
      include: ["src/**/*.{ts,vue}"],
      exclude: [
        "coverage/**",
        "dist/**",
        "**/*.d.ts",
        "**/*.config.*",
        "**/types.ts",
        "**/__tests__/**",
      ],
      thresholds: {
        statements: 94,
        branches: 89,
        functions: 96,
        lines: 95,
      },
    },
  },
});

import { resolve } from "node:path";
import { defineConfig } from "vitest/config";

export default defineConfig({
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
      // package.
      include: ["src/**/*.ts"],
      exclude: [
        "coverage/**",
        "dist/**",
        "**/*.d.ts",
        "**/*.config.*",
        "**/__tests__/**",
      ],
      thresholds: {
        statements: 96,
        branches: 92,
        functions: 98,
        lines: 96,
      },
    },
  },
});

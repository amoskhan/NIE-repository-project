import { fileURLToPath, URL } from "node:url";
import vue from "@vitejs/plugin-vue";
import { defineConfig } from "vitest/config";

// Unit tests for @apptemplate/ui components. The Vue plugin is what lets Vitest import
// a .vue single-file component; without it the test would fail at the import line.
export default defineConfig({
  plugins: [vue()],
  resolve: {
    alias: {
      "@": fileURLToPath(new URL("./src", import.meta.url)),
    },
  },
  test: {
    name: "ui",
    environment: "jsdom",
    include: ["src/**/*.{test,spec}.ts"],
  },
});

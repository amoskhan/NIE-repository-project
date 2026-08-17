import { fileURLToPath, URL } from "node:url";
import { defineConfig } from "vitest/config";

// Unit tests for @apptemplate/shared. Run them from here with `pnpm test:unit`, or
// from src/frontend with `pnpm test:unit --project shared`.
export default defineConfig({
  resolve: {
    alias: {
      "@": fileURLToPath(new URL("./src", import.meta.url)),
    },
  },
  test: {
    name: "shared",
    // jsdom gives the tests a `window`/`document`, which the runtime-config helpers need.
    environment: "jsdom",
    include: ["src/**/*.{test,spec}.ts"],
  },
});

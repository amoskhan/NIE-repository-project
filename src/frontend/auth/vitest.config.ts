import vue from "@vitejs/plugin-vue";
import { defineConfig } from "vitest/config";

// Unit tests for the auth app (login / register / password reset).
export default defineConfig({
  plugins: [vue()],
  test: {
    name: "auth",
    environment: "jsdom",
    include: ["src/**/*.{test,spec}.ts"],
  },
});

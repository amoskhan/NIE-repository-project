import { defineConfig } from "vite";
import vue from "@vitejs/plugin-vue";
import { fileURLToPath, URL } from "node:url";

// https://vite.dev/config/
export default defineConfig({
  plugins: [vue()],
  resolve: {
    alias: {
      "@": fileURLToPath(new URL("./src", import.meta.url)),
    },
  },
  base: "./",
  server: {
    port: 8002,
    strictPort: true,
    host: true,
    proxy: {
      "/api-auth/api": {
        target: "http://localhost:5001",
        changeOrigin: true,
        secure: false,
        rewrite: (path) => path.replace(/^\/api-auth/, ""),
      },
      "/api-main": {
        target: "http://localhost:5002",
        changeOrigin: true,
        secure: false,
        rewrite: (path) => path.replace(/^\/api-main/, ""),
      },
    },
  },
  build: {
    rollupOptions: {
      output: {
        manualChunks(id) {
          const normalizedId = id.replace(/\\/g, "/");
          if (!normalizedId.includes("node_modules")) return;
          if (normalizedId.includes("/@sentry/")) return "sentry";
          if (normalizedId.includes("/@opentelemetry/")) return "otel";
          if (normalizedId.includes("/vue")) return "vue";
          return "vendor";
        },
      },
    },
  },
});

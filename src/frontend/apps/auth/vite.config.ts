import { defineConfig } from "vite";
import vue from "@vitejs/plugin-vue";

// https://vite.dev/config/
export default defineConfig({
  plugins: [vue()],
  base: "./",
  server: {
    port: 8001,
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

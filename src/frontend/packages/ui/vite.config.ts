import { defineConfig } from "vite";
import vue from "@vitejs/plugin-vue";
import dts from "vite-plugin-dts";
import { resolve } from "path";

export default defineConfig({
  plugins: [
    vue(),
    dts({
      insertTypesEntry: true,
      include: ["src/**/*.ts", "src/**/*.vue"],
    }),
  ],
  build: {
    lib: {
      entry: resolve(__dirname, "src/index.ts"),
      name: "AppTemplateUI",
      fileName: "index",
      formats: ["es"],
    },
    rollupOptions: {
      external: ["vue", "vue-router", "@heroicons/vue"],
      output: {
        globals: {
          vue: "Vue",
          "vue-router": "VueRouter",
        },
        assetFileNames: "ui.[ext]",
      },
    },
    cssCodeSplit: false,
  },
  resolve: {
    alias: {
      "@": resolve(__dirname, "src"),
    },
  },
});

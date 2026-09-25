import { defineConfig } from "vite";
import vue from "@vitejs/plugin-vue";
import { fileURLToPath } from "node:url";
import { syllabusPlugin } from "./build/syllabusPlugin.mjs";

export default defineConfig({
  plugins: [vue(), syllabusPlugin()],
  server: {
    host: true,
    port: 18100,
    strictPort: true,
    proxy: {
      "/~ignite/services/syllabus-chat-api": {
        target: "http://127.0.0.1:15100",
        rewrite: (path) => path.replace(/^\/\~ignite\/services\/syllabus-chat-api/, ""),
      },
    },
    fs: {
      allow: [
        fileURLToPath(new URL(".", import.meta.url)),
        fileURLToPath(new URL("../../../../upload/2024 Physical Education Primary Secondary and PreUniversity Syllabus (1).pdf", import.meta.url)),
      ],
    },
  },
});

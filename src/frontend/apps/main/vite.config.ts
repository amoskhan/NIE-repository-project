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
    fs: {
      allow: [
        fileURLToPath(new URL(".", import.meta.url)),
        fileURLToPath(new URL("../../../../upload/2024 Physical Education Primary Secondary and PreUniversity Syllabus (1).pdf", import.meta.url)),
      ],
    },
  },
});

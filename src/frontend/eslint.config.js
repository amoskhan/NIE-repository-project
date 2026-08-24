import js from "@eslint/js";
import { defineConfig, globalIgnores } from "eslint/config";
import pluginVue from "eslint-plugin-vue";
import globals from "globals";
import tseslint from "typescript-eslint";

export default defineConfig([
  globalIgnores([
    "**/dist/**",
    "**/node_modules/**",
    "**/coverage/**",
    "**/.stryker-tmp/**",
    "**/reports/mutation/**",
    "**/*.d.ts",
  ]),
  {
    ...js.configs.recommended,
    files: ["**/*.{js,mjs,cjs}"],
    languageOptions: {
      ...js.configs.recommended.languageOptions,
      globals: globals.node,
    },
  },
  ...tseslint.configs.recommended.map((config) => ({
    ...config,
    files: ["**/*.{ts,vue}"],
  })),
  ...pluginVue.configs["flat/essential"],
  {
    files: ["**/*.{ts,vue}"],
    languageOptions: {
      globals: {
        ...globals.browser,
        ...globals.node,
      },
      parserOptions: {
        parser: tseslint.parser,
      },
    },
    rules: {
      "vue/component-api-style": ["error", ["script-setup"]],
      "vue/multi-word-component-names": "error",
      "vue/no-undef-properties": "error",
      "vue/require-explicit-emits": "error",
    },
  },
  {
    files: ["**/public/sw.js"],
    languageOptions: {
      globals: globals.serviceworker,
    },
  },
]);

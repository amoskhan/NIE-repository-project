import { readFileSync } from "node:fs";
import { resolve } from "node:path";
import { describe, expect, it } from "vitest";

const typographySources = [
  "index.html",
  "../auth/index.html",
  "tailwind.config.js",
  "../auth/tailwind.config.js",
  "../../packages/ui/src/styles/globals.css",
  "../../packages/ui/src/theme/presets.ts",
];

const legacyTypefacePattern = new RegExp(["Lex", "end|Nu", "nito"].join(""));

describe("Ignite typography", () => {
  it("uses Plus Jakarta Sans throughout the main, auth, and shared UI themes", () => {
    for (const relativePath of typographySources) {
      const content = readFileSync(
        resolve(process.cwd(), relativePath),
        "utf8",
      );
      const normalizedContent = content.replaceAll("+", " ");
      expect(normalizedContent, relativePath).toContain("Plus Jakarta Sans");
      expect(content, relativePath).not.toMatch(legacyTypefacePattern);
    }

    const mainEntry = readFileSync(
      resolve(process.cwd(), "src/main.ts"),
      "utf8",
    );
    expect(mainEntry).toContain('import "@nie/ui/styles"');
  });
});

import { defineConfig } from "vitest/config";

/**
 * Workspace-level unit-test runner (Vitest).
 *
 * Playwright, in /tests, covers the running system end to end. Vitest covers the
 * opposite end: single functions and single components, in milliseconds, with no
 * server, no database and no browser.
 *
 * Each entry below is a folder that owns its own `vitest.config.ts`. Running
 * `pnpm test:unit` from src/frontend runs all of them; `--project shared` runs one.
 *
 * TO ADD A PACKAGE:
 *   1. copy an existing `vitest.config.ts` (they are ~10 lines) into the package,
 *      giving it a unique `test.name`,
 *   2. add `"test:unit": "vitest run"` to that package's scripts,
 *   3. add the folder to the list below.
 *
 * TO ADD A TEST: create `<something>.test.ts` next to the code it covers. Colocating
 * tests keeps them visible — a file with no neighbouring test is obvious at a glance.
 */
export default defineConfig({
  test: {
    projects: ["packages/shared", "packages/ui", "auth"],
  },
});

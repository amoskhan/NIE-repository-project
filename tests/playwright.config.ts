import { defineConfig, devices } from "@playwright/test";
import * as dotenv from "dotenv";
import * as path from "path";

// Determine which environment file to load based on TEST_ENV
// TEST_ENV=dev -> .env.dev (local development - default)
// TEST_ENV=stg -> .env.stg (staging)
// TEST_ENV=prod -> .env.prod (production - read-only tests)
const testEnv = process.env.TEST_ENV || "dev";
const envFileMap: Record<string, string> = {
  dev: ".env.dev",
  stg: ".env.stg",
  prod: ".env.prod",
};
const envFile = envFileMap[testEnv] || ".env.dev";

// Load environment variables from the appropriate file
const envPath = path.resolve(__dirname, envFile);
dotenv.config({ path: envPath });

// Also load local overrides if they exist (for secrets)
const localEnvPath = path.resolve(__dirname, `${envFile}.local`);
dotenv.config({ path: localEnvPath, override: true });

// Parse headless setting from environment (default: true for CI, false for local dev)
const isHeadless = process.env.HEADLESS === "true" || process.env.CI === "true";

console.log(`[Playwright Config] Loading environment: ${envFile}`);
console.log(
  `[Playwright Config] Headless mode: ${isHeadless} (HEADLESS=${process.env.HEADLESS}, CI=${process.env.CI})`,
);

/**
 * NIE Template Test Configuration
 * @see https://playwright.dev/docs/test-configuration
 */
export default defineConfig({
  testDir: "./specs",
  /* Run tests in files in parallel */
  fullyParallel: true,
  /* Fail the build on CI if you accidentally left test.only in the source code. */
  forbidOnly: !!process.env.CI,
  /* Retry on CI only */
  retries: process.env.CI ? 2 : 0,
  /* Opt out of parallel tests on CI. */
  workers: process.env.CI ? 1 : undefined,
  /* Reporter to use. See https://playwright.dev/docs/test-reporters */
  reporter: process.env.CI
    ? [
        ["github"],
        ["html", { outputFolder: "reports/html" }],
        ["junit", { outputFile: "reports/junit.xml" }],
        ["json", { outputFile: "reports/results.json" }],
      ]
    : [
        ["html", { outputFolder: "reports/html" }],
        ["junit", { outputFile: "reports/junit.xml" }],
        ["json", { outputFile: "reports/results.json" }],
        ["list"],
      ],
  /* Shared settings for all the projects below. See https://playwright.dev/docs/api/class-testoptions. */
  use: {
    /* Collect trace when retrying the failed test. See https://playwright.dev/docs/trace-viewer */
    trace: "on-first-retry",

    /* Take screenshot on failure (or always for debugging) */
    screenshot: process.env.RECORD_VIDEO === "true" ? "on" : "only-on-failure",

    /* Record video when RECORD_VIDEO=true */
    video: process.env.RECORD_VIDEO === "true" ? "on" : "retain-on-failure",

    /* Ignore HTTPS errors for testing */
    ignoreHTTPSErrors: true,

    /* Timeouts */
    actionTimeout: parseInt(process.env.ACTION_TIMEOUT || "15000"),
    navigationTimeout: parseInt(process.env.NAVIGATION_TIMEOUT || "30000"),
  },

  /* Configure projects for different test types */
  projects: [
    // API Tests - No browser UI needed but uses Playwright's request context
    {
      name: "api-tests",
      testMatch: ["**/auth/**/*.spec.ts", "**/api/**/*.spec.ts"],
      use: {
        ...devices["Desktop Chrome"],
        headless: true, // API tests don't need browser UI
      },
    },

    // E2E Tests - Desktop Chrome
    {
      name: "e2e-chromium",
      testMatch: "**/e2e/**/*.spec.ts",
      use: {
        ...devices["Desktop Chrome"],
        headless: isHeadless,
      },
    },

    // E2E Tests - Mobile viewport (optional)
    {
      name: "e2e-mobile",
      testMatch: "**/e2e/**/*.spec.ts",
      use: {
        ...devices["Pixel 5"],
        headless: isHeadless,
      },
    },
  ],

  /* Output folder for test artifacts */
  outputDir: "test-results/",

  /* Global timeout for each test */
  timeout: 60000,

  /* Expect timeout */
  expect: {
    timeout: parseInt(process.env.EXPECT_TIMEOUT || "10000"),
  },
});

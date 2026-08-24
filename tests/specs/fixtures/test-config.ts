/**
 * Test Configuration
 * Centralized configuration for all tests
 *
 * Environment Selection:
 * - Set TEST_ENV=dev to use .env.dev (local development - default)
 * - Set TEST_ENV=stg to use .env.stg (staging)
 *
 * IMPORTANT: Environment variables are loaded by playwright.config.ts
 * This file only reads from process.env
 */

/**
 * Helper to get required environment variable
 * Throws error if not set - ensures no silent fallbacks
 */
function requireEnv(name: string): string {
  const value = process.env[name];
  if (!value) {
    throw new Error(
      `Missing required environment variable: ${name}. ` +
        `Make sure you're running tests with the correct TEST_ENV setting.`,
    );
  }
  return value;
}

/**
 * Helper to get optional environment variable with default
 * Only use for truly optional config like timeouts
 */
function optionalEnv(name: string, defaultValue: string): string {
  return process.env[name] || defaultValue;
}

export const TestConfig = {
  // Base URL
  baseUrl: optionalEnv("BASE_URL", "http://localhost"),

  // API URLs - REQUIRED, must end with / for proper URL joining
  authApiUrl: requireEnv("AUTH_API_URL"),
  mainApiUrl: requireEnv("MAIN_API_URL"),

  // Frontend URLs - REQUIRED
  frontendAuth: requireEnv("FRONTEND_AUTH"),
  frontendMain: requireEnv("FRONTEND_MAIN"),

  // Timeouts - optional with sensible defaults
  apiTimeout: parseInt(optionalEnv("API_TIMEOUT", "30000")),
  navigationTimeout: parseInt(optionalEnv("NAVIGATION_TIMEOUT", "30000")),
  actionTimeout: parseInt(optionalEnv("ACTION_TIMEOUT", "15000")),
  expectTimeout: parseInt(optionalEnv("EXPECT_TIMEOUT", "10000")),

  // Browser options - optional with sensible defaults
  headless: process.env.HEADLESS !== "false",
  recordVideo: process.env.RECORD_VIDEO === "true",
  screenshotOnFailure: process.env.SCREENSHOT_ON_FAILURE !== "false",
  portalSsoExpectedEnabled: process.env.PORTAL_SSO_EXPECT_ENABLED === "true",
};

/**
 * Frontend Routes for E2E testing
 * Note: No leading slash - frontend URLs already end with /
 */
export const Routes = {
  // Main app routes
  dashboard: "dashboard",
  profile: "profile",
  // Procurement reference sample routes (remove only after an approved domain replacement)
  vendors: "vendors",
  catalog: "catalog",
  orderHistory: "orders",
  approvals: "approvals",

  // Auth app routes
  login: "",
  logout: "logout",
} as const;

/**
 * API Endpoints
 * Paths relative to the API base URL
 */
export const ApiEndpoints = {
  // Auth API
  auth: {
    login: "Auth/Login",
    logout: "Auth/Logout",
    verify: "Auth/Verify",
    getUser: "Auth/GetUser",
    createTestSession: "Auth/CreateTestSession",
    ssoStart: "Auth/SsoStart",
    ssoCallback: "Auth/SsoCallback",
    ssoFinalize: "Auth/SsoFinalize",
  },

  // Main API - Procurement (reference sample; remove only after an approved domain replacement)
  vendor: {
    getAll: "Vendor/GetAll",
    get: (id: string) => `Vendor/Get/${id}`,
    save: "Vendor/Save",
    edit: "Vendor/Edit",
    delete: (id: string) => `Vendor/Delete/${id}`,
  },
  purchaseOrder: {
    getAll: "PurchaseOrder/GetAll",
    get: (id: string) => `PurchaseOrder/Get/${id}`,
    save: "PurchaseOrder/Save",
    edit: "PurchaseOrder/Edit",
    delete: (id: string) => `PurchaseOrder/Delete/${id}`,
  },

  // Main API - Code
  code: {
    getByType: (type: string) => `Code/GetByType/${type}`,
    getAll: "Code/GetAll",
  },

  // Main API - Document
  document: {
    upload: "Document/Upload",
    download: (id: string) => `Document/Download/${id}`,
    delete: (id: string) => `Document/Delete/${id}`,
  },

  // Health checks
  health: {
    auth: "health",
    main: "health",
  },
} as const;

export default TestConfig;

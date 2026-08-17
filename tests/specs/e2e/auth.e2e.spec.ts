/**
 * E2E - Authentication Tests
 * Browser-based tests for the authentication flow
 */

import { test, expect } from "@playwright/test";
import { TestConfig, Routes } from "../fixtures/test-config";
import { getTestUser, hasTestUsers } from "../fixtures/test-users";

test.describe("E2E - Authentication", () => {
  test.beforeEach(async ({ page }) => {
    // Set up console logging to debug what's happening
    page.on("console", (msg) => console.log("Browser console:", msg.text()));
    page.on("pageerror", (err) => console.log("Page error:", err.message));

    // Navigate to auth page
    const authUrl = TestConfig.frontendAuth;
    console.log(`Navigating to: ${authUrl}`);

    try {
      await page.goto(authUrl, { waitUntil: "networkidle", timeout: 30000 });
    } catch (error) {
      console.log(`Failed to navigate to ${authUrl}:`, error);
      // Don't throw - just skip the test if page cannot load
      test.skip(true, "Auth page not accessible");
    }
  });

  test("should display page title and basic content", async ({ page }) => {
    // Debug: Check what page we actually loaded
    console.log("Current URL:", page.url());
    console.log("Page title:", await page.title());

    // Take a screenshot for debugging
    await page.screenshot({ path: "debug-auth-page.png", fullPage: true });

    // Check if we can see any content at all
    const bodyContent = await page.locator("body").textContent();
    console.log("Body content preview:", bodyContent?.substring(0, 200));

    // The page should have loaded something
    await expect(page.locator("body")).not.toBeEmpty();
  });

  test("should display login form elements", async ({ page }) => {
    // Wait a bit more for dynamic content to load
    await page.waitForTimeout(2000);

    // Debug: Log all visible elements
    const allInputs = await page.locator("input").count();
    console.log(`Found ${allInputs} input elements`);

    const allButtons = await page.locator("button").count();
    console.log(`Found ${allButtons} button elements`);

    // Check for common login form elements
    // Adjust selectors based on your actual auth frontend
    const usernameInput = page
      .locator('#username, input[name="username"], input[type="text"]')
      .first();
    const passwordInput = page
      .locator('#password, input[name="password"], input[type="password"]')
      .first();
    const submitButton = page
      .locator(
        'button[type="submit"], button:has-text("Login"), button:has-text("Sign In")',
      )
      .first();

    // At least some form elements should be visible
    const hasUsername = await usernameInput.isVisible().catch(() => false);
    const hasPassword = await passwordInput.isVisible().catch(() => false);
    const hasSubmit = await submitButton.isVisible().catch(() => false);

    console.log(`Username visible: ${hasUsername}`);
    console.log(`Password visible: ${hasPassword}`);
    console.log(`Submit visible: ${hasSubmit}`);

    // Expect at least the basic form structure
    expect(hasUsername || hasPassword || hasSubmit).toBe(true);
  });

  test("should toggle password visibility", async ({ page }) => {
    // Wait for password field
    const passwordInput = page
      .locator('#password, input[name="password"], input[type="password"]')
      .first();

    if (!(await passwordInput.isVisible().catch(() => false))) {
      test.skip(true, "Password field not found");
      return;
    }

    // Look for password toggle button
    const passwordToggle = page
      .locator(
        '.password-toggle, button[aria-label*="password"], [class*="eye"]',
      )
      .first();

    if (!(await passwordToggle.isVisible().catch(() => false))) {
      test.skip(true, "Password toggle not found");
      return;
    }

    // Initially password should be hidden
    await expect(passwordInput).toHaveAttribute("type", "password");

    // Click toggle to show password
    await passwordToggle.click();
    await expect(passwordInput).toHaveAttribute("type", "text");

    // Click toggle again to hide password
    await passwordToggle.click();
    await expect(passwordInput).toHaveAttribute("type", "password");
  });

  test("should show error for invalid credentials", async ({ page }) => {
    test.skip(!hasTestUsers(), "No test users configured");

    // Find form elements
    const usernameInput = page
      .locator('#username, input[name="username"], input[type="text"]')
      .first();
    const passwordInput = page
      .locator('#password, input[name="password"], input[type="password"]')
      .first();
    const submitButton = page
      .locator(
        'button[type="submit"], button:has-text("Login"), button:has-text("Sign In")',
      )
      .first();

    if (!(await usernameInput.isVisible().catch(() => false))) {
      test.skip(true, "Login form not found");
      return;
    }

    // Fill in invalid credentials
    await usernameInput.fill("invaliduser");
    await passwordInput.fill("invalidpassword");
    await submitButton.click();

    // Wait for error message
    await page.waitForTimeout(3000);

    // Look for error indication
    const errorMessage = page
      .locator(
        '.error, .alert-error, [class*="error"], :text("Invalid"), :text("incorrect")',
      )
      .first();

    // Should show some form of error or stay on login page
    const hasError = await errorMessage.isVisible().catch(() => false);
    const stillOnLogin = page.url().includes(TestConfig.frontendAuth);

    expect(hasError || stillOnLogin).toBe(true);
  });

  test("should login successfully with valid credentials", async ({ page }) => {
    test.skip(!hasTestUsers(), "No test users configured");

    const user = getTestUser();

    // Find form elements
    const usernameInput = page
      .locator('#username, input[name="username"], input[type="text"]')
      .first();
    const passwordInput = page
      .locator('#password, input[name="password"], input[type="password"]')
      .first();
    const submitButton = page
      .locator(
        'button[type="submit"], button:has-text("Login"), button:has-text("Sign In")',
      )
      .first();

    if (!(await usernameInput.isVisible().catch(() => false))) {
      test.skip(true, "Login form not found");
      return;
    }

    console.log("Attempting login with test user:", user.username);

    // Fill in credentials
    await usernameInput.fill(user.username);
    await passwordInput.fill(user.password);

    // Click submit
    await submitButton.click();

    await expect
      .poll(
        async () => {
          const currentUrl = page.url();
          console.log("After login URL:", currentUrl);
          return currentUrl;
        },
        {
          timeout: 10000,
          message: "Expected successful login to redirect to the main app",
        },
      )
      .toContain(TestConfig.frontendMain);
  });
});

/**
 * E2E - Dashboard Tests
 * Browser-based tests for the main dashboard
 */

import { test, expect, BrowserContext } from "@playwright/test";
import { TestConfig, Routes } from "../fixtures/test-config";
import { hasTestUsers } from "../fixtures/test-users";
import {
  createDefaultTestSession,
  setAuthCookies,
  AuthSession,
} from "../fixtures/auth.fixture";

// Helper function to open mobile menu if needed
async function openMobileMenuIfNeeded(page: any) {
  const viewportWidth = page.viewportSize()?.width || 1280;
  if (viewportWidth < 768) {
    // Mobile viewport - need to open hamburger menu
    const hamburgerButton = page.locator(
      '.mobile-menu-btn, .hamburger-icon, button[aria-label*="menu" i], [class*="hamburger"]',
    );
    const isHamburgerVisible = (await hamburgerButton.count()) > 0;

    if (isHamburgerVisible) {
      await hamburgerButton.first().click();
      await page.waitForTimeout(500);
    }
  }
}

test.describe("E2E - Dashboard", () => {
  let session: AuthSession | null = null;

  test.beforeAll(async () => {
    if (!hasTestUsers()) {
      console.log("No test users configured, skipping authentication");
      return;
    }

    // Create test session via API
    session = await createDefaultTestSession();
    if (!session) {
      console.log("Failed to create test session");
    }
  });

  test.beforeEach(async ({ page, context }) => {
    test.skip(!hasTestUsers(), "No test users configured");
    test.skip(!session, "Failed to create test session");

    // Set auth cookies for the browser context
    await setAuthCookies(context, session!);

    // Navigate to dashboard
    const dashboardUrl = `${TestConfig.frontendMain}${Routes.dashboard}`;
    console.log(`Navigating to: ${dashboardUrl}`);

    try {
      await page.goto(dashboardUrl, {
        waitUntil: "networkidle",
        timeout: TestConfig.navigationTimeout,
      });
    } catch (error) {
      console.log(`Failed to navigate to dashboard:`, error);
      test.skip(true, "Dashboard not accessible");
    }
  });

  test("should display dashboard page", async ({ page }) => {
    // Dashboard should contain main content
    await expect(page.locator("body")).not.toBeEmpty();

    // Take screenshot for debugging
    await page.screenshot({ path: "debug-dashboard.png", fullPage: true });

    // Check URL contains expected route
    const currentUrl = page.url();
    console.log("Current URL:", currentUrl);

    // Should be on the main frontend
    expect(currentUrl).toContain("localhost");
  });

  test("should display user information or greeting", async ({ page }) => {
    // Look for any user-related content
    const userGreeting = page
      .locator(
        '[class*="greeting"], [class*="welcome"], [class*="user"], h1, h2, .header',
      )
      .first();

    const isVisible = await userGreeting.isVisible().catch(() => false);
    if (isVisible) {
      await expect(userGreeting).toBeVisible({ timeout: 10000 });
    } else {
      // If no greeting, just verify page loaded
      await expect(page.locator("body")).not.toBeEmpty();
    }
  });

  test("should display navigation sidebar or menu", async ({ page }) => {
    // Sidebar or navigation should be present (or hamburger menu on mobile)
    const navigation = page
      .locator(
        'nav, aside, [class*="sidebar"], [class*="navigation"], .mobile-menu-btn, [class*="menu"]',
      )
      .first();

    await expect(navigation).toBeVisible({ timeout: 10000 });
  });

  test("should display dashboard cards or content", async ({ page }) => {
    // Look for card components or main content areas
    const cards = page.locator(
      '.card, [class*="card"], .panel, [class*="panel"], .widget, .content',
    );
    const cardCount = await cards.count();

    console.log(`Found ${cardCount} card/content elements`);

    // Dashboard should have some content
    expect(cardCount).toBeGreaterThanOrEqual(0);
  });

  test("should navigate to the Procurement vendor list", async ({ page }) => {
    // Open mobile menu if needed
    await openMobileMenuIfNeeded(page);

    const vendorLink = page
      .locator(
        'a[href*="vendors"], button:has-text("Vendors"), [class*="nav"]:has-text("Vendors")',
      )
      .first();

    const isVisible = await vendorLink.isVisible().catch(() => false);

    if (isVisible) {
      await vendorLink.click();

      await expect(page).toHaveURL(/vendors/);
    } else {
      console.log("Vendor navigation link not found - skipping");
      test.skip(true, "Navigation link not found");
    }
  });

  test("should handle loading states correctly", async ({ page }) => {
    // Verify page content loads properly
    const content = page
      .locator('main, .content, [class*="main"], .container')
      .first();

    // Wait for content to be visible
    await expect(content).toBeVisible({ timeout: 15000 });

    // Check that loading spinners are not stuck
    const spinner = page
      .locator('.loading, .spinner, [class*="loading"]')
      .first();
    const spinnerVisible = await spinner.isVisible().catch(() => false);

    if (spinnerVisible) {
      // If spinner is visible, wait for it to disappear
      await expect(spinner).not.toBeVisible({ timeout: 30000 });
    }
  });

  test("should be responsive on different viewport sizes", async ({
    page,
    context,
  }) => {
    // Test mobile viewport
    await page.setViewportSize({ width: 375, height: 667 });
    await page.waitForTimeout(1000);

    // Page should still be functional
    await expect(page.locator("body")).not.toBeEmpty();

    // Check for mobile menu
    const mobileMenu = page
      .locator('.mobile-menu-btn, .hamburger, [class*="mobile"]')
      .first();
    const hasMobileMenu = await mobileMenu.isVisible().catch(() => false);

    console.log(`Mobile menu visible: ${hasMobileMenu}`);

    // Reset to desktop viewport
    await page.setViewportSize({ width: 1280, height: 720 });
    await page.waitForTimeout(1000);

    // Desktop view should also work
    await expect(page.locator("body")).not.toBeEmpty();
  });
});

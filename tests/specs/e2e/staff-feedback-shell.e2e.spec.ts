import { expect, test, type Page } from "@playwright/test";
import { TestConfig } from "../fixtures/test-config";

const MAIN_APP_URL = TestConfig.frontendMain;

test.use({ serviceWorkers: "block" });

const mockUser = {
  userId: "feedback-shell-user",
  fullName: "Feedback Shell User",
  email: "feedback.shell@example.edu.sg",
  roles: ["SystemAdmin"],
  roleNames: ["System Administrator"],
  permissions: [
    "screen.operations.view",
    "screen.reports.view",
    "api.report.read",
    "api.chat.use",
    "screen.access-control.view",
    "api.access-control.read",
    "api.access-control.roles.manage",
    "api.access-control.assignments.manage",
  ],
};

async function mockStaffShell(page: Page) {
  const mainOrigin = new URL(MAIN_APP_URL).origin;
  await page.context().addCookies([
    {
      name: "Application-SessionToken",
      value: "feedback-shell-session",
      url: mainOrigin,
    },
    {
      name: "Application-User",
      value: JSON.stringify(mockUser),
      url: mainOrigin,
    },
  ]);

  await page.route("**/api-main/api/**", async (route) => {
    const url = route.request().url();
    let body: unknown = [];

    if (url.includes("/AccessControl/GetCurrentAccessProfile")) {
      body = {
        userId: mockUser.userId,
        roleCodes: mockUser.roles,
        roleNames: mockUser.roleNames,
        accessFunctionCodes: mockUser.permissions,
      };
    }

    if (url.includes("/Feedback/Submit")) {
      body = { acknowledged: true };
    }

    await route.fulfill({
      status: 200,
      contentType: "application/json",
      body: JSON.stringify(body),
    });
  });
}

test.describe("staff shell feedback actions", () => {
  test("shows title-adjacent feedback actions instead of a floating button on desktop", async ({
    page,
  }) => {
    await mockStaffShell(page);
    await page.setViewportSize({ width: 1280, height: 720 });
    await page.goto(`${MAIN_APP_URL}#/vendors`);

    const header = page.getByRole("banner", { name: "Staff portal header" });
    await expect(
      header.getByRole("heading", { name: "Vendors" }),
    ).toBeVisible();
    await expect(header.getByLabel("Share positive feedback")).toBeVisible();
    await expect(header.getByLabel("Share negative feedback")).toBeVisible();
    await expect(page.locator("#floating-feedback-root")).toHaveCount(0);

    const opener = header.getByLabel("Share positive feedback");
    await opener.click();
    const dialog = page.getByRole("dialog", {
      name: "Was Vendors useful?",
    });
    await expect(dialog).toBeVisible();
    await expect(dialog.getByRole("button", { name: "Close dialog" })).toBeFocused();
    await expect(page.getByLabel("Thumbs up")).toHaveClass(/active/);
    await page.keyboard.press("Shift+Tab");
    expect(await dialog.evaluate((element) => element.contains(document.activeElement))).toBe(
      true,
    );
    await page.keyboard.press("Escape");
    await expect(dialog).toHaveCount(0);
    await expect(opener).toBeFocused();
  });

  test("keeps the page title, feedback actions, and feedback popup usable on mobile", async ({
    page,
  }) => {
    await mockStaffShell(page);
    await page.setViewportSize({ width: 390, height: 844 });
    await page.goto(`${MAIN_APP_URL}#/vendors`);

    const header = page.getByRole("banner", { name: "Staff portal header" });
    await expect(
      header.getByRole("heading", { name: "Vendors" }),
    ).toBeVisible();
    await expect(header.getByLabel("Share positive feedback")).toBeVisible();
    await expect(header.getByLabel("Share negative feedback")).toBeVisible();

    await header.getByLabel("Share negative feedback").click();
    const dialog = page.getByRole("dialog", {
      name: "Was Vendors useful?",
    });
    await expect(dialog).toBeVisible();
    await expect
      .poll(async () => {
        const geometry = await dialog.boundingBox();
        return Math.abs((geometry?.y ?? 0) + (geometry?.height ?? 0) - 844);
      })
      .toBeLessThanOrEqual(1);
    expect((await dialog.boundingBox())?.width).toBeLessThanOrEqual(390);
    await expect(page.getByLabel("Thumbs down")).toHaveClass(/active/);
    await dialog.getByLabel("Additional feedback").fill("Mobile feedback");
    await dialog.getByRole("button", { name: "Submit Feedback" }).click();
    await expect(
      page.getByRole("status").filter({ hasText: "Thank you!" }),
    ).toBeVisible();
  });
});

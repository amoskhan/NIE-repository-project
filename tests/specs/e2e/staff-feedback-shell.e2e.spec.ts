import { expect, test, type Page } from "@playwright/test";
import { CookieNames } from "../fixtures/cookie-names";

const MAIN_APP_URL =
  process.env.FEEDBACK_SHELL_FRONTEND_MAIN ?? "http://localhost:8002/";

test.use({ serviceWorkers: "block" });

const mockUser = {
  userId: "feedback-shell-user",
  fullName: "Feedback Shell User",
  email: "feedback.shell@example.edu",
  roles: ["SystemAdmin"],
  roleNames: ["System Administrator"],
  permissions: [
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
  await page.context().addCookies([
    {
      name: CookieNames.session,
      value: "feedback-shell-session",
      domain: "localhost",
      path: "/",
    },
    {
      name: CookieNames.user,
      value: JSON.stringify(mockUser),
      domain: "localhost",
      path: "/",
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

    await header.getByLabel("Share positive feedback").click();
    await expect(page.getByRole("dialog", { name: "Feedback" })).toBeVisible();
    await expect(page.getByLabel("Thumbs up")).toHaveClass(/active/);
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
    const dialog = page.getByRole("dialog", { name: "Feedback" });
    await expect(dialog).toBeVisible();
    await expect(dialog).toHaveClass(/feedback-modal__sheet/);
    await expect(page.getByLabel("Thumbs down")).toHaveClass(/active/);
  });
});

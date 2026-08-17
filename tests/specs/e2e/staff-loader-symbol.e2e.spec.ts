import { expect, test, type Page } from "@playwright/test";
import { CookieNames } from "../fixtures/cookie-names";

const MAIN_APP_URL =
  process.env.LOADER_SYMBOL_FRONTEND_MAIN ??
  process.env.FEEDBACK_SHELL_FRONTEND_MAIN ??
  "http://localhost:8002/";

test.use({ serviceWorkers: "block" });

const mockUser = {
  userId: "loader-symbol-user",
  fullName: "Loader Symbol User",
  email: "loader.symbol@example.edu",
  roles: ["SystemAdmin"],
  roleNames: ["System Administrator"],
  permissions: [
    "screen.access-control.view",
    "api.access-control.read",
    "api.access-control.roles.manage",
    "api.access-control.assignments.manage",
  ],
};

async function mockStaffShellWithDelayedVendors(page: Page) {
  let releaseVendors!: () => void;
  const vendorsGate = new Promise<void>((resolve) => {
    releaseVendors = resolve;
  });

  await page.context().addCookies([
    {
      name: CookieNames.session,
      value: "loader-symbol-session",
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

    if (url.includes("/Vendor/GetAll")) {
      await vendorsGate;
      body = [];
    }

    await route.fulfill({
      status: 200,
      contentType: "application/json",
      body: JSON.stringify(body),
    });
  });

  return releaseVendors;
}

test.describe("staff loading symbol", () => {
  test("shows the shared app loader symbol while data tables load on desktop", async ({
    page,
  }) => {
    const releaseVendors = await mockStaffShellWithDelayedVendors(page);
    await page.setViewportSize({ width: 1280, height: 720 });
    await page.goto(`${MAIN_APP_URL}#/vendors`);

    await expect(page.getByTestId("app-loader-symbol")).toBeVisible();

    releaseVendors();
    await expect(page.getByTestId("app-loader-symbol")).toBeHidden();
  });

  test("shows the shared app loader symbol while data tables load on mobile", async ({
    page,
  }) => {
    const releaseVendors = await mockStaffShellWithDelayedVendors(page);
    await page.setViewportSize({ width: 390, height: 844 });
    await page.goto(`${MAIN_APP_URL}#/vendors`);

    await expect(page.getByTestId("app-loader-symbol")).toBeVisible();

    releaseVendors();
    await expect(page.getByTestId("app-loader-symbol")).toBeHidden();
  });
});

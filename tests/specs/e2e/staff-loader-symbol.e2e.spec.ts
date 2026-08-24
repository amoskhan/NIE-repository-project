import { expect, test, type Page } from "@playwright/test";
import { TestConfig } from "../fixtures/test-config";

const MAIN_APP_URL = TestConfig.frontendMain;

test.use({ serviceWorkers: "block" });

const mockUser = {
  userId: "loader-symbol-user",
  fullName: "Loader Symbol User",
  email: "loader.symbol@example.edu.sg",
  roles: ["SystemAdmin"],
  roleNames: ["System Administrator"],
  permissions: [
    "screen.dashboard.view",
    "screen.operations.view",
    "screen.access-control.view",
    "api.access-control.read",
    "api.access-control.roles.manage",
    "api.access-control.assignments.manage",
    "screen.notification-administration.view",
    "api.notification-configuration.read",
    "api.notification-configuration.manage",
    "api.notification-delivery.read",
    "api.notification-delivery.retry",
  ],
};

async function mockStaffShellWithDelayedVendors(page: Page) {
  const mainOrigin = new URL(MAIN_APP_URL).origin;
  let releaseVendors!: () => void;
  const vendorsGate = new Promise<void>((resolve) => {
    releaseVendors = resolve;
  });

  await page.context().addCookies([
    {
      name: "Application-SessionToken",
      value: "loader-symbol-session",
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

    if (url.includes("/Vendor/Search")) {
      await vendorsGate;
      body = {
        items: [],
        totalCount: 0,
        page: 1,
        pageSize: 10,
        totalPages: 0,
        hasPreviousPage: false,
        hasNextPage: false,
      };
    }

    await route.fulfill({
      status: 200,
      contentType: "application/json",
      body: JSON.stringify(body),
    });
  });

  return releaseVendors;
}

async function mockDashboardFailureThenSuccess(page: Page) {
  let overviewRequests = 0;
  const mainOrigin = new URL(MAIN_APP_URL).origin;
  await page.context().addCookies([
    {
      name: "Application-SessionToken",
      value: "loader-symbol-session",
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
    if (url.includes("/AccessControl/GetCurrentAccessProfile")) {
      await route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          userId: mockUser.userId,
          roleCodes: mockUser.roles,
          roleNames: mockUser.roleNames,
          accessFunctionCodes: mockUser.permissions,
        }),
      });
      return;
    }

    if (url.includes("/PurchaseOrder/GetSpendOverview")) {
      overviewRequests += 1;
      if (overviewRequests === 1) {
        await route.fulfill({
          status: 503,
          contentType: "application/json",
          body: JSON.stringify({ message: "Temporarily unavailable" }),
        });
        return;
      }

      await route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          pendingApprovals: 0,
          monthlySpend: 0,
          recentOrders: 0,
          totalVendors: 0,
          totalOrders: 0,
          totalSpend: 0,
          monthlySpendTrend: [],
          statusBreakdown: [],
          topVendors: [],
          recentOrdersList: [],
        }),
      });
      return;
    }

    await route.fulfill({
      status: 200,
      contentType: "application/json",
      body: "[]",
    });
  });

  return () => overviewRequests;
}

async function mockNotificationOverviewFailureThenSuccess(page: Page) {
  let overviewRequests = 0;
  const mainOrigin = new URL(MAIN_APP_URL).origin;
  await page.context().addCookies([
    {
      name: "Application-SessionToken",
      value: "loader-symbol-session",
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
    if (url.includes("/AccessControl/GetCurrentAccessProfile")) {
      await route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          userId: mockUser.userId,
          roleCodes: mockUser.roles,
          roleNames: mockUser.roleNames,
          accessFunctionCodes: mockUser.permissions,
        }),
      });
      return;
    }

    if (url.includes("/NotificationAdministration/GetOverview")) {
      overviewRequests += 1;
      if (overviewRequests === 1) {
        await route.fulfill({
          status: 503,
          contentType: "application/json",
          body: JSON.stringify({ message: "Temporarily unavailable" }),
        });
        return;
      }

      await route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          policies: [
            {
              id: "0198fc41-bdf2-7a85-8475-7a2412147ebc",
              eventKey: "procurement.purchase-order.submitted",
              displayName: "Purchase order submitted",
              description: "Transactional update",
              category: "Order updates",
              inAppEnabled: true,
              emailEnabled: true,
              pushEnabled: false,
              isActive: true,
              supportsReminderConfiguration: false,
              reminderAfterHours: null,
              escalationAfterHours: null,
            },
          ],
          templates: [],
          recentDeliveries: [],
          deliveryStatusCounts: {},
          channelHealth: {
            emailConfigured: true,
            pushNotificationsConfigured: false,
            realtimeConfigured: true,
          },
          allowedPlaceholders: [],
        }),
      });
      return;
    }

    if (url.includes("/NotificationAdministration/SearchDeliveries")) {
      await route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          items: [],
          totalCount: 0,
          page: 1,
          pageSize: 20,
          totalPages: 0,
          hasPreviousPage: false,
          hasNextPage: false,
        }),
      });
      return;
    }

    await route.fulfill({
      status: 200,
      contentType: "application/json",
      body: "[]",
    });
  });

  return () => overviewRequests;
}

test.describe("staff loading symbol", () => {
  test("shows the shared NIE loader symbol while data tables load on desktop", async ({
    page,
  }) => {
    const releaseVendors = await mockStaffShellWithDelayedVendors(page);
    await page.setViewportSize({ width: 1280, height: 720 });
    await page.goto(`${MAIN_APP_URL}#/vendors`);

    await expect(page.getByTestId("nie-loader-symbol")).toBeVisible();

    releaseVendors();
    await expect(page.getByTestId("nie-loader-symbol")).toBeHidden();
  });

  test("shows the shared NIE loader symbol while data tables load on mobile", async ({
    page,
  }) => {
    const releaseVendors = await mockStaffShellWithDelayedVendors(page);
    await page.setViewportSize({ width: 390, height: 844 });
    await page.goto(`${MAIN_APP_URL}#/vendors`);

    await expect(page.getByTestId("nie-loader-symbol")).toBeVisible();

    releaseVendors();
    await expect(page.getByTestId("nie-loader-symbol")).toBeHidden();
  });

  test("keeps a failed screen state visible and retries it successfully", async ({
    page,
  }) => {
    const overviewRequests = await mockDashboardFailureThenSuccess(page);
    await page.setViewportSize({ width: 1280, height: 720 });
    await page.goto(`${MAIN_APP_URL}#/`);

    const errorState = page.getByTestId("nie-result-state");
    await expect.poll(overviewRequests).toBe(1);
    await expect(
      errorState.getByRole("heading", { name: "Unable to load dashboard" }),
    ).toBeVisible();
    await expect(errorState).toContainText("Dashboard data could not be loaded.");
    expect(overviewRequests()).toBe(1);

    await errorState.getByRole("button", { name: "Try again" }).click();
    await expect(errorState).toHaveCount(0);
    await expect(page.getByText("Monthly Spend", { exact: true })).toBeVisible();
    expect(overviewRequests()).toBe(2);
  });

  test("keeps notification configuration failure visible until retry succeeds", async ({
    page,
  }) => {
    const overviewRequests =
      await mockNotificationOverviewFailureThenSuccess(page);
    await page.setViewportSize({ width: 1280, height: 720 });
    await page.goto(`${MAIN_APP_URL}#/notification-administration`);

    const errorState = page.getByTestId("nie-result-state");
    await expect.poll(overviewRequests).toBe(1);
    await expect(
      errorState.getByRole("heading", {
        name: "Unable to load notification configuration",
      }),
    ).toBeVisible();
    await expect(errorState).toContainText(
      "Notification configuration could not be loaded. Try again.",
    );

    await errorState
      .getByRole("button", {
        name: "Retry loading notification configuration",
      })
      .click();

    await expect(errorState).toHaveCount(0);
    await expect(page.getByText("Purchase order submitted")).toBeVisible();
    expect(overviewRequests()).toBe(2);
  });
});

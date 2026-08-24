import {
  expect,
  test,
  type APIRequestContext,
  type ConsoleMessage,
  type Page,
} from "@playwright/test";
import { ApiEndpoints, TestConfig } from "../fixtures/test-config";

const MAIN_APP_URL = TestConfig.frontendMain;
const AUTH_APP_URL = TestConfig.frontendAuth;
const MAIN_API_URL = TestConfig.mainApiUrl;
const AUTH_API_URL = TestConfig.authApiUrl;

test.use({ serviceWorkers: "block" });

type AuditSession = {
  success: boolean;
  sessionToken: string;
  userId: string;
  userName: string;
  email: string;
};

type AccessProfile = {
  roleCodes?: string[];
  roleNames?: string[];
  accessFunctionCodes?: string[];
};

type PurchaseOrderSummary = {
  id?: string;
};

type Viewport = {
  name: string;
  width: number;
  height: number;
};

const viewports: Viewport[] = [
  { name: "desktop", width: 1920, height: 1080 },
  { name: "tablet", width: 1024, height: 768 },
  { name: "phone", width: 390, height: 844 },
];

// Route-inventory markers are compared with the Main and Auth route sources by
// designSystemParity.spec.ts. Redirect-only legacy paths map to their canonical screen.
// AUDIT-MAIN-ROUTE: <root> -> #/
// AUDIT-MAIN-ROUTE: vendors -> #/vendors
// AUDIT-MAIN-ROUTE: catalog -> #/catalog
// AUDIT-MAIN-ROUTE: new-purchase-request -> #/new-purchase-request
// AUDIT-MAIN-ROUTE: approvals -> #/approvals
// AUDIT-MAIN-ROUTE: orders -> #/orders
// AUDIT-MAIN-ROUTE: purchase-order/:id -> #/purchase-order/{seeded-order-id}
// AUDIT-MAIN-ROUTE: access-control -> #/access-control?tab=users|roles|access-functions
// AUDIT-MAIN-ROUTE: users -> #/access-control?tab=users (legacy redirect)
// AUDIT-MAIN-ROUTE: role-management -> #/access-control?tab=roles (legacy redirect)
// AUDIT-MAIN-ROUTE: access-functions -> #/access-control?tab=access-functions (legacy redirect)
// AUDIT-MAIN-ROUTE: audit-log -> #/audit-log
// AUDIT-MAIN-ROUTE: notification-administration -> #/notification-administration?tab=policies|email-templates|delivery
// AUDIT-MAIN-ROUTE: push-notifications -> #/notification-administration (legacy redirect)
// AUDIT-MAIN-ROUTE: myinfo -> #/myinfo
// AUDIT-MAIN-ROUTE: reports -> #/reports
// AUDIT-MAIN-ROUTE: reports/:type -> #/reports/po-summary
// AUDIT-MAIN-ROUTE: chat -> #/chat
// AUDIT-MAIN-ROUTE: chat/:source -> #/chat/procurement
// AUDIT-MAIN-ROUTE: unauthorized -> #/unauthorized
// AUDIT-MAIN-ROUTE: forbidden -> #/forbidden
// AUDIT-MAIN-ROUTE: request-timeout -> #/request-timeout
// AUDIT-MAIN-ROUTE: too-many-requests -> #/too-many-requests
// AUDIT-MAIN-ROUTE: server-error -> #/server-error
// AUDIT-MAIN-ROUTE: bad-gateway -> #/bad-gateway
// AUDIT-MAIN-ROUTE: service-unavailable -> #/service-unavailable
// AUDIT-MAIN-ROUTE: :pathMatch(.*)* -> #/ui-consistency-missing-page
// AUDIT-AUTH-ROUTE: / -> #/
// AUDIT-AUTH-ROUTE: /:pathMatch(.*)* -> #/ui-consistency-missing-page
const mainRoutes: ReadonlyArray<readonly [string, string]> = [
  ["dashboard", "#/"],
  ["vendors", "#/vendors"],
  ["catalog", "#/catalog"],
  ["new purchase request", "#/new-purchase-request"],
  ["approvals", "#/approvals"],
  ["orders", "#/orders"],
  ["access-control users", "#/access-control?tab=users"],
  ["access-control roles", "#/access-control?tab=roles"],
  ["access functions", "#/access-control?tab=access-functions"],
  ["audit log", "#/audit-log"],
  ["notification policies", "#/notification-administration?tab=policies"],
  [
    "notification email templates",
    "#/notification-administration?tab=email-templates",
  ],
  ["notification delivery", "#/notification-administration?tab=delivery"],
  ["MyInfo", "#/myinfo"],
  ["reports", "#/reports"],
  ["report detail", "#/reports/po-summary"],
  ["chat", "#/chat"],
  ["chat source", "#/chat/procurement"],
  ["401", "#/unauthorized"],
  ["403", "#/forbidden"],
  ["408", "#/request-timeout"],
  ["429", "#/too-many-requests"],
  ["500", "#/server-error"],
  ["502", "#/bad-gateway"],
  ["503", "#/service-unavailable"],
  ["404", "#/ui-consistency-missing-page"],
];

const routeTabPanels: ReadonlyArray<{
  routeName: string;
  tabName: string;
  panelId: string;
}> = [
  {
    routeName: "access-control users",
    tabName: "Users",
    panelId: "access-control-users-panel",
  },
  {
    routeName: "access-control roles",
    tabName: "Roles",
    panelId: "access-control-roles-panel",
  },
  {
    routeName: "access functions",
    tabName: "Access Functions",
    panelId: "access-control-functions-panel",
  },
  {
    routeName: "notification policies",
    tabName: "Policies",
    panelId: "notification-policies-panel",
  },
  {
    routeName: "notification email templates",
    tabName: "Email templates",
    panelId: "notification-templates-panel",
  },
  {
    routeName: "notification delivery",
    tabName: "Delivery",
    panelId: "notification-delivery-panel",
  },
];

const expectedPageTitles: Readonly<Record<string, string>> = {
  dashboard: "Dashboard",
  vendors: "Vendors",
  catalog: "Catalog Items",
  "new purchase request": "New Purchase Request",
  approvals: "Approvals",
  orders: "Order History",
  "purchase order detail": "Purchase Order",
  "access-control users": "Access Control",
  "access-control roles": "Access Control",
  "access functions": "Access Control",
  "audit log": "Audit Logs",
  "notification policies": "Notifications",
  "notification email templates": "Notifications",
  "notification delivery": "Notifications",
  MyInfo: "MyInfo",
  reports: "Reports",
  "report detail": "Report",
  chat: "AI Chat",
  "chat source": "AI Chat",
  "401": "Sign in required",
  "403": "Access denied",
  "408": "Request timed out",
  "429": "Too many requests",
  "500": "Something went wrong",
  "502": "Upstream service unavailable",
  "503": "Service unavailable",
  "404": "Page not found",
};

function endpoint(baseUrl: string, relativePath: string): string {
  return new URL(relativePath, baseUrl).toString();
}

async function createAuditSession(request: APIRequestContext): Promise<{
  session: AuditSession;
  profile: AccessProfile;
  orderId?: string;
}> {
  const sessionResponse = await request.post(
    endpoint(AUTH_API_URL, ApiEndpoints.auth.createTestSession),
    {
      data: {
        userId: "devia",
        name: "UI Consistency Audit",
        email: "ui.consistency.audit@nie.edu.sg",
        department: "Digital Solutions",
      },
    },
  );
  expect(sessionResponse.ok(), "development audit session should be available").toBe(
    true,
  );
  const session = (await sessionResponse.json()) as AuditSession;
  expect(session.success).toBe(true);
  expect(session.sessionToken).toBeTruthy();

  const headers = { "X-Session-Id": session.sessionToken };
  const profileResponse = await request.get(
    endpoint(MAIN_API_URL, "AccessControl/GetCurrentAccessProfile"),
    { headers },
  );
  expect(profileResponse.ok(), "audit access profile should resolve").toBe(true);
  const profile = (await profileResponse.json()) as AccessProfile;

  const ordersResponse = await request.get(
    endpoint(MAIN_API_URL, "PurchaseOrder/GetAll"),
    { headers },
  );
  const orders = ordersResponse.ok()
    ? ((await ordersResponse.json()) as PurchaseOrderSummary[])
    : [];

  return { session, profile, orderId: orders[0]?.id };
}

async function installAuditSession(
  page: Page,
  session: AuditSession,
  profile: AccessProfile,
): Promise<void> {
  const mainOrigin = new URL(MAIN_APP_URL).origin;
  await page.context().addCookies([
    {
      name: "Application-SessionToken",
      value: session.sessionToken,
      url: mainOrigin,
    },
    {
      name: "Application-User",
      value: JSON.stringify({
        userId: session.userId,
        fullName: session.userName,
        email: session.email,
        department: "Digital Solutions",
        roles: profile.roleCodes ?? [],
        roleNames: profile.roleNames ?? [],
        permissions: profile.accessFunctionCodes ?? [],
      }),
      url: mainOrigin,
    },
  ]);
}

async function auditRenderedRoute(
  page: Page,
  label: string,
  url: string,
  expectedPageTitle?: string,
  scopeTitleToStaffHeader = false,
  expectedTabState?: { tabName: string; panelId: string },
): Promise<string[]> {
  const consoleErrors: string[] = [];
  const failedApiRequests: string[] = [];
  const onConsole = (message: ConsoleMessage) => {
    if (message.type() === "error") {
      consoleErrors.push(message.text());
    }
  };
  const onPageError = (error: Error) => consoleErrors.push(error.message);
  const onResponse = (response: {
    status(): number;
    url(): string;
  }) => {
    if (response.status() >= 400 && response.url().includes("/api")) {
      failedApiRequests.push(`${response.status()} ${response.url()}`);
    }
  };
  page.on("console", onConsole);
  page.on("pageerror", onPageError);
  page.on("response", onResponse);

  await page.goto(url, { waitUntil: "domcontentloaded" });
  if (expectedPageTitle) {
    const titleScope = scopeTitleToStaffHeader
      ? page.getByRole("banner", { name: "Staff portal header" })
      : page;
    await expect(
      titleScope.getByRole("heading", { name: expectedPageTitle, exact: true }),
    ).toBeVisible();
  }
  if (expectedTabState) {
    const allTabs = page.locator('[role="tab"]');
    const tab = page.locator(
      `[role="tab"][aria-controls="${expectedTabState.panelId}"]`,
    );
    await expect(tab).toContainText(expectedTabState.tabName);
    await expect(tab).toHaveAttribute("aria-selected", "true");
    await expect(page.locator(`#${expectedTabState.panelId}`)).toBeVisible();

    const currentIndex = await allTabs.evaluateAll(
      (tabs, panelId) =>
        tabs.findIndex((candidate) => candidate.getAttribute("aria-controls") === panelId),
      expectedTabState.panelId,
    );
    expect(currentIndex).toBeGreaterThanOrEqual(0);
    const adjacentTab = allTabs.nth((currentIndex + 1) % (await allTabs.count()));
    await tab.focus();
    await page.keyboard.press("ArrowRight");
    await expect(adjacentTab).toBeFocused();
    await page.keyboard.press("Enter");
    await expect(adjacentTab).toHaveAttribute("aria-selected", "true");
    await tab.click();
    await expect(tab).toHaveAttribute("aria-selected", "true");
    await expect(page.locator(`#${expectedTabState.panelId}`)).toBeVisible();
  }
  const isEmailTemplatePanel =
    expectedTabState?.panelId === "notification-templates-panel";
  if (isEmailTemplatePanel) {
    const preview = page.getByTitle("Email content preview");
    await expect(preview).toBeVisible();
    await expect(preview).toHaveAttribute("sandbox", "allow-same-origin");

    await page.getByRole("button", { name: "Hide preview" }).click();
    await expect(preview).toHaveCount(0);
    await page.getByRole("button", { name: "Show preview" }).click();
    await expect(page.getByTitle("Email content preview")).toBeVisible();
  }
  if (label === "phone/dashboard") {
    const menuButton = page.getByRole("button", {
      name: "Open navigation menu",
    });
    await menuButton.click();
    const mobileNavigation = page.getByRole("dialog", {
      name: "Mobile navigation",
    });
    await expect(mobileNavigation).toBeVisible();
    await expect(
      mobileNavigation.getByRole("button", { name: "Close navigation menu" }),
    ).toBeFocused();
    await page.keyboard.press("Shift+Tab");
    expect(
      await mobileNavigation.evaluate((element) =>
        element.contains(document.activeElement),
      ),
    ).toBe(true);
    await page.keyboard.press("Escape");
    await expect(mobileNavigation).toHaveCount(0);
    await expect(menuButton).toBeFocused();
  }
  if (!scopeTitleToStaffHeader && (await page.locator(".login-page").count())) {
    await expect
      .poll(
        () =>
          page
            .locator(".login-page")
            .first()
            .evaluate((element) => getComputedStyle(element).opacity),
        { timeout: 2_000 },
      )
      .toBe("1");
  }
  if (
    !scopeTitleToStaffHeader &&
    (await page.locator(".fade-in-down:visible").count())
  ) {
    await expect
      .poll(
        () =>
          page
            .locator(".fade-in-down:visible")
            .first()
            .evaluate((element) => getComputedStyle(element).opacity),
        { timeout: 2_000 },
      )
      .toBe("1");
  }
  await expect
    .poll(async () => (await page.locator("body").innerText()).trim(), {
      timeout: 10_000,
      message: `${label} should render application content`,
    })
    .not.toBe("");
  await page.evaluate(async () => {
    await document.fonts.ready;
    await new Promise<void>((resolve) => requestAnimationFrame(() => resolve()));
  });
  await expect(page.locator('[data-testid="nie-loader-symbol"]:visible')).toHaveCount(
    0,
    { timeout: 10_000 },
  );
  await page.evaluate(
    () =>
      new Promise<void>((resolve) =>
        requestAnimationFrame(() => requestAnimationFrame(() => resolve())),
      ),
  );

  const metrics = await page.evaluate(() => {
    const visible = (element: Element): boolean => {
      const style = getComputedStyle(element);
      const rect = element.getBoundingClientRect();
      const isDormantSkipLink =
        element.classList.contains("sr-only") && document.activeElement !== element;
      return (
        !isDormantSkipLink &&
        style.display !== "none" &&
        style.visibility !== "hidden" &&
        rect.width > 0 &&
        rect.height > 0
      );
    };

    const tinyTargets = [
      ...document.querySelectorAll(
        "button, a[href], input, select, textarea, [role='button'], [role='tab']",
      ),
    ]
      .filter(visible)
      .map((element) => {
        const measuredElement =
          element instanceof HTMLInputElement
            ? (element.closest("label") ?? element)
            : element;
        const rect = measuredElement.getBoundingClientRect();
        return {
          label:
            element.getAttribute("aria-label") ??
            element.textContent?.trim().slice(0, 60) ??
            element.tagName.toLowerCase(),
          width: Math.round(rect.width),
          height: Math.round(rect.height),
        };
      })
      .filter((target) => target.width < 24 || target.height < 24);

    const clippedHeadings = [...document.querySelectorAll("h1, h2, h3")]
      .filter(visible)
      .filter((element) => {
        const style = getComputedStyle(element);
        return (
          (/(hidden|clip)/.test(style.overflowX) &&
            element.scrollWidth > element.clientWidth + 1) ||
          (/(hidden|clip)/.test(style.overflowY) &&
            element.scrollHeight > element.clientHeight + 1)
        );
      })
      .map((element) => element.textContent?.trim().slice(0, 80));

    const semanticTextSelector = [
      "h1",
      "h2",
      "h3",
      "h4",
      "p",
      "label",
      "button",
      "a[href]",
      "th",
      "td",
      "li",
      "span",
      "[role='tab']",
    ].join(",");
    const allowedFontSizes = new Set([11, 12, 13, 14, 16, 18, 20, 24]);
    const allowedFontWeights = new Set([400, 500, 600, 700]);
    const semanticText = [...document.querySelectorAll(semanticTextSelector)]
      .filter(visible)
      .filter(
        (element) =>
          !element.closest(
            ".material-symbols-outlined, .font-mono, svg, code, pre, kbd, samp, [aria-hidden='true']",
          ) &&
          !element.closest(":disabled, [aria-disabled='true']") &&
          Number.parseFloat(getComputedStyle(element).opacity) >= 0.75 &&
          Boolean(element.textContent?.trim()),
      );
    const typographyViolations = semanticText
      .map((element) => {
        const style = getComputedStyle(element);
        const size = Number.parseFloat(style.fontSize);
        const weight = Number.parseInt(style.fontWeight, 10);
        const lineHeight =
          style.lineHeight === "normal"
            ? 1.2
            : Number.parseFloat(style.lineHeight) / Math.max(size, 1);
        const validSize =
          allowedFontSizes.has(Math.round(size * 10) / 10) ||
          (size >= 28 && size <= 36) ||
          (size >= 60 && size <= 72);
        const validFamily = style.fontFamily.includes("Plus Jakarta Sans");
        if (
          validFamily &&
          validSize &&
          allowedFontWeights.has(weight) &&
          lineHeight >= 0.95 &&
          lineHeight <= 2.2
        ) {
          return null;
        }
        return {
          text: element.textContent?.trim().slice(0, 48),
          family: style.fontFamily,
          size,
          weight,
          lineHeight: Number(lineHeight.toFixed(2)),
        };
      })
      .filter(Boolean)
      .slice(0, 25);

    const parseColour = (value: string): [number, number, number, number] | null => {
      const srgb = value.match(
        /color\(srgb\s+([\d.]+)\s+([\d.]+)\s+([\d.]+)(?:\s*\/\s*([\d.]+))?\)/,
      );
      if (srgb) {
        return [
          Number(srgb[1]) * 255,
          Number(srgb[2]) * 255,
          Number(srgb[3]) * 255,
          srgb[4] === undefined ? 1 : Number(srgb[4]),
        ];
      }

      const rgb = value.match(
        /rgba?\(\s*([\d.]+)[,\s]+([\d.]+)[,\s]+([\d.]+)(?:\s*[,/]\s*([\d.]+))?\s*\)/,
      );
      if (!rgb) return null;
      return [
        Number(rgb[1]),
        Number(rgb[2]),
        Number(rgb[3]),
        rgb[4] === undefined ? 1 : Number(rgb[4]),
      ];
    };
    const opaqueBackground = (element: Element): [number, number, number, number] | null => {
      let current: Element | null = element;
      while (current) {
        const style = getComputedStyle(current);
        if (
          Number.parseFloat(style.opacity) < 0.99 ||
          style.backgroundImage !== "none"
        ) {
          return null;
        }
        const colour = parseColour(style.backgroundColor);
        if (colour && colour[3] >= 0.99) return colour;
        current = current.parentElement;
      }
      return null;
    };
    const luminance = ([red, green, blue]: [number, number, number, number]) => {
      const channel = (value: number) => {
        const normalized = value / 255;
        return normalized <= 0.03928
          ? normalized / 12.92
          : ((normalized + 0.055) / 1.055) ** 2.4;
      };
      return 0.2126 * channel(red) + 0.7152 * channel(green) + 0.0722 * channel(blue);
    };
    const contrastViolations = semanticText
      .filter((element) =>
        [...element.childNodes].some(
          (node) => node.nodeType === Node.TEXT_NODE && Boolean(node.textContent?.trim()),
        ),
      )
      .map((element) => {
        const style = getComputedStyle(element);
        const foreground = parseColour(style.color);
        const background = opaqueBackground(element);
        if (!foreground || !background || foreground[3] < 0.99) return null;
        const lighter = Math.max(luminance(foreground), luminance(background));
        const darker = Math.min(luminance(foreground), luminance(background));
        const ratio = (lighter + 0.05) / (darker + 0.05);
        const size = Number.parseFloat(style.fontSize);
        const weight = Number.parseInt(style.fontWeight, 10);
        const threshold = size >= 24 || (size >= 18.66 && weight >= 700) ? 3 : 4.5;
        return ratio + 0.01 < threshold
          ? {
              text: element.textContent?.trim().slice(0, 48),
              ratio: Number(ratio.toFixed(2)),
              threshold,
            }
          : null;
      })
      .filter(Boolean)
      .slice(0, 25);

    const controlGeometryViolations = [
      ...document.querySelectorAll<HTMLElement>("[data-nie-control]"),
    ]
      .filter(visible)
      .map((element) => {
        const rect = element.getBoundingClientRect();
        const radius = Number.parseFloat(getComputedStyle(element).borderRadius);
        const validHeight =
          element.dataset.nieControl === "textarea"
            ? rect.height >= 96
            : [40, 44, 48].some(
                (height) => Math.abs(rect.height - height) <= 1,
              );
        return validHeight && radius >= 8 && radius <= 16
          ? null
          : {
              control: element.dataset.nieControl,
              text: element.textContent?.trim().slice(0, 40),
              height: Number(rect.height.toFixed(1)),
              radius,
            };
      })
      .filter(Boolean)
      .slice(0, 25);

    const shellContent = document.querySelector<HTMLElement>("#main-content > div");
    const shellPadding = shellContent
      ? Number.parseFloat(getComputedStyle(shellContent).paddingLeft)
      : null;
    const expectedShellPadding = window.innerWidth < 768 ? 16 : 32;
    const shellPaddingMismatch =
      shellPadding === null || Math.abs(shellPadding - expectedShellPadding) <= 1
        ? null
        : { shellPadding, expectedShellPadding };

    const title = document.querySelector<HTMLElement>(".staff-page-title");
    const actions = document.querySelector<HTMLElement>(".staff-page-feedback");
    const headerOverlap = (() => {
      if (!title || !actions || !visible(title) || !visible(actions)) return false;
      const first = title.getBoundingClientRect();
      const second = actions.getBoundingClientRect();
      return !(
        first.right <= second.left ||
        second.right <= first.left ||
        first.bottom <= second.top ||
        second.bottom <= first.top
      );
    })();

    const clippedControls = [
      ...document.querySelectorAll<HTMLElement>(
        "button, a[href], [role='button'], [role='tab']",
      ),
    ]
      .filter(visible)
      .filter((element) => {
        if (element.matches("[role='switch']")) return false;
        return [...element.childNodes].some(
          (node) => node.nodeType === Node.TEXT_NODE && Boolean(node.textContent?.trim()),
        );
      })
      .filter(
        (element) => {
          const style = getComputedStyle(element);
          return (
            (/(hidden|clip)/.test(style.overflowX) &&
              element.scrollWidth > element.clientWidth + 1) ||
            (/(hidden|clip)/.test(style.overflowY) &&
              element.scrollHeight > element.clientHeight + 1)
          );
        },
      )
      .map(
        (element) =>
          element.getAttribute("aria-label") ??
          element.textContent?.trim().slice(0, 48) ??
          element.tagName,
      )
      .slice(0, 25);

    const bodyStyle = getComputedStyle(document.body);
    return {
      bodyFont: bodyStyle.fontFamily,
      bodyText: document.body.innerText.trim(),
      documentOverflow:
        document.documentElement.scrollWidth >
        document.documentElement.clientWidth + 1,
      scrollWidth: document.documentElement.scrollWidth,
      clientWidth: document.documentElement.clientWidth,
      tinyTargets,
      clippedHeadings,
      typographyViolations,
      contrastViolations,
      controlGeometryViolations,
      shellPaddingMismatch,
      headerOverlap,
      clippedControls,
    };
  });

  page.off("console", onConsole);
  page.off("pageerror", onPageError);
  page.off("response", onResponse);

  const violations: string[] = [];
  if (!metrics.bodyFont.includes("Plus Jakarta Sans")) {
    violations.push(`${label}: unexpected body font ${metrics.bodyFont}`);
  }
  if (!metrics.bodyText) violations.push(`${label}: rendered an empty document`);
  if (metrics.documentOverflow) {
    violations.push(
      `${label}: document overflow ${metrics.scrollWidth}px > ${metrics.clientWidth}px`,
    );
  }
  if (metrics.tinyTargets.length > 0) {
    violations.push(`${label}: undersized controls ${JSON.stringify(metrics.tinyTargets)}`);
  }
  if (metrics.clippedHeadings.length > 0) {
    violations.push(`${label}: clipped headings ${metrics.clippedHeadings.join(", ")}`);
  }
  if (metrics.typographyViolations.length > 0) {
    violations.push(
      `${label}: typography scale ${JSON.stringify(metrics.typographyViolations)}`,
    );
  }
  if (metrics.contrastViolations.length > 0) {
    violations.push(
      `${label}: text contrast ${JSON.stringify(metrics.contrastViolations)}`,
    );
  }
  if (metrics.controlGeometryViolations.length > 0) {
    violations.push(
      `${label}: shared control geometry ${JSON.stringify(metrics.controlGeometryViolations)}`,
    );
  }
  if (metrics.shellPaddingMismatch) {
    violations.push(
      `${label}: shell padding ${JSON.stringify(metrics.shellPaddingMismatch)}`,
    );
  }
  if (metrics.headerOverlap) violations.push(`${label}: header title/action overlap`);
  if (metrics.clippedControls.length > 0) {
    violations.push(
      `${label}: clipped interactive content ${metrics.clippedControls.join(", ")}`,
    );
  }
  if (consoleErrors.length > 0) {
    violations.push(`${label}: console errors ${consoleErrors.join(" | ")}`);
  }
  if (failedApiRequests.length > 0) {
    violations.push(`${label}: failed API requests ${failedApiRequests.join(" | ")}`);
  }
  return violations;
}

test.describe("cross-route UI/UX consistency", () => {
  test.describe.configure({ mode: "serial" });

  test("persists a first table preference without entering repair mode", async ({
    page,
    request,
  }, testInfo) => {
    test.skip(
      testInfo.project.name !== "e2e-chromium",
      "This persistence contract runs once in desktop Chromium.",
    );
    test.skip(
      (process.env.TEST_ENV ?? "dev") !== "dev",
      "The first-save contract uses the development-only CreateTestSession fixture.",
    );

    const { session, profile } = await createAuditSession(request);
    const tableKey = `test.first-save-${Date.now()}`;
    const preferenceUrl = (action: "Get" | "Upsert" | "Delete") =>
      endpoint(
        MAIN_API_URL,
        `UserDataTablePreference/${action}?tableKey=${encodeURIComponent(tableKey)}`,
      );

    await page.route("**/api/UserDataTablePreference/{Get,Upsert}", async (route) => {
      const url = new URL(route.request().url());
      url.searchParams.set("tableKey", tableKey);
      await route.continue({ url: url.toString() });
    });

    try {
      await installAuditSession(page, session, profile);
      await page.goto(new URL("#/vendors", MAIN_APP_URL).toString(), {
        waitUntil: "networkidle",
      });

      await page
        .getByRole("button", { name: "Configure table preferences" })
        .click();
      await page.getByRole("button", { name: "Display", exact: true }).click();
      await page.getByLabel("Density").selectOption("compact");

      const upsertResponse = page.waitForResponse(
        (response) =>
          response.url().includes("/api/UserDataTablePreference/Upsert") &&
          response.request().method() === "PUT",
      );
      await page.getByRole("button", { name: "Save as my default" }).click();
      expect((await upsertResponse).status()).toBe(200);
      await expect(
        page.getByRole("dialog", { name: "Configure table preferences" }),
      ).toHaveCount(0);

      await page
        .getByRole("button", { name: "Configure table preferences" })
        .click();
      const dialog = page.getByRole("dialog", {
        name: "Configure table preferences",
      });
      await expect(dialog.getByText("Your saved view needs repair")).toHaveCount(0);
      await expect(
        dialog.getByRole("button", { name: "Save as my default" }),
      ).toBeVisible();
    } finally {
      await request.delete(preferenceUrl("Delete"), {
        headers: { "X-Session-Id": session.sessionToken },
      });
    }
  });

  test("keeps table preference controls usable at phone, tablet, and desktop widths", async ({
    page,
    request,
  }, testInfo) => {
    test.skip(
      testInfo.project.name !== "e2e-chromium",
      "This contract owns its three explicit viewport sizes.",
    );
    test.skip(
      (process.env.TEST_ENV ?? "dev") !== "dev",
      "The responsive preferences contract uses the development-only session fixture.",
    );

    const { session, profile } = await createAuditSession(request);
    const tableKey = `test.preference-layout-${Date.now()}`;
    await page.route("**/api/UserDataTablePreference/Get**", async (route) => {
      const url = new URL(route.request().url());
      url.searchParams.set("tableKey", tableKey);
      await route.continue({ url: url.toString() });
    });
    await installAuditSession(page, session, profile);

    for (const viewport of [
      { name: "phone", width: 390, height: 844 },
      { name: "tablet", width: 768, height: 1024 },
      { name: "desktop", width: 1440, height: 900 },
    ]) {
      await page.setViewportSize(viewport);
      await page.goto(new URL("#/vendors", MAIN_APP_URL).toString(), {
        waitUntil: "networkidle",
      });
      await page
        .getByRole("button", { name: "Configure table preferences" })
        .click();
      const dialog = page.getByRole("dialog", {
        name: "Configure table preferences",
      });
      await dialog.getByRole("button", { name: "Sorting", exact: true }).click();
      await dialog.getByRole("button", { name: "Add sort" }).click();

      const geometry = await dialog.locator("[data-preference-sort-row]").evaluate(
        (row) => {
          const dialogElement = row.closest<HTMLElement>('[role="dialog"]');
          const controls = row.querySelector<HTMLElement>(
            "[data-preference-sort-controls]",
          );
          const elements = controls
            ? [...controls.querySelectorAll<HTMLElement>("select, button")]
            : [];
          const dialogRect = dialogElement?.getBoundingClientRect();
          const controlRects = elements.map((element) => element.getBoundingClientRect());
          return {
            documentOverflow:
              document.documentElement.scrollWidth >
              document.documentElement.clientWidth + 1,
            dialogWithinViewport: Boolean(
              dialogRect &&
                dialogRect.left >= -1 &&
                dialogRect.right <= window.innerWidth + 1 &&
                dialogRect.top >= -1 &&
                dialogRect.bottom <= window.innerHeight + 1,
            ),
            widths: controlRects.map((rect) => rect.width),
            insideRow: controlRects.every((rect) => {
              const rowRect = row.getBoundingClientRect();
              return rect.left >= rowRect.left - 1 && rect.right <= rowRect.right + 1;
            }),
            noOverlap: controlRects.every((rect, index) =>
              controlRects.slice(index + 1).every(
                (other) =>
                  rect.right <= other.left + 1 ||
                  other.right <= rect.left + 1 ||
                  rect.bottom <= other.top + 1 ||
                  other.bottom <= rect.top + 1,
              ),
            ),
          };
        },
      );

      expect(geometry.documentOverflow).toBe(false);
      expect(geometry.dialogWithinViewport).toBe(true);
      expect(geometry.insideRow).toBe(true);
      expect(geometry.noOverlap).toBe(true);
      expect(geometry.widths).toHaveLength(3);
      expect(Math.min(...geometry.widths)).toBeGreaterThanOrEqual(44);

      await dialog.getByRole("button", { name: "Default filters" }).click();
      const filterColumns = dialog.getByRole("combobox", {
        name: "Default filter column",
      });
      await expect(filterColumns.locator("option")).toHaveCount(8);
      await filterColumns.selectOption("category");
      await expect(dialog.getByText("Category values", { exact: true })).toBeVisible();

      const saveButton = dialog.getByRole("button", { name: "Save as my default" });
      await expect(saveButton).toBeVisible();
      const saveButtonBounds = await saveButton.boundingBox();
      expect(saveButtonBounds).not.toBeNull();
      expect(saveButtonBounds!.height).toBeGreaterThanOrEqual(44);
      expect(saveButtonBounds!.y).toBeGreaterThanOrEqual(0);
      expect(saveButtonBounds!.y + saveButtonBounds!.height).toBeLessThanOrEqual(
        viewport.height + 1,
      );

      const screenshotPath = testInfo.outputPath(
        `data-table-preferences-${viewport.name}.png`,
      );
      await page.screenshot({ path: screenshotPath, fullPage: true });
      await testInfo.attach(`data-table-preferences-${viewport.name}`, {
        path: screenshotPath,
        contentType: "image/png",
      });
      await dialog.getByRole("button", { name: "Cancel" }).click();
    }
  });

  test("warns weekly about saved filters and keeps them only after acknowledgement", async ({
    page,
    request,
  }, testInfo) => {
    test.skip(
      testInfo.project.name !== "e2e-chromium",
      "This reminder contract runs once in Chromium.",
    );
    test.skip(
      (process.env.TEST_ENV ?? "dev") !== "dev",
      "The authenticated reminder contract uses the development-only session fixture.",
    );

    const { session, profile } = await createAuditSession(request);
    const preference = {
      tableKey: "procurement.vendors",
      definitionVersion: 1,
      revision: 5,
      settings: {
        pageSize: 20,
        sorts: [{ key: "name", direction: "asc" }],
        filters: [{ key: "category", values: ["Consulting"] }],
        filterReminderAcknowledgedAtUtc: "2026-07-01T00:00:00.000Z",
        columnOrder: [
          "code",
          "name",
          "contactPerson",
          "email",
          "phone",
          "category",
          "isActive",
          "catalogItemCount",
        ],
        hiddenColumns: [],
        density: "comfortable",
        appearance: "elevated",
      },
      repairRequired: false,
      repairReasons: [],
    };
    let savedRequest: Record<string, unknown> | null = null;

    await page.route("**/api/UserDataTablePreference/Get**", async (route) => {
      await route.fulfill({ status: 200, contentType: "application/json", body: JSON.stringify(preference) });
    });
    await page.route("**/api/UserDataTablePreference/Upsert**", async (route) => {
      savedRequest = route.request().postDataJSON() as Record<string, unknown>;
      const requestSettings = savedRequest.settings as Record<string, unknown>;
      await route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          ...preference,
          revision: 6,
          settings: {
            ...requestSettings,
            filterReminderAcknowledgedAtUtc: "2026-08-07T04:00:00.000Z",
          },
        }),
      });
    });

    await installAuditSession(page, session, profile);
    await page.setViewportSize({ width: 390, height: 844 });
    await page.goto(new URL("#/vendors", MAIN_APP_URL).toString(), {
      waitUntil: "networkidle",
    });

    const reminder = page.getByRole("dialog", {
      name: "Review saved table filters",
    });
    await expect(reminder.getByRole("heading", { name: "Saved filters are active" })).toBeVisible();
    await expect(reminder.getByText("Some records may be hidden", { exact: false })).toBeVisible();
    await expect(reminder.getByText("Category (1)", { exact: true })).toBeVisible();

    const geometry = await reminder.evaluate((dialog) => {
      const rect = dialog.getBoundingClientRect();
      return {
        insideViewport:
          rect.left >= -1 &&
          rect.right <= window.innerWidth + 1 &&
          rect.top >= -1 &&
          rect.bottom <= window.innerHeight + 1,
        documentOverflow:
          document.documentElement.scrollWidth >
          document.documentElement.clientWidth + 1,
      };
    });
    expect(geometry.insideViewport).toBe(true);
    expect(geometry.documentOverflow).toBe(false);

    const screenshotPath = testInfo.outputPath("weekly-saved-filter-reminder-phone.png");
    await page.screenshot({ path: screenshotPath, fullPage: true });
    await testInfo.attach("weekly-saved-filter-reminder-phone", {
      path: screenshotPath,
      contentType: "image/png",
    });

    await reminder
      .getByRole("button", { name: "Keep saved filters for another week" })
      .click();
    await expect(reminder).toHaveCount(0);
    expect(savedRequest).toMatchObject({
      definitionVersion: 1,
      revision: 5,
      settings: {
        filters: [{ key: "category", values: ["Consulting"] }],
      },
    });
  });

  test("keeps the mobile data-table footer compact and clear of the fixed search toolbar", async ({
    page,
    request,
  }, testInfo) => {
    test.skip(
      testInfo.project.name !== "e2e-chromium",
      "This contract owns its explicit 390-pixel viewport.",
    );
    test.skip(
      (process.env.TEST_ENV ?? "dev") !== "dev",
      "The authenticated table contract uses the development-only CreateTestSession fixture.",
    );

    const { session, profile } = await createAuditSession(request);
    await installAuditSession(page, session, profile);
    await page.setViewportSize({ width: 390, height: 844 });
    await page.goto(new URL("#/vendors", MAIN_APP_URL).toString(), {
      waitUntil: "networkidle",
    });

    await expect(page.getByRole("heading", { name: "Vendors", exact: true })).toBeVisible();
    await expect(page.locator("[data-table-total-results]")).toHaveCount(1);
    await expect(page.locator("[data-pagination-summary]")).toHaveCount(0);
    await expect(page.getByRole("combobox", { name: "Rows per page" })).toBeVisible();

    const pagination = page.getByRole("navigation", { name: "Table pagination" });
    await expect(pagination.getByRole("button")).toHaveCount(4);
    await expect(pagination.getByRole("button", { name: "First page" })).toBeVisible();
    await expect(pagination.getByRole("button", { name: "Previous page" })).toBeVisible();
    await expect(pagination.getByRole("button", { name: "Next page" })).toBeVisible();
    await expect(pagination.getByRole("button", { name: "Last page" })).toBeVisible();
    await expect(pagination.locator("[data-pagination-current-page]")).toHaveCount(1);

    const layout = await page.evaluate(() => {
      const footer = document.querySelector<HTMLElement>(
        "[data-table-pagination-footer]",
      );
      const mobileToolbar = document.querySelector<HTMLElement>(
        ".nie-list-mobile-toolbar",
      );
      if (!footer || !mobileToolbar) return null;

      const footerRect = footer.getBoundingClientRect();
      const toolbarRect = mobileToolbar.getBoundingClientRect();
      return {
        documentOverflow:
          document.documentElement.scrollWidth >
          document.documentElement.clientWidth + 1,
        footerHeight: footerRect.height,
        footerBottom: footerRect.bottom,
        toolbarTop: toolbarRect.top,
      };
    });

    expect(layout).not.toBeNull();
    expect(layout?.documentOverflow).toBe(false);
    expect(layout?.footerHeight).toBeLessThanOrEqual(64);
    expect(layout?.footerBottom).toBeLessThanOrEqual(layout?.toolbarTop ?? 0);
  });

  test("keeps every registered screen consistent at desktop, tablet, and phone widths", async ({
    page,
    request,
  }, testInfo) => {
    test.skip(
      testInfo.project.name !== "e2e-chromium",
      "This test owns its explicit desktop, tablet, and phone viewport matrix.",
    );
    test.skip(
      (process.env.TEST_ENV ?? "dev") !== "dev",
      "The authenticated consistency matrix uses the development-only CreateTestSession fixture.",
    );
    test.setTimeout(300_000);

    const { session, profile, orderId } = await createAuditSession(request);
    await installAuditSession(page, session, profile);

    expect(
      orderId,
      "The UI consistency fixture must include a purchase order so its detail screen is never skipped",
    ).toBeTruthy();
    const routes = [
      ...mainRoutes,
      ["purchase order detail", `#/purchase-order/${orderId}`] as const,
    ];
    const violations: string[] = [];

    for (const viewport of viewports) {
      await page.setViewportSize(viewport);
      for (const [routeName, hash] of routes) {
        violations.push(
          ...(await auditRenderedRoute(
            page,
            `${viewport.name}/${routeName}`,
            new URL(hash, MAIN_APP_URL).toString(),
            expectedPageTitles[routeName],
            true,
            routeTabPanels.find((item) => item.routeName === routeName),
          )),
        );
      }
    }

    await page.context().clearCookies();
    for (const viewport of viewports) {
      await page.setViewportSize(viewport);
      for (const [routeName, hash] of [
        ["login", "#/"],
        ["404", "#/ui-consistency-missing-page"],
      ] as const) {
        violations.push(
          ...(await auditRenderedRoute(
            page,
            `${viewport.name}/auth ${routeName}`,
            new URL(hash, AUTH_APP_URL).toString(),
            routeName === "login" ? "Sign In" : "Page not found",
          )),
        );
      }
    }

    expect(violations, violations.join("\n")).toEqual([]);
  });

  test("keeps every registered screen readable in dark mode at every supported width", async ({
    page,
    request,
  }, testInfo) => {
    test.skip(
      testInfo.project.name !== "e2e-chromium",
      "This test owns its explicit desktop, tablet, and phone viewport matrix.",
    );
    test.skip(
      (process.env.TEST_ENV ?? "dev") !== "dev",
      "The authenticated dark-theme check uses the development-only CreateTestSession fixture.",
    );
    test.setTimeout(360_000);

    const mainOrigin = new URL(MAIN_APP_URL).origin;
    const authOrigin = new URL(AUTH_APP_URL).origin;
    await page.addInitScript(
      ({ supportedOrigins }) => {
        if (!supportedOrigins.includes(window.location.origin)) return;
        localStorage.setItem("nie_template_theme_preference", "dark");
        localStorage.setItem("nie_template_theme_mode", "dark");
      },
      { supportedOrigins: [mainOrigin, authOrigin] },
    );

    const { session, profile, orderId } = await createAuditSession(request);
    await installAuditSession(page, session, profile);
    const violations: string[] = [];
    expect(orderId, "Dark-mode audit requires a purchase order fixture").toBeTruthy();
    const darkRoutes = [
      ...mainRoutes,
      ["purchase order detail", `#/purchase-order/${orderId}`] as const,
    ];

    for (const viewport of viewports) {
      await page.setViewportSize(viewport);
      for (const [routeName, hash] of darkRoutes) {
        violations.push(
          ...(await auditRenderedRoute(
            page,
            `dark ${viewport.name}/${routeName}`,
            new URL(hash, MAIN_APP_URL).toString(),
            expectedPageTitles[routeName],
            true,
            routeTabPanels.find((item) => item.routeName === routeName),
          )),
        );
        await expect(page.locator("html")).toHaveClass(/dark/);
      }
    }

    await page.context().clearCookies();
    for (const viewport of viewports) {
      await page.setViewportSize(viewport);
      for (const [routeName, hash] of [
        ["login", "#/"],
        ["404", "#/ui-consistency-missing-page"],
      ] as const) {
        violations.push(
          ...(await auditRenderedRoute(
            page,
            `dark ${viewport.name}/auth ${routeName}`,
            new URL(hash, AUTH_APP_URL).toString(),
            routeName === "login" ? "Sign In" : "Page not found",
          )),
        );
        await expect(page.locator("html")).toHaveClass(/dark/);
      }
    }

    expect(violations, violations.join("\n")).toEqual([]);
  });

  test("keeps primary controls readable across every enabled colour preset", async ({
    page,
    request,
  }, testInfo) => {
    test.skip(
      testInfo.project.name !== "e2e-chromium",
      "The colour-preset contract only needs one browser project.",
    );
    test.skip(
      (process.env.TEST_ENV ?? "dev") !== "dev",
      "The authenticated colour-preset check uses the development-only CreateTestSession fixture.",
    );

    const { session, profile } = await createAuditSession(request);
    await installAuditSession(page, session, profile);
    const vendorsUrl = new URL("#/vendors", MAIN_APP_URL).toString();
    await page.goto(vendorsUrl, { waitUntil: "networkidle" });

    for (const preset of [
      "cobalt",
      "ocean",
      "emerald",
      "rose",
      "amber",
      "violet",
    ]) {
      await page.evaluate((selectedPreset) => {
        localStorage.setItem("nie_template_theme_preference", "light");
        localStorage.setItem("nie_template_theme_preset", selectedPreset);
      }, preset);
      await page.reload({ waitUntil: "networkidle" });

      const primaryAction = page
        .locator('[data-nie-control="button"]')
        .filter({ hasText: "New" })
        .first();
      await expect(primaryAction, preset).toBeVisible();
      const ratio = await primaryAction.evaluate((element) => {
        const parse = (value: string): number[] =>
          value
            .match(/[\d.]+/g)
            ?.slice(0, 3)
            .map(Number) ?? [0, 0, 0];
        const luminance = (rgb: number[]): number => {
          const channels = rgb.map((channel) => {
            const normalized = channel / 255;
            return normalized <= 0.04045
              ? normalized / 12.92
              : ((normalized + 0.055) / 1.055) ** 2.4;
          });
          return (
            0.2126 * (channels[0] ?? 0) +
            0.7152 * (channels[1] ?? 0) +
            0.0722 * (channels[2] ?? 0)
          );
        };
        const style = getComputedStyle(element);
        const foreground = luminance(parse(style.color));
        const background = luminance(parse(style.backgroundColor));
        return (
          (Math.max(foreground, background) + 0.05) /
          (Math.min(foreground, background) + 0.05)
        );
      });

      expect(ratio, preset).toBeGreaterThanOrEqual(4.5);
    }
  });

  test("honours reduced-motion preferences on the animated Auth screen", async ({
    page,
  }, testInfo) => {
    test.skip(
      testInfo.project.name !== "e2e-chromium",
      "The reduced-motion contract only needs one browser project.",
    );
    await page.emulateMedia({ reducedMotion: "reduce" });
    await page.goto(new URL("#/", AUTH_APP_URL).toString());
    await expect(page.locator(".login-page")).toBeVisible();

    const motion = await page
      .locator(
        ".login-page, .fade-in-down, .fade-in-up, .gradient-text, .orb, .logo-3d",
      )
      .evaluateAll((elements) =>
        elements.map((element) => {
          const style = getComputedStyle(element);
          return {
            className: element.className,
            animationName: style.animationName,
            transitionDuration: style.transitionDuration,
          };
        }),
      );

    expect(motion.length).toBeGreaterThan(0);
    expect(
      motion.filter(
        (item) =>
          item.animationName !== "none" ||
          Number.parseFloat(item.transitionDuration) > 0.00001,
      ),
    ).toEqual([]);
  });

  test("honours reduced-motion preferences on the animated Main screen", async ({
    page,
    request,
  }, testInfo) => {
    test.skip(
      testInfo.project.name !== "e2e-chromium",
      "The reduced-motion contract only needs one browser project.",
    );
    test.skip(
      (process.env.TEST_ENV ?? "dev") !== "dev",
      "The authenticated reduced-motion check uses the development-only CreateTestSession fixture.",
    );
    const { session, profile } = await createAuditSession(request);
    await installAuditSession(page, session, profile);
    await page.emulateMedia({ reducedMotion: "reduce" });
    await page.goto(new URL("#/chat", MAIN_APP_URL).toString());
    await expect(page.locator(".welcome-card").first()).toBeVisible();

    const motion = await page.locator(".welcome-card").evaluateAll((elements) =>
      elements.map((element) => {
        const style = getComputedStyle(element);
        return {
          animationDuration: style.animationDuration,
          animationIterationCount: style.animationIterationCount,
          transitionDuration: style.transitionDuration,
        };
      }),
    );

    expect(motion.length).toBeGreaterThan(0);
    expect(
      motion.filter(
        (item) =>
          Number.parseFloat(item.animationDuration) > 0.00001 ||
          item.animationIterationCount !== "1" ||
          Number.parseFloat(item.transitionDuration) > 0.00001,
      ),
    ).toEqual([]);
  });
});

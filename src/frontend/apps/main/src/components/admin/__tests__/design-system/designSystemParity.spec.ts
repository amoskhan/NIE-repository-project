import { existsSync, readFileSync, readdirSync } from "node:fs";
import { resolve } from "node:path";
import { describe, expect, it } from "vitest";
import { themePresets } from "@nie/ui";

const mainRoot = resolve(process.cwd(), "src");
const mainAppRoot = resolve(process.cwd());
const authAppRoot = resolve(process.cwd(), "../auth");
const sharedUiRoot = resolve(process.cwd(), "../../packages/ui/src");
const sharedUiPackageRoot = resolve(sharedUiRoot, "..");

function collectVueFiles(directory: string): string[] {
  return readdirSync(directory, { withFileTypes: true }).flatMap((entry) => {
    const path = resolve(directory, entry.name);
    if (entry.isDirectory()) {
      return collectVueFiles(path);
    }
    return entry.isFile() && entry.name.endsWith(".vue") ? [path] : [];
  });
}

function source(path: string): string {
  return readFileSync(path, "utf8");
}

function collectRenderedSourceFiles(directory: string): string[] {
  return readdirSync(directory, { withFileTypes: true }).flatMap((entry) => {
    const path = resolve(directory, entry.name);
    if (entry.isDirectory()) {
      return entry.name === "__tests__" ? [] : collectRenderedSourceFiles(path);
    }
    return entry.isFile() && /\.(?:vue|ts|tsx)$/.test(entry.name)
      ? [path]
      : [];
  });
}

function declaredLiteralPaths(content: string): string[] {
  return [...content.matchAll(/\bpath:\s*"([^"]*)"/g)].map(
    (match) => match[1] ?? "",
  );
}

function auditRouteMarkers(content: string, application: "MAIN" | "AUTH"): string[] {
  return [
    ...content.matchAll(
      new RegExp(`^\\s*// AUDIT-${application}-ROUTE:\\s*(\\S+)`, "gm"),
    ),
  ].map((match) => (match[1] === "<root>" ? "" : (match[1] ?? "")));
}

const liveVueSources = [mainRoot, authAppRoot, sharedUiRoot].flatMap((root) =>
  collectVueFiles(root).map((path) => ({
    path,
    content: source(path),
  })),
);

const renderedSourceFiles = [mainRoot, resolve(authAppRoot, "src"), sharedUiRoot]
  .flatMap(collectRenderedSourceFiles)
  .map((path) => ({ path, content: source(path) }));

describe("NIE visual design-system parity", () => {
  it("scans shared UI sources from both application Tailwind configs", () => {
    for (const appRoot of [mainAppRoot, authAppRoot]) {
      const tailwindConfig = source(resolve(appRoot, "tailwind.config.js"));

      expect(tailwindConfig, appRoot).toContain(
        '"../../packages/ui/src/**/*.{vue,js,ts,jsx,tsx}"',
      );
      expect(tailwindConfig, appRoot).not.toContain(
        '"../packages/ui/src/**/*.{vue,js,ts,jsx,tsx}"',
      );
    }

    expect(
      source(resolve(sharedUiPackageRoot, "tailwind.config.js")),
    ).toContain('"./src/**/*.{vue,js,ts,jsx,tsx}"');
  });

  it("keeps the cross-route browser audit aligned with every registered screen", () => {
    const applicationRoutes = source(resolve(mainRoot, "app-config/routes.ts"));
    const mainRouter = source(resolve(mainRoot, "router/index.ts"));
    const authRouter = source(resolve(authAppRoot, "src/router/index.ts"));
    const browserAudit = source(
      resolve(
        mainAppRoot,
        "../../../../tests/specs/e2e/ui-ux-consistency.e2e.spec.ts",
      ),
    );

    const declaredMainRoutes = new Set([
      ...declaredLiteralPaths(applicationRoutes),
      ...declaredLiteralPaths(mainRouter).filter((path) => path !== "/"),
    ]);
    const declaredAuthRoutes = new Set(declaredLiteralPaths(authRouter));

    expect(
      [...new Set(auditRouteMarkers(browserAudit, "MAIN"))].sort(),
    ).toEqual([...declaredMainRoutes].sort());
    expect(
      [...new Set(auditRouteMarkers(browserAudit, "AUTH"))].sort(),
    ).toEqual([...declaredAuthRoutes].sort());
  });

  it("waits for every route-addressable tab state before auditing", () => {
    const browserAudit = source(
      resolve(
        mainAppRoot,
        "../../../../tests/specs/e2e/ui-ux-consistency.e2e.spec.ts",
      ),
    );
    const auditedTabPanels = new Set(
      [...browserAudit.matchAll(/panelId:\s*"([^"]+)"/g)].map(
        (match) => match[1],
      ),
    );
    const auditedTabNames = new Set(
      [...browserAudit.matchAll(/tabName:\s*"([^"]+)"/g)].map(
        (match) => match[1],
      ),
    );

    expect(auditedTabPanels).toEqual(
      new Set([
        "access-control-users-panel",
        "access-control-roles-panel",
        "access-control-functions-panel",
        "notification-policies-panel",
        "notification-templates-panel",
        "notification-delivery-panel",
      ]),
    );
    expect(browserAudit).toContain(
      'toHaveAttribute("aria-selected", "true")',
    );
    expect(auditedTabNames).toEqual(
      new Set([
        "Users",
        "Roles",
        "Access Functions",
        "Policies",
        "Email templates",
        "Delivery",
      ]),
    );
  });

  it("keeps Auth, Main, and shared UI on the same semantic radius contract", () => {
    for (const appRoot of [mainAppRoot, authAppRoot, sharedUiPackageRoot]) {
      const tailwindConfig = source(resolve(appRoot, "tailwind.config.js"));
      for (const mapping of [
        'DEFAULT: "var(--theme-radius-control)"',
        'sm: "var(--theme-radius-control)"',
        'md: "var(--theme-radius-control)"',
        'lg: "var(--theme-radius-control)"',
        'xl: "var(--theme-radius-panel)"',
        '"2xl": "var(--theme-radius-panel)"',
        '"3xl": "var(--theme-radius-dialog)"',
        'full: "var(--theme-radius-pill)"',
      ]) {
        expect(tailwindConfig, `${appRoot}: ${mapping}`).toContain(mapping);
      }
    }
  });

  it("keeps the Auth login screen on semantic type and motion contracts", () => {
    const login = source(resolve(authAppRoot, "src/components/LoginPage.vue"));
    const template = login.match(/<template>([\s\S]*?)<\/template>/)?.[1] ?? "";

    expect(template).not.toMatch(
      /\b(?:xs:|sm:|md:|lg:|xl:)?text-(?:xs|sm|base|lg|xl|[2-9]xl|\[[^\]]+\])\b/,
    );
    expect(login).toMatch(/@media\s*\(prefers-reduced-motion:\s*reduce\)/);
    expect(login).toMatch(
      /prefers-reduced-motion:[\s\S]*?\.login-page[\s\S]*?animation:\s*none/,
    );
  });

  it("honours operating-system reduced-motion preferences in shared UI", () => {
    const globals = source(resolve(sharedUiRoot, "styles/globals.css"));

    expect(globals).toMatch(
      /@media\s*\(prefers-reduced-motion:\s*reduce\)[\s\S]*?animation-duration:\s*0\.01ms\s*!important/,
    );
    expect(globals).toMatch(
      /@media\s*\(prefers-reduced-motion:\s*reduce\)[\s\S]*?animation-iteration-count:\s*1\s*!important/,
    );
  });

  it("maps every frontend package typography utility onto the shared semantic scale", () => {
    for (const appRoot of [mainAppRoot, authAppRoot, sharedUiPackageRoot]) {
      const tailwindConfig = source(resolve(appRoot, "tailwind.config.js"));

      for (const token of [
        "--theme-font-size-navigation",
        "--theme-font-size-caption",
        "--theme-font-size-label",
        "--theme-font-size-body",
        "--theme-font-size-body-lg",
        "--theme-font-size-card-title",
        "--theme-font-size-section-title",
        "--theme-font-size-page-title",
        "--theme-font-size-hero",
        "--theme-font-size-display",
        "--theme-font-weight-regular",
        "--theme-font-weight-medium",
        "--theme-font-weight-semibold",
        "--theme-font-weight-bold",
        "--theme-letter-spacing-tight",
        "--theme-letter-spacing-wide",
      ]) {
        expect(tailwindConfig, `${appRoot}: ${token}`).toContain(token);
      }

      expect(tailwindConfig, `${appRoot}: display font fallback chain`).toMatch(
        /display:\s*\[[\s\S]*?Plus Jakarta Sans[\s\S]*?Inter[\s\S]*?system-ui[\s\S]*?sans-serif[\s\S]*?\]/,
      );
      expect(tailwindConfig, `${appRoot}: body font fallback chain`).toMatch(
        /body:\s*\[[\s\S]*?Plus Jakarta Sans[\s\S]*?Inter[\s\S]*?system-ui[\s\S]*?sans-serif[\s\S]*?\]/,
      );
    }
  });

  it("keeps sidebar navigation compact, strong, and semantic", () => {
    const staffLayout = source(resolve(mainRoot, "staff/layouts/StaffLayout.vue"));
    const globals = source(resolve(sharedUiRoot, "styles/globals.css"));

    expect(globals).toContain("--theme-font-size-navigation: 0.6875rem");
    expect(staffLayout.match(/\btext-navigation\b/g)).toHaveLength(7);
    expect(
      staffLayout.match(
        /font-semibold transition-all w-full text-left text-navigation/g,
      ),
    ).toHaveLength(4);
    expect(
      staffLayout.match(/text-navigation font-semibold transition-colors/g),
    ).toHaveLength(3);
  });

  it("does not create private template typography, tracking, or radius scales", () => {
    for (const file of liveVueSources) {
      const template =
        file.content.match(/<template>([\s\S]*?)<\/template>/)?.[1] ?? "";

      expect(template, file.path).not.toMatch(
        /\bfont-(?:thin|extralight|light|extrabold|black)\b/,
      );
      expect(template, file.path).not.toMatch(/\btext-\[(?:\d*\.)?\d+(?:px|rem)\]/);
      expect(template, file.path).not.toMatch(
        /\btracking-(?:tighter|normal|wider|widest|\[[^\]]+\])\b/,
      );
      expect(template, file.path).not.toMatch(
        /\brounded-\[(?:\d*\.)?\d+(?:px|rem)\]/,
      );
    }
  });

  it("keeps template padding, margin, and gap utilities on the shared spacing scale", () => {
    const allowedSpacing = new Set([
      "0",
      "0.5",
      "1",
      "1.5",
      "2",
      "2.5",
      "3",
      "4",
      "4.5",
      "5",
      "6",
      "8",
      "10",
      "12",
      "16",
    ]);
    const spacingUtility =
      /\b(?:[a-z-]+:)*-?(?:p|px|py|pt|pr|pb|pl|m|mx|my|mt|mr|mb|ml|gap|gap-x|gap-y|space-x|space-y)-(\d+(?:\.5)?)\b/g;
    const arbitrarySpacingUtility =
      /\b(?:[a-z-]+:)*-?(?:p|px|py|pt|pr|pb|pl|m|mx|my|mt|mr|mb|ml|gap|gap-x|gap-y|space-x|space-y)-\[[^\]]+\]/;

    for (const file of liveVueSources) {
      const template =
        file.content.match(/<template>([\s\S]*?)<\/template>/)?.[1] ?? "";
      expect(template, file.path).not.toMatch(arbitrarySpacingUtility);
      for (const match of template.matchAll(spacingUtility)) {
        expect(
          allowedSpacing.has(match[1] ?? ""),
          `${file.path}: ${match[0]}`,
        ).toBe(true);
      }
    }
  });

  it("keeps component elevation on the shared semantic shadow scale", () => {
    for (const appRoot of [mainAppRoot, authAppRoot, sharedUiPackageRoot]) {
      const tailwindConfig = source(resolve(appRoot, "tailwind.config.js"));
      for (const mapping of [
        'soft: "var(--theme-shadow-soft)"',
        'card: "var(--theme-shadow-card)"',
        'float: "var(--theme-shadow-float)"',
        'inset: "var(--theme-shadow-inset)"',
      ]) {
        expect(tailwindConfig, `${appRoot}: ${mapping}`).toContain(mapping);
      }
    }

    for (const file of liveVueSources) {
      expect(file.content, file.path).not.toMatch(
        /\b(?:hover:)?shadow-(?:sm|md|lg|xl|2xl)\b/,
      );
      expect(file.content, file.path).not.toMatch(
        /\bshadow-\[(?!var\(--theme-shadow-)/,
      );

      for (const match of file.content.matchAll(/box-shadow:\s*([^;]+);/g)) {
        const declaration = match[1]?.trim() ?? "";
        const isSemanticElevation =
          declaration === "none" ||
          declaration.includes("var(--theme-shadow-") ||
          /^0\s+0\s+0\s+/.test(declaration);
        expect(
          isSemanticElevation,
          `${file.path}: box-shadow: ${declaration}`,
        ).toBe(true);
      }
    }
  });

  it("keeps rendered and source copy free of UTF-8 mojibake", () => {
    for (const file of liveVueSources) {
      expect(file.content, file.path).not.toMatch(/[âÃÂ�]/);
    }
  });

  it("uses one visible label for the additional feedback field", () => {
    const feedback = source(
      resolve(mainRoot, "components/feedback/FloatingFeedbackButton.vue"),
    );

    expect(feedback).toContain('label="Additional feedback"');
    expect(feedback).not.toContain(
      '<p class="feedback-modal__section-title">Additional Feedback</p>',
    );
  });

  it("uses semantic colour families instead of framework palette names", () => {
    const rawPalette =
      /\b(?:bg|text|border|ring|placeholder|from|via|to|shadow|divide|outline|accent|fill|stroke)-(?:slate|gray|neutral|zinc|stone|red|rose|orange|amber|yellow|lime|green|emerald|teal|cyan|sky|blue|indigo|violet|purple|fuchsia|pink)-\d{2,3}(?:\/\d+)?\b/;

    for (const file of renderedSourceFiles) {
      expect(file.content, file.path).not.toMatch(rawPalette);
    }
  });

  it("uses the shared loader for notifications at every responsive layout", () => {
    const staffLayout = source(resolve(mainRoot, "staff/layouts/StaffLayout.vue"));

    expect(
      staffLayout.match(
        /<NieLoaderSymbol size="sm" label="Loading notifications" \/>/g,
      ),
    ).toHaveLength(2);
  });

  it("uses shared loading states for procurement actions and history", () => {
    const newRequest = source(
      resolve(mainRoot, "staff/pages/procurement/NewPurchaseRequest.vue"),
    );
    const orderDetail = source(
      resolve(mainRoot, "staff/pages/procurement/PurchaseOrderDetail.vue"),
    );
    const workflowTimeline = source(
      resolve(mainRoot, "components/workflow/WorkflowTimeline.vue"),
    );

    expect(newRequest).toContain(':loading="isSaving"');
    expect(newRequest).toContain(':loading="isSubmitting"');
    expect(newRequest).not.toMatch(/isSaving\s*\?\s*"Saving\.\.\."/);
    expect(newRequest).not.toMatch(/isSubmitting\s*\?\s*"Submitting\.\.\."/);
    expect(orderDetail).toContain(':loading="isUploading"');
    expect(orderDetail).not.toMatch(/isUploading\s*\?\s*"Uploading\.\.\."/);
    expect(orderDetail).toMatch(
      /<NieLoaderSymbol[\s\S]*?size="sm"[\s\S]*?label="Deleting document"[\s\S]*?\/>/,
    );
    expect(workflowTimeline).toMatch(
      /<NieLoaderSymbol[\s\S]*?size="sm"[\s\S]*?label="Loading workflow history"[\s\S]*?\/>/,
    );
  });

  it("keeps paired form values in one column until the small breakpoint", () => {
    const newRequest = source(
      resolve(mainRoot, "staff/pages/procurement/NewPurchaseRequest.vue"),
    );

    expect(newRequest).toContain(
      'class="grid grid-cols-1 gap-4 text-sm sm:grid-cols-2"',
    );
    expect(newRequest).not.toContain(
      'class="grid grid-cols-2 gap-4 text-sm"',
    );
  });

  it("keeps routed procurement failures visible with retry actions", () => {
    const dashboard = source(
      resolve(mainRoot, "staff/pages/procurement/ProcurementDashboard.vue"),
    );
    const orderDetail = source(
      resolve(mainRoot, "staff/pages/procurement/PurchaseOrderDetail.vue"),
    );

    for (const page of [dashboard, orderDetail]) {
      expect(page).toContain("<NieResultState");
      expect(page).toContain('variant="error"');
      expect(page).toContain("Try again");
    }
    expect(orderDetail).not.toContain(
      'router.push({ name: "order-history" })',
    );
  });

  it("programmatically labels text entry, file, and icon-only controls", () => {
    const expectedLabels = [
      ["components/chat/ChatInputBox.vue", 'aria-label="Chat message"'],
      ["components/chat/ChatSidebar.vue", 'aria-label="Rename conversation"'],
      [
        "../../../packages/ui/src/components/ui/select/NieSelect.vue",
        ':aria-label="`Search ${label || placeholder}`"',
      ],
      [
        "../../../packages/ui/src/components/composite/filter-bar/NieFilterBar.vue",
        ':aria-label="searchPlaceholder"',
      ],
      [
        "../../../packages/ui/src/components/composite/data-table/NieColumnFilterMenu.vue",
        ':aria-label="`Filter ${columnLabel} values`"',
      ],
      [
        "../../../packages/ui/src/components/composite/file-upload/NieFileUploadField.vue",
        ':aria-label="`Remove ${file.name}`"',
      ],
    ] as const;

    for (const [relativePath, label] of expectedLabels) {
      expect(source(resolve(mainRoot, relativePath)), relativePath).toContain(
        label,
      );
    }

    const orderDetail = source(
      resolve(mainRoot, "staff/pages/procurement/PurchaseOrderDetail.vue"),
    );
    expect(orderDetail).toContain('for="purchase-order-document"');
    expect(orderDetail).toContain('id="purchase-order-document"');
  });

  it("keeps recent dashboard orders keyboard accessible", () => {
    const dashboard = source(
      resolve(mainRoot, "staff/pages/procurement/ProcurementDashboard.vue"),
    );

    expect(dashboard).toContain(':aria-label="`Open order ${order.poNumber}`"');
    expect(dashboard).toContain('@keydown.enter="openOrder(order.id)"');
    expect(dashboard).toContain('@keydown.space.prevent="openOrder(order.id)"');
  });

  it("adapts neutral utility surfaces and status colours in dark mode", () => {
    const globals = source(resolve(sharedUiRoot, "styles/globals.css"));

    for (const selector of [
      "html.dark .text-secondary-900",
      "html.dark .text-secondary-600",
      "html.dark .text-secondary-400",
      "html.dark .bg-secondary-50",
      "html.dark .bg-secondary-100",
      "html.dark .border-secondary-200",
      "html.dark .bg-success-50",
      "html.dark .text-success-700",
      "html.dark .bg-warning-50",
      "html.dark .text-warning-700",
      "html.dark .bg-danger-50",
      "html.dark .text-danger-700",
      "html.dark .bg-info-50",
      "html.dark .text-info-700",
    ]) {
      expect(globals, selector).toContain(selector);
    }

    const browserAudit = source(
      resolve(
        mainAppRoot,
        "../../../../tests/specs/e2e/ui-ux-consistency.e2e.spec.ts",
      ),
    );
    expect(browserAudit).toContain(
      'localStorage.setItem("nie_template_theme_preference", "dark")',
    );
    expect(browserAudit).toContain("const darkRoutes = [");
    expect(browserAudit).toContain("for (const viewport of viewports)");
  });

  it("keeps Auth login surfaces adaptive in both colour modes", () => {
    const login = source(resolve(authAppRoot, "src/components/LoginPage.vue"));

    expect(login).toMatch(
      /\.login-panel\s*\{[\s\S]*?var\(--theme-color-surface-canvas\)/,
    );
    expect(login).toMatch(
      /\.input-shell\s*\{[\s\S]*?background:\s*var\(--theme-color-surface-subtle\)/,
    );
    expect(login).not.toMatch(
      /\.input-shell\s*\{[\s\S]*?background:\s*var\(--theme-color-neutral-50\)/,
    );
    expect(login).not.toContain("var(--theme-color-text-inverse)");
    expect(login).toContain("var(--theme-color-static-white)");
  });

  it("uses a dedicated accessible foreground on branded surfaces", () => {
    const globals = source(resolve(sharedUiRoot, "styles/globals.css"));
    const runtime = source(resolve(sharedUiRoot, "theme/runtime.ts"));
    const reportDetail = source(
      resolve(mainRoot, "pages/reports/ReportDetail.vue"),
    );

    expect(globals).toContain(
      "--theme-color-on-brand: var(--theme-color-static-white)",
    );
    expect(globals).toMatch(
      /\.portal-primary-btn\s*\{[\s\S]*?color:\s*var\(--theme-color-on-brand\)/,
    );
    for (const appRoot of [mainAppRoot, authAppRoot, sharedUiPackageRoot]) {
      expect(source(resolve(appRoot, "tailwind.config.js")), appRoot).toContain(
        '"on-brand": "var(--theme-color-on-brand)"',
      );
    }
    expect(runtime).toContain(
      'root.style.setProperty("--theme-color-on-brand", colors.brandContrast)',
    );
    for (const file of liveVueSources) {
      expect(file.content, file.path).not.toContain(
        "var(--theme-color-text-inverse)",
      );
    }
    expect(reportDetail).toMatch(
      /\.report-breadcrumb__link\s*\{[\s\S]*?color:\s*var\(--color-text,\s*var\(--theme-color-text-strong\)\)/,
    );
  });

  it("provides WCAG AA brand foregrounds for every enabled preset and mode", () => {
    function luminance(hex: string): number {
      const channels = [1, 3, 5].map((start) => {
        const channel = Number.parseInt(hex.slice(start, start + 2), 16) / 255;
        return channel <= 0.04045
          ? channel / 12.92
          : ((channel + 0.055) / 1.055) ** 2.4;
      });
      return (
        0.2126 * (channels[0] ?? 0) +
        0.7152 * (channels[1] ?? 0) +
        0.0722 * (channels[2] ?? 0)
      );
    }

    function contrast(first: string, second: string): number {
      const lighter = Math.max(luminance(first), luminance(second));
      const darker = Math.min(luminance(first), luminance(second));
      return (lighter + 0.05) / (darker + 0.05);
    }

    for (const manifest of Object.values(themePresets)) {
      for (const mode of ["light", "dark"] as const) {
        const colors = manifest.tokens[mode].colors;
        expect(
          contrast(colors.brand[600], colors.brandContrast),
          `${manifest.id}/${mode}`,
        ).toBeGreaterThanOrEqual(4.5);
      }
    }
  });

  it("maps every semantic status shade through the shared colour tokens", () => {
    for (const appRoot of [mainAppRoot, authAppRoot, sharedUiPackageRoot]) {
      const tailwindConfig = source(resolve(appRoot, "tailwind.config.js"));
      for (const tone of ["success", "warning", "danger", "info"]) {
        for (const shade of [50, 100, 200, 300, 400, 500, 600, 700, 800, 900, 950]) {
          expect(tailwindConfig, `${appRoot}: ${tone}-${shade}`).toContain(
            `var(--theme-color-${tone}-${shade})`,
          );
        }
      }
    }
  });

  it("defines one semantic scale for typography, spacing, radii, controls, and status colours", () => {
    const globals = source(resolve(sharedUiRoot, "styles/globals.css"));

    for (const token of [
      "--theme-font-size-caption",
      "--theme-font-size-body",
      "--theme-font-size-section-title",
      "--theme-font-size-page-title",
      "--theme-font-size-display",
      "--theme-font-weight-regular",
      "--theme-font-weight-medium",
      "--theme-font-weight-semibold",
      "--theme-font-weight-bold",
      "--theme-space-1",
      "--theme-space-2",
      "--theme-space-3",
      "--theme-space-4",
      "--theme-space-5",
      "--theme-space-6",
      "--theme-radius-control",
      "--theme-radius-panel",
      "--theme-radius-dialog",
      "--theme-radius-pill",
      "--theme-control-height-sm",
      "--theme-control-height-md",
      "--theme-control-height-lg",
      "--theme-color-success-surface",
      "--theme-color-warning-surface",
      "--theme-color-danger-surface",
      "--theme-color-info-surface",
    ]) {
      expect(globals, token).toContain(token);
    }
  });

  it("does not override shared primitive geometry from the application stylesheet", () => {
    const applicationStyles = source(resolve(mainRoot, "style.css"));

    expect(applicationStyles).not.toMatch(/button:not\(/);
    expect(applicationStyles).not.toMatch(/\.rounded-xl\s*\{/);
    expect(applicationStyles).not.toMatch(/\btable\s*\{/);
    expect(applicationStyles).not.toMatch(
      /input:not\(\[type="checkbox"\]\)[\s\S]*border-radius/,
    );
  });

  it("keeps feature SFC styling on semantic tokens", () => {
    const literalColour = /(?:#[0-9a-f]{3,8}\b|rgba?\()/i;
    const nonstandardWeight = /font-weight\s*:\s*(?:650|750|800|850)\b/i;
    const fontSizeDeclarations = /font-size\s*:\s*([^;}]+)/gi;
    const radiusDeclarations = /border-radius\s*:\s*([^;}]+)/gi;
    const spacingDeclarations =
      /(?:padding(?:-[a-z]+)?|margin(?:-[a-z]+)?|gap|row-gap|column-gap)\s*:\s*([^;}]+)/gi;

    for (const file of liveVueSources) {
      expect(file.content, file.path).not.toMatch(literalColour);
      expect(file.content, file.path).not.toMatch(nonstandardWeight);
      const styleContent = [...file.content.matchAll(/<style[^>]*>([\s\S]*?)<\/style>/gi)]
        .map((match) => match[1] ?? "")
        .join("\n");
      for (const match of styleContent.matchAll(fontSizeDeclarations)) {
        expect(match[1]?.trim(), `${file.path}: ${match[0]}`).toMatch(
          /^var\(--theme-font-size-/,
        );
      }
      for (const match of styleContent.matchAll(radiusDeclarations)) {
        expect(match[1]?.trim(), `${file.path}: ${match[0]}`).toMatch(
          /^var\(--theme-radius-/,
        );
      }
      for (const match of styleContent.matchAll(spacingDeclarations)) {
        const value = match[1]?.trim() ?? "";
        expect(
            /var\(--theme-(?:space|shell)-/.test(value) ||
            /^(?:0|auto|normal)$/.test(value) ||
            /^(?:(?:0|auto|var\(--theme-(?:space|shell)-[^)]+\))\s*)+$/.test(
              value,
            ) ||
            /(?:calc|env|max|min|clamp)\(/.test(value),
          `${file.path}: ${match[0]}`,
        ).toBe(true);
      }
    }
  });

  it("applies runtime radius and motion changes reactively", () => {
    const runtime = source(resolve(sharedUiRoot, "theme/runtime.ts"));
    expect(runtime).toMatch(
      /watch\(\s*\[\s*mode,\s*preset,\s*density,\s*scenario,\s*layoutVariant,\s*themePreference,\s*radius,\s*motion,/s,
    );
    expect(runtime).toContain('radius: "nie_template_theme_radius"');
    expect(runtime).toContain('motion: "nie_template_theme_motion"');
    expect(runtime).toMatch(/localStorage\.setItem\(STORAGE_KEYS\.radius, radius\.value\)/);
    expect(runtime).toMatch(/localStorage\.setItem\(STORAGE_KEYS\.motion, motion\.value\)/);
    expect(runtime).toMatch(/radius:\s*radius\.value,/);
    expect(runtime).toMatch(/motion:\s*motion\.value,/);
  });

  it("keeps the staff page title readable at compact widths", () => {
    const staffLayout = source(
      resolve(mainRoot, "staff/layouts/StaffLayout.vue"),
    );

    expect(staffLayout).toContain('class="staff-page-title');
    expect(staffLayout).toContain('class="staff-page-feedback');
    expect(staffLayout).not.toMatch(
      /<h1[\s\S]{0,180}class="[^"]*\btruncate\b/,
    );
    expect(staffLayout).not.toMatch(
      /<h1[\s\S]{0,180}class="[^"]*\bline-clamp-/,
    );
    expect(staffLayout).toContain(
      'class="flex flex-wrap items-center justify-between',
    );
    expect(staffLayout).toContain(
      'class="flex w-full min-w-0 flex-none basis-full items-center gap-3 sm:w-auto sm:flex-1 sm:basis-auto',
    );
    expect(staffLayout).toContain('role="dialog"');
    expect(staffLayout).toContain('aria-modal="true"');
    expect(staffLayout).toContain('aria-label="Mobile navigation"');
    expect(staffLayout).toContain('aria-label="Close navigation menu"');
    expect(staffLayout).toContain('@keydown="handleMobileSidebarKeydown"');
    expect(staffLayout).toContain(
      ":aria-label=\"sidebarCollapsed ? 'Expand navigation' : 'Collapse navigation'\"",
    );
  });

  it("uses shared primitives for administration data and multiline fields", () => {
    const uiIndex = source(resolve(sharedUiRoot, "index.ts"));
    const notifications = source(
      resolve(mainRoot, "components/admin/notifications/NotificationAdministration.vue"),
    );
    expect(uiIndex).toContain('export * from "./components/ui/textarea"');
    expect(notifications).toContain("<NieDataTable");
    expect(notifications).not.toContain("<table");
    expect(notifications).toContain("<NieTextarea");
    expect(notifications).toContain('variant="brand"');
    expect(notifications).toContain('size="lg"');
  });

  it("owns application feedback, confirmation, modal, and loading visuals in shared UI", () => {
    const applicationVueSources = collectVueFiles(mainRoot).map((path) => ({
      path,
      content: source(path),
    }));

    expect(existsSync(resolve(mainRoot, "components/ToastContainer.vue"))).toBe(false);
    expect(existsSync(resolve(mainRoot, "components/common/ConfirmDialog.vue"))).toBe(false);
    expect(existsSync(resolve(mainRoot, "composables/useToast.ts"))).toBe(false);

    for (const file of applicationVueSources) {
      expect(file.content, file.path).not.toContain('from "@/composables/useToast"');
      expect(file.content, file.path).not.toMatch(/\banimate-spin\b/);
      expect(file.content, file.path).not.toMatch(/__spinner\b/);
      expect(file.content, file.path).not.toMatch(/@keyframes\s+[\w-]*spin\b/);
      expect(file.content, file.path).not.toContain("progress_activity");
      expect(file.content, file.path).not.toContain("<Teleport");
      if (!file.path.endsWith("ChatInputBox.vue")) {
        expect(file.content, file.path).not.toContain("<textarea");
      }
      expect(file.content, file.path).not.toMatch(/@keyframes\s+spin\b/);
      expect(file.content, file.path).not.toMatch(/animation:\s*spin\b/);
    }

    const sharedModal = source(
      resolve(sharedUiRoot, "components/ui/modal/NieModal.vue"),
    );
    const sharedConfirm = source(
      resolve(sharedUiRoot, "components/composite/confirm/NieConfirmDialog.vue"),
    );
    expect(sharedModal).toContain('role="dialog"');
    expect(sharedModal).toContain(':aria-labelledby="title ? titleId : undefined"');
    expect(sharedModal).toContain(':aria-label="!title ? resolvedAriaLabel : undefined"');
    expect(sharedModal).toContain('@keydown.tab="containFocus"');
    expect(sharedModal).toContain('aria-label="Close dialog"');
    expect(sharedModal).toMatch(/prefers-reduced-motion:\s*reduce/);
    expect(sharedConfirm).toContain(':close-on-overlay="!loading"');
    expect(sharedConfirm).toContain(':close-on-escape="!loading"');
    expect(sharedConfirm).toContain(':show-close="!loading"');
  });

  it("keeps specialised text-entry surfaces on the shared control geometry", () => {
    const chatInput = source(
      resolve(mainRoot, "components/chat/ChatInputBox.vue"),
    );

    expect(chatInput).toMatch(
      /\.composer-input\s*\{[\s\S]*?min-height:\s*var\(--theme-control-height-md\)/,
    );
    expect(chatInput).toMatch(
      /\.composer-input\s*\{[\s\S]*?border-radius:\s*var\(--theme-radius-control\)/,
    );
  });

  it("keeps every custom data-table toolbar action on the medium control height", () => {
    for (const file of liveVueSources) {
      const toolbarBlocks = [
        ...file.content.matchAll(
          /<template\s+#toolbar-actions[^>]*>([\s\S]*?)<\/template>/g,
        ),
      ];

      for (const block of toolbarBlocks) {
        expect(block[1], file.path).not.toContain('size="sm"');
      }
    }
  });

  it("uses one medium control height for report filters and their apply action", () => {
    const reportFilters = source(
      resolve(mainRoot, "components/reports/ReportFilterBar.vue"),
    );

    expect(reportFilters).toMatch(
      /\.report-filter-bar__input\s*\{[\s\S]*?height:\s*var\(--theme-control-height-md\)/,
    );
    expect(reportFilters).toMatch(
      /\.report-filter-bar__apply\s*\{[\s\S]*?height:\s*var\(--theme-control-height-md\)/,
    );
    expect(reportFilters).not.toMatch(/height:\s*(?:34|36|42)px/);
  });

  it("removes the unused legacy dashboard stylesheet", () => {
    expect(existsSync(resolve(mainRoot, "assets/dashboard.css"))).toBe(false);
  });
});

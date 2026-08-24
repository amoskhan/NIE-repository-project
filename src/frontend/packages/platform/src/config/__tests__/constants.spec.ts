import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import {
  FRONTEND_CONSTANTS,
  getAppBasePath,
  getBackendBaseUrl,
  getBackendUrl,
  getCookieAttributes,
  getFrontendAssetUrl,
  getFrontendUrl,
  getRuntimeEnvironment,
  getRuntimeHostname,
  isLocalHostname,
} from "../constants";

const originalLocation = Object.getOwnPropertyDescriptor(window, "location");

function stubLocation(href: string): void {
  const url = new URL(href);
  Object.defineProperty(window, "location", {
    configurable: true,
    writable: true,
    value: {
      href: url.href,
      hostname: url.hostname,
      pathname: url.pathname,
      origin: url.origin,
    },
  });
}

afterEach(() => {
  if (originalLocation) {
    Object.defineProperty(window, "location", originalLocation);
  }
  delete window.__NIE_APPLICATION_CONFIG__;
  delete window.__NIE_RUNTIME_SERVICES__;
  document.head.querySelectorAll("meta[name^='nie:']").forEach((meta) => {
    meta.remove();
  });
  vi.resetModules();
});

describe("getRuntimeHostname", () => {
  it("lower-cases the browser hostname", () => {
    stubLocation("https://APP.NIE.EDU.SG/");

    expect(getRuntimeHostname()).toBe("app.nie.edu.sg");
  });
});

describe("isLocalHostname", () => {
  it("recognises every loopback form", () => {
    expect(isLocalHostname("localhost")).toBe(true);
    expect(isLocalHostname("127.0.0.1")).toBe(true);
    expect(isLocalHostname("::1")).toBe(true);
  });

  it("rejects deployed hostnames", () => {
    expect(isLocalHostname("app.nie.edu.sg")).toBe(false);
    expect(isLocalHostname("localhost.nie.edu.sg")).toBe(false);
  });
});

describe("getRuntimeEnvironment", () => {
  it("maps each NIE hostname suffix to its environment", () => {
    expect(getRuntimeEnvironment("localhost")).toBe("local");
    expect(getRuntimeEnvironment("app.stg.nie.edu.sg")).toBe("stg");
    expect(getRuntimeEnvironment("app.dev.nie.edu.sg")).toBe("dev");
    expect(getRuntimeEnvironment("app.nie.edu.sg")).toBe("prd");
  });

  it("checks the staging suffix before the bare production suffix", () => {
    // Both suffixes match ".nie.edu.sg"; staging must win.
    expect(getRuntimeEnvironment("a.b.stg.nie.edu.sg")).toBe("stg");
    expect(getRuntimeEnvironment("a.b.dev.nie.edu.sg")).toBe("dev");
  });

  it("falls back to the build mode for unknown hostnames", () => {
    // Vitest builds in "test" mode, which is neither production nor staging.
    expect(getRuntimeEnvironment("app.example.com")).toBe("dev");
  });
});

describe("getAppBasePath", () => {
  it("returns an empty base path at the site root", () => {
    expect(getAppBasePath("/")).toBe("");
    expect(getAppBasePath("")).toBe("");
  });

  it("returns an empty base path for reserved root-level segments", () => {
    expect(getAppBasePath("/api-main/users")).toBe("");
    expect(getAppBasePath("/login/")).toBe("");
    expect(getAppBasePath("/assets/index.js")).toBe("");
    expect(getAppBasePath("/SW.JS")).toBe("");
  });

  it("treats any other leading segment as the deployed sub-path", () => {
    expect(getAppBasePath("/procurement/orders/42")).toBe("/procurement");
    expect(getAppBasePath("/procurement")).toBe("/procurement");
  });

  it("reads the current pathname when none is supplied", () => {
    stubLocation("https://portal.nie.edu.sg/procurement/orders");

    expect(getAppBasePath()).toBe("/procurement");
  });
});

describe("getBackendUrl", () => {
  it("routes each service through its own gateway segment", () => {
    stubLocation("https://portal.nie.edu.sg/");

    expect(getBackendBaseUrl("auth")).toBe("/api-auth");
    expect(getBackendBaseUrl("main")).toBe("/api-main");
    expect(getBackendUrl("main", "/api/users")).toBe("/api-main/api/users");
  });

  it("keeps the deployed sub-path in front of the gateway segment", () => {
    stubLocation("https://portal.nie.edu.sg/procurement/orders");

    expect(getBackendUrl("auth", "/api")).toBe("/procurement/api-auth/api");
  });

  it("trims leading and trailing slashes from the requested path", () => {
    stubLocation("https://portal.nie.edu.sg/");

    expect(getBackendUrl("main", "///api/users///")).toBe(
      "/api-main/api/users",
    );
    expect(getBackendUrl("main", "/")).toBe("/api-main");
    expect(getBackendUrl("main")).toBe("/api-main");
  });
});

describe("getFrontendUrl", () => {
  it("uses absolute dev-server URLs while running locally", () => {
    stubLocation("http://localhost:8002/");

    expect(getFrontendUrl("main")).toBe("http://localhost:8002/");
    expect(getFrontendUrl("auth")).toBe("http://localhost:8001/login/");
  });

  it("keeps the loopback IP the user actually typed", () => {
    stubLocation("http://127.0.0.1:8002/");

    expect(getFrontendUrl("auth")).toBe("http://127.0.0.1:8001/login/");
  });

  it("uses trailing-slash relative URLs once deployed", () => {
    stubLocation("https://portal.nie.edu.sg/procurement/orders");

    expect(getFrontendUrl("main")).toBe("/procurement/");
    expect(getFrontendUrl("auth")).toBe("/procurement/login/");
  });
});

describe("getFrontendAssetUrl", () => {
  it("resolves assets against the deployed sub-path", () => {
    stubLocation("https://portal.nie.edu.sg/procurement/orders");

    expect(getFrontendAssetUrl("/manifest.json")).toBe(
      "/procurement/manifest.json",
    );
    expect(getFrontendAssetUrl("logo.svg", "auth")).toBe(
      "/procurement/login/logo.svg",
    );
  });
});

describe("FRONTEND_CONSTANTS", () => {
  it("disables optional integrations when no runtime config is injected", () => {
    expect(FRONTEND_CONSTANTS.sentry.enabled).toBe(false);
    expect(FRONTEND_CONSTANTS.sentry.dsn).toBe("");
    expect(FRONTEND_CONSTANTS.openTelemetry.enabled).toBe(false);
    expect(FRONTEND_CONSTANTS.oneSignal.enabled).toBe(false);
    expect(FRONTEND_CONSTANTS.auth.portalSsoEnabled).toBe(false);
    expect(FRONTEND_CONSTANTS.session.timeoutMinutes).toBe(60);
    expect(FRONTEND_CONSTANTS.cookies.session).toBe("Application-SessionToken");
  });

  it("omits the cookie domain when none is configured", () => {
    expect(getCookieAttributes()).toEqual({});
  });
});

describe("FRONTEND_CONSTANTS (runtime configuration)", () => {
  beforeEach(() => {
    vi.resetModules();
  });

  it("reads the injected window configuration", async () => {
    stubLocation("https://portal.nie.edu.sg/");
    window.__NIE_APPLICATION_CONFIG__ = {
      backendAuthBaseUrl:
        "/coder/@owner/workspace/apps/preview-auth/~ignite/services/auth-api/",
      backendMainBaseUrl:
        "/coder/@owner/workspace/apps/preview-auth/~ignite/services/main-api/",
      cookieDomain: ".nie.edu.sg",
      cookiePath: "/coder/@owner/workspace/apps/",
      frontendAuthUrl: "/coder/@owner/workspace/apps/preview-auth/",
      frontendMainUrl: "/coder/@owner/workspace/apps/preview-main/",
      oneSignalAppId: "one-signal-app",
      openTelemetryExporterEndpoint: "https://otel.nie.edu.sg/v1/traces",
      portalSsoEnabled: true,
      sentryDsn: "https://key@sentry.nie.edu.sg/1",
      sentryTracesSampleRate: 0.75,
      sessionCookieName: "Application-SessionToken-workspace",
      userCookieName: "Application-User-workspace",
    };

    const { FRONTEND_CONSTANTS: constants, getCookieAttributes: attributes } =
      await import("../constants");

    expect(constants.sentry.enabled).toBe(true);
    expect(constants.sentry.dsn).toBe("https://key@sentry.nie.edu.sg/1");
    expect(constants.sentry.environment).toBe("prd");
    expect(constants.sentry.tracesSampleRate).toBe(0.75);
    expect(constants.openTelemetry.enabled).toBe(true);
    expect(constants.oneSignal.enabled).toBe(true);
    expect(constants.oneSignal.allowLocalhostAsSecureOrigin).toBe(false);
    expect(constants.auth.portalSsoEnabled).toBe(true);
    expect(constants.backend.auth).toBe(
      "/coder/@owner/workspace/apps/preview-auth/~ignite/services/auth-api",
    );
    expect(constants.api.auth).toBe(
      "/coder/@owner/workspace/apps/preview-auth/~ignite/services/auth-api/api",
    );
    expect(constants.apps.auth).toBe(
      "/coder/@owner/workspace/apps/preview-auth/",
    );
    expect(constants.apps.main).toBe(
      "/coder/@owner/workspace/apps/preview-main/",
    );
    expect(constants.cookies.session).toBe(
      "Application-SessionToken-workspace",
    );
    expect(constants.cookies.user).toBe("Application-User-workspace");
    expect(attributes()).toEqual({
      domain: ".nie.edu.sg",
      path: "/coder/@owner/workspace/apps/",
    });
  });

  it("uses a validated semantic service registry for any added service", async () => {
    stubLocation("https://portal.nie.edu.sg/");
    window.__NIE_RUNTIME_SERVICES__ = {
      version: 1,
      services: {
        "preview-auth": {
          id: "preview-auth",
          displayName: "Authentication",
          kind: "frontend",
          baseUrl: "/coder/@owner/workspace/apps/preview-auth/",
        },
        "auth-api": {
          id: "auth-api",
          displayName: "Authentication API",
          kind: "backend",
          baseUrl:
            "/coder/@owner/workspace/apps/preview-auth/~ignite/services/auth-api/",
        },
        "reports-api": {
          id: "reports-api",
          displayName: "Reports API",
          kind: "backend",
          baseUrl:
            "/coder/@owner/workspace/apps/preview-auth/~ignite/services/reports-api/",
        },
      },
    };

    const constants = await import("../constants");

    expect(constants.getFrontendUrl("auth")).toBe(
      "/coder/@owner/workspace/apps/preview-auth/",
    );
    expect(constants.getBackendUrl("auth", "/api/Auth/Login")).toBe(
      "/coder/@owner/workspace/apps/preview-auth/~ignite/services/auth-api/api/Auth/Login",
    );
    expect(constants.getRuntimeServiceBaseUrl("reports-api")).toBe(
      "/coder/@owner/workspace/apps/preview-auth/~ignite/services/reports-api/",
    );
    expect(constants.getRuntimeServiceUrl("reports-api", "/api/export")).toBe(
      "/coder/@owner/workspace/apps/preview-auth/~ignite/services/reports-api/api/export",
    );
  });

  it("rejects cross-origin, protocol-relative, and malformed runtime routes", async () => {
    stubLocation("https://portal.nie.edu.sg/");
    window.__NIE_APPLICATION_CONFIG__ = {
      backendAuthBaseUrl: "https://attacker.example/api",
      backendMainBaseUrl: "//attacker.example/api",
      frontendAuthUrl: "/safe/path?redirect=https://attacker.example",
    };
    window.__NIE_RUNTIME_SERVICES__ = {
      version: 1,
      services: {
        "auth-api": {
          id: "different-id",
          displayName: "Authentication API",
          kind: "backend",
          baseUrl: "/runtime/auth/",
        },
      },
    };

    const constants = await import("../constants");

    expect(constants.getBackendBaseUrl("auth")).toBe("/api-auth");
    expect(constants.getBackendBaseUrl("main")).toBe("/api-main");
    expect(constants.getFrontendUrl("auth")).toBe("/login/");
    expect(constants.getRuntimeServiceBaseUrl("auth-api")).toBeUndefined();
    expect(constants.getRuntimeServiceBaseUrl("../auth-api")).toBeUndefined();
  });

  it("overrides a broad deployment cookie domain with workspace scope", async () => {
    window.__NIE_APPLICATION_CONFIG__ = {
      cookieDomain: "",
      cookiePath: "/coder/@owner/workspace/apps/",
      sessionCookieName: "Application-SessionToken-a1b2c3d4e5f6",
      userCookieName: "Application-User-a1b2c3d4e5f6",
    };
    document.head.innerHTML =
      '<meta name="nie:cookieDomain" content=".nie.edu.sg" />';

    const { FRONTEND_CONSTANTS: constants, getCookieAttributes: attributes } =
      await import("../constants");

    expect(constants.cookies.domain).toBe("");
    expect(constants.cookies.session).toBe(
      "Application-SessionToken-a1b2c3d4e5f6",
    );
    expect(attributes()).toEqual({ path: "/coder/@owner/workspace/apps/" });
  });

  it("falls back to nie: meta tags when the window configuration is absent", async () => {
    stubLocation("https://portal.nie.edu.sg/");
    document.head.innerHTML = `
      <meta name="nie:cookieDomain" content="  .meta.nie.edu.sg  " />
      <meta name="nie:portalSsoEnabled" content="TRUE" />
      <meta name="nie:sentryTracesSampleRate" content="0.4" />
      <meta name="nie:sentryEnvironment" content="stg" />
    `;

    const { FRONTEND_CONSTANTS: constants } = await import("../constants");

    expect(constants.cookies.domain).toBe(".meta.nie.edu.sg");
    expect(constants.auth.portalSsoEnabled).toBe(true);
    expect(constants.sentry.tracesSampleRate).toBe(0.4);
    expect(constants.sentry.environment).toBe("stg");
  });

  it("ignores blank and unparseable meta values", async () => {
    stubLocation("https://portal.nie.edu.sg/");
    document.head.innerHTML = `
      <meta name="nie:cookieDomain" content="   " />
      <meta name="nie:portalSsoEnabled" content="false" />
      <meta name="nie:sentryTracesSampleRate" content="not-a-number" />
    `;

    const { FRONTEND_CONSTANTS: constants } = await import("../constants");

    expect(constants.cookies.domain).toBeUndefined();
    expect(constants.auth.portalSsoEnabled).toBe(false);
    expect(constants.sentry.tracesSampleRate).toBe(0.2);
  });

  it("prefers the window configuration over a conflicting meta tag", async () => {
    stubLocation("https://portal.nie.edu.sg/");
    document.head.innerHTML = `<meta name="nie:cookieDomain" content=".meta.nie.edu.sg" />`;
    window.__NIE_APPLICATION_CONFIG__ = { cookieDomain: ".window.nie.edu.sg" };

    const { FRONTEND_CONSTANTS: constants } = await import("../constants");

    expect(constants.cookies.domain).toBe(".window.nie.edu.sg");
  });
});

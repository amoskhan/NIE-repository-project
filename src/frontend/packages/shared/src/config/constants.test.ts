// EXAMPLE UNIT TEST — locks down the runtime-configuration contract.
//
// getRuntimeEnvironment() decides which deployment a bundle thinks it is running in.
// The template must NOT infer that from a hardcoded domain, so these tests assert the
// documented resolution order: local hostname > runtime config > optional hostname
// suffix map > Vite build mode.
//
// The rest of the file covers the URL/cookie overrides that let ONE build run under a
// deep proxy mount (a hosted workspace preview such as Ignite/Coder) without any
// import.meta.env.VITE_* baked into the bundle.

import { afterEach, describe, expect, it, vi } from "vitest";
import {
  FRONTEND_CONSTANTS,
  getAppBasePath,
  getBackendBaseUrl,
  getBackendUrl,
  getFrontendAssetUrl,
  getFrontendUrl,
  getRuntimeEnvironment,
} from "./constants";

// A hosted workspace preview mounts each app on a DEEP path and gives each service its
// own sibling mount — nothing here is derivable from the first path segment.
const MOUNT_MAIN = "/ignite/coder/@u/w/apps/preview-main/";
const MOUNT_AUTH = "/ignite/coder/@u/w/apps/preview-auth/";
const MOUNT_MAIN_API = "/ignite/coder/@u/w/apps/main-api/";
const MOUNT_AUTH_API = "/ignite/coder/@u/w/apps/auth-api/";

/** Move the jsdom document to a path, the way a real preview URL would. */
function setPathname(pathname: string): void {
  window.history.replaceState({}, "", pathname);
}

/**
 * Re-import the module with the current runtime config in place.
 *
 * FRONTEND_CONSTANTS is frozen at module-evaluation time (that is the point: the config
 * is read once, from HTML the server wrote), so a fresh import is the only honest way to
 * test what a real page load would see.
 */
async function loadConstantsFresh() {
  vi.resetModules();
  return await import("./constants");
}

afterEach(() => {
  delete window.__APP_TEMPLATE_CONFIG__;
  setPathname("/");
});

describe("getRuntimeEnvironment", () => {
  it("treats loopback hostnames as local", () => {
    expect(getRuntimeEnvironment("localhost")).toBe("local");
    expect(getRuntimeEnvironment("127.0.0.1")).toBe("local");
  });

  it("prefers the environment supplied by runtime config", () => {
    window.__APP_TEMPLATE_CONFIG__ = { environment: "stg" };

    expect(getRuntimeEnvironment("anything.example.org")).toBe("stg");
  });

  it("ignores an unrecognised environment value and falls through", () => {
    window.__APP_TEMPLATE_CONFIG__ = { environment: "banana" };

    // "banana" is not one of local/dev/stg/prd, so resolution continues to the build
    // mode rather than silently trusting bad configuration.
    expect(getRuntimeEnvironment("anything.example.org")).toBe("dev");
  });

  it("falls back to the configurable hostname suffix map", () => {
    window.__APP_TEMPLATE_CONFIG__ = {
      environmentHostnameSuffixes: {
        ".example.org": "prd",
        ".dev.example.org": "dev",
      },
    };

    // Longest matching suffix wins, so the more specific ".dev.example.org" beats
    // the broader ".example.org".
    expect(getRuntimeEnvironment("app.dev.example.org")).toBe("dev");
    expect(getRuntimeEnvironment("app.example.org")).toBe("prd");
    expect(getRuntimeEnvironment("app.somewhere-else.org")).toBe("dev");
  });
});

describe("getAppBasePath", () => {
  it("returns an empty base path for root-level segments", () => {
    // These segments belong to the deployment itself, not to an app mounted on a
    // sub-path — mistaking one for a base path breaks every generated URL.
    expect(getAppBasePath("/login/")).toBe("");
    expect(getAppBasePath("/assets/index.js")).toBe("");
    expect(getAppBasePath("/api-main/api/Vendor/GetAll")).toBe("");
    expect(getAppBasePath("/")).toBe("");
  });

  it("returns the first segment when the app is mounted on a sub-path", () => {
    expect(getAppBasePath("/MYAPP/vendors")).toBe("/MYAPP");
    expect(getAppBasePath("/MYAPP/login/")).toBe("/MYAPP");
  });

  it("prefers an explicit appBasePath over the first-segment guess", () => {
    window.__APP_TEMPLATE_CONFIG__ = { appBasePath: MOUNT_MAIN };

    // The override states where the app is mounted; the current URL cannot.
    expect(getAppBasePath()).toBe("/ignite/coder/@u/w/apps/preview-main");
    expect(getAppBasePath("/MYAPP/vendors")).toBe(
      "/ignite/coder/@u/w/apps/preview-main",
    );
  });

  it("normalises a configured base path to the derived shape", () => {
    window.__APP_TEMPLATE_CONFIG__ = { appBasePath: "MYAPP/" };
    expect(getAppBasePath()).toBe("/MYAPP");

    window.__APP_TEMPLATE_CONFIG__ = { appBasePath: "/" };
    expect(getAppBasePath()).toBe("");
  });

  it("REGRESSION: a multi-segment mount is unguessable without the override", () => {
    // Documented failure mode. Under "/ignite/coder/@u/w/apps/preview-main/" the
    // first-segment heuristic answers "/ignite", so every derived URL 404s. This is
    // exactly why appBasePath (and the per-service overrides below) exist.
    expect(getAppBasePath(MOUNT_MAIN)).toBe("/ignite");
  });
});

describe("getBackendBaseUrl / getBackendUrl", () => {
  it("derives sibling API paths from the base path when nothing is overridden", () => {
    expect(getBackendBaseUrl("main")).toBe("/api-main");
    expect(getBackendBaseUrl("auth")).toBe("/api-auth");
    expect(getBackendUrl("main", "/api")).toBe("/api-main/api");
    expect(getBackendUrl("auth", "/api")).toBe("/api-auth/api");

    setPathname("/MYAPP/vendors");
    expect(getBackendBaseUrl("main")).toBe("/MYAPP/api-main");
    expect(getBackendUrl("auth", "api/Auth/Login")).toBe(
      "/MYAPP/api-auth/api/Auth/Login",
    );
  });

  it("uses the explicit per-service URLs when runtime config supplies them", () => {
    window.__APP_TEMPLATE_CONFIG__ = {
      mainApiBaseUrl: MOUNT_MAIN_API,
      authApiBaseUrl: MOUNT_AUTH_API,
    };

    // Trailing slash normalised away so the derived and configured shapes match.
    expect(getBackendBaseUrl("main")).toBe("/ignite/coder/@u/w/apps/main-api");
    expect(getBackendBaseUrl("auth")).toBe("/ignite/coder/@u/w/apps/auth-api");
    expect(getBackendUrl("main", "/api")).toBe(
      "/ignite/coder/@u/w/apps/main-api/api",
    );
    expect(getBackendUrl("auth", "/api/Auth/Login")).toBe(
      "/ignite/coder/@u/w/apps/auth-api/api/Auth/Login",
    );
  });

  it("keeps an absolute API URL intact instead of mangling it into a path", () => {
    window.__APP_TEMPLATE_CONFIG__ = {
      mainApiBaseUrl: "https://api.example.edu/main/",
    };

    expect(getBackendBaseUrl("main")).toBe("https://api.example.edu/main");
    expect(getBackendUrl("main", "/api")).toBe(
      "https://api.example.edu/main/api",
    );
  });

  it("REGRESSION: the API mount is wrong under a deep mount without the override", () => {
    setPathname(MOUNT_MAIN);

    // Without configuration the app asks for "/ignite/api-main/api" — a 404, because the
    // API actually lives at the sibling mount asserted below.
    expect(getBackendUrl("main", "/api")).toBe("/ignite/api-main/api");

    window.__APP_TEMPLATE_CONFIG__ = { mainApiBaseUrl: MOUNT_MAIN_API };
    expect(getBackendUrl("main", "/api")).toBe(
      "/ignite/coder/@u/w/apps/main-api/api",
    );
  });
});

describe("getFrontendUrl", () => {
  it("keeps the local dev ports when nothing is overridden", () => {
    expect(getFrontendUrl("main")).toBe("http://localhost:8002/");
    expect(getFrontendUrl("auth")).toBe("http://localhost:8001/login/");
  });

  it("uses the explicit app URLs when runtime config supplies them", () => {
    window.__APP_TEMPLATE_CONFIG__ = {
      mainAppUrl: MOUNT_MAIN,
      authAppUrl: MOUNT_AUTH,
    };

    // The override wins even on localhost: the two apps are only siblings under a shared
    // prefix by convention, and behind a per-app proxy mount they are not.
    expect(getFrontendUrl("main")).toBe(MOUNT_MAIN);
    expect(getFrontendUrl("auth")).toBe(MOUNT_AUTH);
  });

  it("always ends an overridden app URL with a slash", () => {
    window.__APP_TEMPLATE_CONFIG__ = {
      authAppUrl: "https://accounts.example.edu",
    };

    expect(getFrontendUrl("auth")).toBe("https://accounts.example.edu/");
  });
});

describe("getFrontendAssetUrl", () => {
  it("resolves assets against the derived base path by default", () => {
    expect(getFrontendAssetUrl("/app-logo.svg")).toBe("/app-logo.svg");

    setPathname("/MYAPP/vendors");
    expect(getFrontendAssetUrl("/app-logo.svg")).toBe("/MYAPP/app-logo.svg");
  });

  it("resolves assets against an overridden base path", () => {
    window.__APP_TEMPLATE_CONFIG__ = { appBasePath: MOUNT_MAIN };

    expect(getFrontendAssetUrl("/app-logo.svg")).toBe(
      "/ignite/coder/@u/w/apps/preview-main/app-logo.svg",
    );
  });
});

describe("FRONTEND_CONSTANTS", () => {
  it("uses the template's default cookie names when nothing is configured", () => {
    expect(FRONTEND_CONSTANTS.cookies.session).toBe("AppTemplate-SessionToken");
    expect(FRONTEND_CONSTANTS.cookies.user).toBe("AppTemplate-User");
    expect(FRONTEND_CONSTANTS.cookies.domain).toBeUndefined();
  });

  it("honours configured cookie names", async () => {
    // Isolation, not cosmetics: several workspaces are served from ONE public host, so a
    // fixed cookie name lets two of them overwrite each other's session in one browser.
    window.__APP_TEMPLATE_CONFIG__ = {
      sessionCookieName: "NieIgniteWorkspace42-SessionToken",
      userCookieName: "NieIgniteWorkspace42-User",
      cookieDomain: "example.edu",
    };

    const { FRONTEND_CONSTANTS: constants, getCookieAttributes } =
      await loadConstantsFresh();

    expect(constants.cookies.session).toBe("NieIgniteWorkspace42-SessionToken");
    expect(constants.cookies.user).toBe("NieIgniteWorkspace42-User");
    expect(getCookieAttributes()).toEqual({ domain: "example.edu" });
  });

  it("builds every URL from the overrides on a real page load", async () => {
    setPathname(MOUNT_MAIN);
    window.__APP_TEMPLATE_CONFIG__ = {
      appBasePath: MOUNT_MAIN,
      mainApiBaseUrl: MOUNT_MAIN_API,
      authApiBaseUrl: MOUNT_AUTH_API,
      mainAppUrl: MOUNT_MAIN,
      authAppUrl: MOUNT_AUTH,
    };

    const { FRONTEND_CONSTANTS: constants } = await loadConstantsFresh();

    expect(constants.api.main).toBe("/ignite/coder/@u/w/apps/main-api/api");
    expect(constants.api.auth).toBe("/ignite/coder/@u/w/apps/auth-api/api");
    expect(constants.backend.main).toBe("/ignite/coder/@u/w/apps/main-api");
    expect(constants.backend.auth).toBe("/ignite/coder/@u/w/apps/auth-api");
    expect(constants.apps.main).toBe(MOUNT_MAIN);
    expect(constants.apps.auth).toBe(MOUNT_AUTH);
  });

  it("falls back to exactly the pre-existing values with no runtime config", async () => {
    const { FRONTEND_CONSTANTS: constants } = await loadConstantsFresh();

    // Guards the "purely additive" promise: an unconfigured build must be unchanged.
    expect(constants.api).toEqual({
      auth: "/api-auth/api",
      main: "/api-main/api",
    });
    expect(constants.backend).toEqual({ auth: "/api-auth", main: "/api-main" });
    expect(constants.apps).toEqual({
      auth: "http://localhost:8001/login/",
      main: "http://localhost:8002/",
    });
    expect(constants.cookies.session).toBe("AppTemplate-SessionToken");
    expect(constants.cookies.user).toBe("AppTemplate-User");
  });
});

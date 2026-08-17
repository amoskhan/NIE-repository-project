// EXAMPLE UNIT TEST — a service that talks HTTP, tested WITHOUT a server.
//
// Two techniques worth stealing:
//   1. vi.mock() replaces a whole module, so this test never loads the real
//      @apptemplate/shared barrel (Sentry, OpenTelemetry, ...) just to check a URL.
//   2. a stubbed global.fetch lets us assert exactly what was sent and script exactly
//      what comes back — including the failure paths a live backend rarely produces.

import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

vi.mock("@apptemplate/shared", () => ({
  getBackendUrl: (_service: string, path = "") => `/api-auth${path}`,
  getRuntimeEnvironment: () => "local",
}));

import {
  forgotPassword,
  getExternalProviders,
  isDevelopmentEnvironment,
  login,
} from "./authApi";

/** Build a minimal stand-in for the Response object fetch would resolve with. */
function jsonResponse(status: number, body: unknown): Response {
  return {
    ok: status >= 200 && status < 300,
    status,
    json: async () => body,
  } as Response;
}

const fetchMock = vi.fn();

beforeEach(() => {
  fetchMock.mockReset();
  vi.stubGlobal("fetch", fetchMock);
});

afterEach(() => {
  vi.unstubAllGlobals();
});

describe("login", () => {
  it("posts the fixed { userid, pd } wire shape", async () => {
    fetchMock.mockResolvedValue(
      jsonResponse(200, { isAuthenticated: true, sessionToken: "abc" }),
    );

    const result = await login("admin", "Admin@12345");

    expect(fetchMock).toHaveBeenCalledTimes(1);
    const [url, init] = fetchMock.mock.calls[0];
    expect(url).toBe("/api-auth/api/Auth/Login");
    expect(init.method).toBe("POST");
    // The whole stack (Main API middleware, Playwright specs) depends on these two
    // field names. If this assertion ever fails, it is the change that is wrong.
    expect(JSON.parse(init.body)).toEqual({
      userid: "admin",
      pd: "Admin@12345",
    });
    expect(result.ok).toBe(true);
    expect(result.data?.sessionToken).toBe("abc");
  });

  it("surfaces the server message on a 401", async () => {
    fetchMock.mockResolvedValue(
      jsonResponse(401, { isAuthenticated: false, message: "Account locked." }),
    );

    const result = await login("admin", "wrong");

    expect(result.ok).toBe(false);
    expect(result.status).toBe(401);
    expect(result.message).toBe("Account locked.");
  });

  it("reports a friendly message when the network fails", async () => {
    fetchMock.mockRejectedValue(new Error("connection refused"));

    const result = await login("admin", "Admin@12345");

    expect(result.ok).toBe(false);
    expect(result.status).toBe(0);
    expect(result.message).toContain("Could not reach");
  });
});

describe("forgotPassword", () => {
  it("posts the userIdOrEmail field the backend binds", async () => {
    fetchMock.mockResolvedValue(jsonResponse(200, { success: true }));

    await forgotPassword("jane@example.edu");

    const [url, init] = fetchMock.mock.calls[0];
    expect(url).toBe("/api-auth/api/Auth/ForgotPassword");
    // ForgotPasswordRequest.UserIdOrEmail. Posting { email } binds to nothing and 400s.
    expect(JSON.parse(init.body)).toEqual({
      userIdOrEmail: "jane@example.edu",
    });
  });
});

describe("getExternalProviders", () => {
  it("returns the advertised providers", async () => {
    const provider = {
      name: "university",
      displayName: "University SSO",
      startUrl: "/api/Auth/ExternalStart?provider=university",
    };
    fetchMock.mockResolvedValue(jsonResponse(200, [provider]));

    await expect(getExternalProviders()).resolves.toEqual([provider]);
  });

  it("returns an empty list when the optional endpoint is unavailable", async () => {
    // The external IdP slot ships DISABLED. A 404/500 here must never break password
    // login — the login page simply renders no provider buttons.
    fetchMock.mockResolvedValue(jsonResponse(404, { message: "Not found" }));

    await expect(getExternalProviders()).resolves.toEqual([]);
  });

  it("drops malformed entries rather than rendering a broken button", async () => {
    // A provider with no startUrl has nowhere to send the browser, so it is unusable.
    fetchMock.mockResolvedValue(
      jsonResponse(200, [
        { displayName: "Nameless", startUrl: "/api/Auth/ExternalStart" },
        { name: "no-start-url", displayName: "Dead end" },
        { name: "ok", startUrl: "/api/Auth/ExternalStart?provider=ok" },
      ]),
    );

    await expect(getExternalProviders()).resolves.toEqual([
      { name: "ok", startUrl: "/api/Auth/ExternalStart?provider=ok" },
    ]);
  });
});

describe("isDevelopmentEnvironment", () => {
  it("is true for a local runtime environment", () => {
    // Drives the seeded-credentials hint on the login page, which must never show up
    // in staging or production.
    expect(isDevelopmentEnvironment()).toBe(true);
  });
});

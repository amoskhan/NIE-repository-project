// PROJECT-OWNED — safe to edit.
//
// Thin, dependency-free client for the LOCAL identity provider exposed by the Auth API
// (src/backend/Auth/Controllers/AuthController.cs).
//
// Every call goes through getBackendUrl("auth", ...) so the same build works locally
// (Vite proxies /api-auth -> http://localhost:5001) and behind nginx in a deployment,
// where the app may be served from a sub-path. Never hardcode an absolute API URL here.

import { getBackendUrl, getRuntimeEnvironment } from "@apptemplate/shared";

/** Shape returned by POST /api/Auth/Login on success (IssuedLoginResponse). */
export interface IssuedLogin {
  isAuthenticated: boolean;
  userId?: string;
  userName?: string;
  fullName?: string;
  email?: string;
  department?: string;
  sessionToken?: string;
  /** Optional extras the Auth API may include; used to seed the user cookie. */
  role?: unknown;
  roles?: Array<{ RoleName?: string | null } | string>;
  permissions?: string[];
}

/**
 * One entry from GET /api/Auth/ExternalProviders (backend: ExternalProviderSummary).
 * An EMPTY list means "hide the section".
 *
 * These three fields are ALL the backend sends — do not add speculative ones here
 * (an icon field, for example, would silently render nothing).
 */
export interface ExternalProvider {
  /** Stable key echoed back as ?provider= to GET /api/Auth/ExternalStart. */
  name: string;
  /** Human-readable button label, e.g. "Continue with University SSO". */
  displayName: string;
  /**
   * Backend-supplied entry point for the redirect handshake, relative to the Auth API
   * (e.g. "/api/Auth/ExternalStart?provider=university"). Use it as given — never
   * reassemble it here, or the two will drift.
   */
  startUrl: string;
}

/** Uniform result wrapper so callers never have to touch Response directly. */
export interface ApiResult<T> {
  ok: boolean;
  status: number;
  data: T | null;
  /** Server-supplied message, or a friendly fallback when the network failed. */
  message: string;
}

/** Payload for POST /api/Auth/Register. */
export interface RegisterRequest {
  userId: string;
  email: string;
  fullName: string;
  password: string;
  department?: string;
}

/**
 * Payload for POST /api/Auth/ResetPassword.
 * The token is single-use AND self-identifying — the backend looks the account up from
 * the token hash alone, so no user id is sent (and none should be added).
 */
export interface ResetPasswordRequest {
  token: string;
  newPassword: string;
}

const GENERIC_ERROR = "Something went wrong. Please try again.";
const NETWORK_ERROR = "Could not reach the sign-in service. Please try again.";

/** Pull the best available human-readable message out of an error body. */
function readMessage(body: unknown, fallback: string): string {
  if (!body || typeof body !== "object") {
    return fallback;
  }

  const record = body as Record<string, unknown>;
  for (const key of ["message", "detail", "title", "errorMessage"]) {
    const value = record[key];
    if (typeof value === "string" && value.trim()) {
      return value.trim();
    }
  }

  return fallback;
}

async function readBody(response: Response): Promise<unknown> {
  // 204s and HTML error pages must not blow up the caller.
  try {
    return await response.json();
  } catch {
    return null;
  }
}

async function request<T>(
  path: string,
  init: RequestInit,
  fallbackError = GENERIC_ERROR,
): Promise<ApiResult<T>> {
  try {
    const response = await fetch(getBackendUrl("auth", path), {
      credentials: "include",
      ...init,
      headers: {
        "Content-Type": "application/json",
        ...(init.headers ?? {}),
      },
    });

    const data = await readBody(response);

    return {
      ok: response.ok,
      status: response.status,
      data: (data as T) ?? null,
      message: response.ok ? "" : readMessage(data, fallbackError),
    };
  } catch {
    return { ok: false, status: 0, data: null, message: NETWORK_ERROR };
  }
}

function postJson<T>(
  path: string,
  body: unknown,
  fallbackError = GENERIC_ERROR,
): Promise<ApiResult<T>> {
  return request<T>(
    path,
    { method: "POST", body: JSON.stringify(body) },
    fallbackError,
  );
}

/**
 * POST /api/Auth/Login — the ONE wire shape the whole stack depends on.
 * The field names are `userid` / `pd`; do not "tidy" them without changing the
 * backend, the Main API session middleware and the Playwright specs together.
 */
export function login(
  userid: string,
  pd: string,
): Promise<ApiResult<IssuedLogin>> {
  return postJson<IssuedLogin>(
    "/api/Auth/Login",
    { userid, pd },
    "Login failed. Please check your credentials.",
  );
}

/** POST /api/Auth/Logout — revokes the session server-side. Safe to call with a stale token. */
export function logout(sessionToken: string): Promise<ApiResult<unknown>> {
  return request(
    "/api/Auth/Logout",
    {
      method: "POST",
      body: JSON.stringify(sessionToken),
      headers: { "X-Session-Id": sessionToken },
    },
    "",
  );
}

/** POST /api/Auth/Register — creates a local account. */
export function register(
  payload: RegisterRequest,
): Promise<ApiResult<{ success?: boolean }>> {
  return postJson(
    "/api/Auth/Register",
    payload,
    "Registration failed. Please review the form and try again.",
  );
}

/**
 * POST /api/Auth/ForgotPassword — always answers 200 so the response cannot be used to
 * probe which accounts exist. Treat any non-200 as an infrastructure problem.
 *
 * The field name is `userIdOrEmail`, matching ForgotPasswordRequest on the backend: the
 * endpoint accepts EITHER a username or an email address. Sending `email` binds to
 * nothing and 400s.
 */
export function forgotPassword(
  userIdOrEmail: string,
): Promise<ApiResult<{ success?: boolean }>> {
  return postJson("/api/Auth/ForgotPassword", { userIdOrEmail });
}

/** POST /api/Auth/ResetPassword — consumes a single-use token from the reset email. */
export function resetPassword(
  payload: ResetPasswordRequest,
): Promise<ApiResult<{ success?: boolean }>> {
  return postJson(
    "/api/Auth/ResetPassword",
    payload,
    "This reset link is invalid or has expired. Please request a new one.",
  );
}

/**
 * GET /api/Auth/ExternalProviders — the OPTIONAL external OIDC slot.
 * Ships disabled, so this normally returns []. Callers must render nothing when the
 * list is empty, and must not treat a failure here as a login failure.
 */
export async function getExternalProviders(): Promise<ExternalProvider[]> {
  const result = await request<ExternalProvider[]>(
    "/api/Auth/ExternalProviders",
    {
      method: "GET",
    },
  );

  if (!result.ok || !Array.isArray(result.data)) {
    return [];
  }

  return result.data.filter(
    (provider) =>
      provider &&
      typeof provider.name === "string" &&
      provider.name &&
      typeof provider.startUrl === "string" &&
      provider.startUrl,
  );
}

/**
 * Full-page navigation to the provider's own `startUrl`, which redirects to the external
 * IdP. This is a browser redirect, not fetch(): the IdP needs to own the address bar.
 *
 * `startUrl` is Auth-API-relative (it already carries ?provider=), so it is resolved
 * through getBackendUrl() exactly like every other call in this file; only `returnUrl`
 * is added here, because only the browser knows where the user should land afterwards.
 */
export function startExternalLogin(
  provider: ExternalProvider,
  returnUrl: string,
): void {
  const separator = provider.startUrl.indexOf("?");
  const path =
    separator < 0 ? provider.startUrl : provider.startUrl.slice(0, separator);
  const query = separator < 0 ? "" : provider.startUrl.slice(separator + 1);

  const params = new URLSearchParams(query);
  params.set("returnUrl", returnUrl);

  // Absolute URLs are passed through untouched; relative ones go through the same
  // base-path resolution as every other Auth API call.
  const base = /^https?:\/\//i.test(path) ? path : getBackendUrl("auth", path);
  window.location.href = `${base}?${params.toString()}`;
}

/**
 * True when this bundle is serving a local or dev deployment. Drives the seeded-account
 * hint on the login page — it must never appear in staging or production.
 */
export function isDevelopmentEnvironment(): boolean {
  const environment = getRuntimeEnvironment();
  return environment === "local" || environment === "dev";
}

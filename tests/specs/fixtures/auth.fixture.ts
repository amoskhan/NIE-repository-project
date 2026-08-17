/**
 * Authentication Fixture
 * Provides authenticated context for tests using API-based session creation
 */

import { Page, BrowserContext } from "@playwright/test";
import { createAuthApiClient, LoginResponse } from "./api-client";
import { CookieNames } from "./cookie-names";
import { ApiEndpoints } from "./test-config";
import { getTestUser } from "./test-users";

export interface AuthSession {
  sessionToken: string;
  userId: string;
  userName: string;
  email: string;
  roles?: string[];
  permissions?: string[];
}

/**
 * Request body for POST /api/Auth/Login.
 * These field names are the wire contract — do not rename them.
 */
export interface LoginRequest {
  userid: string;
  pd: string;
}

/**
 * Response from the test session API
 */
export interface TestSessionResponse {
  success: boolean;
  sessionToken?: string;
  userId?: string;
  userName?: string;
  email?: string;
  errorMessage?: string;
}

/**
 * Login with credentials and return session
 */
export async function login(
  username: string,
  password: string,
): Promise<AuthSession | null> {
  const client = createAuthApiClient();
  await client.init();

  try {
    // The Auth API's login contract uses `userid` / `pd`, not `username` / `password`.
    const response = await client.post<LoginResponse>(ApiEndpoints.auth.login, {
      userid: username,
      pd: password,
    });

    if (response.status === 200 && response.data.isAuthenticated) {
      return {
        sessionToken: response.data.sessionToken,
        userId: response.data.userId,
        userName: response.data.fullName || response.data.userName,
        email: response.data.email,
        roles: response.data.roles,
        permissions: response.data.permissions,
      };
    }

    console.error(
      "Login failed:",
      response.data.errorMessage ||
        response.data.message ||
        `Status: ${response.status}`,
    );
    return null;
  } catch (error) {
    console.error("Login error:", error);
    return null;
  } finally {
    await client.dispose();
  }
}

/**
 * Login with the default test user
 */
export async function loginWithTestUser(): Promise<AuthSession | null> {
  const user = getTestUser();
  return login(user.username, user.password);
}

/**
 * Create a test session via API (bypasses normal login).
 * Useful when a spec needs an authenticated browser without exercising the login form.
 * Note: the Auth API only exposes CreateTestSession in the Development environment.
 */
export async function createTestSession(
  userId?: string,
  name?: string,
): Promise<AuthSession | null> {
  const client = createAuthApiClient();
  await client.init();

  const user = getTestUser();

  try {
    const response = await client.post<TestSessionResponse>(
      ApiEndpoints.auth.createTestSession,
      {
        userId: userId || user.username,
        name: name || user.name || "Test User",
        email: user.email,
      },
    );

    if (response.status === 200 && response.data.success) {
      return {
        sessionToken: response.data.sessionToken!,
        userId: response.data.userId!,
        userName: response.data.userName!,
        email: response.data.email!,
      };
    }

    console.error(
      "Failed to create test session:",
      response.data.errorMessage || `Status: ${response.status}`,
    );
    return null;
  } catch (error) {
    console.error("Create test session error:", error);
    return null;
  } finally {
    await client.dispose();
  }
}

/**
 * Create a default test session using the default test user
 */
export async function createDefaultTestSession(): Promise<AuthSession | null> {
  // First try to login normally
  const session = await loginWithTestUser();
  if (session) {
    return session;
  }

  // If normal login fails, try to create a test session
  // (requires CreateTestSession endpoint)
  return createTestSession();
}

/**
 * Set authentication cookies in the browser context.
 *
 * Uses the CANONICAL cookie names from
 * src/frontend/packages/shared/src/config/constants.ts. The main app's router reads
 * `AppTemplate-SessionToken` to decide whether a visitor is signed in, and parses
 * `AppTemplate-User` (JSON) for roles and permissions — so the shape below has to match
 * what the auth app writes on a real login, not just the names.
 */
export async function setAuthCookies(
  context: BrowserContext,
  session: AuthSession,
  domain = "localhost",
): Promise<void> {
  const roles = session.roles ?? [];

  await context.addCookies([
    {
      name: CookieNames.session,
      value: session.sessionToken,
      domain,
      path: "/",
    },
    {
      name: CookieNames.user,
      value: JSON.stringify({
        userId: session.userId,
        fullName: session.userName,
        email: session.email,
        department: "",
        roles,
        roleNames: roles,
        permissions: session.permissions ?? [],
      }),
      domain,
      path: "/",
    },
  ]);
}

/**
 * Get an authenticated page with session cookies already set
 */
export async function getAuthenticatedPage(
  page: Page,
  context: BrowserContext,
  session: AuthSession,
): Promise<Page> {
  await setAuthCookies(context, session);
  return page;
}

/**
 * Clear authentication cookies from the browser context
 */
export async function clearAuthCookies(context: BrowserContext): Promise<void> {
  await context.clearCookies();
}

export default {
  login,
  loginWithTestUser,
  createTestSession,
  createDefaultTestSession,
  setAuthCookies,
  getAuthenticatedPage,
  clearAuthCookies,
};

/**
 * Authentication Fixture
 * Provides authenticated context for tests using API-based session creation
 */

import { Page, BrowserContext } from "@playwright/test";
import { ApiClient, createAuthApiClient, LoginResponse } from "./api-client";
import { TestConfig, ApiEndpoints } from "./test-config";
import { getTestUser, TestUser } from "./test-users";

export interface AuthSession {
  sessionToken: string;
  userId: string;
  userName: string;
  email: string;
  roles?: string[];
}

/**
 * Request body for login
 */
export interface LoginRequest {
  username: string;
  password: string;
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
    const response = await client.post<LoginResponse>(ApiEndpoints.auth.login, {
      username,
      password,
    });

    if (response.status === 200 && response.data.isAuthenticated) {
      return {
        sessionToken: response.data.sessionToken,
        userId: response.data.userId,
        userName: response.data.userName,
        email: response.data.email,
        roles: response.data.roles,
      };
    }

    console.error(
      "Login failed:",
      response.data.errorMessage || `Status: ${response.status}`,
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
 * Create a test session via API (bypasses normal login)
 * This is used for automated testing when 2FA or SSO is in place
 * Note: Requires a CreateTestSession endpoint in the Auth API
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
 * Set authentication cookies in the browser context
 */
export async function setAuthCookies(
  context: BrowserContext,
  session: AuthSession,
): Promise<void> {
  await context.addCookies([
    {
      name: "SessionToken",
      value: session.sessionToken,
      domain: "localhost",
      path: "/",
    },
    {
      name: "UserId",
      value: session.userId,
      domain: "localhost",
      path: "/",
    },
    {
      name: "UserName",
      value: session.userName,
      domain: "localhost",
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

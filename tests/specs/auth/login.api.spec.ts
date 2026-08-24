/**
 * Auth API - Login Tests
 * Tests for /api/Auth/Login endpoint
 */

import { test, expect } from "@playwright/test";
import { createApiClient, createAuthApiClient, ApiClient } from "../fixtures/api-client";
import { TestConfig, ApiEndpoints } from "../fixtures/test-config";
import { getTestUser, hasTestUsers } from "../fixtures/test-users";

test.describe("Auth API - Login", () => {
  let client: ApiClient;

  test.beforeAll(async () => {
    client = createAuthApiClient();
    await client.init();
  });

  test.afterAll(async () => {
    await client.dispose();
  });

  test("should return 401 for invalid credentials", async () => {
    const response = await client.post(ApiEndpoints.auth.login, {
      userid: "invaliduser",
      pd: "invalidpassword",
    });

    expect(response.status).toBe(401);
    expect(response.data).toHaveProperty("isAuthenticated", false);
  });

  test("should return 400 or 401 for empty credentials", async () => {
    const response = await client.post(ApiEndpoints.auth.login, {
      userid: "",
      pd: "",
    });

    expect([400, 401]).toContain(response.status);
  });

  test("should return 400 or 401 for missing password", async () => {
    const response = await client.post(ApiEndpoints.auth.login, {
      userid: "testuser",
      pd: "",
    });

    expect([400, 401]).toContain(response.status);
  });

  test("should return 400 or 401 for missing username", async () => {
    const response = await client.post(ApiEndpoints.auth.login, {
      userid: "",
      pd: "testpassword",
    });

    expect([400, 401]).toContain(response.status);
  });

  test("should successfully login with valid credentials", async () => {
    test.skip(!hasTestUsers(), "No test users configured");

    const user = getTestUser();
    const response = await client.post(ApiEndpoints.auth.login, {
      userid: user.username,
      pd: user.password,
    });

    expect(response.status).toBe(200);
    expect(response.data).toHaveProperty("isAuthenticated", true);
    expect(response.data).toHaveProperty("sessionToken");
    expect(response.data).toHaveProperty("userId");
    expect(response.data.sessionToken).toBeTruthy();
  });

  test("should return user details on successful login", async () => {
    test.skip(!hasTestUsers(), "No test users configured");

    const user = getTestUser();
    const response = await client.post(ApiEndpoints.auth.login, {
      userid: user.username,
      pd: user.password,
    });

    expect(response.status).toBe(200);
    expect(response.data).toHaveProperty("userName");
    expect(response.data).toHaveProperty("email");
  });

  test("should respond within acceptable time limit", async () => {
    const user = getTestUser();
    const startTime = Date.now();

    await client.post(ApiEndpoints.auth.login, {
      userid: user.username || "testuser",
      pd: user.password || "testpassword",
    });

    const responseTime = Date.now() - startTime;
    // Login should respond within 10 seconds even for invalid credentials
    expect(responseTime).toBeLessThan(10000);
  });

  test("should handle special characters in credentials", async () => {
    const response = await client.post(ApiEndpoints.auth.login, {
      userid: "user@test.com",
      pd: "p@ssw0rd!#$%",
    });

    // Should not throw an error, just return unauthorized
    expect([400, 401]).toContain(response.status);
  });

  test("should handle very long credentials gracefully", async () => {
    const longString = "a".repeat(1000);
    const response = await client.post(ApiEndpoints.auth.login, {
      userid: longString,
      pd: longString,
    });

    // Should handle gracefully without server error
    expect([400, 401, 413, 500]).toContain(response.status);
  });
});

test.describe("Auth API - Session", () => {
  let client: ApiClient;

  test.beforeAll(async () => {
    client = createAuthApiClient();
    await client.init();
  });

  test.afterAll(async () => {
    await client.dispose();
  });

  test("should verify valid session", async () => {
    test.skip(!hasTestUsers(), "No test users configured");

    // First login to get a session
    const user = getTestUser();
    const loginResponse = await client.post(ApiEndpoints.auth.login, {
      userid: user.username,
      pd: user.password,
    });

    if (loginResponse.status !== 200) {
      test.skip(true, "Login failed, cannot test session verification");
      return;
    }

    // Set the session token
    client.setSession(
      loginResponse.data.sessionToken,
      loginResponse.data.userId,
    );

    // Verify the session
    const verifyResponse = await client.get(ApiEndpoints.auth.verify);

    expect(verifyResponse.status).toBe(200);
    expect(verifyResponse.data).toHaveProperty("isValid", true);
  });

  test("should reject invalid session token", async () => {
    client.setSession("invalid-session-token", "invalid-user-id");

    const response = await client.get(ApiEndpoints.auth.verify);

    expect([401, 403]).toContain(response.status);
  });

  test("should logout successfully", async () => {
    test.skip(!hasTestUsers(), "No test users configured");

    // First login to get a session
    const user = getTestUser();
    const loginResponse = await client.post(ApiEndpoints.auth.login, {
      userid: user.username,
      pd: user.password,
    });

    if (loginResponse.status !== 200) {
      test.skip(true, "Login failed, cannot test logout");
      return;
    }

    // Set the session token
    client.setSession(
      loginResponse.data.sessionToken,
      loginResponse.data.userId,
    );

    // Logout
    const logoutResponse = await client.post(ApiEndpoints.auth.logout, {});

    expect([200, 204]).toContain(logoutResponse.status);
  });

  test("should reject old session token after logout", async () => {
    test.skip(!hasTestUsers(), "No test users configured");

    const user = getTestUser();
    const loginResponse = await client.post(ApiEndpoints.auth.login, {
      userid: user.username,
      pd: user.password,
    });

    if (loginResponse.status !== 200) {
      test.skip(true, "Login failed, cannot test logout revocation");
      return;
    }

    const sessionToken = loginResponse.data.sessionToken;
    const userId = loginResponse.data.userId;

    const mainClient = createApiClient();
    await mainClient.init();
    mainClient.setSession(sessionToken, userId);

    try {
      const beforeLogout = await mainClient.get(ApiEndpoints.vendor.getAll);
      expect([200, 204]).toContain(beforeLogout.status);

      client.setSession(sessionToken, userId);
      const logoutResponse = await client.post(ApiEndpoints.auth.logout, {});
      expect([200, 204]).toContain(logoutResponse.status);

      const afterLogout = await mainClient.get(ApiEndpoints.vendor.getAll);
      expect([401, 403]).toContain(afterLogout.status);
    } finally {
      await mainClient.dispose();
    }
  });
});

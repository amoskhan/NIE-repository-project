/**
 * Auth API - Portal SSO Tests
 * Verifies the new dual-path auth endpoints are present and predictable.
 */

import { expect, test } from "@playwright/test";
import { ApiClient, createAuthApiClient } from "../fixtures/api-client";
import { ApiEndpoints, TestConfig } from "../fixtures/test-config";

test.describe("Auth API - Portal SSO", () => {
  let client: ApiClient;

  test.beforeAll(async () => {
    client = createAuthApiClient();
    await client.init();
  });

  test.afterAll(async () => {
    await client.dispose();
  });

  test("should expose the SSO start endpoint", async () => {
    const response = await client.get(ApiEndpoints.auth.ssoStart, {
      returnUrl: TestConfig.frontendAuth,
    });

    if (TestConfig.portalSsoExpectedEnabled) {
      expect(response.status).toBe(200);
      expect(response.data).toHaveProperty("state");
      expect(response.data).toHaveProperty("nonce");
      expect(response.data).toHaveProperty("launchUrl");
    } else {
      expect(response.status).toBe(503);
    }
  });

  test("should reject SSO finalize for an unknown state", async () => {
    const response = await client.get(ApiEndpoints.auth.ssoFinalize, {
      state: "missing-state",
    });

    if (TestConfig.portalSsoExpectedEnabled) {
      expect(response.status).toBe(401);
      expect(response.data).toHaveProperty("message");
    } else {
      expect(response.status).toBe(503);
    }
  });

  test("should reject malformed SSO callback requests", async () => {
    const response = await client.post(ApiEndpoints.auth.ssoCallback, {
      state: "missing-state",
      encryptedPayload: "not-a-token",
    });

    if (TestConfig.portalSsoExpectedEnabled) {
      expect([400, 401]).toContain(response.status);
    } else {
      expect(response.status).toBe(503);
    }
  });
});

/**
 * Auth API - Local Identity Provider
 *
 * The template authenticates against its OWN user store: there is no external gateway
 * to stand in front of these tests. That makes the whole sign-in lifecycle testable end
 * to end against a locally running Auth API + Valkey.
 *
 * Covered here:
 *   - sign in with the seeded development account
 *   - a wrong password is rejected with 401
 *   - repeated failures lock the account
 *   - forgot-password answers identically for known and unknown accounts
 *   - register accepts fullName and it survives to the sign-in response
 *   - the full reset lifecycle: forgot -> token -> reset -> sign in, and no replay
 *   - logout revokes the session, and Verify then rejects the token
 *   - the optional external-provider slot ships disabled
 *
 * Prerequisites: a running Auth API on AUTH_API_URL, backed by Valkey and a seeded
 * database. The seeded credentials come from SEED_ADMIN_USERNAME / SEED_ADMIN_PASSWORD
 * (see tests/.env.dev), so a differently-seeded environment needs no code changes.
 */

import { expect, test } from "@playwright/test";
import { ApiClient, createAuthApiClient } from "../fixtures/api-client";
import { ApiEndpoints, TestConfig } from "../fixtures/test-config";

/** POST /api/Auth/Login body — the field names are the wire contract. */
function credentials(userid: string, pd: string) {
  return { userid, pd };
}

/** Unique-per-run identifier so reruns never collide on an existing account. */
function uniqueSuffix(): string {
  return `${Date.now()}${Math.floor(Math.random() * 1000)}`;
}

/**
 * Strips the Development-only reset token so two forgot-password bodies can be compared
 * for everything the endpoint is allowed to vary — which is nothing.
 */
function withoutDevelopmentToken(body: unknown): Record<string, unknown> {
  const { developmentToken: _ignored, ...rest } = (body ?? {}) as Record<
    string,
    unknown
  >;
  return rest;
}

test.describe("Auth API - local identity provider", () => {
  let client: ApiClient;

  test.beforeEach(async () => {
    // A fresh client per test: setSession() mutates client state, and sharing it
    // between tests makes failures depend on execution order.
    client = createAuthApiClient();
    await client.init();
  });

  test.afterEach(async () => {
    await client.dispose();
  });

  test("signs in with the seeded development account", async () => {
    const response = await client.post(
      ApiEndpoints.auth.login,
      credentials(TestConfig.seedUsername, TestConfig.seedPassword),
    );

    expect(response.status).toBe(200);
    expect(response.data).toHaveProperty("isAuthenticated", true);
    // The frontend writes sessionToken straight into the AppTemplate-SessionToken
    // cookie, and every API reads it back as the X-Session-Id header.
    expect(response.data.sessionToken).toBeTruthy();
    expect(response.data.userId).toBeTruthy();
    expect(response.data).toHaveProperty("email");
  });

  test("rejects a wrong password with 401", async () => {
    const response = await client.post(
      ApiEndpoints.auth.login,
      credentials(TestConfig.seedUsername, "definitely-not-the-password"),
    );

    expect(response.status).toBe(401);
    expect(response.data).toHaveProperty("isAuthenticated", false);
    // A failed sign-in must never leak a session token.
    expect(response.data?.sessionToken).toBeFalsy();
  });

  test("does not reveal whether an unknown username exists", async () => {
    const unknown = await client.post(
      ApiEndpoints.auth.login,
      credentials(`ghost-${uniqueSuffix()}`, "definitely-not-the-password"),
    );
    const wrongPassword = await client.post(
      ApiEndpoints.auth.login,
      credentials(TestConfig.seedUsername, "definitely-not-the-password"),
    );

    // Same status for "no such user" and "bad password" — otherwise the login form
    // becomes a username oracle.
    expect(unknown.status).toBe(401);
    expect(wrongPassword.status).toBe(401);
  });

  test("answers forgot-password identically for a known and an unknown account", async () => {
    // The field name is the wire contract: ForgotPasswordRequest.UserIdOrEmail accepts
    // EITHER the login name or the email address. Posting anything else binds to null
    // and the endpoint 400s on [Required] — which would still "not enumerate", but for
    // the wrong reason, so the assertions below would pass against a broken endpoint.
    const known = await client.post(ApiEndpoints.auth.forgotPassword, {
      userIdOrEmail: TestConfig.seedUsername,
    });
    const unknown = await client.post(ApiEndpoints.auth.forgotPassword, {
      userIdOrEmail: `nobody-${uniqueSuffix()}@example.edu`,
    });

    expect(known.status).toBe(200);
    expect(unknown.status).toBe(200);
    expect(known.data?.success).toBe(true);
    expect(unknown.data?.success).toBe(true);

    // Same wording too. A different message for "no such account" would turn this
    // endpoint into an account-enumeration oracle just as surely as a different status.
    expect(known.data?.message).toBe(unknown.data?.message);

    // ...and nothing else differs either. developmentToken is the ONE documented
    // exception: AuthController echoes the raw token only when the environment is
    // Development, so students can finish a reset without an email server. Everything
    // outside that field must match exactly.
    expect(withoutDevelopmentToken(known.data)).toEqual(
      withoutDevelopmentToken(unknown.data),
    );
    // The escape hatch must never leak for an account that does not exist.
    expect(unknown.data?.developmentToken).toBeUndefined();
  });

  test("accepts fullName on register and returns it on sign-in", async () => {
    // Guards the Register wire contract. RegisterRequest exposes `fullName`; if the
    // backend ever renames that property, this body binds it to null, the account falls
    // back to using the login name as its display name, and the assertion below fails.
    const userId = `fullname-${uniqueSuffix()}`;
    const password = "FullName@12345";
    const fullName = "Wire Contract Tester";

    const registerResponse = await client.post(ApiEndpoints.auth.register, {
      userId,
      email: `${userId}@example.edu`,
      fullName,
      password,
    });

    test.skip(
      ![200, 201].includes(registerResponse.status),
      "Self-service registration is disabled in this environment",
    );

    const loginResponse = await client.post(
      ApiEndpoints.auth.login,
      credentials(userId, password),
    );

    expect(loginResponse.status).toBe(200);
    expect(loginResponse.data.fullName).toBe(fullName);
    // Not merely "truthy": defaulting to the login name is exactly the failure mode a
    // renamed property produces, so it has to be ruled out explicitly.
    expect(loginResponse.data.fullName).not.toBe(userId);
  });

  test("revokes the session on logout and Verify then rejects the token", async () => {
    const loginResponse = await client.post(
      ApiEndpoints.auth.login,
      credentials(TestConfig.seedUsername, TestConfig.seedPassword),
    );

    test.skip(
      loginResponse.status !== 200,
      "Could not sign in with the seeded account",
    );

    const sessionToken = loginResponse.data.sessionToken;
    client.setSession(sessionToken, loginResponse.data.userId);

    // The token is good before logout...
    const beforeLogout = await client.get(ApiEndpoints.auth.verify);
    expect(beforeLogout.status).toBe(200);
    expect(beforeLogout.data).toHaveProperty("isValid", true);

    const logoutResponse = await client.post(ApiEndpoints.auth.logout, {});
    expect([200, 204]).toContain(logoutResponse.status);

    // ...and dead afterwards. Logout deletes session:{token} from Valkey, so this is
    // a real revocation rather than the browser merely forgetting a cookie.
    const afterLogout = await client.get(ApiEndpoints.auth.verify);
    expect([401, 403]).toContain(afterLogout.status);
    expect(afterLogout.data?.isValid).toBeFalsy();
  });

  test("rejects a session token that was never issued", async () => {
    client.setSession(`never-issued-${uniqueSuffix()}`, "nobody");

    const response = await client.get(ApiEndpoints.auth.verify);

    expect([401, 403]).toContain(response.status);
  });

  test("exposes the external-provider slot as disabled by default", async () => {
    const response = await client.get(ApiEndpoints.auth.externalProviders);

    test.skip(
      response.status === 404,
      "ExternalProviders endpoint is not deployed in this environment",
    );

    expect(response.status).toBe(200);
    // The login page renders provider buttons ONLY for a non-empty list, so an array
    // is the contract even when the optional external IdP is switched off.
    expect(Array.isArray(response.data)).toBe(true);
  });
});

/**
 * Lockout gets its own serial block against a THROWAWAY account.
 *
 * Hammering the shared seeded admin would lock it out for every other spec in the run,
 * so this registers a disposable user first. If self-service registration is disabled
 * in this environment the whole block skips rather than reporting a false failure.
 */
test.describe.serial("Auth API - account lockout", () => {
  const userId = `lockout-${uniqueSuffix()}`;
  const password = "Lockout@12345";

  let client: ApiClient;
  let accountIsUsable = false;

  test.beforeAll(async () => {
    client = createAuthApiClient();
    await client.init();

    const registerResponse = await client.post(ApiEndpoints.auth.register, {
      userId,
      email: `${userId}@example.edu`,
      fullName: "Lockout Test User",
      password,
    });

    if (registerResponse.status !== 200 && registerResponse.status !== 201) {
      return;
    }

    // Baseline: prove the correct password works BEFORE we start failing. Without
    // this, a later rejection could just mean "account needs approval" and the
    // lockout assertion would pass for the wrong reason.
    const baseline = await client.post(
      ApiEndpoints.auth.login,
      credentials(userId, password),
    );
    accountIsUsable = baseline.status === 200;
  });

  test.afterAll(async () => {
    await client?.dispose();
  });

  test("locks the account after repeated failed sign-ins", async () => {
    test.skip(
      !accountIsUsable,
      "Registration or first sign-in unavailable; cannot test lockout",
    );

    for (let attempt = 0; attempt < TestConfig.lockoutThreshold; attempt += 1) {
      const failure = await client.post(
        ApiEndpoints.auth.login,
        credentials(userId, `wrong-password-${attempt}`),
      );
      // 401 while counting up, 423 Locked once the threshold is crossed.
      expect([401, 423]).toContain(failure.status);
    }

    // The signature of a lockout: even the CORRECT password is now refused.
    const afterLockout = await client.post(
      ApiEndpoints.auth.login,
      credentials(userId, password),
    );

    expect([401, 423]).toContain(afterLockout.status);
    expect(afterLockout.data?.sessionToken).toBeFalsy();
  });
});

/**
 * The password-reset lifecycle, end to end, against a THROWAWAY account.
 *
 * ForgotPassword -> developmentToken -> ResetPassword -> sign in with the new password,
 * then prove the same token cannot be used twice.
 *
 * Two contracts are load-bearing here and neither was covered before:
 *   1. ResetPassword takes ONLY { token, newPassword }. The token is self-identifying —
 *      the account is resolved from the stored hash of the token alone. The bodies below
 *      deliberately carry no user ID, so if the backend ever starts requiring one again
 *      this block fails instead of silently drifting away from the frontend.
 *   2. The token is single use. Clearing it is the only thing stopping a leaked reset
 *      link from being replayed after the legitimate user has already used it.
 *
 * Runs serially: every step depends on the password the previous step left in place.
 */
test.describe.serial("Auth API - password reset round trip", () => {
  const userId = `reset-${uniqueSuffix()}`;
  const originalPassword = "Original@12345";
  const resetPassword = "ResetOnce@12345";
  const replayPassword = "Replayed@12345";

  let client: ApiClient;
  let accountIsUsable = false;
  /** The raw token from ForgotPassword; only echoed back in Development. */
  let developmentToken = "";

  test.beforeAll(async () => {
    client = createAuthApiClient();
    await client.init();

    const registerResponse = await client.post(ApiEndpoints.auth.register, {
      userId,
      email: `${userId}@example.edu`,
      fullName: "Password Reset Test User",
      password: originalPassword,
    });

    if (![200, 201].includes(registerResponse.status)) {
      return;
    }

    // Baseline: the original password works BEFORE anything is reset. Without it, a
    // later successful sign-in could not be attributed to the reset having worked.
    const baseline = await client.post(
      ApiEndpoints.auth.login,
      credentials(userId, originalPassword),
    );
    accountIsUsable = baseline.status === 200;
  });

  test.afterAll(async () => {
    await client?.dispose();
  });

  test("issues a reset token for a known account", async () => {
    test.skip(
      !accountIsUsable,
      "Registration or first sign-in unavailable; cannot test password reset",
    );

    const response = await client.post(ApiEndpoints.auth.forgotPassword, {
      userIdOrEmail: userId,
    });

    expect(response.status).toBe(200);
    expect(response.data?.success).toBe(true);

    test.skip(
      !response.data?.developmentToken,
      "No developmentToken echoed back; the Auth API is not running in Development",
    );

    developmentToken = response.data.developmentToken;
    expect(typeof developmentToken).toBe("string");
  });

  test("rejects a token that was never issued", async () => {
    test.skip(!developmentToken, "No reset token available");

    const response = await client.post(ApiEndpoints.auth.resetPassword, {
      token: `never-issued-${uniqueSuffix()}`,
      newPassword: replayPassword,
    });

    expect(response.status).toBe(400);
    expect(response.data?.success).toBeFalsy();

    // The forged token must not have moved the account anywhere.
    const stillOriginal = await client.post(
      ApiEndpoints.auth.login,
      credentials(userId, originalPassword),
    );
    expect(stillOriginal.status).toBe(200);
  });

  test("resets the password with the token and no user ID", async () => {
    test.skip(!developmentToken, "No reset token available");

    // Note the body: token + newPassword, nothing that names an account.
    const response = await client.post(ApiEndpoints.auth.resetPassword, {
      token: developmentToken,
      newPassword: resetPassword,
    });

    expect(response.status).toBe(200);
    expect(response.data?.success).toBe(true);
  });

  test("signs in with the new password and refuses the old one", async () => {
    test.skip(!developmentToken, "No reset token available");

    const withNew = await client.post(
      ApiEndpoints.auth.login,
      credentials(userId, resetPassword),
    );

    expect(withNew.status).toBe(200);
    expect(withNew.data).toHaveProperty("isAuthenticated", true);
    expect(withNew.data.sessionToken).toBeTruthy();

    // The old password is genuinely gone, not merely superseded.
    const withOld = await client.post(
      ApiEndpoints.auth.login,
      credentials(userId, originalPassword),
    );

    expect([401, 423]).toContain(withOld.status);
    expect(withOld.data?.sessionToken).toBeFalsy();
  });

  test("refuses to replay a spent reset token", async () => {
    test.skip(!developmentToken, "No reset token available");

    const replay = await client.post(ApiEndpoints.auth.resetPassword, {
      token: developmentToken,
      newPassword: replayPassword,
    });

    expect(replay.status).toBe(400);
    expect(replay.data?.success).toBeFalsy();

    // Belt and braces: a rejected replay must also have changed nothing. Checking the
    // status alone would pass even if the reset had gone through and then errored.
    const withReplayPassword = await client.post(
      ApiEndpoints.auth.login,
      credentials(userId, replayPassword),
    );
    expect([401, 423]).toContain(withReplayPassword.status);

    const withResetPassword = await client.post(
      ApiEndpoints.auth.login,
      credentials(userId, resetPassword),
    );
    expect(withResetPassword.status).toBe(200);
  });
});

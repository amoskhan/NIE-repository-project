/**
 * Canonical browser cookie names — the single source of truth for tests.
 *
 * MUST stay in lock-step with `FRONTEND_CONSTANTS.cookies` in
 * src/frontend/packages/shared/src/config/constants.ts. The auth app writes these on a
 * successful sign-in and the main app's router reads them to decide whether a visitor
 * is authenticated. A test that invents its own cookie names silently passes against a
 * broken app, which is exactly the bug class this file exists to prevent.
 *
 * Deliberately side-effect free (no env lookups), so specs that mock the whole backend
 * can import it without needing a provisioned .env file.
 */
export const CookieNames = {
  session: "AppTemplate-SessionToken",
  user: "AppTemplate-User",
} as const;

export default CookieNames;

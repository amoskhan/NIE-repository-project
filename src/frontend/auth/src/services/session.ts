// PROJECT-OWNED — safe to edit.
//
// Browser-side session handoff between the auth app and the main app.
//
// The auth app owns exactly two cookies, both named in @apptemplate/shared:
//   AppTemplate-SessionToken -> the opaque token the APIs accept as `X-Session-Id`
//   AppTemplate-User         -> a JSON snapshot the main app's router reads for gating
// The token itself is only meaningful to the backend (it keys `session:{token}` in
// Valkey); nothing here should ever try to decode it.

import Cookie from "js-cookie";
import { FRONTEND_CONSTANTS, getCookieAttributes } from "@apptemplate/shared";
import { logout } from "./authApi";
import type { IssuedLogin } from "./authApi";

const cookieSettings = getCookieAttributes();

/** Remove both auth cookies. Called before every fresh sign-in attempt. */
export function clearAuthCookies(): void {
  Cookie.remove(FRONTEND_CONSTANTS.cookies.session, cookieSettings);
  Cookie.remove(FRONTEND_CONSTANTS.cookies.user, cookieSettings);
}

/**
 * Best-effort revocation of a token left behind by a previous visit, so returning to
 * the login page really does end the old session. Failures are ignored on purpose —
 * the local cookies still get cleared even if the Auth API is down.
 */
export async function revokeExistingSession(
  sessionToken = Cookie.get(FRONTEND_CONSTANTS.cookies.session),
): Promise<void> {
  if (sessionToken) {
    await logout(sessionToken);
  }
}

/** Flatten the role objects the Auth API may return into plain role-name strings. */
function toRoleNames(roles: IssuedLogin["roles"]): string[] {
  return (
    roles
      ?.map((role) =>
        typeof role === "string" ? role : (role.RoleName ?? null),
      )
      .filter((name): name is string => Boolean(name)) ?? []
  );
}

/**
 * Write the issued session to cookies and hand control to the main app.
 * The main app re-fetches roles and access functions from the Main API on first
 * navigation, so the cookie only needs to be good enough to bootstrap the shell.
 */
export function completeLogin(data: IssuedLogin): void {
  if (data.sessionToken) {
    const roleNames = toRoleNames(data.roles);

    Cookie.set(
      FRONTEND_CONSTANTS.cookies.session,
      data.sessionToken,
      cookieSettings,
    );
    Cookie.set(
      FRONTEND_CONSTANTS.cookies.user,
      JSON.stringify({
        userId: data.userId,
        fullName: data.fullName || data.userName || "",
        email: data.email || "",
        department: data.department || "",
        role: data.role,
        roles: roleNames,
        roleNames,
        permissions: data.permissions?.filter(Boolean) || [],
      }),
      cookieSettings,
    );
  }

  window.location.href = FRONTEND_CONSTANTS.apps.main;
}

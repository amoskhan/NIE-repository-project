import Cookie from "js-cookie";
import { FRONTEND_CONSTANTS, getCookieAttributes } from "@nie/platform";
import api from "../core/api";

const cookieSettings = getCookieAttributes();

export interface CurrentAccessProfile {
  userId: string;
  roleCodes: string[];
  roleNames: string[];
  accessFunctionCodes: string[];
}

/**
 * Fetch the signed-in user's role + access-function codes from the Main API
 * and merge them into the user cookie so permission-gated nav items resolve
 * correctly. The IDP login response only carries identity claims — app-level
 * permissions live in the Main API and have to be fetched separately.
 *
 * Cached for the lifetime of the page (set `force: true` to re-fetch).
 */
let profilePromise: Promise<CurrentAccessProfile | null> | null = null;

export async function refreshAccessProfile(
  options: { force?: boolean } = {},
): Promise<CurrentAccessProfile | null> {
  if (!options.force && profilePromise) {
    return profilePromise;
  }

  profilePromise = (async () => {
    try {
      const res = await api.get<CurrentAccessProfile>(
        "/api/AccessControl/GetCurrentAccessProfile",
      );
      const profile = res.data;

      const userKey = FRONTEND_CONSTANTS.cookies.user;
      const userJson = Cookie.get(userKey);
      if (userJson) {
        try {
          const user = JSON.parse(userJson);
          user.roles = profile.roleCodes ?? [];
          user.roleNames = profile.roleNames ?? [];
          user.permissions = profile.accessFunctionCodes ?? [];
          Cookie.set(userKey, JSON.stringify(user), cookieSettings);
        } catch {
          // Cookie corrupt — leave it alone, /login will rewrite it.
        }
      }

      return profile;
    } catch {
      profilePromise = null; // allow retry on next nav
      return null;
    }
  })();

  return profilePromise;
}

/** Full URL of the auth frontend login page */
export function getAuthLoginUrl(): string {
  return FRONTEND_CONSTANTS.apps.auth;
}

async function revokeSession(sessionId?: string): Promise<void> {
  if (!sessionId) {
    return;
  }

  try {
    await fetch(`${FRONTEND_CONSTANTS.api.auth}/Auth/Logout`, {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        "X-Session-Id": sessionId,
      },
      body: JSON.stringify(sessionId),
      credentials: "include",
    });
  } catch {
    // Local cleanup must still happen if the revocation request cannot complete.
  }
}

function clearLocalSession(): void {
  Cookie.remove(FRONTEND_CONSTANTS.cookies.session, cookieSettings);
  Cookie.remove(FRONTEND_CONSTANTS.cookies.user, cookieSettings);
  localStorage.removeItem("accessModeEntered");
}

const authService = {
  /**
   * Redirect to auth service login if sessionId is missing
   */
  ensureAuthenticated(): void {
    const sessionId = Cookie.get(FRONTEND_CONSTANTS.cookies.session);
    if (!sessionId) {
      clearLocalSession();
      window.location.href = getAuthLoginUrl();
    }
  },

  /**
   * Redirect to auth service login (for 403 or manual use)
   */
  redirectToLogin(): void {
    clearLocalSession();
    window.location.href = getAuthLoginUrl();
  },

  async logout(): Promise<void> {
    const sessionId = Cookie.get(FRONTEND_CONSTANTS.cookies.session);
    await revokeSession(sessionId);
    clearLocalSession();
    window.location.href = getAuthLoginUrl();
  },
};

export { authService };
export default authService;

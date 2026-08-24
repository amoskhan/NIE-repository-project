import Cookies from "js-cookie";
import { afterEach, beforeEach, describe, expect, it } from "vitest";
import { FRONTEND_CONSTANTS } from "../../config";
import { useAuth } from "../useAuth";

const originalLocation = Object.getOwnPropertyDescriptor(window, "location");

function stubLocation(): { href: string } {
  const location = { href: "http://localhost:8002/", hostname: "localhost" };
  Object.defineProperty(window, "location", {
    configurable: true,
    writable: true,
    value: location,
  });
  return location;
}

beforeEach(() => {
  useAuth().clearSession();
});

afterEach(() => {
  useAuth().clearSession();
  if (originalLocation) {
    Object.defineProperty(window, "location", originalLocation);
  }
});

describe("useAuth session token", () => {
  it("round-trips the session token through the session cookie", () => {
    const { getSessionToken, setSessionToken } = useAuth();

    expect(getSessionToken()).toBeUndefined();

    setSessionToken("session-abc");

    expect(getSessionToken()).toBe("session-abc");
    expect(Cookies.get(FRONTEND_CONSTANTS.cookies.session)).toBe("session-abc");
  });

  it("clearSession removes both the cookie and the cached user", () => {
    const auth = useAuth();
    auth.setSessionToken("session-abc");
    auth.setUser({
      id: "1",
      email: "staff@nie.edu.sg",
      name: "Staff",
      roles: ["Staff"],
    });

    auth.clearSession();

    expect(auth.getSessionToken()).toBeUndefined();
    expect(auth.user.value).toBeNull();
    expect(auth.isAuthenticated.value).toBe(false);
  });
});

describe("useAuth identity state", () => {
  it("is shared across every call site", () => {
    const first = useAuth();
    const second = useAuth();

    first.setUser({
      id: "1",
      email: "staff@nie.edu.sg",
      name: "Staff",
      roles: ["Staff"],
    });

    expect(second.user.value?.email).toBe("staff@nie.edu.sg");
    expect(second.isAuthenticated.value).toBe(true);
  });

  it("reports isLoading as a read-only ref", () => {
    expect(useAuth().isLoading.value).toBe(false);
  });
});

describe("useAuth role checks", () => {
  it("returns false for every role while signed out", () => {
    const { hasRole, hasAnyRole } = useAuth();

    expect(hasRole("Administrator")).toBe(false);
    expect(hasAnyRole(["Administrator", "Staff"])).toBe(false);
  });

  it("matches only the roles the user actually holds", () => {
    const auth = useAuth();
    auth.setUser({
      id: "1",
      email: "staff@nie.edu.sg",
      name: "Staff",
      roles: ["Staff", "Approver"],
    });

    expect(auth.hasRole("Staff")).toBe(true);
    expect(auth.hasRole("Administrator")).toBe(false);
    expect(auth.hasRole("staff")).toBe(false);
    expect(auth.hasAnyRole(["Administrator", "Approver"])).toBe(true);
    expect(auth.hasAnyRole(["Administrator", "Auditor"])).toBe(false);
    expect(auth.hasAnyRole([])).toBe(false);
  });
});

describe("useAuth logout", () => {
  it("clears the session and sends the browser to the auth app", async () => {
    const location = stubLocation();
    const auth = useAuth();
    auth.setSessionToken("session-abc");
    auth.setUser({
      id: "1",
      email: "staff@nie.edu.sg",
      name: "Staff",
      roles: ["Staff"],
    });

    await auth.logout();

    expect(auth.getSessionToken()).toBeUndefined();
    expect(auth.user.value).toBeNull();
    expect(location.href).toBe(FRONTEND_CONSTANTS.apps.auth);
  });
});

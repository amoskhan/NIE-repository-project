import { ref, computed, readonly } from "vue";
import Cookies from "js-cookie";
import { FRONTEND_CONSTANTS, getCookieAttributes } from "../config";

interface User {
  id: string;
  email: string;
  name: string;
  roles: string[];
}

const user = ref<User | null>(null);
const isAuthenticated = computed(() => !!user.value);
const isLoading = ref(false);
const cookieSettings = getCookieAttributes();

export function useAuth() {
  function getSessionToken(): string | undefined {
    return Cookies.get(FRONTEND_CONSTANTS.cookies.session);
  }

  function setSessionToken(token: string): void {
    Cookies.set(FRONTEND_CONSTANTS.cookies.session, token, {
      ...cookieSettings,
      expires: 1,
    });
  }

  function clearSession(): void {
    Cookies.remove(FRONTEND_CONSTANTS.cookies.session, cookieSettings);
    user.value = null;
  }

  function setUser(userData: User): void {
    user.value = userData;
  }

  function hasRole(role: string): boolean {
    return user.value?.roles.includes(role) ?? false;
  }

  function hasAnyRole(roles: string[]): boolean {
    return roles.some((role) => hasRole(role));
  }

  async function logout(): Promise<void> {
    clearSession();
    window.location.href = FRONTEND_CONSTANTS.apps.auth;
  }

  return {
    user: readonly(user),
    isAuthenticated,
    isLoading: readonly(isLoading),
    getSessionToken,
    setSessionToken,
    clearSession,
    setUser,
    hasRole,
    hasAnyRole,
    logout,
  };
}

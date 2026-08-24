import { ref, computed } from "vue";
import Cookie from "js-cookie";
import { FRONTEND_CONSTANTS } from "@nie/platform";
import { removePushNotificationExternalUserId } from "@/services/notifications/oneSignalService";
import authService from "@/services/auth/authService";

interface UserInfo {
  userId: string;
  fullName: string;
  email: string;
  department?: string;
  lastLoginAt?: string | null;
  roles: string[];
  roleNames?: string[];
  permissions?: string[];
}

const ADMIN_ROLES = ["SystemAdmin", "ProgrammeAdmin", "Admin"];

const currentUser = ref<UserInfo | null>(null);
const SESSION_KEY = FRONTEND_CONSTANTS.cookies.session;
const USER_KEY = FRONTEND_CONSTANTS.cookies.user;

export function useAuth() {
  const isAuthenticated = computed(() => !!Cookie.get(SESSION_KEY));

  const isAdmin = computed(
    () =>
      currentUser.value?.roles?.some((r) => ADMIN_ROLES.includes(r)) ?? false,
  );

  function loadUser() {
    const userJson = Cookie.get(USER_KEY);
    if (userJson) {
      try {
        currentUser.value = JSON.parse(userJson);
      } catch {
        currentUser.value = null;
      }
    }
  }

  function ensureAuthenticated(): boolean {
    if (!Cookie.get(SESSION_KEY) || !Cookie.get(USER_KEY)) {
      return false;
    }
    if (!currentUser.value) {
      loadUser();
    }
    return true;
  }

  async function logout() {
    removePushNotificationExternalUserId();
    await authService.logout();
    currentUser.value = null;
  }

  function hasRole(role: string): boolean {
    return currentUser.value?.roles?.includes(role) ?? false;
  }

  // Load on first use
  if (!currentUser.value && Cookie.get(USER_KEY)) {
    loadUser();
  }

  return {
    currentUser,
    isAuthenticated,
    isAdmin,
    loadUser,
    logout,
    hasRole,
    ensureAuthenticated,
  };
}

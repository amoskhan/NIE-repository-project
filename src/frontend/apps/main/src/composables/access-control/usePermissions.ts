// TEMPLATE-OWNED SHELL — do not add project data here.
// Menu items live in src/frontend/apps/main/src/app-config/navigation.ts; access codes,
// role maps, and role labels live in src/frontend/apps/main/src/app-config/accessFunctions.ts.
// See .ai/GLOBAL-RULES.md and .ai/FEATURE-authorization-access-functions.md.

import { computed } from "vue";
import { useAuth } from "../auth/useAuth";
import {
  resolvePermissions,
  type PermissionResolutionMaps,
} from "@/constants/permissions";
import {
  ACCESS_FUNCTION_PERMISSION_MAP,
  ADMIN_LABEL_PERMISSIONS,
  ADMIN_ROLE_LABEL,
  AUDITOR_LABEL_PERMISSION,
  AUDITOR_ROLE_LABEL,
  DEFAULT_ROLE_LABEL,
  LEGACY_ROLE_PERMISSIONS,
  ROLE_LABELS,
} from "@/app-config/accessFunctions";
import { ADMIN_NAV_ITEMS, PRIMARY_NAV_ITEMS } from "@/app-config/navigation";
import type { NavItem } from "../shell/navTypes";

// Re-exported for back-compat: consumers (e.g. StaffLayout.vue) import NavItem from here.
export type { NavItem } from "../shell/navTypes";

const PERMISSION_MAPS: PermissionResolutionMaps = {
  legacyRolePermissions: LEGACY_ROLE_PERMISSIONS,
  accessFunctionPermissionMap: ACCESS_FUNCTION_PERMISSION_MAP,
};

export function usePermissions() {
  const { currentUser, hasRole } = useAuth();

  const userPermissions = computed<string[]>(() => {
    return resolvePermissions(currentUser.value, PERMISSION_MAPS);
  });

  function hasPermission(permission: string): boolean {
    return userPermissions.value.includes(permission);
  }

  function filterNavItems(items: NavItem[]): NavItem[] {
    return items.filter((item) => {
      if (item.permissions && item.permissions.length > 0) {
        return item.permissions.some((permission) => hasPermission(permission));
      }

      if (!item.permission) return true;
      return hasPermission(item.permission);
    });
  }

  const navItems = computed<NavItem[]>(() => {
    return filterNavItems(PRIMARY_NAV_ITEMS);
  });

  const adminNavItems = computed<NavItem[]>(() =>
    filterNavItems(ADMIN_NAV_ITEMS),
  );

  const userRoleLabel = computed(() => {
    const roleNames = currentUser.value?.roleNames?.filter(Boolean) ?? [];
    if (roleNames.length > 1) {
      return `${roleNames[0]} +${roleNames.length - 1}`;
    }
    if (roleNames.length === 1) {
      return roleNames[0];
    }

    const role = currentUser.value?.roles?.[0];

    if (
      ADMIN_LABEL_PERMISSIONS.some((permission) =>
        userPermissions.value.includes(permission),
      )
    ) {
      return ADMIN_ROLE_LABEL;
    }

    if (userPermissions.value.includes(AUDITOR_LABEL_PERMISSION)) {
      return AUDITOR_ROLE_LABEL;
    }

    return role ? ROLE_LABELS[role] || role : DEFAULT_ROLE_LABEL;
  });

  return {
    userPermissions,
    hasPermission,
    hasRole,
    navItems,
    adminNavItems,
    userRoleLabel,
  };
}

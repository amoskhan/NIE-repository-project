// TEMPLATE-OWNED SHELL — do not add project data here.
// Routes live in src/frontend/main/src/app-config/routes.ts; access codes live in
// src/frontend/main/src/app-config/accessFunctions.ts; the brand label lives in
// src/frontend/main/src/theme/appTheme.ts.
// See .ai/common/11-customization-boundary.md. Changing this file requires a template task.

import { createRouter, createWebHashHistory } from "vue-router";
import type { RouteRecordRaw } from "vue-router";
import Cookie from "js-cookie";
import { FRONTEND_CONSTANTS } from "@apptemplate/shared";
import {
  resolvePermissions,
  type PermissionResolutionMaps,
} from "@/constants/permissions";
import {
  ACCESS_FUNCTION_PERMISSION_MAP,
  LEGACY_ROLE_PERMISSIONS,
} from "@/app-config/accessFunctions";
import { OPTIONAL_ROUTES, PROJECT_ROUTES } from "@/app-config/routes";
import { mainThemeConfig } from "@/theme/appTheme";
import { getAuthLoginUrl, refreshAccessProfile } from "@/services/authService";
import { useAuth } from "@/composables/useAuth";

// Resolve optional pages at build time so a derived repo that deleted a page (e.g. via a
// removal task) still builds — the route is simply skipped when its module is absent.
// Keep this glob in this file: its keys are build-time literals relative to src/router/,
// and OPTIONAL_ROUTES supplies those literal pagePath keys.
const optionalPages = import.meta.glob("../**/*.vue");

const PERMISSION_MAPS: PermissionResolutionMaps = {
  legacyRolePermissions: LEGACY_ROLE_PERMISSIONS,
  accessFunctionPermissionMap: ACCESS_FUNCTION_PERMISSION_MAP,
};

function optionalChildRoute(
  path: string,
  name: string,
  pagePath: string,
  title: string,
  meta: Record<string, unknown> = {},
): RouteRecordRaw[] {
  const component = optionalPages[pagePath];
  return component
    ? [
        {
          path,
          name,
          component,
          meta: { title, ...meta },
        },
      ]
    : [];
}

const routes: RouteRecordRaw[] = [
  {
    path: "/",
    component: () => import("@/staff/layouts/StaffLayout.vue"),
    children: [
      ...PROJECT_ROUTES,
      ...OPTIONAL_ROUTES.flatMap((route) =>
        optionalChildRoute(
          route.path,
          route.name,
          route.pagePath,
          route.title,
          route.meta,
        ),
      ),
    ],
  },
  { path: "/:pathMatch(.*)*", redirect: "/" },
];

const router = createRouter({
  history: createWebHashHistory(),
  routes,
});

function getUserPermissions(): string[] {
  const userJson = Cookie.get(FRONTEND_CONSTANTS.cookies.user);
  if (!userJson) return [];
  try {
    const user = JSON.parse(userJson) as {
      permissions?: string[];
      roles?: string[];
    };
    return resolvePermissions(user, PERMISSION_MAPS);
  } catch {
    return [];
  }
}

router.beforeEach(async (to) => {
  const hasSession = !!Cookie.get(FRONTEND_CONSTANTS.cookies.session);

  if (!hasSession) {
    window.location.href = getAuthLoginUrl();
    return;
  }

  // Pull roles + access-function codes from the Main API into the user cookie
  // so permission-gated routes/nav can resolve. Cached after the first call —
  // safe to await on every navigation.
  const profile = await refreshAccessProfile();
  if (profile) {
    useAuth().loadUser();
  }

  const requiredPermissions = to.meta?.permissions as string[] | undefined;
  if (requiredPermissions && requiredPermissions.length > 0) {
    const perms = getUserPermissions();
    if (!requiredPermissions.some((permission) => perms.includes(permission))) {
      return { name: "dashboard" };
    }
  }

  const requiredPermission = to.meta?.permission as string | undefined;
  if (requiredPermission) {
    const perms = getUserPermissions();
    if (!perms.includes(requiredPermission)) {
      return { name: "dashboard" };
    }
  }

  const pageTitle = to.meta?.title as string | undefined;
  const brand = mainThemeConfig.brandLabel ?? "App Template";
  document.title = pageTitle ? `${pageTitle} | ${brand}` : brand;
});

export default router;

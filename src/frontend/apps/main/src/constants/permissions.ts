// TEMPLATE-OWNED SHELL — do not add project data here.
// Access codes, role maps, and permission bundles live in
// src/frontend/apps/main/src/app-config/accessFunctions.ts.
// This file holds only the permission-resolution machinery the shell depends on.
// See .ai/GLOBAL-RULES.md and .ai/FEATURE-authorization-access-functions.md.

export type NestedValues<T> = T extends string
  ? T
  : { [K in keyof T]: NestedValues<T[K]> }[keyof T];

export interface PermissionUser {
  permissions?: string[];
  roles?: string[];
}

export interface PermissionResolutionMaps {
  /** Role name → permission codes granted (legacy role-based fallback). */
  legacyRolePermissions: Record<string, string[]>;
  /** Permission/code → additional implied permission codes. */
  accessFunctionPermissionMap: Record<string, string[]>;
}

/**
 * Resolve the effective permission codes for a user.
 *
 * The project-specific maps are injected by the caller so this file stays free of
 * project data — the maps live in app-config/accessFunctions.ts. Callers wire them
 * once (see usePermissions.ts and router/index.ts).
 */
export function resolvePermissions(
  user: PermissionUser | null | undefined,
  maps: PermissionResolutionMaps,
): string[] {
  if (!user) return [];

  const directPermissions = user.permissions?.filter(Boolean) ?? [];
  if (directPermissions.length > 0) {
    const permissions = new Set<string>();

    for (const permission of directPermissions) {
      permissions.add(permission);
      maps.accessFunctionPermissionMap[permission]?.forEach(
        (mappedPermission) => permissions.add(mappedPermission),
      );
    }

    return Array.from(permissions);
  }

  const permissions = new Set<string>();
  for (const role of user.roles ?? []) {
    const rolePermissions = maps.legacyRolePermissions[role];
    rolePermissions?.forEach((permission) => permissions.add(permission));
  }

  return Array.from(permissions);
}

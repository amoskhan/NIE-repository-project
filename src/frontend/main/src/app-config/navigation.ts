// PROJECT-OWNED — safe to edit. The locked shell imports from here.
//
// Sidebar navigation for THIS project. The staff shell (staff/layouts/StaffLayout.vue)
// renders these via usePermissions(); each item is shown/hidden by its permission(s)
// gate. Add or remove menu items HERE — never in the shell components.

import type { NavItem } from "@/composables/navTypes";
import {
  ACCESS_CONTROL_PERMISSIONS,
  AUDIT_PERMISSIONS,
  CHAT_PERMISSIONS,
  REPORT_PERMISSIONS,
  UiPermission,
} from "@/app-config/accessFunctions";

export const PRIMARY_NAV_ITEMS: NavItem[] = [
  { name: "Dashboard", icon: "dashboard", route: "dashboard" },
  { name: "Vendors", icon: "storefront", route: "vendors" },
  { name: "Catalog", icon: "inventory_2", route: "catalog" },
  {
    name: "New Purchase Request",
    icon: "add_shopping_cart",
    route: "new-purchase-request",
  },
  { name: "Approvals", icon: "approval", route: "approvals" },
  {
    name: "Order History",
    icon: "history",
    route: "order-history",
    activeRoutes: ["order-history", "purchase-order-detail"],
  },
  {
    name: "Reports",
    icon: "summarize",
    route: "reports",
    activeRoutes: ["reports", "report-detail"],
    permissions: [...REPORT_PERMISSIONS],
  },
  {
    name: "AI Chat",
    icon: "smart_toy",
    route: "chat",
    activeRoutes: ["chat", "chat-source"],
    permissions: [...CHAT_PERMISSIONS],
  },
];

export const ADMIN_NAV_ITEMS: NavItem[] = [
  {
    name: "Users & Roles",
    icon: "manage_accounts",
    route: "users",
    activeRoutes: ["users", "role-management"],
    permissions: [...ACCESS_CONTROL_PERMISSIONS],
  },
  {
    name: "Access Functions",
    icon: "key",
    route: "access-functions",
    permissions: [...ACCESS_CONTROL_PERMISSIONS],
  },
  {
    name: "Audit Logs",
    icon: "history",
    route: "audit-log",
    permissions: [...AUDIT_PERMISSIONS],
  },
  {
    name: "Global Settings",
    icon: "tune",
    route: "global-settings",
    permission: UiPermission.SettingsManage,
  },
  {
    name: "Monitoring",
    icon: "monitoring",
    route: "monitoring",
    permission: UiPermission.SettingsManage,
  },
];

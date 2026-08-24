// PROJECT-OWNED — safe to edit. The locked shell imports from here.
//
// Sidebar navigation for THIS project. The staff shell (staff/layouts/StaffLayout.vue)
// renders these via usePermissions(); each item is shown/hidden by its permission(s)
// gate. Add or remove menu items HERE — never in the shell components.

import type { NavItem } from "@/composables/shell/navTypes";
import {
  AccessFunctionCode,
  ACCESS_CONTROL_PERMISSIONS,
  AUDIT_PERMISSIONS,
  CHAT_PERMISSIONS,
  MYINFO_PERMISSIONS,
  NOTIFICATION_ADMINISTRATION_PERMISSIONS,
  REPORT_PERMISSIONS,
} from "@/app-config/accessFunctions";

export const PRIMARY_NAV_ITEMS: NavItem[] = [
  {
    name: "Dashboard",
    icon: "dashboard",
    route: "dashboard",
    permission: AccessFunctionCode.Screen.DashboardView,
  },
  {
    name: "Vendors",
    icon: "storefront",
    route: "vendors",
    permission: AccessFunctionCode.Screen.OperationsView,
  },
  {
    name: "Catalog",
    icon: "inventory_2",
    route: "catalog",
    permission: AccessFunctionCode.Screen.OperationsView,
  },
  {
    name: "New Purchase Request",
    icon: "add_shopping_cart",
    route: "new-purchase-request",
    permission: AccessFunctionCode.Screen.OperationsView,
  },
  {
    name: "Approvals",
    icon: "approval",
    route: "approvals",
    permission: AccessFunctionCode.Screen.OperationsView,
  },
  {
    name: "Order History",
    icon: "history",
    route: "order-history",
    activeRoutes: ["order-history", "purchase-order-detail"],
    permission: AccessFunctionCode.Screen.OperationsView,
  },
  {
    name: "Reports",
    icon: "summarize",
    route: "reports",
    activeRoutes: ["reports", "report-detail"],
    permissions: [...REPORT_PERMISSIONS],
  },
  {
    name: "MyInfo",
    icon: "fingerprint",
    route: "myinfo",
    permissions: [...MYINFO_PERMISSIONS],
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
    name: "Access Control",
    icon: "manage_accounts",
    route: "access-control",
    activeRoutes: [
      "access-control",
      "users",
      "role-management",
      "access-functions",
    ],
    permissions: [...ACCESS_CONTROL_PERMISSIONS],
  },
  {
    name: "Audit Logs",
    icon: "history",
    route: "audit-log",
    permissions: [...AUDIT_PERMISSIONS],
  },
  {
    name: "Notifications",
    icon: "notifications_active",
    route: "notification-administration",
    permissions: [...NOTIFICATION_ADMINISTRATION_PERMISSIONS],
  },
];

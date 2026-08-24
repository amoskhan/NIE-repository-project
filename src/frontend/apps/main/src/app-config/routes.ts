// PROJECT-OWNED — safe to edit. The locked shell imports from here.
//
// Routes for THIS project. router/index.ts mounts PROJECT_ROUTES as children of the
// staff shell layout and applies the permission guard from each record's
// meta.permission (scalar) / meta.permissions (array). OPTIONAL_ROUTES are pages that
// may have been removed in a derived repo — the router only adds them if the .vue file
// still exists (via import.meta.glob). Add or remove routes HERE — never in the shell.

import type { RouteRecordRaw } from "vue-router";
import {
  AccessFunctionCode,
  ACCESS_CONTROL_PERMISSIONS,
  AUDIT_PERMISSIONS,
  CHAT_PERMISSIONS,
  MYINFO_PERMISSIONS,
  NOTIFICATION_ADMINISTRATION_PERMISSIONS,
  REPORT_PERMISSIONS,
} from "@/app-config/accessFunctions";

export interface OptionalRouteDescriptor {
  path: string;
  name: string;
  /**
   * Page module path. MUST stay a "../"-relative literal, resolved against the
   * import.meta.glob base in router/index.ts (i.e. relative to src/router/). Do not
   * rewrite it to use the "@/" alias — the glob matches literal keys only.
   */
  pagePath: string;
  title: string;
  meta?: Record<string, unknown>;
}

export const PROJECT_ROUTES: RouteRecordRaw[] = [
  {
    path: "",
    name: "dashboard",
    component: () => import("@/staff/pages/procurement/ProcurementDashboard.vue"),
    meta: {
      permission: AccessFunctionCode.Screen.DashboardView,
      title: "Dashboard",
    },
  },
  {
    path: "vendors",
    name: "vendors",
    component: () => import("@/staff/pages/procurement/VendorManagement.vue"),
    meta: {
      permission: AccessFunctionCode.Screen.OperationsView,
      title: "Vendors",
    },
  },
  {
    path: "catalog",
    name: "catalog",
    component: () => import("@/staff/pages/procurement/CatalogItems.vue"),
    meta: {
      permission: AccessFunctionCode.Screen.OperationsView,
      title: "Catalog Items",
    },
  },
  {
    path: "new-purchase-request",
    name: "new-purchase-request",
    component: () => import("@/staff/pages/procurement/NewPurchaseRequest.vue"),
    meta: {
      permission: AccessFunctionCode.Screen.OperationsView,
      title: "New Purchase Request",
    },
  },
  {
    path: "approvals",
    name: "approvals",
    component: () => import("@/staff/pages/procurement/ApprovalQueue.vue"),
    meta: {
      permission: AccessFunctionCode.Screen.OperationsView,
      title: "Approvals",
    },
  },
  {
    path: "orders",
    name: "order-history",
    component: () => import("@/staff/pages/procurement/OrderHistory.vue"),
    meta: {
      permission: AccessFunctionCode.Screen.OperationsView,
      title: "Order History",
    },
  },
  {
    path: "purchase-order/:id",
    name: "purchase-order-detail",
    component: () => import("@/staff/pages/procurement/PurchaseOrderDetail.vue"),
    meta: {
      permission: AccessFunctionCode.Screen.OperationsView,
      title: "Purchase Order",
    },
  },
  {
    path: "access-control",
    name: "access-control",
    component: () =>
      import("@/staff/pages/admin/access-control/AccessControlUsersPage.vue"),
    meta: {
      permissions: [...ACCESS_CONTROL_PERMISSIONS],
      title: "Access Control",
    },
  },
  {
    path: "users",
    name: "users",
    redirect: (route) => ({
      name: "access-control",
      query: { ...route.query, tab: "users" },
    }),
  },
  {
    path: "role-management",
    name: "role-management",
    redirect: (route) => ({
      name: "access-control",
      query: { ...route.query, tab: "roles" },
    }),
  },
  {
    path: "access-functions",
    name: "access-functions",
    redirect: (route) => ({
      name: "access-control",
      query: { ...route.query, tab: "access-functions" },
    }),
  },
  {
    path: "audit-log",
    name: "audit-log",
    component: () => import("@/staff/pages/admin/audit/AuditLog.vue"),
    meta: {
      permissions: [...AUDIT_PERMISSIONS],
      title: "Audit Logs",
    },
  },
  {
    path: "notification-administration",
    name: "notification-administration",
    component: () =>
      import("@/components/admin/notifications/NotificationAdministration.vue"),
    meta: {
      permissions: [...NOTIFICATION_ADMINISTRATION_PERMISSIONS],
      title: "Notifications",
    },
  },
  {
    path: "push-notifications",
    redirect: { name: "notification-administration" },
  },
];

export const OPTIONAL_ROUTES: OptionalRouteDescriptor[] = [
  {
    path: "myinfo",
    name: "myinfo",
    pagePath: "../staff/pages/myinfo/MyInfoPage.vue",
    title: "MyInfo",
    meta: { permissions: [...MYINFO_PERMISSIONS] },
  },
  {
    path: "reports",
    name: "reports",
    pagePath: "../pages/reports/ReportsIndex.vue",
    title: "Reports",
    meta: { permissions: [...REPORT_PERMISSIONS] },
  },
  {
    path: "reports/:type",
    name: "report-detail",
    pagePath: "../pages/reports/ReportDetail.vue",
    title: "Report",
    meta: { permissions: [...REPORT_PERMISSIONS] },
  },
  {
    path: "chat",
    name: "chat",
    pagePath: "../pages/chat/ChatView.vue",
    title: "AI Chat",
    meta: { permissions: [...CHAT_PERMISSIONS] },
  },
  {
    path: "chat/:source",
    name: "chat-source",
    pagePath: "../pages/chat/ChatView.vue",
    title: "AI Chat",
    meta: { permissions: [...CHAT_PERMISSIONS] },
  },
];

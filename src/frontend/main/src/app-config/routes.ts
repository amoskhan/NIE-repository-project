// PROJECT-OWNED — safe to edit. The locked shell imports from here.
//
// Routes for THIS project. router/index.ts mounts PROJECT_ROUTES as children of the
// staff shell layout and applies the permission guard from each record's
// meta.permission (scalar) / meta.permissions (array). OPTIONAL_ROUTES are pages that
// may have been removed in a derived repo — the router only adds them if the .vue file
// still exists (via import.meta.glob). Add or remove routes HERE — never in the shell.

import type { RouteRecordRaw } from "vue-router";
import {
  ACCESS_CONTROL_PERMISSIONS,
  AUDIT_PERMISSIONS,
  CHAT_PERMISSIONS,
  REPORT_PERMISSIONS,
  UiPermission,
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
    component: () => import("@/staff/pages/staff/ProcurementDashboard.vue"),
    meta: {
      title: "Dashboard",
    },
  },
  {
    path: "vendors",
    name: "vendors",
    component: () => import("@/staff/pages/staff/VendorManagement.vue"),
    meta: {
      title: "Vendors",
    },
  },
  {
    path: "catalog",
    name: "catalog",
    component: () => import("@/staff/pages/staff/CatalogItems.vue"),
    meta: { title: "Catalog Items" },
  },
  {
    path: "new-purchase-request",
    name: "new-purchase-request",
    component: () => import("@/staff/pages/staff/NewPurchaseRequest.vue"),
    meta: { title: "New Purchase Request" },
  },
  {
    path: "approvals",
    name: "approvals",
    component: () => import("@/staff/pages/staff/ApprovalQueue.vue"),
    meta: { title: "Approvals" },
  },
  {
    path: "orders",
    name: "order-history",
    component: () => import("@/staff/pages/staff/OrderHistory.vue"),
    meta: { title: "Order History" },
  },
  {
    path: "purchase-order/:id",
    name: "purchase-order-detail",
    component: () => import("@/staff/pages/staff/PurchaseOrderDetail.vue"),
    meta: { title: "Purchase Order" },
  },
  {
    path: "users",
    name: "users",
    component: () => import("@/staff/pages/admin/Users.vue"),
    meta: {
      permissions: [...ACCESS_CONTROL_PERMISSIONS],
      title: "Users & Roles",
    },
  },
  {
    path: "role-management",
    name: "role-management",
    component: () => import("@/staff/pages/admin/Users.vue"),
    meta: {
      permissions: [...ACCESS_CONTROL_PERMISSIONS],
      title: "Users & Roles",
    },
  },
  {
    path: "access-functions",
    name: "access-functions",
    component: () => import("@/staff/pages/admin/AccessFunctionsPage.vue"),
    meta: {
      permissions: [...ACCESS_CONTROL_PERMISSIONS],
      title: "Access Functions",
    },
  },
  {
    path: "audit-log",
    name: "audit-log",
    component: () => import("@/staff/pages/admin/AuditLog.vue"),
    meta: {
      permissions: [...AUDIT_PERMISSIONS],
      title: "Audit Logs",
    },
  },
  {
    path: "global-settings",
    name: "global-settings",
    component: () => import("@/staff/pages/admin/GlobalSettingsPage.vue"),
    meta: {
      permission: UiPermission.SettingsManage,
      title: "Global Settings",
    },
  },
  {
    path: "push-notifications",
    redirect: { name: "global-settings" },
  },
  {
    path: "monitoring",
    name: "monitoring",
    component: () => import("@/staff/pages/admin/MonitoringPage.vue"),
    meta: {
      permission: UiPermission.SettingsManage,
      title: "Monitoring",
    },
  },
];

export const OPTIONAL_ROUTES: OptionalRouteDescriptor[] = [
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

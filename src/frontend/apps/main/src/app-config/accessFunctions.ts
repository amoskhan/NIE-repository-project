// PROJECT-OWNED — safe to edit. The locked shell imports from here.
//
// Access-function codes, UI permission keys, permission bundles, role→permission
// maps, and role display labels for THIS project. Keep the codes in lock-step with
// the backend catalog at
//   src/backend/Core/Domain/Security/AccessFunctionCatalog.cs
// (see .ai/FEATURE-authorization-access-functions.md).

import type { NestedValues } from "@/constants/permissions";

export const AccessFunctionCode = {
  Screen: {
    DashboardView: "screen.dashboard.view",
    OperationsView: "screen.operations.view",
    ReportsView: "screen.reports.view",
    AuditView: "screen.audit.view",
    AccessControlView: "screen.access-control.view",
    NotificationAdministrationView: "screen.notification-administration.view",
  },
  Api: {
    CodeRead: "api.code.read",
    DocumentDownload: "api.document.download",
    DocumentManage: "api.document.manage",
    WorkflowRead: "api.workflow.read",
    WorkflowTransition: "api.workflow.transition",
    ReportRead: "api.report.read",
    ChatUse: "api.chat.use",
    MyInfoUse: "api.myinfo.use",
    AccessProfileRead: "api.access-control.profile.read",
    AuditRead: "api.audit-log.read",
    AccessControlRead: "api.access-control.read",
    AccessControlRolesManage: "api.access-control.roles.manage",
    AccessControlAssignmentsManage: "api.access-control.assignments.manage",
    ApplicationAccessManage: "api.access-control.applications.manage",
    NotificationConfigurationRead: "api.notification-configuration.read",
    NotificationConfigurationManage: "api.notification-configuration.manage",
    NotificationDeliveryRead: "api.notification-delivery.read",
    NotificationDeliveryRetry: "api.notification-delivery.retry",
    NotificationRead: "api.notification.read",
    NotificationPreferenceManage: "api.notification-preference.manage",
    DataTablePreferenceManage: "api.data-table-preference.manage",
    // === SAMPLE: procurement (reference vertical; remove only after approved replacement) ===
    ProcurementVendorRead: "api.procurement.vendor.read",
    ProcurementVendorManage: "api.procurement.vendor.manage",
    ProcurementCatalogRead: "api.procurement.catalog.read",
    ProcurementCatalogManage: "api.procurement.catalog.manage",
    ProcurementOrderRead: "api.procurement.order.read",
    ProcurementOrderManage: "api.procurement.order.manage",
    ProcurementOrderApprove: "api.procurement.order.approve",
    // === END SAMPLE ===
  },
} as const;

export type AccessFunctionCodeValue = NestedValues<typeof AccessFunctionCode>;

export const UiPermission = {
  SettingsManage: "SETTINGS_MANAGE",
  VendorManage: "VENDOR_MANAGE",
  CatalogManage: "CATALOG_MANAGE",
} as const;

export const ACCESS_CONTROL_PERMISSIONS = [
  AccessFunctionCode.Screen.AccessControlView,
  AccessFunctionCode.Api.AccessControlRead,
  AccessFunctionCode.Api.AccessControlRolesManage,
  AccessFunctionCode.Api.AccessControlAssignmentsManage,
  AccessFunctionCode.Api.ApplicationAccessManage,
] as const;

export const AUDIT_PERMISSIONS = [
  AccessFunctionCode.Screen.AuditView,
  AccessFunctionCode.Api.AuditRead,
] as const;

export const NOTIFICATION_ADMINISTRATION_PERMISSIONS = [
  AccessFunctionCode.Screen.NotificationAdministrationView,
  AccessFunctionCode.Api.NotificationConfigurationRead,
  AccessFunctionCode.Api.NotificationConfigurationManage,
  AccessFunctionCode.Api.NotificationDeliveryRead,
  AccessFunctionCode.Api.NotificationDeliveryRetry,
] as const;

export const REPORT_PERMISSIONS = [
  AccessFunctionCode.Screen.ReportsView,
  AccessFunctionCode.Api.ReportRead,
] as const;

export const MYINFO_PERMISSIONS = [AccessFunctionCode.Api.MyInfoUse] as const;

export const CHAT_PERMISSIONS = [AccessFunctionCode.Api.ChatUse] as const;

export const LEGACY_ROLE_PERMISSIONS: Record<string, string[]> = {
  Requester: [
    "CreatePurchaseOrder",
    "ViewOwnOrders",
    "UploadDocuments",
    AccessFunctionCode.Api.ChatUse,
  ],
  Manager: [
    "CreatePurchaseOrder",
    "ViewOwnOrders",
    "UploadDocuments",
    "ApproveOrders",
    "ViewAllOrders",
    AccessFunctionCode.Api.ChatUse,
  ],
  FinanceApprover: [
    "ViewAllOrders",
    "ApproveOrders",
    "ViewReports",
    AccessFunctionCode.Api.ChatUse,
  ],
  ProcurementManager: [
    "CreatePurchaseOrder",
    "ViewAllOrders",
    "ApproveOrders",
    "ManageVendors",
    "ManageCatalog",
    "UploadDocuments",
    "ViewReports",
    AccessFunctionCode.Api.ChatUse,
  ],
  SystemAdmin: [
    "CreatePurchaseOrder",
    "ViewAllOrders",
    "ApproveOrders",
    "ManageVendors",
    "ManageCatalog",
    "UploadDocuments",
    "ViewReports",
    "ManageUsers",
    "ViewAuditLog",
    "ManageConfig",
    AccessFunctionCode.Api.ChatUse,
  ],
};

export const ACCESS_FUNCTION_PERMISSION_MAP: Record<string, string[]> = {
  [UiPermission.SettingsManage]: [UiPermission.SettingsManage],
  [UiPermission.VendorManage]: [UiPermission.VendorManage],
  [UiPermission.CatalogManage]: [UiPermission.CatalogManage],
  [AccessFunctionCode.Screen.AccessControlView]: [UiPermission.SettingsManage],
  [AccessFunctionCode.Api.AccessControlRead]: [UiPermission.SettingsManage],
  [AccessFunctionCode.Api.AccessControlRolesManage]: [
    UiPermission.SettingsManage,
  ],
  [AccessFunctionCode.Api.AccessControlAssignmentsManage]: [
    UiPermission.SettingsManage,
  ],
  [AccessFunctionCode.Api.ApplicationAccessManage]: [
    UiPermission.SettingsManage,
  ],
};

// Role display labels (UI only — does NOT affect auth or access checks).
// Consumed by usePermissions().userRoleLabel.
export const ROLE_LABELS: Record<string, string> = {
  SystemAdmin: "System Administrator",
  ProcurementManager: "Procurement Manager",
  FinanceApprover: "Finance Approver",
  Manager: "Manager",
  Requester: "Requester",
  Admin: "Administrator",
};

// When the user holds any of these permissions, the shell labels them "Administrator".
export const ADMIN_LABEL_PERMISSIONS: readonly string[] =
  ACCESS_CONTROL_PERMISSIONS;
// When the user holds this permission (and is not an admin), label them "Auditor".
export const AUDITOR_LABEL_PERMISSION: string =
  AccessFunctionCode.Screen.AuditView;
export const ADMIN_ROLE_LABEL = "Administrator";
export const AUDITOR_ROLE_LABEL = "Auditor";
export const DEFAULT_ROLE_LABEL = "Staff";

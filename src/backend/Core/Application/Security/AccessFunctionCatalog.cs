using Domain.Enums;

namespace Application.Security;

/// <summary>
/// Seed definition for a system access function.
/// </summary>
public sealed record AccessFunctionSeedDefinition(
    string Code,
    string Name,
    string Module,
    EAccessFunctionType Type,
    string ResourceName,
    string? Route,
    string? HttpMethod,
    string Description,
    int DisplayOrder);

/// <summary>
/// Seed definition for a system role and its granted access functions.
/// </summary>
public sealed record RoleSeedDefinition(
    Guid Id,
    string Code,
    string Name,
    string Description,
    int DisplayOrder,
    IReadOnlyList<string> AccessFunctionCodes);

/// <summary>
/// Canonical access function codes used across API authorization and screen access checks.
/// </summary>
public static class AccessFunctionCodes
{
    public static class Screen
    {
        public const string DashboardView = "screen.dashboard.view";
        public const string OperationsView = "screen.operations.view";
        public const string ReportsView = "screen.reports.view";
        public const string AuditView = "screen.audit.view";
        public const string AccessControlView = "screen.access-control.view";
        public const string NotificationAdministrationView = "screen.notification-administration.view";
    }

    public static class Api
    {
        public const string CodeRead = "api.code.read";
        public const string DocumentDownload = "api.document.download";
        public const string DocumentManage = "api.document.manage";
        public const string WorkflowRead = "api.workflow.read";
        public const string WorkflowTransition = "api.workflow.transition";
        public const string ReportRead = "api.report.read";
        public const string ChatUse = "api.chat.use";
        public const string MyInfoUse = "api.myinfo.use";
        public const string AccessProfileRead = "api.access-control.profile.read";
        public const string AuditRead = "api.audit-log.read";
        public const string AccessControlRead = "api.access-control.read";
        public const string AccessControlRolesManage = "api.access-control.roles.manage";
        public const string AccessControlAssignmentsManage = "api.access-control.assignments.manage";
        public const string ApplicationAccessManage = "api.access-control.applications.manage";
        public const string NotificationConfigurationRead = "api.notification-configuration.read";
        public const string NotificationConfigurationManage = "api.notification-configuration.manage";
        public const string NotificationDeliveryRead = "api.notification-delivery.read";
        public const string NotificationDeliveryRetry = "api.notification-delivery.retry";
        public const string NotificationRead = "api.notification.read";
        public const string NotificationPreferenceManage = "api.notification-preference.manage";
        public const string DataTablePreferenceManage = "api.data-table-preference.manage";

        // === SAMPLE: procurement access-function codes (reference vertical; remove only after approved replacement) ===
        public const string ProcurementVendorRead = "api.procurement.vendor.read";
        public const string ProcurementVendorManage = "api.procurement.vendor.manage";
        public const string ProcurementCatalogRead = "api.procurement.catalog.read";
        public const string ProcurementCatalogManage = "api.procurement.catalog.manage";
        public const string ProcurementOrderRead = "api.procurement.order.read";
        public const string ProcurementOrderManage = "api.procurement.order.manage";
        public const string ProcurementOrderApprove = "api.procurement.order.approve";
        // === END SAMPLE ===
    }
}

/// <summary>
/// Central catalog for all system-defined access functions and predefined roles.
/// </summary>
public static class AccessFunctionCatalog
{
    public static IReadOnlyList<AccessFunctionSeedDefinition> AccessFunctions { get; } =
        new List<AccessFunctionSeedDefinition>
        {
            new(
                AccessFunctionCodes.Screen.DashboardView,
                "View dashboard",
                "Dashboard",
                EAccessFunctionType.Screen,
                "dashboard",
                "/",
                null,
                "Open the main staff dashboard shell.",
                10),
            new(
                AccessFunctionCodes.Screen.OperationsView,
                "View operations list",
                "Operations",
                EAccessFunctionType.Screen,
                "operations",
                "/operations",
                null,
                "Open the operations list and detail workflow.",
                20),
            new(
                AccessFunctionCodes.Screen.ReportsView,
                "View reports",
                "Reporting",
                EAccessFunctionType.Screen,
                "reports",
                "/reports",
                null,
                "Open reporting and dashboard summaries.",
                30),
            new(
                AccessFunctionCodes.Screen.AuditView,
                "View audit trail",
                "Audit",
                EAccessFunctionType.Screen,
                "audit",
                "/audit",
                null,
                "Open the audit log management screen.",
                40),
            new(
                AccessFunctionCodes.Screen.AccessControlView,
                "View access control",
                "Administration",
                EAccessFunctionType.Screen,
                "access-control",
                "/access-control",
                null,
                "Open the access control administration screen.",
                50),
            new(
                AccessFunctionCodes.Screen.NotificationAdministrationView,
                "View notification administration",
                "Notifications",
                EAccessFunctionType.Screen,
                "notification-administration",
                "/notification-administration",
                null,
                "Open notification policy, email template, and delivery administration.",
                60),
            new(
                AccessFunctionCodes.Api.CodeRead,
                "Read code tables",
                "Reference Data",
                EAccessFunctionType.Api,
                "CodeController.GetAll",
                "/api/Code",
                "GET",
                "Read code-table and lookup data.",
                100),
            new(
                AccessFunctionCodes.Api.DocumentDownload,
                "Download documents",
                "Documents",
                EAccessFunctionType.Api,
                "DocumentController.DownloadFile",
                "/api/Document/DownloadFile",
                "GET",
                "Download stored documents.",
                130),
            new(
                AccessFunctionCodes.Api.DocumentManage,
                "Manage documents",
                "Documents",
                EAccessFunctionType.Api,
                "DocumentController.UploadFile/DeleteFile",
                "/api/Document",
                "POST",
                "Upload or delete stored documents.",
                140),
            new(
                AccessFunctionCodes.Api.WorkflowRead,
                "Read workflow state",
                "Workflow",
                EAccessFunctionType.Api,
                "WorkflowController.Get*",
                "/api/Workflow",
                "GET",
                "Read workflow state, history, and available transitions.",
                145),
            new(
                AccessFunctionCodes.Api.WorkflowTransition,
                "Transition workflow state",
                "Workflow",
                EAccessFunctionType.Api,
                "WorkflowController.TransitionState",
                "/api/Workflow",
                "POST",
                "Move workflow-enabled records through configured states.",
                146),
            new(
                AccessFunctionCodes.Api.ReportRead,
                "Generate reports",
                "Reporting",
                EAccessFunctionType.Api,
                "ReportController.*",
                "/api/Report",
                "GET/POST",
                "Preview and download application reports.",
                147),
            new(
                AccessFunctionCodes.Api.ChatUse,
                "Use AI chat",
                "AI",
                EAccessFunctionType.Api,
                "ChatController.*",
                "/api/Chat",
                "GET/POST/DELETE",
                "Use AI chat conversations and streaming responses.",
                148),
            new(
                AccessFunctionCodes.Api.AuditRead,
                "Read audit logs",
                "Audit",
                EAccessFunctionType.Api,
                "AuditLogController.Get*",
                "/api/AuditLog",
                "GET",
                "Read audit history, summaries, and entity change details.",
                150),
            new(
                AccessFunctionCodes.Api.AccessControlRead,
                "Read access control",
                "Administration",
                EAccessFunctionType.Api,
                "AccessControlController.Get*",
                "/api/AccessControl",
                "GET",
                "Read roles, assignments, and access function configuration.",
                160),
            new(
                AccessFunctionCodes.Api.AccessControlRolesManage,
                "Manage roles",
                "Administration",
                EAccessFunctionType.Api,
                "AccessControlController.CreateRole/UpdateRole/DeleteRole",
                "/api/AccessControl",
                "POST/DELETE",
                "Create, update, or delete roles and their access functions.",
                170),
            new(
                AccessFunctionCodes.Api.AccessControlAssignmentsManage,
                "Manage role assignments",
                "Administration",
                EAccessFunctionType.Api,
                "AccessControlController.AssignRole/RemoveAssignment",
                "/api/AccessControl",
                "POST/DELETE",
                "Assign or remove roles for users.",
                180),
            new(
                AccessFunctionCodes.Api.ApplicationAccessManage,
                "Manage application access",
                "Administration",
                EAccessFunctionType.Api,
                "AccessControlController.AssignAccess/GetApplicationAccess/RemoveApplicationAccess",
                "/api/AccessControl",
                "GET/POST/DELETE",
                "Assign or remove roles within a specific application boundary.",
                181),
            new(
                AccessFunctionCodes.Api.NotificationConfigurationRead,
                "Read notification configuration",
                "Notifications",
                EAccessFunctionType.Api,
                "NotificationAdministrationController.GetOverview",
                "/api/NotificationAdministration/GetOverview",
                "GET",
                "Read notification policies, template versions, and provider health without secrets.",
                181),
            new(
                AccessFunctionCodes.Api.NotificationConfigurationManage,
                "Manage notification configuration",
                "Notifications",
                EAccessFunctionType.Api,
                "NotificationAdministrationController.UpdatePolicy/SaveTemplate/PublishTemplate/SendTest",
                "/api/NotificationAdministration",
                "PUT/POST",
                "Change notification policies, publish email templates, and send channel tests.",
                182),
            new(
                AccessFunctionCodes.Api.NotificationDeliveryRead,
                "Read notification deliveries",
                "Notifications",
                EAccessFunctionType.Api,
                "NotificationAdministrationController.GetOverview/GetDeliveries",
                "/api/NotificationAdministration",
                "GET",
                "Read recent notification delivery status and safe failure metadata.",
                183),
            new(
                AccessFunctionCodes.Api.NotificationDeliveryRetry,
                "Retry notification deliveries",
                "Notifications",
                EAccessFunctionType.Api,
                "NotificationAdministrationController.RetryDelivery",
                "/api/NotificationAdministration/RetryDelivery",
                "POST",
                "Queue a failed or skipped notification delivery for another attempt.",
                184),
            new(
                AccessFunctionCodes.Api.NotificationRead,
                "Read personal notifications",
                "Notifications",
                EAccessFunctionType.Api,
                "NotificationController.Get*",
                "/api/Notification",
                "GET",
                "Read the signed-in user's notification inbox, unread count, and provider configuration.",
                185),
            new(
                AccessFunctionCodes.Api.NotificationPreferenceManage,
                "Manage personal notifications",
                "Notifications",
                EAccessFunctionType.Api,
                "NotificationController.Mark*/UpdatePreferences",
                "/api/Notification",
                "POST/PUT",
                "Mark the signed-in user's notifications and manage personal delivery preferences.",
                186),
            new(
                AccessFunctionCodes.Api.DataTablePreferenceManage,
                "Manage personal data table preferences",
                "User Preferences",
                EAccessFunctionType.Api,
                "UserDataTablePreferenceController.GetAll/Get/Upsert/Delete",
                "/api/UserDataTablePreference",
                "GET/PUT/DELETE",
                "Manage only the signed-in user's saved table layouts, filters, sorts, and page size.",
                187),
            // === SAMPLE: procurement access-function seeds (reference vertical; remove only after approved replacement) ===
            new(
                AccessFunctionCodes.Api.ProcurementVendorRead,
                "Read vendors",
                "Procurement",
                EAccessFunctionType.Api,
                "VendorController.Get*",
                "/api/Vendor",
                "GET",
                "Read vendor records.",
                200),
            new(
                AccessFunctionCodes.Api.ProcurementVendorManage,
                "Manage vendors",
                "Procurement",
                EAccessFunctionType.Api,
                "VendorController.Save/Edit/Delete*",
                "/api/Vendor",
                "POST",
                "Create, update, or delete vendor records.",
                210),
            new(
                AccessFunctionCodes.Api.ProcurementCatalogRead,
                "Read catalog items",
                "Procurement",
                EAccessFunctionType.Api,
                "CatalogItemController.Get*",
                "/api/CatalogItem",
                "GET",
                "Read catalog item records.",
                220),
            new(
                AccessFunctionCodes.Api.ProcurementCatalogManage,
                "Manage catalog items",
                "Procurement",
                EAccessFunctionType.Api,
                "CatalogItemController.Save/Edit/Delete*",
                "/api/CatalogItem",
                "POST",
                "Create, update, or delete catalog items.",
                230),
            new(
                AccessFunctionCodes.Api.ProcurementOrderRead,
                "Read purchase orders",
                "Procurement",
                EAccessFunctionType.Api,
                "PurchaseOrderController.Get*",
                "/api/PurchaseOrder",
                "GET",
                "Read purchase order records.",
                240),
            new(
                AccessFunctionCodes.Api.ProcurementOrderManage,
                "Manage purchase orders",
                "Procurement",
                EAccessFunctionType.Api,
                "PurchaseOrderController.Save/Edit/Delete*",
                "/api/PurchaseOrder",
                "POST",
                "Create, update, or delete purchase orders.",
                250),
            new(
                AccessFunctionCodes.Api.ProcurementOrderApprove,
                "Approve purchase orders",
                "Procurement",
                EAccessFunctionType.Api,
                "PurchaseOrderController.Approve/Reject",
                "/api/PurchaseOrder",
                "POST",
                "Approve or reject submitted purchase orders.",
                260),
            // === END SAMPLE ===
            new(
                AccessFunctionCodes.Api.MyInfoUse,
                "Use MyInfo",
                "Identity",
                EAccessFunctionType.Api,
                "MyInfoController.*",
                "/api/MyInfo",
                "GET/POST",
                "Start and complete Singpass MyInfo data retrieval.",
                270),
            new(
                AccessFunctionCodes.Api.AccessProfileRead,
                "Read current access profile",
                "Administration",
                EAccessFunctionType.Api,
                "AccessControlController.GetCurrentAccessProfile",
                "/api/AccessControl/GetCurrentAccessProfile",
                "GET",
                "Read the signed-in user's roles and access functions.",
                280)
        };

    public static IReadOnlyList<RoleSeedDefinition> Roles { get; } =
        new List<RoleSeedDefinition>
        {
            new(
                SystemRoleIds.Administrator,
                "SYSTEM_ADMIN",
                "System Administrator",
                "Full access to screens, APIs, audit logs, and access control administration.",
                10,
                AccessFunctions.Select(definition => definition.Code).ToList()),
            new(
                SystemRoleIds.Manager,
                "OPERATIONS_MANAGER",
                "Operations Manager",
                "Manage operational records and review audit activity without changing access control.",
                20,
                new[]
                {
                    AccessFunctionCodes.Screen.DashboardView,
                    AccessFunctionCodes.Screen.OperationsView,
                    AccessFunctionCodes.Screen.ReportsView,
                    AccessFunctionCodes.Screen.AuditView,
                    AccessFunctionCodes.Api.CodeRead,
                    AccessFunctionCodes.Api.DocumentDownload,
                    AccessFunctionCodes.Api.DocumentManage,
                    AccessFunctionCodes.Api.WorkflowRead,
                    AccessFunctionCodes.Api.WorkflowTransition,
                    AccessFunctionCodes.Api.ReportRead,
                    AccessFunctionCodes.Api.ChatUse,
                    AccessFunctionCodes.Api.MyInfoUse,
                    AccessFunctionCodes.Api.AccessProfileRead,
                    AccessFunctionCodes.Api.AuditRead,
                    AccessFunctionCodes.Api.NotificationRead,
                    AccessFunctionCodes.Api.NotificationPreferenceManage,
                    AccessFunctionCodes.Api.DataTablePreferenceManage,
                    AccessFunctionCodes.Api.ProcurementVendorRead,
                    AccessFunctionCodes.Api.ProcurementVendorManage,
                    AccessFunctionCodes.Api.ProcurementCatalogRead,
                    AccessFunctionCodes.Api.ProcurementCatalogManage,
                    AccessFunctionCodes.Api.ProcurementOrderRead,
                    AccessFunctionCodes.Api.ProcurementOrderManage,
                    AccessFunctionCodes.Api.ProcurementOrderApprove
                }),
            new(
                SystemRoleIds.User,
                "OPERATIONS_USER",
                "Operations User",
                "Work on operational records and document actions in the staff workspace.",
                30,
                new[]
                {
                    AccessFunctionCodes.Screen.DashboardView,
                    AccessFunctionCodes.Screen.OperationsView,
                    AccessFunctionCodes.Api.CodeRead,
                    AccessFunctionCodes.Api.DocumentDownload,
                    AccessFunctionCodes.Api.DocumentManage,
                    AccessFunctionCodes.Api.WorkflowRead,
                    AccessFunctionCodes.Api.WorkflowTransition,
                    AccessFunctionCodes.Api.ReportRead,
                    AccessFunctionCodes.Api.ChatUse,
                    AccessFunctionCodes.Api.MyInfoUse,
                    AccessFunctionCodes.Api.AccessProfileRead,
                    AccessFunctionCodes.Api.NotificationRead,
                    AccessFunctionCodes.Api.NotificationPreferenceManage,
                    AccessFunctionCodes.Api.DataTablePreferenceManage,
                    AccessFunctionCodes.Api.ProcurementVendorRead,
                    AccessFunctionCodes.Api.ProcurementCatalogRead,
                    AccessFunctionCodes.Api.ProcurementOrderRead,
                    AccessFunctionCodes.Api.ProcurementOrderManage
                }),
            new(
                SystemRoleIds.Viewer,
                "READ_ONLY_VIEWER",
                "Read Only Viewer",
                "Read-only access to dashboards, reporting, and entity details.",
                40,
                new[]
                {
                    AccessFunctionCodes.Screen.DashboardView,
                    AccessFunctionCodes.Screen.ReportsView,
                    AccessFunctionCodes.Api.CodeRead,
                    AccessFunctionCodes.Api.DocumentDownload,
                    AccessFunctionCodes.Api.WorkflowRead,
                    AccessFunctionCodes.Api.ReportRead,
                    AccessFunctionCodes.Api.ChatUse,
                    AccessFunctionCodes.Api.MyInfoUse,
                    AccessFunctionCodes.Api.AccessProfileRead,
                    AccessFunctionCodes.Api.NotificationRead,
                    AccessFunctionCodes.Api.NotificationPreferenceManage,
                    AccessFunctionCodes.Api.DataTablePreferenceManage,
                    AccessFunctionCodes.Api.ProcurementVendorRead,
                    AccessFunctionCodes.Api.ProcurementCatalogRead,
                    AccessFunctionCodes.Api.ProcurementOrderRead
                })
        };
}

import type { NotificationItem } from "@/types";

export interface NotificationCategoryDefinition<TCategoryId extends string = string> {
  id: TCategoryId;
  label: string;
  description: string;
}

export interface NotificationPreferenceDefinition {
  key: ENotificationPreferenceKey;
  label: string;
  description: string;
  categoryId: ENotificationCategory;
  defaultEnabled: boolean;
}

export type UserNotificationSubscriptionMap = Record<
  ENotificationPreferenceKey,
  boolean
>;

export interface UserNotificationPreferences {
  desktopAlerts: boolean;
  subscriptions: UserNotificationSubscriptionMap;
}

export interface AdminNotificationSettingDefinition {
  key: EAdminNotificationSettingKey;
  label: string;
  description: string;
  categoryId: EAdminNotificationCategory;
  defaultValue: boolean;
}

const STORAGE_KEY_PREFIX = "nie-template-notification-preferences";

export enum ENotificationCategory {
  Orders = "orders",
  Catalog = "catalog",
  Workspace = "workspace",
}

export enum ENotificationPreferenceKey {
  OrderUpdates = "orderUpdates",
  ApprovalReminders = "approvalReminders",
  ApprovalDecisions = "approvalDecisions",
  VendorUpdates = "vendorUpdates",
  CatalogRefreshes = "catalogRefreshes",
  WorkspaceAnnouncements = "workspaceAnnouncements",
}

export enum EAdminNotificationCategory {
  Operations = "operations",
  Security = "security",
  Configuration = "configuration",
}

export enum EAdminNotificationSettingKey {
  ApprovalBacklogEnabled = "Notifications.Admin.ApprovalBacklog.Enabled",
  AccessControlEnabled = "Notifications.Admin.AccessControl.Enabled",
  AuditEnabled = "Notifications.Admin.Audit.Enabled",
  ConfigurationEnabled = "Notifications.Admin.Configuration.Enabled",
}

export enum ENotificationType {
  OrderUpdate = "OrderUpdate",
  ApprovalReminder = "ApprovalReminder",
  ApprovalUpdate = "ApprovalUpdate",
  ApprovalDecision = "ApprovalDecision",
  CatalogRefresh = "CatalogRefresh",
  VendorUpdate = "VendorUpdate",
  SystemAlert = "SystemAlert",
  WorkspaceAnnouncement = "WorkspaceAnnouncement",
  InterviewScheduled = "InterviewScheduled",
  SupportTicketCreated = "SupportTicketCreated",
  SupportTicketReply = "SupportTicketReply",
  AdmissionMessageReceived = "AdmissionMessageReceived",
  ApplicationSubmitted = "ApplicationSubmitted",
}

const NOTIFICATION_TYPE_PREFERENCE_MAP: Partial<
  Record<string, ENotificationPreferenceKey>
> = {
  [ENotificationType.OrderUpdate]: ENotificationPreferenceKey.OrderUpdates,
  [ENotificationType.ApprovalReminder]:
    ENotificationPreferenceKey.ApprovalReminders,
  [ENotificationType.ApprovalUpdate]:
    ENotificationPreferenceKey.ApprovalDecisions,
  [ENotificationType.ApprovalDecision]:
    ENotificationPreferenceKey.ApprovalDecisions,
  [ENotificationType.CatalogRefresh]:
    ENotificationPreferenceKey.CatalogRefreshes,
  [ENotificationType.VendorUpdate]: ENotificationPreferenceKey.VendorUpdates,
  [ENotificationType.SystemAlert]:
    ENotificationPreferenceKey.WorkspaceAnnouncements,
  [ENotificationType.WorkspaceAnnouncement]:
    ENotificationPreferenceKey.WorkspaceAnnouncements,
  [ENotificationType.InterviewScheduled]:
    ENotificationPreferenceKey.WorkspaceAnnouncements,
  [ENotificationType.SupportTicketCreated]:
    ENotificationPreferenceKey.WorkspaceAnnouncements,
  [ENotificationType.SupportTicketReply]:
    ENotificationPreferenceKey.WorkspaceAnnouncements,
  [ENotificationType.AdmissionMessageReceived]:
    ENotificationPreferenceKey.WorkspaceAnnouncements,
  [ENotificationType.ApplicationSubmitted]:
    ENotificationPreferenceKey.WorkspaceAnnouncements,
};

const SOURCE_ENTITY_TYPE_PREFERENCE_MAP: Partial<
  Record<string, ENotificationPreferenceKey>
> = {
  PurchaseOrder: ENotificationPreferenceKey.OrderUpdates,
  PurchaseOrderApproval: ENotificationPreferenceKey.ApprovalReminders,
  Vendor: ENotificationPreferenceKey.VendorUpdates,
  CatalogItem: ENotificationPreferenceKey.CatalogRefreshes,
};

export const USER_NOTIFICATION_CATEGORIES: NotificationCategoryDefinition<ENotificationCategory>[] = [
  {
    id: ENotificationCategory.Orders,
    label: "Orders & Approvals",
    description:
      "Updates tied to purchase requests, approvals, and fulfilment milestones.",
  },
  {
    id: ENotificationCategory.Catalog,
    label: "Catalog & Vendors",
    description:
      "Changes to vendor records, catalogs, and procurement reference data.",
  },
  {
    id: ENotificationCategory.Workspace,
    label: "Workspace",
    description:
      "General procurement announcements and reminders for the signed-in user.",
  },
];

export const USER_NOTIFICATION_DEFINITIONS: NotificationPreferenceDefinition[] =
  [
    {
      key: ENotificationPreferenceKey.OrderUpdates,
      label: "Order updates",
      description:
        "Status changes, fulfilment milestones, and request progress for your orders.",
      categoryId: ENotificationCategory.Orders,
      defaultEnabled: true,
    },
    {
      key: ENotificationPreferenceKey.ApprovalReminders,
      label: "Approval reminders",
      description:
        "Pending approvals, escalations, and reminders when action is needed.",
      categoryId: ENotificationCategory.Orders,
      defaultEnabled: true,
    },
    {
      key: ENotificationPreferenceKey.ApprovalDecisions,
      label: "Approval decisions",
      description:
        "Approvals, rejections, and comments on submitted procurement requests.",
      categoryId: ENotificationCategory.Orders,
      defaultEnabled: true,
    },
    {
      key: ENotificationPreferenceKey.VendorUpdates,
      label: "Vendor updates",
      description:
        "Vendor onboarding, suspension, and important profile or compliance changes.",
      categoryId: ENotificationCategory.Catalog,
      defaultEnabled: false,
    },
    {
      key: ENotificationPreferenceKey.CatalogRefreshes,
      label: "Catalog refreshes",
      description:
        "Catalog additions, pricing refreshes, and availability changes.",
      categoryId: ENotificationCategory.Catalog,
      defaultEnabled: true,
    },
    {
      key: ENotificationPreferenceKey.WorkspaceAnnouncements,
      label: "Workspace announcements",
      description:
        "General procurement notices, policy reminders, and release updates.",
      categoryId: ENotificationCategory.Workspace,
      defaultEnabled: true,
    },
  ];

export const ADMIN_NOTIFICATION_CATEGORIES: NotificationCategoryDefinition<EAdminNotificationCategory>[] = [
  {
    id: EAdminNotificationCategory.Operations,
    label: "Operations",
    description:
      "System-level notifications for approvals, incidents, and service health.",
  },
  {
    id: EAdminNotificationCategory.Security,
    label: "Security & Access",
    description:
      "Changes that affect roles, access functions, and sensitive activity review.",
  },
  {
    id: EAdminNotificationCategory.Configuration,
    label: "Configuration",
    description:
      "Alerts for system-wide notification administration changes.",
  },
];

export const ADMIN_NOTIFICATION_SETTING_DEFINITIONS: AdminNotificationSettingDefinition[] =
  [
    {
      key: EAdminNotificationSettingKey.ApprovalBacklogEnabled,
      label: "Approval backlog alerts",
      description:
        "Notify administrators when approval queues age, spike, or breach SLA thresholds.",
      categoryId: EAdminNotificationCategory.Operations,
      defaultValue: true,
    },
    {
      key: EAdminNotificationSettingKey.AccessControlEnabled,
      label: "Access control changes",
      description:
        "Notify administrators when roles, assignments, or access functions are changed.",
      categoryId: EAdminNotificationCategory.Security,
      defaultValue: true,
    },
    {
      key: EAdminNotificationSettingKey.AuditEnabled,
      label: "Audit review alerts",
      description:
        "Notify administrators about unusual audit activity that needs follow-up.",
      categoryId: EAdminNotificationCategory.Security,
      defaultValue: false,
    },
    {
      key: EAdminNotificationSettingKey.ConfigurationEnabled,
      label: "Configuration changes",
      description:
        "Notify administrators when system-wide notification defaults change.",
      categoryId: EAdminNotificationCategory.Configuration,
      defaultValue: true,
    },
  ];

function createDefaultSubscriptions(): UserNotificationSubscriptionMap {
  return Object.fromEntries(
    USER_NOTIFICATION_DEFINITIONS.map((definition) => [
      definition.key,
      definition.defaultEnabled,
    ]),
  ) as UserNotificationSubscriptionMap;
}

export function getDefaultNotificationPreferences(): UserNotificationPreferences {
  return {
    desktopAlerts: true,
    subscriptions: createDefaultSubscriptions(),
  };
}

export function normalizeNotificationPreferences(
  preferences?: Partial<UserNotificationPreferences> | null,
): UserNotificationPreferences {
  const defaults = getDefaultNotificationPreferences();
  const subscriptions = { ...defaults.subscriptions };

  Object.entries(preferences?.subscriptions ?? {}).forEach(([key, enabled]) => {
    if (key in subscriptions && typeof enabled === "boolean") {
      subscriptions[key] = enabled;
    }
  });

  return {
    desktopAlerts:
      typeof preferences?.desktopAlerts === "boolean"
        ? preferences.desktopAlerts
        : defaults.desktopAlerts,
    subscriptions,
  };
}

function getStorageKey(userId?: string | number | null): string {
  return `${STORAGE_KEY_PREFIX}:${String(userId ?? "guest")}`;
}

export function loadNotificationPreferences(
  userId?: string | number | null,
): UserNotificationPreferences {
  if (typeof window === "undefined") {
    return getDefaultNotificationPreferences();
  }

  try {
    const raw = window.localStorage.getItem(getStorageKey(userId));
    if (!raw) {
      return getDefaultNotificationPreferences();
    }

    return normalizeNotificationPreferences(
      JSON.parse(raw) as Partial<UserNotificationPreferences>,
    );
  } catch {
    return getDefaultNotificationPreferences();
  }
}

export function saveNotificationPreferences(
  userId: string | number | null | undefined,
  preferences: Partial<UserNotificationPreferences>,
): UserNotificationPreferences {
  const normalized = normalizeNotificationPreferences(preferences);

  if (typeof window === "undefined") {
    return normalized;
  }

  try {
    window.localStorage.setItem(
      getStorageKey(userId),
      JSON.stringify(normalized),
    );
  } catch {
    // Ignore local storage failures and return the normalized value.
  }

  return normalized;
}

export function setAllNotificationSubscriptions(
  preferences: UserNotificationPreferences,
  enabled: boolean,
  categoryId?: ENotificationCategory,
): UserNotificationPreferences {
  const normalized = normalizeNotificationPreferences(preferences);
  const subscriptions = { ...normalized.subscriptions };
  const definitions = categoryId
    ? USER_NOTIFICATION_DEFINITIONS.filter(
        (definition) => definition.categoryId === categoryId,
      )
    : USER_NOTIFICATION_DEFINITIONS;

  definitions.forEach((definition) => {
    subscriptions[definition.key] = enabled;
  });

  return {
    ...normalized,
    subscriptions,
  };
}

export function getSubscribedNotificationDefinitions(
  preferences: UserNotificationPreferences,
): NotificationPreferenceDefinition[] {
  const normalized = normalizeNotificationPreferences(preferences);

  return USER_NOTIFICATION_DEFINITIONS.filter(
    (definition) => normalized.subscriptions[definition.key],
  );
}

export function getNotificationPreferenceSummary(
  preferences: UserNotificationPreferences,
): string {
  const count = getSubscribedNotificationDefinitions(preferences).length;
  return count === 1 ? "1 subscribed" : `${count} subscribed`;
}

export function isNotificationPreferencesEnabled(
  preferences: UserNotificationPreferences,
  permission: NotificationPermission | "unsupported",
): boolean {
  const normalized = normalizeNotificationPreferences(preferences);

  return normalized.desktopAlerts && permission === "granted";
}

export function resolveNotificationPreferenceKey(
  notification: Pick<NotificationItem, "type" | "sourceEntityType">,
): ENotificationPreferenceKey {
  const byType = NOTIFICATION_TYPE_PREFERENCE_MAP[notification.type];
  if (byType) {
    return byType;
  }

  const sourceEntityType = notification.sourceEntityType ?? "";
  return (
    SOURCE_ENTITY_TYPE_PREFERENCE_MAP[sourceEntityType] ??
    ENotificationPreferenceKey.WorkspaceAnnouncements
  );
}

export function isNotificationEnabledForPreferences(
  notification: Pick<NotificationItem, "type" | "sourceEntityType">,
  preferences: UserNotificationPreferences,
): boolean {
  const normalized = normalizeNotificationPreferences(preferences);
  return normalized.subscriptions[resolveNotificationPreferenceKey(notification)];
}

export function filterNotificationsByPreferences<T extends NotificationItem>(
  notifications: T[],
  preferences: UserNotificationPreferences,
): T[] {
  return notifications.filter((notification) =>
    isNotificationEnabledForPreferences(notification, preferences),
  );
}

export function canShowDesktopNotification(
  notification: Pick<NotificationItem, "type" | "sourceEntityType">,
  preferences: UserNotificationPreferences,
  permission: NotificationPermission | "unsupported",
): boolean {
  const normalized = normalizeNotificationPreferences(preferences);
  return (
    normalized.desktopAlerts &&
    permission === "granted" &&
    isNotificationEnabledForPreferences(notification, normalized)
  );
}

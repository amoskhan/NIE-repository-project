import api from "./api";
import type { NotificationItem } from "@/types";
import { ENotificationType } from "@/services/notificationPreferencesService";
import { FRONTEND_CONSTANTS } from "@apptemplate/shared";

const NOTIFICATION_STORAGE_KEY = "app-template-demo-notifications";
const useDemoNotifications = FRONTEND_CONSTANTS.features.useDemoNotifications;

function createDemoNotifications(): NotificationItem[] {
  const now = Date.now();

  return [
    {
      id: 1,
      recipientType: "User",
      recipientUserId: null,
      recipientEmail: null,
      recipientName: "Alice Tan",
      title: "Purchase order approved",
      message:
        "PO-2025-0003 has moved into the approved state and is ready for fulfilment.",
      type: ENotificationType.ApprovalUpdate,
      isRead: false,
      readAt: null,
      link: "/orders",
      sourceEntityType: "PurchaseOrder",
      sourceEntityId: 3,
      createdOn: new Date(now - 1000 * 60 * 18).toISOString(),
    },
    {
      id: 2,
      recipientType: "User",
      recipientUserId: null,
      recipientEmail: null,
      recipientName: "Alice Tan",
      title: "Vendor catalog refreshed",
      message:
        "Ten seeded procurement catalog items are now available for vendor browsing and order creation.",
      type: ENotificationType.CatalogRefresh,
      isRead: false,
      readAt: null,
      link: "/catalog",
      sourceEntityType: "CatalogItem",
      sourceEntityId: null,
      createdOn: new Date(now - 1000 * 60 * 52).toISOString(),
    },
    {
      id: 3,
      recipientType: "User",
      recipientUserId: null,
      recipientEmail: null,
      recipientName: "Alice Tan",
      title: "Admin monitoring review",
      message:
        "Check monitoring configuration to confirm Sentry and uptime endpoints are ready for the environment.",
      type: ENotificationType.SystemAlert,
      isRead: true,
      readAt: new Date(now - 1000 * 60 * 120).toISOString(),
      link: "/monitoring",
      sourceEntityType: "Monitoring",
      sourceEntityId: null,
      createdOn: new Date(now - 1000 * 60 * 140).toISOString(),
    },
  ];
}

function readDemoNotifications(): NotificationItem[] {
  const fallback = createDemoNotifications();

  if (typeof window === "undefined") {
    return fallback;
  }

  try {
    const raw = window.localStorage.getItem(NOTIFICATION_STORAGE_KEY);
    if (!raw) {
      window.localStorage.setItem(
        NOTIFICATION_STORAGE_KEY,
        JSON.stringify(fallback),
      );
      return fallback;
    }

    const parsed = JSON.parse(raw) as NotificationItem[];
    return parsed.sort(
      (left, right) =>
        new Date(right.createdOn).getTime() -
        new Date(left.createdOn).getTime(),
    );
  } catch {
    return fallback;
  }
}

function writeDemoNotifications(items: NotificationItem[]) {
  if (typeof window === "undefined") {
    return;
  }

  try {
    window.localStorage.setItem(
      NOTIFICATION_STORAGE_KEY,
      JSON.stringify(items),
    );
  } catch {
    // Ignore local storage failures for demo-only notifications.
  }
}

const notificationService = {
  async getAll(limit: number = 20): Promise<NotificationItem[]> {
    if (useDemoNotifications) {
      return readDemoNotifications().slice(0, limit);
    }

    return (
      await api.get<NotificationItem[]>("/api/Notification/GetAll", {
        params: { limit },
      })
    ).data;
  },

  async getUnread(): Promise<NotificationItem[]> {
    if (useDemoNotifications) {
      return readDemoNotifications().filter(
        (notification) => !notification.isRead,
      );
    }

    return (await api.get<NotificationItem[]>("/api/Notification/GetUnread"))
      .data;
  },

  async getUnreadCount(): Promise<number> {
    if (useDemoNotifications) {
      return readDemoNotifications().filter(
        (notification) => !notification.isRead,
      ).length;
    }

    return (await api.get<number>("/api/Notification/GetUnreadCount")).data;
  },

  async markAsRead(id: number): Promise<void> {
    if (useDemoNotifications) {
      writeDemoNotifications(
        readDemoNotifications().map((notification) =>
          notification.id === id
            ? {
                ...notification,
                isRead: true,
                readAt: new Date().toISOString(),
              }
            : notification,
        ),
      );
      return;
    }

    await api.post("/api/Notification/MarkAsRead", null, {
      params: { id },
    });
  },

  async markAllAsRead(): Promise<void> {
    if (useDemoNotifications) {
      writeDemoNotifications(
        readDemoNotifications().map((notification) => ({
          ...notification,
          isRead: true,
          readAt: notification.readAt ?? new Date().toISOString(),
        })),
      );
      return;
    }

    await api.post("/api/Notification/MarkAllAsRead");
  },
};

export default notificationService;

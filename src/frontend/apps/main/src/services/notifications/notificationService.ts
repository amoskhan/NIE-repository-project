import api from "../core/api";
import type { NotificationItem } from "@/types";
import { ENotificationType } from "@/services/notifications/notificationPreferencesService";
import { FRONTEND_CONSTANTS } from "@nie/platform";

const NOTIFICATION_STORAGE_KEY = "nie-template-demo-notifications";
const useDemoNotifications = FRONTEND_CONSTANTS.features.useDemoNotifications;

function createDemoNotifications(): NotificationItem[] {
  const now = Date.now();

  return [
    {
      id: "019fc37a-71b9-7858-86f2-9fea26d10e34",
      recipientType: "User",
      recipientUserId: null,
      recipientEmail: null,
      recipientName: "Devi Arputharajan",
      title: "Purchase order approved",
      message:
        "PO-2025-0003 has moved into the approved state and is ready for fulfilment.",
      type: ENotificationType.ApprovalUpdate,
      isRead: false,
      readAt: null,
      link: "/orders",
      sourceEntityType: "PurchaseOrder",
      sourceEntityId: "019fc37a-71b9-70b3-85a4-d3a9cce07de8",
      createdOn: new Date(now - 1000 * 60 * 18).toISOString(),
    },
    {
      id: "019fc37a-71b9-7ff2-84c5-f7a5d698a116",
      recipientType: "User",
      recipientUserId: null,
      recipientEmail: null,
      recipientName: "Devi Arputharajan",
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

  async markAsRead(id: string): Promise<void> {
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


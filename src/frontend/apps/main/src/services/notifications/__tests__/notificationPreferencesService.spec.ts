import { describe, expect, it } from "vitest";
import type { NotificationItem } from "../../../types";
import {
  ENotificationPreferenceKey,
  ENotificationType,
  canShowDesktopNotification,
  filterNotificationsByPreferences,
  normalizeNotificationPreferences,
  resolveNotificationPreferenceKey,
} from "../notificationPreferencesService";

function notification(id: string, type: ENotificationType): NotificationItem {
  return {
    id,
    recipientType: "User",
    title: `Notification ${id}`,
    message: `Message ${id}`,
    type,
    isRead: false,
    createdOn: new Date("2026-05-25T00:00:00.000Z").toISOString(),
  };
}

describe("notification preferences", () => {
  it("maps notification types to stable preference keys", () => {
    expect(
      resolveNotificationPreferenceKey(
        notification(
          "019fc37a-71b9-7858-86f2-9fea26d10e34",
          ENotificationType.ApprovalUpdate,
        ),
      ),
    ).toBe(ENotificationPreferenceKey.ApprovalDecisions);
    expect(
      resolveNotificationPreferenceKey(
        notification(
          "019fc37a-71b9-7ff2-84c5-f7a5d698a116",
          ENotificationType.CatalogRefresh,
        ),
      ),
    ).toBe(ENotificationPreferenceKey.CatalogRefreshes);
    expect(
      resolveNotificationPreferenceKey(
        notification(
          "019fc37a-71b9-7255-9908-f50c815425eb",
          ENotificationType.SystemAlert,
        ),
      ),
    ).toBe(ENotificationPreferenceKey.WorkspaceAnnouncements);
  });

  it("filters disabled notification types from inbox and desktop alerts", () => {
    const preferences = normalizeNotificationPreferences({
      desktopAlerts: true,
      subscriptions: {
        [ENotificationPreferenceKey.CatalogRefreshes]: false,
        [ENotificationPreferenceKey.WorkspaceAnnouncements]: false,
      },
    });

    const items: NotificationItem[] = [
      notification(
        "019fc37a-71b9-7858-86f2-9fea26d10e34",
        ENotificationType.ApprovalUpdate,
      ),
      notification(
        "019fc37a-71b9-7ff2-84c5-f7a5d698a116",
        ENotificationType.CatalogRefresh,
      ),
      notification(
        "019fc37a-71b9-7255-9908-f50c815425eb",
        ENotificationType.SystemAlert,
      ),
    ];

    expect(
      filterNotificationsByPreferences(items, preferences).map(
        (item: NotificationItem) => item.id,
      ),
    ).toEqual(["019fc37a-71b9-7858-86f2-9fea26d10e34"]);

    expect(
      canShowDesktopNotification(
        notification(
          "019fc37a-71b9-7ff2-84c5-f7a5d698a116",
          ENotificationType.CatalogRefresh,
        ),
        preferences,
        "granted",
      ),
    ).toBe(false);
  });
});

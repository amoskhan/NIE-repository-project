import { expect, test } from "@playwright/test";
import type { NotificationItem } from "../../../src/frontend/main/src/types";
import {
  ENotificationPreferenceKey,
  ENotificationType,
  canShowDesktopNotification,
  filterNotificationsByPreferences,
  normalizeNotificationPreferences,
  resolveNotificationPreferenceKey,
} from "../../../src/frontend/main/src/services/notificationPreferencesService";

function notification(id: number, type: ENotificationType): NotificationItem {
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

test.describe("notification preferences", () => {
  test("map notification types to stable preference keys", () => {
    expect(
      resolveNotificationPreferenceKey(
        notification(1, ENotificationType.ApprovalUpdate),
      ),
    ).toBe(ENotificationPreferenceKey.ApprovalDecisions);
    expect(
      resolveNotificationPreferenceKey(
        notification(2, ENotificationType.CatalogRefresh),
      ),
    ).toBe(ENotificationPreferenceKey.CatalogRefreshes);
    expect(
      resolveNotificationPreferenceKey(
        notification(3, ENotificationType.SystemAlert),
      ),
    ).toBe(ENotificationPreferenceKey.WorkspaceAnnouncements);
  });

  test("filter disabled notification types from inbox and desktop alerts", () => {
    const preferences = normalizeNotificationPreferences({
      desktopAlerts: true,
      subscriptions: {
        [ENotificationPreferenceKey.CatalogRefreshes]: false,
        [ENotificationPreferenceKey.WorkspaceAnnouncements]: false,
      },
    });

    const items = [
      notification(1, ENotificationType.ApprovalUpdate),
      notification(2, ENotificationType.CatalogRefresh),
      notification(3, ENotificationType.SystemAlert),
    ];

    expect(
      filterNotificationsByPreferences(items, preferences).map(
        (item) => item.id,
      ),
    ).toEqual([1]);
    expect(
      canShowDesktopNotification(
        notification(2, ENotificationType.CatalogRefresh),
        preferences,
        "granted",
      ),
    ).toBe(false);
  });
});

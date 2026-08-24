import { describe, expect, it } from "vitest";
import {
  buildNotificationPolicyUpdatePayload,
  validateNotificationPolicyTiming,
} from "@/components/admin/notifications/notificationPolicyTiming";
import type { NotificationPolicy } from "@/services/notifications/notificationAdministrationService";

function policy(
  overrides: Partial<NotificationPolicy> = {},
): NotificationPolicy {
  return {
    id: "0198fc41-bdf2-7a85-8475-7a2412147ebc",
    eventKey: "procurement.purchase-order.submitted",
    displayName: "Purchase order submitted",
    description: "Transactional update",
    category: "Order updates",
    inAppEnabled: true,
    emailEnabled: true,
    pushEnabled: false,
    isActive: true,
    supportsReminderConfiguration: false,
    reminderAfterHours: null,
    escalationAfterHours: null,
    ...overrides,
  };
}

describe("notification policy timing", () => {
  it("omits reminder and escalation values from non-reminder policies", () => {
    const payload = buildNotificationPolicyUpdatePayload(
      policy({ reminderAfterHours: 24, escalationAfterHours: 72 }),
    );

    expect(payload.reminderAfterHours).toBeNull();
    expect(payload.escalationAfterHours).toBeNull();
    expect(validateNotificationPolicyTiming(policy())).toBeNull();
  });

  it("allows either optional timing value for reminder policies", () => {
    const reminder = policy({
      eventKey: "procurement.purchase-order.delivery.reminder",
      supportsReminderConfiguration: true,
      reminderAfterHours: 24,
      escalationAfterHours: null,
    });

    expect(validateNotificationPolicyTiming(reminder)).toBeNull();
    expect(
      validateNotificationPolicyTiming(
        policy({
          supportsReminderConfiguration: true,
          reminderAfterHours: null,
          escalationAfterHours: 72,
        }),
      ),
    ).toBeNull();
    expect(buildNotificationPolicyUpdatePayload(reminder)).toMatchObject({
      reminderAfterHours: 24,
      escalationAfterHours: null,
    });
  });

  it("requires escalation to follow the reminder when both are configured", () => {
    const message = validateNotificationPolicyTiming(
      policy({
        supportsReminderConfiguration: true,
        reminderAfterHours: 72,
        escalationAfterHours: 24,
      }),
    );

    expect(message).toBe("Escalation must be later than the reminder");
  });
});

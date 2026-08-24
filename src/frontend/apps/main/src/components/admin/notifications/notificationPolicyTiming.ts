import { z } from "zod";
import type { NotificationPolicyUpdatePayload } from "@/services/notifications/notificationAdministrationService";

export interface NotificationPolicyTimingSource {
  inAppEnabled: boolean;
  emailEnabled: boolean;
  pushEnabled: boolean;
  isActive: boolean;
  supportsReminderConfiguration: boolean;
  reminderAfterHours: number | null;
  escalationAfterHours: number | null;
}

const reminderTimingSchema = z
  .object({
    reminderAfterHours: z.number().int().min(1).max(720).nullable(),
    escalationAfterHours: z.number().int().min(1).max(2160).nullable(),
  })
  .refine(
    ({ reminderAfterHours, escalationAfterHours }) =>
      reminderAfterHours === null ||
      escalationAfterHours === null ||
      escalationAfterHours > reminderAfterHours,
    {
      message: "Escalation must be later than the reminder",
      path: ["escalationAfterHours"],
    },
  );

export function validateNotificationPolicyTiming(
  policy: NotificationPolicyTimingSource,
): string | null {
  if (!policy.supportsReminderConfiguration) return null;

  const result = reminderTimingSchema.safeParse(policy);
  return result.success
    ? null
    : (result.error.issues[0]?.message ?? "Policy timing is invalid");
}

export function buildNotificationPolicyUpdatePayload(
  policy: NotificationPolicyTimingSource,
): NotificationPolicyUpdatePayload {
  const supportsTiming = policy.supportsReminderConfiguration;
  return {
    inAppEnabled: policy.inAppEnabled,
    emailEnabled: policy.emailEnabled,
    pushEnabled: policy.pushEnabled,
    isActive: policy.isActive,
    reminderAfterHours: supportsTiming ? policy.reminderAfterHours : null,
    escalationAfterHours: supportsTiming ? policy.escalationAfterHours : null,
  };
}

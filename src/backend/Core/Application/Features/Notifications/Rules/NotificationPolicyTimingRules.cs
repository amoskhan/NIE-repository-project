namespace Application.Features.Notifications;

public static class NotificationPolicyTimingRules
{
    public static IReadOnlyList<string> Validate(
        NotificationEventDefinition? definition,
        int? reminderAfterHours,
        int? escalationAfterHours)
    {
        var errors = new List<string>();

        if (definition?.SupportsReminderConfiguration != true)
        {
            if (reminderAfterHours.HasValue || escalationAfterHours.HasValue)
            {
                errors.Add("Reminder and escalation timing can be configured only for reminder emails");
            }

            return errors;
        }

        if (reminderAfterHours is < 1 or > 720)
        {
            errors.Add("Reminder hours must be between 1 and 720 when configured");
        }

        if (escalationAfterHours is < 1 or > 2160)
        {
            errors.Add("Escalation hours must be between 1 and 2160 when configured");
        }

        if (reminderAfterHours.HasValue &&
            escalationAfterHours.HasValue &&
            escalationAfterHours <= reminderAfterHours)
        {
            errors.Add("Escalation must be later than the reminder");
        }

        return errors;
    }
}

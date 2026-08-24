using Application.Features.Notifications;

namespace Application.Tests;

public sealed class NotificationPolicyTimingRulesTests
{
    private const string TimingOnlyForReminders =
        "Reminder and escalation timing can be configured only for reminder emails";
    private const string ReminderRange = "Reminder hours must be between 1 and 720 when configured";
    private const string EscalationRange = "Escalation hours must be between 1 and 2160 when configured";
    private const string EscalationOrder = "Escalation must be later than the reminder";

    private static NotificationEventDefinition Definition(bool supportsReminders) =>
        new(
            "procurement.purchase-order.approval.overdue",
            "Approval overdue",
            "Description",
            "Approval reminders",
            true,
            true,
            true,
            "Subject",
            "Content")
        {
            SupportsReminderConfiguration = supportsReminders,
        };

    [Fact]
    public void Accepts_an_unknown_event_that_configures_no_timing()
    {
        Assert.Empty(NotificationPolicyTimingRules.Validate(null, null, null));
    }

    [Theory]
    [InlineData(4, null)]
    [InlineData(null, 8)]
    [InlineData(4, 8)]
    public void Rejects_timing_on_an_unknown_event(int? reminder, int? escalation)
    {
        var errors = NotificationPolicyTimingRules.Validate(null, reminder, escalation);

        Assert.Equal(TimingOnlyForReminders, Assert.Single(errors));
    }

    [Theory]
    [InlineData(4, null)]
    [InlineData(null, 8)]
    public void Rejects_timing_on_an_event_that_does_not_support_reminders(int? reminder, int? escalation)
    {
        var errors = NotificationPolicyTimingRules.Validate(Definition(false), reminder, escalation);

        Assert.Equal(TimingOnlyForReminders, Assert.Single(errors));
    }

    [Fact]
    public void Accepts_an_event_without_reminder_support_when_no_timing_is_configured()
    {
        Assert.Empty(NotificationPolicyTimingRules.Validate(Definition(false), null, null));
    }

    [Fact]
    public void Accepts_a_reminder_event_that_leaves_timing_unset()
    {
        Assert.Empty(NotificationPolicyTimingRules.Validate(Definition(true), null, null));
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(-1, true)]
    [InlineData(1, false)]
    [InlineData(720, false)]
    [InlineData(721, true)]
    public void Bounds_the_reminder_window_between_one_and_seven_hundred_and_twenty_hours(
        int reminder,
        bool expectError)
    {
        var errors = NotificationPolicyTimingRules.Validate(Definition(true), reminder, null);

        Assert.Equal(expectError, errors.Contains(ReminderRange));
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(-5, true)]
    [InlineData(1, false)]
    [InlineData(2160, false)]
    [InlineData(2161, true)]
    public void Bounds_the_escalation_window_between_one_and_two_thousand_one_hundred_and_sixty_hours(
        int escalation,
        bool expectError)
    {
        var errors = NotificationPolicyTimingRules.Validate(Definition(true), null, escalation);

        Assert.Equal(expectError, errors.Contains(EscalationRange));
    }

    [Theory]
    [InlineData(24, 24)]
    [InlineData(24, 12)]
    public void Requires_escalation_to_fall_after_the_reminder(int reminder, int escalation)
    {
        var errors = NotificationPolicyTimingRules.Validate(Definition(true), reminder, escalation);

        Assert.Equal(EscalationOrder, Assert.Single(errors));
    }

    [Fact]
    public void Accepts_an_escalation_that_falls_after_the_reminder()
    {
        Assert.Empty(NotificationPolicyTimingRules.Validate(Definition(true), 24, 48));
    }

    [Fact]
    public void Reports_range_and_ordering_problems_together()
    {
        var errors = NotificationPolicyTimingRules.Validate(Definition(true), 0, 0);

        Assert.Equal(3, errors.Count);
        Assert.Contains(ReminderRange, errors);
        Assert.Contains(EscalationRange, errors);
        Assert.Contains(EscalationOrder, errors);
    }
}

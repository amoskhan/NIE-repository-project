using Api.Validation;
using Application.Contracts;
using Application.Features.Notifications;

namespace Api.Tests.Validation;

public sealed class NotificationRequestValidatorsTests
{
    private readonly UpdateNotificationPolicyDtoValidator _policy = new();
    private readonly TestNotificationDtoValidator _testNotification = new();

    [Fact]
    public void A_policy_with_no_reminder_timings_is_accepted()
    {
        Assert.True(_policy.Validate(new UpdateNotificationPolicyDto()).IsValid);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(24)]
    [InlineData(720)]
    public void Reminder_hours_are_accepted_across_the_supported_range(int hours)
    {
        var request = new UpdateNotificationPolicyDto { ReminderAfterHours = hours };

        Assert.True(_policy.Validate(request).IsValid);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(721)]
    public void Reminder_hours_outside_the_supported_range_are_rejected(int hours)
    {
        var request = new UpdateNotificationPolicyDto { ReminderAfterHours = hours };

        Assert.False(_policy.Validate(request).IsValid);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2161)]
    public void Escalation_hours_outside_the_supported_range_are_rejected(int hours)
    {
        var request = new UpdateNotificationPolicyDto { EscalationAfterHours = hours };

        Assert.False(_policy.Validate(request).IsValid);
    }

    [Theory]
    [InlineData(24, 24)]
    [InlineData(24, 12)]
    public void An_escalation_may_not_land_on_or_before_its_reminder(int reminderHours, int escalationHours)
    {
        var request = new UpdateNotificationPolicyDto
        {
            ReminderAfterHours = reminderHours,
            EscalationAfterHours = escalationHours,
        };

        Assert.False(_policy.Validate(request).IsValid);
    }

    [Fact]
    public void An_escalation_after_its_reminder_is_accepted()
    {
        var request = new UpdateNotificationPolicyDto
        {
            ReminderAfterHours = 24,
            EscalationAfterHours = 25,
        };

        Assert.True(_policy.Validate(request).IsValid);
    }

    [Fact]
    public void An_escalation_without_a_reminder_is_accepted()
    {
        var request = new UpdateNotificationPolicyDto { EscalationAfterHours = 48 };

        Assert.True(_policy.Validate(request).IsValid);
    }

    [Theory]
    [InlineData(NotificationChannels.InApp)]
    [InlineData(NotificationChannels.Email)]
    [InlineData(NotificationChannels.Push)]
    [InlineData("email")]
    [InlineData("PUSH")]
    public void Supported_test_channels_are_accepted_in_any_case(string channel)
    {
        var request = new TestNotificationDto { Channel = channel };

        Assert.True(_testNotification.Validate(request).IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("sms")]
    [InlineData("in-app")]
    public void An_unsupported_test_channel_is_rejected(string channel)
    {
        var request = new TestNotificationDto { Channel = channel };

        Assert.False(_testNotification.Validate(request).IsValid);
    }

    [Fact]
    public void A_test_email_address_is_only_validated_when_supplied()
    {
        var withoutEmail = new TestNotificationDto { Channel = NotificationChannels.Email };
        var withBadEmail = new TestNotificationDto { Channel = NotificationChannels.Email, Email = "not-an-address" };
        var withGoodEmail = new TestNotificationDto { Channel = NotificationChannels.Email, Email = "ada@example.edu.sg" };

        Assert.True(_testNotification.Validate(withoutEmail).IsValid);
        Assert.False(_testNotification.Validate(withBadEmail).IsValid);
        Assert.True(_testNotification.Validate(withGoodEmail).IsValid);
    }

    [Fact]
    public void A_template_must_carry_an_event_key_a_subject_and_content()
    {
        var validator = new SaveNotificationTemplateDtoValidator();
        var complete = new SaveNotificationTemplateDto
        {
            EventKey = "procurement.order.submitted",
            Subject = "Your order was submitted",
            Content = "<p>Hello</p>",
        };

        Assert.True(validator.Validate(complete).IsValid);
        Assert.False(validator.Validate(new SaveNotificationTemplateDto
        {
            EventKey = string.Empty,
            Subject = "s",
            Content = "c",
        }).IsValid);
        Assert.False(validator.Validate(new SaveNotificationTemplateDto
        {
            EventKey = "procurement.order.submitted",
            Subject = "   ",
            Content = "c",
        }).IsValid);
    }

    [Fact]
    public void Template_content_is_capped_to_keep_the_renderer_bounded()
    {
        var validator = new SaveNotificationTemplateDtoValidator();
        var request = new SaveNotificationTemplateDto
        {
            EventKey = "procurement.order.submitted",
            Subject = "Subject",
            Content = new string('x', 20_001),
        };

        Assert.False(validator.Validate(request).IsValid);
    }
}

using Application.Contracts;
using Application.Features.Notifications;
using FluentValidation;

namespace Api.Validation;

public sealed class UpdateNotificationPolicyDtoValidator : AbstractValidator<UpdateNotificationPolicyDto>
{
    public UpdateNotificationPolicyDtoValidator()
    {
        RuleFor(request => request.ReminderAfterHours)
            .Must(hours => hours is null or >= 1 and <= 720)
            .WithMessage("Reminder hours must be between 1 and 720 when configured");
        RuleFor(request => request.EscalationAfterHours)
            .Must(hours => hours is null or >= 1 and <= 2160)
            .WithMessage("Escalation hours must be between 1 and 2160 when configured")
            .Must((request, hours) =>
                hours is null ||
                request.ReminderAfterHours is null ||
                hours > request.ReminderAfterHours)
            .WithMessage("Escalation must be later than the reminder");
    }
}

public sealed class SaveNotificationTemplateDtoValidator : AbstractValidator<SaveNotificationTemplateDto>
{
    public SaveNotificationTemplateDtoValidator()
    {
        RuleFor(request => request.EventKey)
            .NotEmpty()
            .MaximumLength(160);
        RuleFor(request => request.Subject)
            .NotEmpty()
            .MaximumLength(240);
        RuleFor(request => request.Content)
            .NotEmpty()
            .MaximumLength(20_000);
    }
}

public sealed class TestNotificationDtoValidator : AbstractValidator<TestNotificationDto>
{
    private static readonly string[] SupportedChannels =
    [
        NotificationChannels.InApp,
        NotificationChannels.Email,
        NotificationChannels.Push,
    ];

    public TestNotificationDtoValidator()
    {
        RuleFor(request => request.Channel)
            .NotEmpty()
            .Must(channel => SupportedChannels.Contains(channel, StringComparer.OrdinalIgnoreCase))
            .WithMessage("Channel must be InApp, Email, or Push.");
        RuleFor(request => request.Email)
            .EmailAddress()
            .MaximumLength(320)
            .When(request => !string.IsNullOrWhiteSpace(request.Email));
    }
}

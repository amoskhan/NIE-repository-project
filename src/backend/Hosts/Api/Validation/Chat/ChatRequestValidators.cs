using Api.Controllers;
using FluentValidation;

namespace Api.Validation;

public sealed class CreateConversationRequestValidator : AbstractValidator<CreateConversationRequest>
{
    public CreateConversationRequestValidator()
    {
        RuleFor(request => request.Title).MaximumLength(200);
        RuleFor(request => request.Source).MaximumLength(50);
    }
}

public sealed class RenameConversationRequestValidator : AbstractValidator<RenameConversationRequest>
{
    public RenameConversationRequestValidator()
    {
        RuleFor(request => request.Title).NotEmpty().MaximumLength(200);
    }
}

public sealed class FeedbackRequestValidator : AbstractValidator<FeedbackRequest>
{
    private static readonly string[] SupportedFeedbackTypes = ["thumbs_up", "thumbs_down"];

    public FeedbackRequestValidator()
    {
        RuleFor(request => request.Type)
            .NotEmpty()
            .Must(type => SupportedFeedbackTypes.Contains(type, StringComparer.Ordinal))
            .WithMessage("Feedback type must be thumbs_up or thumbs_down.");
        RuleFor(request => request.Comment).MaximumLength(1_000);
    }
}

public sealed class SendMessageRequestValidator : AbstractValidator<SendMessageRequest>
{
    public SendMessageRequestValidator()
    {
        RuleFor(request => request.Content).NotEmpty().MaximumLength(4_000);
    }
}

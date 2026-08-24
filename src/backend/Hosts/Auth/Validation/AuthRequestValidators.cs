using Auth.Models;
using FluentValidation;

namespace Auth.Validation;

public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(request => request.userid).NotEmpty().MaximumLength(100);
        RuleFor(request => request.pd).NotEmpty().MaximumLength(512);
    }
}

public sealed class SsoCallbackRequestValidator : AbstractValidator<SsoCallbackRequest>
{
    public SsoCallbackRequestValidator()
    {
        RuleFor(request => request.state).NotEmpty().MaximumLength(512);
        RuleFor(request => request.encryptedPayload).NotEmpty().MaximumLength(32_768);
    }
}

public sealed class CreateTestSessionRequestValidator : AbstractValidator<CreateTestSessionRequest>
{
    public CreateTestSessionRequestValidator()
    {
        RuleFor(request => request.UserId).MaximumLength(100);
        RuleFor(request => request.Name).MaximumLength(200);
        RuleFor(request => request.Email)
            .MaximumLength(320)
            .EmailAddress()
            .When(request => !string.IsNullOrWhiteSpace(request.Email));
        RuleFor(request => request.Department).MaximumLength(200);
    }
}

using Api.Controllers;
using FluentValidation;

namespace Api.Validation;

public sealed class MyInfoCallbackRequestValidator : AbstractValidator<MyInfoCallbackRequest>
{
    public MyInfoCallbackRequestValidator()
    {
        RuleFor(request => request.AuthCode).NotEmpty().MaximumLength(4_096);
        RuleFor(request => request.State).NotEmpty().MaximumLength(512);
    }
}

public sealed class TransitionRequestValidator : AbstractValidator<TransitionRequest>
{
    public TransitionRequestValidator()
    {
        RuleFor(request => request.ToState)
            .NotEmpty()
            .MaximumLength(100);
        RuleFor(request => request.Remarks).MaximumLength(2_000);
    }
}

using Contracts.Events.VendorMaster;
using FluentValidation;

namespace Application.Integration.Validation;

public sealed class VendorProfileChangedV1Validator : AbstractValidator<VendorProfileChangedV1>
{
    public VendorProfileChangedV1Validator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(message => message.VendorCode)
            .NotEmpty()
            .MaximumLength(50)
            .Matches("^[A-Za-z0-9][A-Za-z0-9._-]*$");
        RuleFor(message => message.Name)
            .NotEmpty()
            .MaximumLength(200);
        RuleFor(message => message.ContactPerson).MaximumLength(200);
        RuleFor(message => message.Email)
            .MaximumLength(320)
            .EmailAddress()
            .When(message => !string.IsNullOrWhiteSpace(message.Email));
        RuleFor(message => message.Phone).MaximumLength(50);
        RuleFor(message => message.Address).MaximumLength(1_000);
        RuleFor(message => message.Category).MaximumLength(100);
        RuleFor(message => message.ChangedAtUtc).NotEqual(default(DateTimeOffset));
    }
}

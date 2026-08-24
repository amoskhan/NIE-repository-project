using Application.Contracts.Report;
using FluentValidation;

namespace Api.Validation;

public sealed class ReportRequestDtoValidator : AbstractValidator<ReportRequestDto>
{
    private static readonly string[] SupportedFormats = ["A4", "A3", "A5", "Letter", "Legal"];
    private static readonly string[] SupportedOrientations = ["Portrait", "Landscape"];

    public ReportRequestDtoValidator()
    {
        RuleFor(request => request.ReportType).NotEmpty().MaximumLength(100);
        RuleFor(request => request.Status).MaximumLength(50);
        RuleFor(request => request.VendorId)
            .Must(id => !id.HasValue || id.Value != Guid.Empty)
            .WithMessage("Vendor identifier must be a non-empty UUID.");
        RuleFor(request => request.Category).MaximumLength(100);
        RuleFor(request => request.UserId).MaximumLength(100);
        RuleFor(request => request.DateTo)
            .GreaterThanOrEqualTo(request => request.DateFrom)
            .When(request => request.DateFrom.HasValue && request.DateTo.HasValue)
            .WithMessage("End date must be on or after start date.");
        RuleFor(request => request.Format)
            .Must(value => value is null || SupportedFormats.Contains(value, StringComparer.OrdinalIgnoreCase))
            .WithMessage("Format must be A4, A3, A5, Letter, or Legal.");
        RuleFor(request => request.Orientation)
            .Must(value => value is null || SupportedOrientations.Contains(value, StringComparer.OrdinalIgnoreCase))
            .WithMessage("Orientation must be Portrait or Landscape.");
    }
}

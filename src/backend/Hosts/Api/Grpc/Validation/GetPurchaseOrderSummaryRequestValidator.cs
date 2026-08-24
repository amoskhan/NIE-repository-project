using Contracts.Grpc.Procurement.V1;
using FluentValidation;

namespace Api.Grpc.Validation;

public sealed class GetPurchaseOrderSummaryRequestValidator
    : AbstractValidator<GetPurchaseOrderSummaryRequest>
{
    public GetPurchaseOrderSummaryRequestValidator()
    {
        RuleFor(request => request.PurchaseOrderId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MaximumLength(36)
            .Must(BeUuidVersion7)
            .WithMessage("A UUIDv7 purchase order identifier is required.");
    }

    private static bool BeUuidVersion7(string value) =>
        Guid.TryParse(value, out var identifier)
        && identifier != Guid.Empty
        && identifier.Version == 7;
}

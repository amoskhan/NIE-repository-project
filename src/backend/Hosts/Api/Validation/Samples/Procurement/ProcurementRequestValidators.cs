using Application.Contracts;
using FluentValidation;

namespace Api.Validation.Samples.Procurement;

public sealed class VendorDtoValidator : AbstractValidator<VendorDto>
{
    public VendorDtoValidator()
    {
        RuleFor(vendor => vendor.Name).NotEmpty().MaximumLength(200);
        RuleFor(vendor => vendor.Code).NotEmpty().MaximumLength(50);
        RuleFor(vendor => vendor.ContactPerson).MaximumLength(200);
        RuleFor(vendor => vendor.Email)
            .MaximumLength(320)
            .EmailAddress()
            .When(vendor => !string.IsNullOrWhiteSpace(vendor.Email));
        RuleFor(vendor => vendor.Phone).MaximumLength(50);
        RuleFor(vendor => vendor.Address).MaximumLength(1_000);
        RuleFor(vendor => vendor.Category).MaximumLength(100);
        RuleFor(vendor => vendor.Notes).MaximumLength(2_000);
    }
}

public sealed class CatalogItemDtoValidator : AbstractValidator<CatalogItemDto>
{
    public CatalogItemDtoValidator()
    {
        RuleFor(item => item.Name).NotEmpty().MaximumLength(200);
        RuleFor(item => item.Sku).NotEmpty().MaximumLength(100);
        RuleFor(item => item.Description).MaximumLength(2_000);
        RuleFor(item => item.Category).MaximumLength(100);
        RuleFor(item => item.UnitOfMeasure).MaximumLength(50);
        RuleFor(item => item.UnitPrice).GreaterThanOrEqualTo(0).LessThanOrEqualTo(100_000_000);
        RuleFor(item => item.VendorId).NotEmpty();
    }
}

public sealed class PurchaseOrderDtoValidator : AbstractValidator<PurchaseOrderDto>
{
    public PurchaseOrderDtoValidator()
    {
        RuleFor(order => order.VendorId).NotEmpty().WithMessage("Vendor is required.");
        RuleFor(order => order.DeliveryAddress).MaximumLength(500);
        RuleFor(order => order.Notes).MaximumLength(2_000);
        RuleFor(order => order.Lines)
            .NotNull()
            .NotEmpty()
            .WithMessage("At least one line item is required.")
            .Must(lines => lines.Count <= 100)
            .WithMessage("A purchase order cannot contain more than 100 line items.");
        RuleForEach(order => order.Lines).SetValidator(new PurchaseOrderLineDtoValidator());
    }
}

public sealed class ApprovalActionDtoValidator : AbstractValidator<ApprovalActionDto>
{
    public ApprovalActionDtoValidator()
    {
        RuleFor(action => action.PurchaseOrderId).NotEmpty();
        RuleFor(action => action.Action).IsInEnum();
        RuleFor(action => action.Comments).MaximumLength(2_000);
    }
}

public sealed class PurchaseOrderSearchDtoValidator : AbstractValidator<PurchaseOrderSearchDto>
{
    public PurchaseOrderSearchDtoValidator()
    {
        RuleFor(search => search.Search).MaximumLength(200);
        RuleFor(search => search.VendorId)
            .Must(id => !id.HasValue || id.Value != Guid.Empty)
            .WithMessage("Vendor identifier must be a non-empty UUID.");
        RuleFor(search => search.SortBy).MaximumLength(100);
        RuleFor(search => search.ToDate)
            .GreaterThanOrEqualTo(search => search.FromDate)
            .When(search => search.FromDate.HasValue && search.ToDate.HasValue)
            .WithMessage("To date must be on or after from date.");
    }
}

internal sealed class PurchaseOrderLineDtoValidator : AbstractValidator<PurchaseOrderLineDto>
{
    public PurchaseOrderLineDtoValidator()
    {
        RuleFor(line => line.LineNumber).GreaterThan(0);
        RuleFor(line => line.ItemName).NotEmpty().MaximumLength(200);
        RuleFor(line => line.Description).MaximumLength(2_000);
        RuleFor(line => line.UnitOfMeasure).MaximumLength(50);
        RuleFor(line => line.Quantity).GreaterThan(0).LessThanOrEqualTo(1_000_000);
        RuleFor(line => line.UnitPrice).GreaterThanOrEqualTo(0).LessThanOrEqualTo(100_000_000);
        RuleFor(line => line.CatalogItemId)
            .Must(id => !id.HasValue || id.Value != Guid.Empty)
            .WithMessage("Catalog item identifier must be a non-empty UUID.");
    }
}

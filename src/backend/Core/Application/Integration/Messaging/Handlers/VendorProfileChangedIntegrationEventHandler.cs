using Application.Abstractions;
using Contracts.Events.VendorMaster;
using Contracts.Integration;
using Domain.Models;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Application.Integration;

public sealed class VendorProfileChangedIntegrationEventHandler(
    IApplicationDbContext dbContext,
    IValidator<VendorProfileChangedV1> validator) : IntegrationEventHandler<VendorProfileChangedV1>
{
    private readonly IApplicationDbContext _dbContext = dbContext;
    private readonly IValidator<VendorProfileChangedV1> _validator = validator;

    public override IntegrationContractDescriptor Contract =>
        IntegrationContractCatalog.VendorProfileChanged;

    protected override async Task HandleAsync(
        VendorProfileChangedV1 payload,
        IntegrationEventContext context,
        CancellationToken cancellationToken)
    {
        var validation = await _validator.ValidateAsync(payload, cancellationToken);
        if (!validation.IsValid)
        {
            throw new PermanentIntegrationEventException(
                "The vendor profile event payload failed contract validation.");
        }

        var normalizedCode = payload.VendorCode.Trim().ToUpperInvariant();
        var changedAtUtc = payload.ChangedAtUtc.ToUniversalTime();
        var vendors = _dbContext.Set<Vendor>();
        var vendor = await vendors.SingleOrDefaultAsync(
            candidate => candidate.Code == normalizedCode,
            cancellationToken);
        if (vendor is null)
        {
            vendor = new Vendor { Code = normalizedCode };
            vendors.Add(vendor);
        }
        else if (vendor.SourceChangedAtUtc >= changedAtUtc)
        {
            return;
        }

        vendor.Name = payload.Name.Trim();
        vendor.ContactPerson = Normalize(payload.ContactPerson);
        vendor.Email = Normalize(payload.Email);
        vendor.Phone = Normalize(payload.Phone);
        vendor.Address = Normalize(payload.Address);
        vendor.Category = Normalize(payload.Category);
        vendor.IsActive = payload.IsActive;
        vendor.SourceChangedAtUtc = changedAtUtc;
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

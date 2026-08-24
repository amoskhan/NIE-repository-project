using Contracts.Events.Procurement;
using Contracts.Integration;
using Domain.Models;
using Infrastructure.Integrations;
using Microsoft.EntityFrameworkCore;

namespace Integration.Tests;

public class IntegrationOutboxAtomicityTests
{
    [Fact]
    public async Task Domain_mutation_and_outbox_row_commit_or_roll_back_as_one_unit()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var database = await PostgresTestDatabase.CreateAsync(cancellationToken);
        var vendorCode = $"ATOMIC-{Guid.CreateVersion7():N}";

        await using (var successContext = database.CreateContext())
        {
            await successContext.Database.EnsureCreatedAsync(cancellationToken);
            successContext.Vendors.Add(CreateVendor(vendorCode, "Atomic Supplier"));
            var publisher = new EfIntegrationEventPublisher(successContext, "procurement");
            await publisher.EnqueueAsync(
                IntegrationContractCatalog.PurchaseOrderStatusChanged,
                CreatePayload("PO-ATOMIC-SUCCESS"),
                cancellationToken: cancellationToken);

            await successContext.SaveChangesAsync(cancellationToken);
        }

        await using (var successVerification = database.CreateContext())
        {
            Assert.Equal(
                1,
                await successVerification.Vendors.CountAsync(
                    vendor => vendor.Code == vendorCode,
                    cancellationToken));
            Assert.Equal(
                1,
                await successVerification.IntegrationOutboxMessages.CountAsync(cancellationToken));
        }

        await using (var failureContext = database.CreateContext())
        {
            failureContext.Vendors.Add(CreateVendor(vendorCode, "Duplicate Supplier"));
            var publisher = new EfIntegrationEventPublisher(failureContext, "procurement");
            await publisher.EnqueueAsync(
                IntegrationContractCatalog.PurchaseOrderStatusChanged,
                CreatePayload("PO-ATOMIC-ROLLBACK"),
                cancellationToken: cancellationToken);

            await Assert.ThrowsAsync<DbUpdateException>(() =>
                failureContext.SaveChangesAsync(cancellationToken));
        }

        await using var rollbackVerification = database.CreateContext();
        Assert.Equal(
            1,
            await rollbackVerification.Vendors.CountAsync(
                vendor => vendor.Code == vendorCode,
                cancellationToken));
        Assert.Equal(
            1,
            await rollbackVerification.IntegrationOutboxMessages.CountAsync(cancellationToken));
    }

    private static Vendor CreateVendor(string code, string name) => new()
    {
        Code = code,
        Name = name,
        IsActive = true,
    };

    private static PurchaseOrderStatusChangedV1 CreatePayload(string purchaseOrderNumber) => new(
        Guid.CreateVersion7(),
        purchaseOrderNumber,
        "Draft",
        "PendingManagerApproval",
        1250m,
        "SGD",
        DateTimeOffset.UtcNow);
}

using Contracts.Events.Procurement;
using Contracts.Events.VendorMaster;

namespace Contracts.Integration;

/// <summary>
/// Compile-time catalog matching the application integration manifest.
/// </summary>
public static class IntegrationContractCatalog
{
    public static readonly IntegrationContractDescriptor PurchaseOrderStatusChanged = new(
        "nie.procurement.purchase-order.status-changed",
        1,
        typeof(PurchaseOrderStatusChangedV1));

    public static readonly IntegrationContractDescriptor VendorProfileChanged = new(
        "nie.vendor-master.vendor-profile.changed",
        1,
        typeof(VendorProfileChangedV1));

    public static IReadOnlyList<IntegrationContractDescriptor> Published { get; } =
        [PurchaseOrderStatusChanged];

    public static IReadOnlyList<IntegrationContractDescriptor> Subscribed { get; } =
        [VendorProfileChanged];
}

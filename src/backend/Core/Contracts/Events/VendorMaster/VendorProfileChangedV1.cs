namespace Contracts.Events.VendorMaster;

/// <summary>
/// Consumed when the authoritative vendor-master application changes a vendor profile.
/// </summary>
public sealed record VendorProfileChangedV1(
    string VendorCode,
    string Name,
    string? ContactPerson,
    string? Email,
    string? Phone,
    string? Address,
    string? Category,
    bool IsActive,
    DateTimeOffset ChangedAtUtc);

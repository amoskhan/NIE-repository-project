namespace Application.Integration;

public sealed record VendorSnapshot(
    Guid VendorId,
    string VendorCode,
    string Name,
    bool IsActive);

public interface IVendorDirectoryClient
{
    Task<VendorSnapshot?> GetVendorSnapshotAsync(
        string vendorCode,
        CancellationToken cancellationToken);
}

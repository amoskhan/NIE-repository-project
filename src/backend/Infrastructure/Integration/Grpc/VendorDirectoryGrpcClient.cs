using Application.Integration;
using Contracts.Grpc.Vendor.V1;
using Grpc.Core;
using Infrastructure.Integration.Options;
using Microsoft.Extensions.Options;

namespace Infrastructure.Integration.Grpc;

public sealed class VendorDirectoryGrpcClient(
    VendorDirectory.VendorDirectoryClient client,
    IOptions<ServiceIntegrationOptions> options) : IVendorDirectoryClient
{
    private readonly VendorDirectory.VendorDirectoryClient _client = client;
    private readonly GrpcIntegrationOptions _options = options.Value.Grpc;

    public async Task<VendorSnapshot?> GetVendorSnapshotAsync(
        string vendorCode,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(vendorCode))
        {
            throw new ArgumentException("The vendor code is required.", nameof(vendorCode));
        }

        try
        {
            var response = await _client.GetVendorSnapshotAsync(
                new GetVendorSnapshotRequest { VendorCode = vendorCode.Trim() },
                deadline: DateTime.UtcNow.AddMilliseconds(_options.DeadlineMilliseconds),
                cancellationToken: cancellationToken);
            if (!Guid.TryParse(response.VendorId, out var vendorId)
                || vendorId == Guid.Empty
                || vendorId.Version != 7)
            {
                throw new RpcException(new Status(
                    StatusCode.DataLoss,
                    "The vendor service returned an invalid UUIDv7 identifier."));
            }

            return new VendorSnapshot(
                vendorId,
                response.VendorCode,
                response.Name,
                response.IsActive);
        }
        catch (RpcException exception) when (exception.StatusCode == StatusCode.NotFound)
        {
            return null;
        }
    }
}

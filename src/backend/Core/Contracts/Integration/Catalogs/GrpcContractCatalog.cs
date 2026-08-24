using Contracts.Grpc.Procurement.V1;
using Contracts.Grpc.Vendor.V1;
using Google.Protobuf.Reflection;

namespace Contracts.Integration;

/// <summary>
/// Describes one generated gRPC service contract and its distributable protobuf source.
/// </summary>
public sealed record GrpcContractDescriptor(
    string Service,
    string ProtoPath,
    IReadOnlyList<string> Methods);

/// <summary>
/// Compile-time gRPC direction catalog matching the application integration manifest.
/// </summary>
public static class GrpcContractCatalog
{
    public static readonly GrpcContractDescriptor ProcurementQueryService = Create(
        ProcurementQuery.Descriptor,
        "Protos/procurement/v1/procurement_query.proto");

    public static readonly GrpcContractDescriptor VendorDirectoryService = Create(
        VendorDirectory.Descriptor,
        "Protos/vendor/v1/vendor_directory.proto");

    public static IReadOnlyList<GrpcContractDescriptor> Provided { get; } =
        [ProcurementQueryService];

    public static IReadOnlyList<GrpcContractDescriptor> Consumed { get; } =
        [VendorDirectoryService];

    private static GrpcContractDescriptor Create(
        ServiceDescriptor descriptor,
        string protoPath) =>
        new(
            descriptor.FullName,
            protoPath,
            descriptor.Methods.Select(method => method.Name).ToArray());
}

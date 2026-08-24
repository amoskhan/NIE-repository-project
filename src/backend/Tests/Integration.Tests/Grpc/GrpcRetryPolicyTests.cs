using Contracts.Grpc.Vendor.V1;
using Infrastructure.Integration;

namespace Integration.Tests.Grpc;

public sealed class GrpcRetryPolicyTests
{
    [Fact]
    public void One_attempt_disables_the_retry_policy()
    {
        var serviceConfig = ServiceIntegrationServiceCollectionExtensions
            .CreateReadOnlyRetryServiceConfig(1);

        Assert.Null(serviceConfig);
    }

    [Fact]
    public void Multiple_attempts_target_only_the_idempotent_vendor_lookup()
    {
        var serviceConfig = ServiceIntegrationServiceCollectionExtensions
            .CreateReadOnlyRetryServiceConfig(3);

        var methodConfig = Assert.Single(serviceConfig!.MethodConfigs);
        var method = Assert.Single(methodConfig.Names);
        Assert.Equal(VendorDirectory.Descriptor.FullName, method.Service);
        Assert.Equal("GetVendorSnapshot", method.Method);
        Assert.Equal(3, methodConfig.RetryPolicy!.MaxAttempts);
    }
}

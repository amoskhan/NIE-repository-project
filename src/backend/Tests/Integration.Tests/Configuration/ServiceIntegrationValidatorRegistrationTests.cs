using Api;
using Application.Integration;
using Contracts.Events.VendorMaster;
using Contracts.Grpc.Procurement.V1;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Validation;

namespace Integration.Tests;

public sealed class ServiceIntegrationValidatorRegistrationTests
{
    [Fact]
    public void Production_assembly_scanning_registers_both_trust_boundary_validators()
    {
        var services = new ServiceCollection();
        services
            .AddControllers()
            .AddNieRequestValidation(
                typeof(Program).Assembly,
                typeof(VendorProfileChangedIntegrationEventHandler).Assembly);
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        Assert.NotNull(scope.ServiceProvider
            .GetService<IValidator<GetPurchaseOrderSummaryRequest>>());
        Assert.NotNull(scope.ServiceProvider
            .GetService<IValidator<VendorProfileChangedV1>>());
    }
}

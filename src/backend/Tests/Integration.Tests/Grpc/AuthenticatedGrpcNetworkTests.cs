using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using Api.Grpc;
using Api.Grpc.Validation;
using Application.Integration;
using Contracts.Grpc.Procurement.V1;
using Grpc.Core;
using Grpc.Health.V1;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;

namespace Integration.Tests.Grpc;

public sealed class AuthenticatedGrpcNetworkTests
{
    [Fact]
    public async Task Http2_endpoint_rejects_missing_token_and_accepts_valid_audience_and_scope()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var issuer = "https://identity.test";
        var audience = "procurement-query";
        var signingKey = new SymmetricSecurityKey(RandomNumberGenerator.GetBytes(32));
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = "Development",
        });
        builder.WebHost.ConfigureKestrel(options =>
            options.Listen(IPAddress.Loopback, 0, listen => listen.Protocols = HttpProtocols.Http2));
        builder.Services.AddGrpc();
        builder.Services.AddGrpcHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy());
        builder.Services.Configure<HealthCheckPublisherOptions>(options =>
        {
            options.Delay = TimeSpan.Zero;
            options.Period = TimeSpan.FromSeconds(1);
        });
        builder.Services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = issuer,
                    ValidateAudience = true,
                    ValidAudience = audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = signingKey,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero,
                };
            });
        builder.Services.AddAuthorization(options =>
            options.AddPolicy("ServiceIntegration", policy =>
            {
                policy.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme);
                policy.RequireAuthenticatedUser();
                policy.RequireAssertion(context =>
                    ServiceIntegrationAuthorization.HasRequiredScope(
                        context.User,
                        "procurement-query.read"));
            }));
        var purchaseOrderId = Guid.CreateVersion7();
        builder.Services.AddSingleton<IProcurementIntegrationQuery>(
            new NetworkTestQuery(purchaseOrderId));
        builder.Services.AddSingleton<FluentValidation.IValidator<GetPurchaseOrderSummaryRequest>>(
            new GetPurchaseOrderSummaryRequestValidator());

        await using var app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapGrpcService<ProcurementQueryGrpcService>()
            .RequireAuthorization("ServiceIntegration");
        app.MapGrpcHealthChecksService().AllowAnonymous();
        await app.StartAsync(cancellationToken);

        var server = app.Services.GetRequiredService<IServer>();
        var address = Assert.Single(server.Features.Get<IServerAddressesFeature>()!.Addresses);
        using var channel = GrpcChannel.ForAddress(address);
        var client = new ProcurementQuery.ProcurementQueryClient(channel);
        var request = new GetPurchaseOrderSummaryRequest
        {
            PurchaseOrderId = purchaseOrderId.ToString("D"),
        };

        var health = new Health.HealthClient(channel);
        var healthResponse = await WaitForServingAsync(health, cancellationToken);
        Assert.Equal(HealthCheckResponse.Types.ServingStatus.Serving, healthResponse.Status);

        var unauthorized = await Assert.ThrowsAsync<RpcException>(async () =>
            await client.GetPurchaseOrderSummaryAsync(
                request,
                deadline: DateTime.UtcNow.AddSeconds(5),
                cancellationToken: cancellationToken));
        Assert.Equal(StatusCode.Unauthenticated, unauthorized.StatusCode);

        var insufficientToken = CreateToken(
            issuer,
            audience,
            signingKey,
            "different-scope");
        var insufficientHeaders = new Metadata
        {
            { "Authorization", $"Bearer {insufficientToken}" },
        };
        var forbidden = await Assert.ThrowsAsync<RpcException>(async () =>
            await client.GetPurchaseOrderSummaryAsync(
                request,
                insufficientHeaders,
                deadline: DateTime.UtcNow.AddSeconds(5),
                cancellationToken: cancellationToken));
        Assert.Equal(StatusCode.PermissionDenied, forbidden.StatusCode);

        var token = CreateToken(
            issuer,
            audience,
            signingKey,
            "procurement-query.read");
        var headers = new Metadata { { "Authorization", $"Bearer {token}" } };
        var response = await client.GetPurchaseOrderSummaryAsync(
            request,
            headers,
            deadline: DateTime.UtcNow.AddSeconds(5),
            cancellationToken: cancellationToken);

        Assert.Equal(purchaseOrderId.ToString("D"), response.PurchaseOrderId);
        await app.StopAsync(cancellationToken);
    }

    private static async Task<HealthCheckResponse> WaitForServingAsync(
        Health.HealthClient health,
        CancellationToken cancellationToken)
    {
        var timeoutAt = DateTime.UtcNow.AddSeconds(5);
        while (DateTime.UtcNow < timeoutAt)
        {
            var response = await health.CheckAsync(
                new HealthCheckRequest(),
                deadline: timeoutAt,
                cancellationToken: cancellationToken);
            if (response.Status == HealthCheckResponse.Types.ServingStatus.Serving)
            {
                return response;
            }

            await Task.Delay(100, cancellationToken);
        }

        throw new TimeoutException("The anonymous gRPC readiness service did not become Serving.");
    }

    private static string CreateToken(
        string issuer,
        string audience,
        SecurityKey signingKey,
        string scope)
    {
        var token = new JwtSecurityToken(
            issuer,
            audience,
            [new Claim("scope", scope)],
            notBefore: DateTime.UtcNow.AddMinutes(-1),
            expires: DateTime.UtcNow.AddMinutes(5),
            signingCredentials: new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private sealed class NetworkTestQuery(Guid purchaseOrderId) : IProcurementIntegrationQuery
    {
        public Task<ProcurementPurchaseOrderSummary?> GetPurchaseOrderSummaryAsync(
            Guid requestedId,
            CancellationToken cancellationToken) =>
            Task.FromResult<ProcurementPurchaseOrderSummary?>(
                new ProcurementPurchaseOrderSummary(
                    purchaseOrderId,
                    "PO-2026-00999",
                    "Approved",
                    Guid.CreateVersion7(),
                    "NIE Supplier",
                    42m,
                    "SGD"));
    }
}

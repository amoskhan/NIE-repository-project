using System.Net.Http.Headers;
using Application.Integration;
using Infrastructure.Integration.Options;
using Microsoft.Extensions.Options;

namespace Infrastructure.Integration.Grpc;

public sealed class ServiceAccessTokenHandler(
    IServiceAccessTokenProvider accessTokenProvider,
    IOptions<ServiceIntegrationOptions> options) : DelegatingHandler
{
    private readonly IServiceAccessTokenProvider _accessTokenProvider = accessTokenProvider;
    private readonly GrpcIntegrationOptions _options = options.Value.Grpc;

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (_options.RequireAuthentication)
        {
            var accessToken = await _accessTokenProvider.GetAccessTokenAsync(cancellationToken);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}

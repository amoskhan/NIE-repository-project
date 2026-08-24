using System.Net;
using System.Text;
using Application.Integration;
using Infrastructure.Integration.Grpc;
using Infrastructure.Integration.Options;
using Microsoft.Extensions.Options;

namespace Integration.Tests.Grpc;

public sealed class OAuthClientCredentialsAccessTokenProviderTests
{
    [Fact]
    public async Task Provider_uses_client_credentials_and_caches_the_token()
    {
        var handler = new TokenEndpointHandler();
        using var provider = new OAuthClientCredentialsAccessTokenProvider(
            new TestHttpClientFactory(handler),
            Options.Create(CreateOptions()));

        var first = await provider.GetAccessTokenAsync(TestContext.Current.CancellationToken);
        var second = await provider.GetAccessTokenAsync(TestContext.Current.CancellationToken);

        Assert.Equal("test-access-token", first);
        Assert.Equal(first, second);
        Assert.Equal(1, handler.CallCount);
        Assert.Contains("grant_type=client_credentials", handler.RequestBody, StringComparison.Ordinal);
        Assert.Contains("scope=vendor-directory.read", handler.RequestBody, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Delegating_handler_adds_the_bearer_token_only_when_required()
    {
        var terminal = new AuthorizationHeaderHandler();
        var handler = new ServiceAccessTokenHandler(
            new StaticTokenProvider(),
            Options.Create(CreateOptions()))
        {
            InnerHandler = terminal,
        };
        using var client = new HttpClient(handler);

        using var response = await client.GetAsync(
            "https://peer.internal/query",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Bearer", terminal.Scheme);
        Assert.Equal("test-access-token", terminal.Parameter);
    }

    private static ServiceIntegrationOptions CreateOptions() => new()
    {
        Enabled = true,
        Grpc = new GrpcIntegrationOptions
        {
            Enabled = true,
            RequireAuthentication = true,
            TokenEndpoint = "https://identity.internal/oauth2/token",
            ClientId = "procurement-service",
            ClientSecret = "not-a-secret-test-value",
            Scope = "vendor-directory.read",
        },
    };

    private sealed class TestHttpClientFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
    }

    private sealed class TokenEndpointHandler : HttpMessageHandler
    {
        public int CallCount { get; private set; }

        public string RequestBody { get; private set; } = string.Empty;

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            CallCount++;
            RequestBody = await request.Content!.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    "{\"access_token\":\"test-access-token\",\"expires_in\":3600}",
                    Encoding.UTF8,
                    "application/json"),
            };
        }
    }

    private sealed class StaticTokenProvider : IServiceAccessTokenProvider
    {
        public Task<string> GetAccessTokenAsync(CancellationToken cancellationToken) =>
            Task.FromResult("test-access-token");
    }

    private sealed class AuthorizationHeaderHandler : HttpMessageHandler
    {
        public string? Scheme { get; private set; }

        public string? Parameter { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Scheme = request.Headers.Authorization?.Scheme;
            Parameter = request.Headers.Authorization?.Parameter;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }
}

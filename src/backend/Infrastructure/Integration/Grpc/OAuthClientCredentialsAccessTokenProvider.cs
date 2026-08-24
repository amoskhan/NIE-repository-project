using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Application.Integration;
using Infrastructure.Integration.Options;
using Microsoft.Extensions.Options;

namespace Infrastructure.Integration.Grpc;

public sealed class OAuthClientCredentialsAccessTokenProvider(
    IHttpClientFactory httpClientFactory,
    IOptions<ServiceIntegrationOptions> options) : IServiceAccessTokenProvider, IDisposable
{
    public const string HttpClientName = "ServiceIntegrationOAuth";

    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly GrpcIntegrationOptions _options = options.Value.Grpc;
    private readonly SemaphoreSlim _refreshGate = new(1, 1);
    private string? _accessToken;
    private DateTimeOffset _refreshAtUtc;

    public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(_accessToken) && DateTimeOffset.UtcNow < _refreshAtUtc)
        {
            return _accessToken;
        }

        await _refreshGate.WaitAsync(cancellationToken);
        try
        {
            if (!string.IsNullOrWhiteSpace(_accessToken) && DateTimeOffset.UtcNow < _refreshAtUtc)
            {
                return _accessToken;
            }

            using var request = new HttpRequestMessage(HttpMethod.Post, _options.TokenEndpoint)
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["grant_type"] = "client_credentials",
                    ["client_id"] = _options.ClientId,
                    ["client_secret"] = _options.ClientSecret,
                    ["scope"] = _options.Scope,
                }),
            };
            using var client = _httpClientFactory.CreateClient(HttpClientName);
            using var response = await client.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException(
                    $"The service identity provider returned HTTP {(int)response.StatusCode}.",
                    inner: null,
                    response.StatusCode);
            }

            var token = await response.Content.ReadFromJsonAsync<OAuthTokenResponse>(
                cancellationToken: cancellationToken)
                ?? throw new InvalidOperationException("The service identity provider returned an empty token response.");
            if (string.IsNullOrWhiteSpace(token.AccessToken) || token.ExpiresIn < 30)
            {
                throw new InvalidOperationException("The service identity provider returned an invalid access token lifetime.");
            }

            _accessToken = token.AccessToken;
            _refreshAtUtc = DateTimeOffset.UtcNow.AddSeconds(Math.Max(5, token.ExpiresIn - 60));
            return _accessToken;
        }
        finally
        {
            _refreshGate.Release();
        }
    }

    public void Dispose() => _refreshGate.Dispose();

    private sealed record OAuthTokenResponse(
        [property: JsonPropertyName("access_token")] string AccessToken,
        [property: JsonPropertyName("expires_in")] int ExpiresIn);
}

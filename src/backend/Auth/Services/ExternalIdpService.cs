using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Auth.Models;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Shared.Helpers;

namespace Auth.Services;

/// <summary>
/// A textbook OAuth 2.0 / OpenID Connect authorization-code flow with PKCE, kept deliberately
/// explicit so it can be read end to end.
/// <para>
/// Step 1 (<see cref="BuildAuthorizationUrlAsync"/>): make a random <c>state</c>, a random PKCE
/// <c>code_verifier</c> and a random <c>nonce</c>; keep them server-side in Valkey; send the
/// browser to the provider with only the SHA-256 <c>code_challenge</c>.
/// </para>
/// <para>
/// Step 2 (<see cref="HandleCallbackAsync"/>): the provider redirects back with a one-time
/// <c>code</c>; look the <c>state</c> up in Valkey and delete it, POST the code plus the original
/// <c>code_verifier</c> to the token endpoint, validate the returned <c>id_token</c> signature /
/// issuer / audience / nonce, and turn the resulting identity into an application session.
/// </para>
/// </summary>
public class ExternalIdpService : IExternalIdpService
{
    /// <summary>Valkey key prefix for in-flight sign-ins.</summary>
    private const string StateKeyPrefix = "extidp:state:";

    /// <summary>How long a user has to finish signing in at the provider.</summary>
    private static readonly TimeSpan StateLifetime = TimeSpan.FromMinutes(10);

    /// <summary>
    /// OIDC discovery documents and their signing keys, cached per authority.
    /// <see cref="ConfigurationManager{T}"/> refreshes them on its own schedule.
    /// </summary>
    private static readonly ConcurrentDictionary<string, ConfigurationManager<OpenIdConnectConfiguration>> DiscoveryCache = new();

    private readonly ExternalIdpOptions _options;
    private readonly IDistributedCache _cache;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILocalIdentityService _localIdentity;
    private readonly IAuthSessionService _authSessionService;
    private readonly ILogger<ExternalIdpService> _logger;

    public ExternalIdpService(
        IOptions<ExternalIdpOptions> options,
        IDistributedCache cache,
        IHttpClientFactory httpClientFactory,
        ILocalIdentityService localIdentity,
        IAuthSessionService authSessionService,
        ILogger<ExternalIdpService> logger)
    {
        _options = options.Value;
        _cache = cache;
        _httpClientFactory = httpClientFactory;
        _localIdentity = localIdentity;
        _authSessionService = authSessionService;
        _logger = logger;
    }

    /// <inheritdoc />
    public bool IsEnabled => _options.GetUsableProviders().Count > 0;

    /// <inheritdoc />
    public IReadOnlyList<ExternalProviderSummary> GetEnabledProviders() =>
        _options.GetUsableProviders()
            .Select(entry => new ExternalProviderSummary
            {
                Name = entry.Key,
                DisplayName = string.IsNullOrWhiteSpace(entry.Value.DisplayName) ? entry.Key : entry.Value.DisplayName,
                StartUrl = $"/api/Auth/ExternalStart?provider={Uri.EscapeDataString(entry.Key)}"
            })
            .ToList();

    /// <inheritdoc />
    public async Task<string> BuildAuthorizationUrlAsync(
        string providerName,
        string? returnUrl,
        string fallbackRedirectUri,
        CancellationToken cancellationToken = default)
    {
        var provider = _options.FindUsableProvider(providerName)
            ?? throw new InvalidOperationException($"External provider '{providerName}' is not enabled or not configured.");

        var endpoints = await ResolveEndpointsAsync(provider, cancellationToken);

        if (string.IsNullOrWhiteSpace(endpoints.AuthorizationEndpoint))
            throw new InvalidOperationException($"External provider '{providerName}' has no authorization endpoint.");

        var redirectUri = string.IsNullOrWhiteSpace(provider.RedirectUri) ? fallbackRedirectUri : provider.RedirectUri;

        // state: opaque, single use, ties the callback back to this browser's request.
        var state = RandomUrlSafeToken();
        // code_verifier: kept secret on the server; only its SHA-256 hash travels to the provider,
        // so an attacker who steals the authorization code still cannot redeem it.
        var codeVerifier = RandomUrlSafeToken();
        var nonce = RandomUrlSafeToken();

        await _cache.SetStringAsync(
            StateKeyPrefix + state,
            JsonSerializer.Serialize(new ExternalLoginState
            {
                Provider = providerName,
                CodeVerifier = codeVerifier,
                Nonce = nonce,
                RedirectUri = redirectUri,
                ReturnUrl = returnUrl,
                StartedOn = DateTimeHelper.Now
            }),
            new DistributedCacheEntryOptions().SetAbsoluteExpiration(StateLifetime),
            cancellationToken);

        var query = new Dictionary<string, string?>
        {
            ["client_id"] = provider.ClientId,
            ["response_type"] = "code",
            ["redirect_uri"] = redirectUri,
            ["scope"] = string.IsNullOrWhiteSpace(provider.Scopes) ? "openid profile email" : provider.Scopes,
            ["state"] = state,
            ["nonce"] = nonce,
            ["code_challenge"] = Base64UrlEncode(SHA256.HashData(Encoding.UTF8.GetBytes(codeVerifier))),
            ["code_challenge_method"] = "S256"
        };

        return BuildUrl(endpoints.AuthorizationEndpoint, query);
    }

    /// <inheritdoc />
    public async Task<ExternalCallbackResult> HandleCallbackAsync(
        string? code,
        string? state,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(state))
            return Failed("The sign-in response was incomplete. Please try again.");

        var stateKey = StateKeyPrefix + state;
        var stateJson = await _cache.GetStringAsync(stateKey, cancellationToken);

        // Single use: consume the state immediately, whether or not the rest succeeds.
        await _cache.RemoveAsync(stateKey, cancellationToken);

        if (string.IsNullOrEmpty(stateJson))
            return Failed("This sign-in link has expired. Please try again.");

        var loginState = JsonSerializer.Deserialize<ExternalLoginState>(stateJson);
        if (loginState is null)
            return Failed("This sign-in link is not valid. Please try again.");

        var provider = _options.FindUsableProvider(loginState.Provider);
        if (provider is null)
            return Failed("That sign-in provider is no longer available.");

        var endpoints = await ResolveEndpointsAsync(provider, cancellationToken);
        if (string.IsNullOrWhiteSpace(endpoints.TokenEndpoint))
            return Failed("That sign-in provider is not fully configured.");

        TokenEndpointResponse tokens;
        try
        {
            tokens = await ExchangeCodeAsync(provider, endpoints, loginState, code, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Token exchange with {Provider} failed.", loginState.Provider);
            return Failed("Could not complete sign-in with the external provider.");
        }

        var profile = await ReadProfileAsync(provider, endpoints, loginState, tokens, cancellationToken);
        if (profile is null)
            return Failed("The external provider did not return a usable identity.");

        var loginResponse = await _localIdentity.ResolveExternalUserAsync(
            loginState.Provider,
            profile.Subject,
            profile.Name,
            profile.Email,
            provider.AllowAutoProvision,
            cancellationToken);

        if (!loginResponse.isAuthenticated)
            return Failed(loginResponse.errorMessage ?? "Sign-in was refused.", loginState.ReturnUrl);

        var issued = await _authSessionService.IssueSessionAsync(loginResponse, cancellationToken);

        _logger.LogInformation(
            "External sign-in success for {UserId} via {Provider}.",
            loginResponse.userId,
            loginState.Provider);

        return new ExternalCallbackResult
        {
            Success = true,
            Login = issued,
            ReturnUrl = loginState.ReturnUrl
        };
    }

    /// <summary>
    /// Swaps the one-time authorization code (plus the PKCE verifier) for tokens.
    /// </summary>
    private async Task<TokenEndpointResponse> ExchangeCodeAsync(
        ExternalProviderOptions provider,
        ProviderEndpoints endpoints,
        ExternalLoginState loginState,
        string code,
        CancellationToken cancellationToken)
    {
        using var client = _httpClientFactory.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Post, endpoints.TokenEndpoint)
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "authorization_code",
                ["code"] = code,
                ["redirect_uri"] = loginState.RedirectUri,
                ["client_id"] = provider.ClientId,
                ["client_secret"] = provider.ClientSecret,
                ["code_verifier"] = loginState.CodeVerifier
            })
        };
        // GitHub returns form-encoded data unless JSON is explicitly requested.
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using var response = await client.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<TokenEndpointResponse>(cancellationToken);
        return payload ?? new TokenEndpointResponse();
    }

    /// <summary>
    /// Reads the signed-in user's identity, preferring the validated <c>id_token</c> and falling
    /// back to the userinfo endpoint for plain OAuth 2.0 providers such as GitHub.
    /// </summary>
    private async Task<ExternalProfile?> ReadProfileAsync(
        ExternalProviderOptions provider,
        ProviderEndpoints endpoints,
        ExternalLoginState loginState,
        TokenEndpointResponse tokens,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(tokens.id_token) && endpoints.Configuration is not null)
        {
            var profile = await ReadProfileFromIdTokenAsync(provider, endpoints.Configuration, loginState, tokens.id_token);
            if (profile is not null)
                return profile;
        }

        if (!string.IsNullOrWhiteSpace(tokens.access_token) && !string.IsNullOrWhiteSpace(endpoints.UserInfoEndpoint))
            return await ReadProfileFromUserInfoAsync(endpoints.UserInfoEndpoint, tokens.access_token, cancellationToken);

        return null;
    }

    /// <summary>
    /// Validates the id_token signature, issuer, audience, lifetime and nonce, then pulls out the
    /// handful of claims this application cares about.
    /// </summary>
    private async Task<ExternalProfile?> ReadProfileFromIdTokenAsync(
        ExternalProviderOptions provider,
        OpenIdConnectConfiguration configuration,
        ExternalLoginState loginState,
        string idToken)
    {
        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = configuration.Issuer,
            ValidateAudience = true,
            ValidAudience = provider.ClientId,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKeys = configuration.SigningKeys,
            ClockSkew = TimeSpan.FromMinutes(5)
        };

        var result = await new JsonWebTokenHandler().ValidateTokenAsync(idToken, validationParameters);

        if (!result.IsValid)
        {
            _logger.LogWarning(result.Exception, "id_token from {Provider} failed validation.", loginState.Provider);
            return null;
        }

        var claims = result.Claims;

        // The nonce proves the token was minted for the sign-in we started, not replayed from another.
        if (claims.TryGetValue("nonce", out var nonce)
            && !string.Equals(nonce?.ToString(), loginState.Nonce, StringComparison.Ordinal))
        {
            _logger.LogWarning("id_token nonce mismatch from {Provider}.", loginState.Provider);
            return null;
        }

        var subject = ClaimText(claims, "sub");
        if (string.IsNullOrWhiteSpace(subject))
            return null;

        return new ExternalProfile(
            subject,
            ClaimText(claims, "name") ?? ClaimText(claims, "preferred_username"),
            ClaimText(claims, "email"));
    }

    /// <summary>
    /// Fallback profile lookup for providers that issue no id_token.
    /// </summary>
    private async Task<ExternalProfile?> ReadProfileFromUserInfoAsync(
        string userInfoEndpoint,
        string accessToken,
        CancellationToken cancellationToken)
    {
        using var client = _httpClientFactory.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Get, userInfoEndpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        // GitHub rejects requests without a User-Agent.
        request.Headers.UserAgent.ParseAdd("AppTemplate-Auth");

        using var response = await client.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Userinfo request failed with status {Status}.", (int)response.StatusCode);
            return null;
        }

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        var root = document.RootElement;

        // "sub" is the OIDC name; "id" is what GitHub returns.
        var subject = JsonText(root, "sub") ?? JsonText(root, "id");
        if (string.IsNullOrWhiteSpace(subject))
            return null;

        return new ExternalProfile(
            subject,
            JsonText(root, "name") ?? JsonText(root, "login"),
            JsonText(root, "email"));
    }

    /// <summary>
    /// Works out the provider's endpoints: explicit configuration always wins, otherwise the
    /// OIDC discovery document at <c>{Authority}/.well-known/openid-configuration</c> is used.
    /// </summary>
    private async Task<ProviderEndpoints> ResolveEndpointsAsync(
        ExternalProviderOptions provider,
        CancellationToken cancellationToken)
    {
        OpenIdConnectConfiguration? configuration = null;

        if (!string.IsNullOrWhiteSpace(provider.Authority))
        {
            try
            {
                var metadataAddress = $"{provider.Authority.TrimEnd('/')}/.well-known/openid-configuration";
                var manager = DiscoveryCache.GetOrAdd(
                    metadataAddress,
                    address => new ConfigurationManager<OpenIdConnectConfiguration>(
                        address,
                        new OpenIdConnectConfigurationRetriever()));

                configuration = await manager.GetConfigurationAsync(cancellationToken);
            }
            catch (Exception ex) when (ex is HttpRequestException or InvalidOperationException or IOException)
            {
                // Non-fatal: the explicit endpoint overrides below may still be enough.
                _logger.LogWarning(ex, "Could not read the OIDC discovery document from {Authority}.", provider.Authority);
            }
        }

        return new ProviderEndpoints(
            FirstNonEmpty(provider.AuthorizationEndpoint, configuration?.AuthorizationEndpoint),
            FirstNonEmpty(provider.TokenEndpoint, configuration?.TokenEndpoint),
            FirstNonEmpty(provider.UserInfoEndpoint, configuration?.UserInfoEndpoint),
            configuration);
    }

    private static ExternalCallbackResult Failed(string error, string? returnUrl = null) =>
        new() { Success = false, Error = error, ReturnUrl = returnUrl };

    private static string? FirstNonEmpty(params string?[] candidates) =>
        candidates.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

    private static string? ClaimText(IDictionary<string, object> claims, string name) =>
        claims.TryGetValue(name, out var value) ? value?.ToString() : null;

    private static string? JsonText(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var property) && property.ValueKind is not JsonValueKind.Null
            ? property.ToString()
            : null;

    /// <summary>256 bits of URL-safe randomness - long enough to be unguessable.</summary>
    private static string RandomUrlSafeToken() => Base64UrlEncode(RandomNumberGenerator.GetBytes(32));

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static string BuildUrl(string baseUrl, IDictionary<string, string?> query)
    {
        var separator = baseUrl.Contains('?', StringComparison.Ordinal) ? "&" : "?";
        var queryString = string.Join(
            "&",
            query.Where(pair => !string.IsNullOrEmpty(pair.Value))
                 .Select(pair => $"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(pair.Value!)}"));

        return baseUrl + separator + queryString;
    }

    /// <summary>The endpoints in play for one provider, plus the discovery document when there is one.</summary>
    private sealed record ProviderEndpoints(
        string? AuthorizationEndpoint,
        string? TokenEndpoint,
        string? UserInfoEndpoint,
        OpenIdConnectConfiguration? Configuration);

    /// <summary>The subset of an external profile this application stores.</summary>
    private sealed record ExternalProfile(string Subject, string? Name, string? Email);

    /// <summary>The standard OAuth 2.0 token-endpoint response (lowercase to match the wire format).</summary>
    private sealed class TokenEndpointResponse
    {
        public string? access_token { get; set; }
        public string? id_token { get; set; }
        public string? token_type { get; set; }
        public int expires_in { get; set; }
    }
}

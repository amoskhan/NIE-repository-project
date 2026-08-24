using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using Auth.Models;
using BuildingBlocks.Helpers;
using Jose;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Auth.Services;

public class PortalSsoService : IPortalSsoService
{
    private const string StatePrefix = "sso:state:";
    private const string ReplayPrefix = "sso:jti:";

    private readonly IDistributedCache _cache;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IAuthSessionService _authSessionService;
    private readonly PortalSsoOptions _options;
    private readonly ILogger<PortalSsoService> _logger;
    private readonly JsonWebTokenHandler _tokenHandler = new();
    private string? _decryptionPrivateKeyPem;
    private string? _signingPublicKeyPem;

    public PortalSsoService(
        IDistributedCache cache,
        IHttpClientFactory httpClientFactory,
        IAuthSessionService authSessionService,
        IOptions<PortalSsoOptions> options,
        ILogger<PortalSsoService> logger)
    {
        _cache = cache;
        _httpClientFactory = httpClientFactory;
        _authSessionService = authSessionService;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<SsoStartResponse> StartAsync(string? returnUrl, string? callbackUrl, CancellationToken cancellationToken = default)
    {
        EnsureEnabled();

        if (string.IsNullOrWhiteSpace(_options.LaunchUrlTemplate))
            throw new InvalidOperationException("PortalSso:LaunchUrlTemplate configuration is required.");

        var state = CreateOpaqueToken();
        var nonce = CreateOpaqueToken();
        var finalReturnUrl = BuildReturnUrl(returnUrl, state);
        var effectiveCallbackUrl = !string.IsNullOrWhiteSpace(callbackUrl)
            ? callbackUrl
            : _options.CallbackUrl;

        if (string.IsNullOrWhiteSpace(effectiveCallbackUrl))
            throw new InvalidOperationException("Portal SSO callback URL could not be resolved.");

        var launchUrl = BuildLaunchUrl(state, nonce, finalReturnUrl, effectiveCallbackUrl);
        var record = new SsoStateRecord
        {
            State = state,
            Nonce = nonce,
            ReturnUrl = finalReturnUrl,
            Status = SsoStateStatus.Pending,
            CreatedAt = DateTimeHelper.Now
        };

        await SetStateAsync(record, cancellationToken);

        return new SsoStartResponse
        {
            state = state,
            nonce = nonce,
            launchUrl = launchUrl,
            pollIntervalMs = _options.FinalizePollIntervalMs
        };
    }

    public async Task<SsoFinalizeResult> HandleCallbackAsync(SsoCallbackRequest request, IPAddress? remoteIp, CancellationToken cancellationToken = default)
    {
        EnsureEnabled();

        var stateRecord = await GetStateAsync(request.state, cancellationToken);
        if (stateRecord == null)
            throw new SecurityTokenException("Unknown or expired SSO state.");

        if (!string.Equals(stateRecord.Status, SsoStateStatus.Pending, StringComparison.Ordinal))
            throw new SecurityTokenException("The SSO state is no longer pending.");

        if (!IsRemoteIpAllowed(remoteIp))
        {
            await FailStateAsync(request.state, "SSO callback source is not allowlisted.", cancellationToken);
            throw new SecurityTokenException("SSO callback source is not allowlisted.");
        }

        try
        {
            var payload = await ValidatePayloadAsync(request, stateRecord, cancellationToken);
            await EnsureNotReplayedAsync(payload.Jti, cancellationToken);

            var exchangedLogin = await ExchangeAsync(payload, cancellationToken);
            if (exchangedLogin.isAuthenticated != true)
            {
                await FailStateAsync(request.state, exchangedLogin.errorMessage ?? "The SSO exchange did not authenticate the user.", cancellationToken);
                return new SsoFinalizeResult
                {
                    status = SsoStateStatus.Failed,
                    message = exchangedLogin.errorMessage ?? "The SSO exchange did not authenticate the user."
                };
            }

            var issuedLogin = await _authSessionService.IssueSessionAsync(exchangedLogin, cancellationToken);
            stateRecord.Status = SsoStateStatus.Completed;
            stateRecord.Login = issuedLogin;
            stateRecord.CompletedAt = DateTimeHelper.Now;
            stateRecord.ErrorMessage = null;

            await SetStateAsync(stateRecord, cancellationToken);

            _logger.LogInformation(
                "Portal SSO login completed for user {UserId} from source {SourceSystemId}.",
                issuedLogin.userId,
                payload.SourceSystemId);

            return new SsoFinalizeResult
            {
                status = SsoStateStatus.Completed,
                login = issuedLogin,
                pollIntervalMs = _options.FinalizePollIntervalMs
            };
        }
        catch (Exception ex) when (ex is SecurityTokenException or InvalidOperationException or HttpRequestException)
        {
            await FailStateAsync(request.state, ex.Message, cancellationToken);
            _logger.LogWarning(ex, "Portal SSO callback failed for state {State}.", request.state);
            throw;
        }
    }

    public async Task<SsoFinalizeResult> FinalizeAsync(string state, CancellationToken cancellationToken = default)
    {
        EnsureEnabled();

        var record = await GetStateAsync(state, cancellationToken);
        if (record == null)
        {
            return new SsoFinalizeResult
            {
                status = SsoStateStatus.Failed,
                message = "The SSO request has expired or could not be found.",
                pollIntervalMs = _options.FinalizePollIntervalMs
            };
        }

        return record.Status switch
        {
            SsoStateStatus.Completed => new SsoFinalizeResult
            {
                status = record.Status,
                login = record.Login,
                pollIntervalMs = _options.FinalizePollIntervalMs
            },
            SsoStateStatus.Failed => new SsoFinalizeResult
            {
                status = record.Status,
                message = record.ErrorMessage ?? "The SSO request failed.",
                pollIntervalMs = _options.FinalizePollIntervalMs
            },
            _ => new SsoFinalizeResult
            {
                status = SsoStateStatus.Pending,
                pollIntervalMs = _options.FinalizePollIntervalMs
            }
        };
    }

    private void EnsureEnabled()
    {
        if (!_options.Enabled)
            throw new InvalidOperationException("Portal SSO is not enabled.");
    }

    private async Task<SsoValidatedPayload> ValidatePayloadAsync(
        SsoCallbackRequest request,
        SsoStateRecord stateRecord,
        CancellationToken cancellationToken)
    {
        var token = request.encryptedPayload;
        ValidateOuterHeader(token);
        var decryptionPrivateKeyPem = GetDecryptionPrivateKeyPem();
        var signingPublicKeyPem = GetSigningPublicKeyPem();

        using var decryptionRsa = RSA.Create();
        decryptionRsa.ImportFromPem(decryptionPrivateKeyPem.AsSpan());

        using var signingRsa = RSA.Create();
        signingRsa.ImportFromPem(signingPublicKeyPem.AsSpan());

        string innerToken;
        try
        {
            innerToken = JWT.Decode(token, decryptionRsa);
        }
        catch (Exception ex)
        {
            throw new SecurityTokenException("The SSO token could not be decrypted.", ex);
        }

        var validationParameters = new TokenValidationParameters
        {
            ValidIssuer = _options.Issuer,
            ValidateIssuer = true,
            ValidAudience = _options.Audience,
            ValidateAudience = true,
            ValidateLifetime = true,
            RequireSignedTokens = true,
            RequireExpirationTime = true,
            IssuerSigningKey = new RsaSecurityKey(signingRsa),
            ClockSkew = TimeSpan.FromMinutes(1)
        };

        var result = await _tokenHandler.ValidateTokenAsync(innerToken, validationParameters);
        if (!result.IsValid || result.ClaimsIdentity == null)
            throw new SecurityTokenException(result.Exception?.Message ?? "The SSO token is invalid.");

        ValidateInnerHeader(result.SecurityToken);

        var claims = result.ClaimsIdentity;
        var tokenState = GetRequiredClaim(claims, "state");
        if (!string.Equals(tokenState, request.state, StringComparison.Ordinal))
            throw new SecurityTokenException("The SSO state claim does not match the requested state.");

        var tokenNonce = GetRequiredClaim(claims, "nonce");
        if (!string.Equals(tokenNonce, stateRecord.Nonce, StringComparison.Ordinal))
            throw new SecurityTokenException("The SSO nonce claim does not match the pending login.");

        var sourceSystemId = GetRequiredClaim(claims, _options.SourceSystemClaim);
        if (!string.Equals(sourceSystemId, _options.SourceSystemId, StringComparison.Ordinal))
            throw new SecurityTokenException("The SSO payload source system is not trusted.");

        var sourceUrl = claims.FindFirst(_options.SourceUrlClaim)?.Value;
        ValidateSourceUrl(sourceUrl);

        var exchangeToken = GetRequiredClaim(claims, _options.ExchangeTokenClaim);
        var jti = GetRequiredClaim(claims, JwtRegisteredClaimNames.Jti);

        return new SsoValidatedPayload
        {
            State = tokenState,
            Jti = jti,
            SourceSystemId = sourceSystemId,
            SourceUrl = sourceUrl,
            ExchangeToken = exchangeToken,
            Email = claims.FindFirst(_options.EmailClaim)?.Value,
            UserName = claims.FindFirst(_options.UsernameClaim)?.Value,
            Subject = claims.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? claims.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
        };
    }

    private async Task<LoginResponse> ExchangeAsync(SsoValidatedPayload payload, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.ExchangeApi.BaseUrl))
            throw new InvalidOperationException("PortalSso:ExchangeApi:BaseUrl configuration is required.");
        if (string.IsNullOrWhiteSpace(_options.ExchangeApi.Path))
            throw new InvalidOperationException("PortalSso:ExchangeApi:Path configuration is required.");

        var client = _httpClientFactory.CreateClient();
        client.BaseAddress = new Uri(_options.ExchangeApi.BaseUrl, UriKind.Absolute);

        if (!string.IsNullOrWhiteSpace(_options.ExchangeApi.SubscriptionHeaderName) &&
            !string.IsNullOrWhiteSpace(_options.ExchangeApi.SubscriptionKey))
        {
            client.DefaultRequestHeaders.Remove(_options.ExchangeApi.SubscriptionHeaderName);
            client.DefaultRequestHeaders.Add(_options.ExchangeApi.SubscriptionHeaderName, _options.ExchangeApi.SubscriptionKey);
        }

        if (!string.IsNullOrWhiteSpace(_options.ExchangeApi.SourceHeaderName))
        {
            client.DefaultRequestHeaders.Remove(_options.ExchangeApi.SourceHeaderName);
            client.DefaultRequestHeaders.Add(_options.ExchangeApi.SourceHeaderName, payload.SourceSystemId);
        }

        var response = await client.PostAsJsonAsync(
            _options.ExchangeApi.Path,
            new
            {
                state = payload.State,
                exchangeToken = payload.ExchangeToken,
                sourceSystemId = payload.SourceSystemId,
                sourceUrl = payload.SourceUrl,
                username = payload.UserName,
                email = payload.Email,
                subject = payload.Subject
            },
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>(cancellationToken: cancellationToken);
        if (loginResponse == null)
            throw new InvalidOperationException("The SSO exchange API returned an empty login response.");

        return loginResponse;
    }

    private async Task EnsureNotReplayedAsync(string jti, CancellationToken cancellationToken)
    {
        var existing = await _cache.GetStringAsync($"{ReplayPrefix}{jti}", cancellationToken);
        if (!string.IsNullOrWhiteSpace(existing))
            throw new SecurityTokenException("The SSO payload has already been used.");

        var replayOptions = new DistributedCacheEntryOptions()
            .SetAbsoluteExpiration(TimeSpan.FromMinutes(_options.ReplayTtlMinutes));

        await _cache.SetStringAsync($"{ReplayPrefix}{jti}", DateTimeHelper.Now.ToString("O"), replayOptions, cancellationToken);
    }

    private async Task<SsoStateRecord?> GetStateAsync(string state, CancellationToken cancellationToken)
    {
        var json = await _cache.GetStringAsync($"{StatePrefix}{state}", cancellationToken);
        return string.IsNullOrWhiteSpace(json)
            ? null
            : JsonSerializer.Deserialize<SsoStateRecord>(json);
    }

    private async Task SetStateAsync(SsoStateRecord record, CancellationToken cancellationToken)
    {
        var cacheOptions = new DistributedCacheEntryOptions()
            .SetAbsoluteExpiration(TimeSpan.FromMinutes(_options.StateTtlMinutes));

        await _cache.SetStringAsync(
            $"{StatePrefix}{record.State}",
            JsonSerializer.Serialize(record),
            cacheOptions,
            cancellationToken);
    }

    private async Task FailStateAsync(string state, string errorMessage, CancellationToken cancellationToken)
    {
        var record = await GetStateAsync(state, cancellationToken);
        if (record == null)
            return;

        record.Status = SsoStateStatus.Failed;
        record.ErrorMessage = errorMessage;
        record.CompletedAt = DateTimeHelper.Now;
        await SetStateAsync(record, cancellationToken);
    }

    private void ValidateOuterHeader(string token)
    {
        if (string.IsNullOrWhiteSpace(_options.Crypto.DecryptionPrivateKeyPem) ||
            string.IsNullOrWhiteSpace(_options.Crypto.SigningPublicKeyPem))
        {
            _ = GetDecryptionPrivateKeyPem();
            _ = GetSigningPublicKeyPem();
        }

        var parts = token.Split('.');
        if (parts.Length != 5)
            throw new SecurityTokenException("The SSO payload must be a JWE compact token.");

        var headerJson = Base64UrlEncoder.Decode(parts[0]);
        using var document = JsonDocument.Parse(headerJson);
        var root = document.RootElement;

        var alg = root.TryGetProperty("alg", out var algProperty) ? algProperty.GetString() : null;
        var enc = root.TryGetProperty("enc", out var encProperty) ? encProperty.GetString() : null;

        if (!string.Equals(alg, _options.Crypto.RequiredOuterAlg, StringComparison.Ordinal))
            throw new SecurityTokenException($"The SSO payload uses unsupported JWE alg '{alg}'.");

        if (!string.Equals(enc, _options.Crypto.RequiredEnc, StringComparison.Ordinal))
            throw new SecurityTokenException($"The SSO payload uses unsupported JWE enc '{enc}'.");
    }

    private void ValidateInnerHeader(SecurityToken? securityToken)
    {
        if (securityToken is not JsonWebToken outerToken)
            throw new SecurityTokenException("The validated SSO payload is not a JWT token.");

        var innerToken = outerToken.InnerToken ?? outerToken;
        var innerAlg = innerToken.Alg;
        if (!string.Equals(innerAlg, _options.Crypto.RequiredInnerAlg, StringComparison.Ordinal))
            throw new SecurityTokenException($"The SSO payload uses unsupported JWS alg '{innerAlg}'.");
    }

    private string BuildReturnUrl(string? requestedReturnUrl, string state)
    {
        var baseReturnUrl = !string.IsNullOrWhiteSpace(requestedReturnUrl)
            ? requestedReturnUrl
            : _options.DefaultReturnUrl;

        if (string.IsNullOrWhiteSpace(baseReturnUrl))
            throw new InvalidOperationException("PortalSso:DefaultReturnUrl configuration is required.");

        return QueryHelpers.AddQueryString(baseReturnUrl, new Dictionary<string, string?>
        {
            ["sso"] = "1",
            ["state"] = state
        });
    }

    private string BuildLaunchUrl(string state, string nonce, string returnUrl, string callbackUrl)
    {
        return _options.LaunchUrlTemplate
            .Replace("{state}", Uri.EscapeDataString(state), StringComparison.Ordinal)
            .Replace("{nonce}", Uri.EscapeDataString(nonce), StringComparison.Ordinal)
            .Replace("{returnUrl}", Uri.EscapeDataString(returnUrl), StringComparison.Ordinal)
            .Replace("{callbackUrl}", Uri.EscapeDataString(callbackUrl), StringComparison.Ordinal);
    }

    private bool IsRemoteIpAllowed(IPAddress? remoteIp)
    {
        if (_options.AllowedIpRanges.Length == 0 || remoteIp == null)
            return true;

        return _options.AllowedIpRanges.Any(range => IpAddressMatchesRange(remoteIp, range));
    }

    private static bool IpAddressMatchesRange(IPAddress address, string range)
    {
        if (string.IsNullOrWhiteSpace(range))
            return false;

        if (!range.Contains('/'))
            return string.Equals(address.ToString(), range, StringComparison.OrdinalIgnoreCase);

        var parts = range.Split('/', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length != 2)
            return false;

        if (!IPAddress.TryParse(parts[0], out var network) || !int.TryParse(parts[1], out var prefixLength))
            return false;

        var addressBytes = address.GetAddressBytes();
        var networkBytes = network.GetAddressBytes();
        if (addressBytes.Length != networkBytes.Length)
            return false;

        var bits = prefixLength;
        for (var i = 0; i < addressBytes.Length; i++)
        {
            if (bits <= 0)
                return true;

            var mask = bits >= 8 ? 0xFF : (byte)(0xFF << (8 - bits));
            if ((addressBytes[i] & mask) != (networkBytes[i] & mask))
                return false;

            bits -= 8;
        }

        return true;
    }

    private static string GetRequiredClaim(ClaimsIdentity identity, string claimType)
    {
        var value = identity.FindFirst(claimType)?.Value;
        return !string.IsNullOrWhiteSpace(value)
            ? value
            : throw new SecurityTokenException($"Required SSO claim '{claimType}' is missing.");
    }

    private void ValidateSourceUrl(string? sourceUrl)
    {
        if (_options.AllowedSourceUrls.Length == 0)
            return;

        if (string.IsNullOrWhiteSpace(sourceUrl))
            throw new SecurityTokenException($"Required SSO claim '{_options.SourceUrlClaim}' is missing.");

        var normalizedSource = NormalizeUrl(sourceUrl);
        var allowed = _options.AllowedSourceUrls
            .Select(NormalizeUrl)
            .Where(url => !string.IsNullOrWhiteSpace(url))
            .ToArray();

        if (!allowed.Contains(normalizedSource, StringComparer.OrdinalIgnoreCase))
            throw new SecurityTokenException("The SSO payload source URL is not trusted.");
    }

    private static string NormalizeUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return string.Empty;

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return url.TrimEnd('/').Trim();

        return uri.GetLeftPart(UriPartial.Path).TrimEnd('/');
    }

    private static string CreateOpaqueToken()
    {
        Span<byte> bytes = stackalloc byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Base64UrlEncoder.Encode(bytes.ToArray());
    }

    private string GetDecryptionPrivateKeyPem()
    {
        if (!string.IsNullOrWhiteSpace(_decryptionPrivateKeyPem))
            return _decryptionPrivateKeyPem;

        _decryptionPrivateKeyPem = ReadKeyMaterial(_options.Crypto.DecryptionPrivateKeyPem, _options.Crypto.DecryptionPrivateKeyPath, "PortalSso:Crypto:DecryptionPrivateKeyPem");
        return _decryptionPrivateKeyPem;
    }

    private string GetSigningPublicKeyPem()
    {
        if (!string.IsNullOrWhiteSpace(_signingPublicKeyPem))
            return _signingPublicKeyPem;

        _signingPublicKeyPem = ReadKeyMaterial(_options.Crypto.SigningPublicKeyPem, _options.Crypto.SigningPublicKeyPath, "PortalSso:Crypto:SigningPublicKeyPem");
        return _signingPublicKeyPem;
    }

    private static string ReadKeyMaterial(string keyPem, string keyPath, string configKey)
    {
        if (!string.IsNullOrWhiteSpace(keyPath))
            return File.ReadAllText(keyPath);

        if (!string.IsNullOrWhiteSpace(keyPem))
            return keyPem;

        throw new InvalidOperationException($"{configKey} configuration is required.");
    }
}

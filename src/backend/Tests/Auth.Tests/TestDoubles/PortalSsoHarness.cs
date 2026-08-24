using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using Auth.Models;
using Auth.Services;
using Jose;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Auth.Tests.TestDoubles;

/// <summary>
/// Builds a <see cref="PortalSsoService"/> with substituted collaborators and real RSA key material,
/// so the whole SSO validation chain can run without a web host or a live portal.
/// </summary>
internal sealed class PortalSsoHarness
{
    public const string StateKeyPrefix = "sso:state:";
    public const string ReplayKeyPrefix = "sso:jti:";
    public const string IssuedSessionToken = "issued-session-token";

    public PortalSsoHarness(
        Action<PortalSsoOptions>? configure = null,
        Func<CapturedHttpRequest, HttpResponseMessage>? exchange = null,
        SsoSigningKeyPair? signingKeys = null)
    {
        SigningKeys = signingKeys ?? SsoSigningKeyPair.Shared;
        Options.Crypto.DecryptionPrivateKeyPem = SsoCryptoMaterial.DecryptionPrivateKeyPem;
        Options.Crypto.SigningPublicKeyPem = SigningKeys.PublicKeyPem;
        configure?.Invoke(Options);

        Exchange = new StubHttpMessageHandler(exchange ?? ExchangeResponses.Authenticated());

        SessionService
            .IssueSessionAsync(Arg.Any<LoginResponse>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var login = call.Arg<LoginResponse>();
                return Task.FromResult(new IssuedLoginResponse
                {
                    isAuthenticated = true,
                    userId = login?.userId,
                    userName = login?.userName,
                    email = login?.email,
                    sessionToken = IssuedSessionToken
                });
            });

        var options = Substitute.For<IOptions<PortalSsoOptions>>();
        options.Value.Returns(Options);

        Service = new PortalSsoService(
            Cache.Cache,
            HttpClientFactoryStub.Create(Exchange),
            SessionService,
            options,
            Logger);
    }

    public PortalSsoOptions Options { get; } = new()
    {
        Enabled = true,
        LaunchUrlTemplate =
            "https://portal.nie.edu.sg/launch?state={state}&nonce={nonce}&return={returnUrl}&callback={callbackUrl}",
        DefaultReturnUrl = "https://app.nie.edu.sg/sso",
        CallbackUrl = "https://auth.nie.edu.sg/api/Auth/SsoCallback",
        Issuer = SsoTokenFactory.Issuer,
        Audience = SsoTokenFactory.Audience,
        SourceSystemId = SsoTokenFactory.SourceSystemId,
        StateTtlMinutes = 5,
        ReplayTtlMinutes = 15,
        FinalizePollIntervalMs = 1500,
        ExchangeApi = new PortalSsoExchangeApiOptions
        {
            BaseUrl = "https://exchange.nie.edu.sg/",
            Path = "sso/exchange",
            SubscriptionHeaderName = "x-sso-api-key",
            SubscriptionKey = "sso-subscription-key",
            SourceHeaderName = "X-Source-System"
        }
    };

    public CacheSubstitute Cache { get; } = CacheSubstitute.Create();

    public RecordingLogger<PortalSsoService> Logger { get; } = new();

    public IAuthSessionService SessionService { get; } = Substitute.For<IAuthSessionService>();

    public StubHttpMessageHandler Exchange { get; }

    public SsoSigningKeyPair SigningKeys { get; }

    public PortalSsoService Service { get; }

    /// <summary>
    /// Builds a callback request whose nested payload is signed with this harness's signing key.
    /// </summary>
    public SsoCallbackRequest Callback(
        Action<Dictionary<string, object>>? mutateClaims = null,
        string state = "pending-state",
        string nonce = "pending-nonce",
        string? signingPrivateKeyPem = null,
        JwsAlgorithm signingAlgorithm = JwsAlgorithm.PS256)
    {
        var claims = SsoTokenFactory.Claims(state, nonce);
        mutateClaims?.Invoke(claims);

        return new SsoCallbackRequest
        {
            state = state,
            encryptedPayload = SsoTokenFactory.Encrypt(
                claims,
                signingPrivateKeyPem ?? SigningKeys.PrivateKeyPem,
                signingAlgorithm)
        };
    }

    public void SeedReplayMarker(string jti) =>
        Cache.WriteString(ReplayKeyPrefix + jti, DateTime.Now.ToString("O", CultureInfo.InvariantCulture));

    public SsoStateRecord? ReadState(string state)
    {
        var json = Cache.ReadString(StateKeyPrefix + state);
        return json is null ? null : JsonSerializer.Deserialize<SsoStateRecord>(json);
    }

    public void SeedState(SsoStateRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        Cache.WriteString(StateKeyPrefix + record.State, JsonSerializer.Serialize(record));
    }

    public SsoStateRecord SeedPendingState(string state = "pending-state", string nonce = "pending-nonce")
    {
        var record = new SsoStateRecord
        {
            State = state,
            Nonce = nonce,
            ReturnUrl = "https://app.nie.edu.sg/sso",
            Status = SsoStateStatus.Pending,
            CreatedAt = DateTime.Now
        };

        SeedState(record);
        return record;
    }

    public bool HasReplayMarker(string jti) => Cache.ContainsKey(ReplayKeyPrefix + jti);
}

internal static class ExchangeResponses
{
    public static Func<CapturedHttpRequest, HttpResponseMessage> Authenticated(
        string userId = "devia",
        string sessionToken = "portal-session-token") =>
        Body(HttpStatusCode.OK, JsonSerializer.Serialize(new
        {
            isAuthenticated = true,
            userId,
            fullName = "Dev IA",
            userName = "dev.ia",
            email = "dev.ia@nie.edu.sg",
            department = "Digital Solutions",
            sessionToken
        }));

    public static Func<CapturedHttpRequest, HttpResponseMessage> Denied(string? errorMessage) =>
        Body(HttpStatusCode.OK, JsonSerializer.Serialize(new { isAuthenticated = false, errorMessage }));

    public static Func<CapturedHttpRequest, HttpResponseMessage> Empty() =>
        Body(HttpStatusCode.OK, "null");

    public static Func<CapturedHttpRequest, HttpResponseMessage> Failure(HttpStatusCode statusCode) =>
        Body(statusCode, "{\"error\":\"upstream\"}");

    private static Func<CapturedHttpRequest, HttpResponseMessage> Body(HttpStatusCode statusCode, string json) =>
        _ => new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
}

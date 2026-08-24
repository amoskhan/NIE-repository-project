using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using BuildingBlocks.Helpers;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using JsonWebTokenHandler = Microsoft.IdentityModel.JsonWebTokens.JsonWebTokenHandler;

namespace Infrastructure.Providers.MyInfo;

public class MyInfoService : IMyInfoService
{
    private const string ClientAssertionType = "urn:ietf:params:oauth:client-assertion-type:jwt-bearer";
    private const string DefaultAuthority = "https://stg-id.singpass.gov.sg";
    private const string DefaultScopeList =
        "openid uinfin name sex race nationality dob birthcountry residentialstatus marital email mobileno regadd";
    private static readonly TimeSpan DiscoveryCacheLifetime = TimeSpan.FromHours(1);
    private static readonly TimeSpan SigningKeyCacheLifetime = TimeSpan.FromHours(1);
    private static readonly TimeSpan ClientKeyCacheLifetime = TimeSpan.FromMinutes(15);

    private static readonly string[] DefaultAllowedHosts =
    {
        "stg-id.singpass.gov.sg",
        "id.singpass.gov.sg",
        "test.api.myinfo.gov.sg",
        "api.myinfo.gov.sg"
    };

    private readonly HttpClient _httpClient;
    private readonly ILogger<MyInfoService> _logger;
    private readonly IMemoryCache _memoryCache;
    private readonly string _clientId;
    private readonly string _redirectUri;
    private readonly string _discoveryUrl;
    private readonly string _scope;
    private readonly string _privateJwksPath;
    private readonly string _signingKeyId;
    private readonly string _encryptionKeyId;
    private readonly IReadOnlyCollection<string> _allowedHosts;

    public MyInfoService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<MyInfoService> logger,
        IMemoryCache memoryCache)
    {
        _httpClient = httpClient;
        _logger = logger;
        _memoryCache = memoryCache;
        _clientId = configuration["MyInfo:ClientId"] ?? string.Empty;
        _redirectUri = configuration["MyInfo:RedirectUri"] ?? string.Empty;
        _scope = BuildScopeString(configuration);
        _privateJwksPath = ResolveFilePath(configuration["MyInfo:JwtClientAuthentication:PrivateJwksPath"]);
        _signingKeyId = configuration["MyInfo:JwtClientAuthentication:SigningKeyId"] ?? string.Empty;
        _encryptionKeyId = configuration["MyInfo:JwtClientAuthentication:EncryptionKeyId"] ?? string.Empty;

        var configuredDiscoveryUrl = configuration["MyInfo:DiscoveryUrl"];
        var configuredBaseUrl = configuration["MyInfo:BaseUrl"] ?? DefaultAuthority;
        _discoveryUrl = ResolveDiscoveryUrl(configuredDiscoveryUrl, configuredBaseUrl);

        // SSRF allowlist for outbound calls — closes OWASP W-A10 / API7. Override via
        // MyInfo:AllowedHosts in appsettings.json. Each entry is exact (api.myinfo.gov.sg)
        // or wildcard (*.gov.sg). The validation runs once here so misconfiguration is
        // immediate and loud rather than letting a tampered config silently proxy.
        var configuredAllowedHosts = configuration.GetSection("MyInfo:AllowedHosts").Get<string[]>();
        _allowedHosts = (configuredAllowedHosts != null && configuredAllowedHosts.Length > 0)
            ? configuredAllowedHosts
            : DefaultAllowedHosts;

        if (IsConfigured)
        {
            SsrfGuard.Validate(_discoveryUrl, _allowedHosts, "MyInfo Discovery URL");
        }
    }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_clientId) &&
        Uri.TryCreate(_redirectUri, UriKind.Absolute, out _) &&
        !string.IsNullOrWhiteSpace(_signingKeyId) &&
        !string.IsNullOrWhiteSpace(_encryptionKeyId) &&
        !string.IsNullOrWhiteSpace(_privateJwksPath) &&
        File.Exists(_privateJwksPath);

    public async Task<MyInfoAuthorizationRequest> CreateAuthorizationRequestAsync(
        string state,
        CancellationToken cancellationToken = default)
    {
        EnsureConfigured();

        var discovery = await GetDiscoveryDocumentAsync(cancellationToken);
        var codeVerifier = Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(64));
        var codeChallenge = Base64UrlEncoder.Encode(
            SHA256.HashData(Encoding.ASCII.GetBytes(codeVerifier)));
        var nonce = Guid.NewGuid().ToString("N");
        var dpopPrivateKey = string.Empty;
        string authorizeUrl;

        if (SupportsFapi(discovery))
        {
            var dpopKey = CreateDpopKey();
            var requestUri = await CreatePushedAuthorizationRequestAsync(
                discovery,
                state,
                nonce,
                codeChallenge,
                dpopKey,
                cancellationToken);

            dpopPrivateKey = JsonSerializer.Serialize(dpopKey);
            authorizeUrl = $"{discovery.AuthorizationEndpoint}?{BuildQueryString(new Dictionary<string, string>
            {
                ["client_id"] = _clientId,
                ["request_uri"] = requestUri,
            })}";
        }
        else
        {
            authorizeUrl = $"{discovery.AuthorizationEndpoint}?{BuildQueryString(new Dictionary<string, string>
            {
                ["response_type"] = "code",
                ["client_id"] = _clientId,
                ["redirect_uri"] = _redirectUri,
                ["scope"] = _scope,
                ["state"] = state,
                ["nonce"] = nonce,
                ["code_challenge"] = codeChallenge,
                ["code_challenge_method"] = "S256",
            })}";
        }

        return new MyInfoAuthorizationRequest(authorizeUrl, codeVerifier, nonce, dpopPrivateKey);
    }

    public async Task<MyInfoPersonData> GetPersonDataAsync(
        string authCode,
        string codeVerifier,
        string nonce,
        string dpopPrivateKey,
        CancellationToken cancellationToken = default)
    {
        EnsureConfigured();

        var discovery = await GetDiscoveryDocumentAsync(cancellationToken);
        var dpopKey = SupportsFapi(discovery)
            ? LoadDpopPrivateKey(dpopPrivateKey)
            : null;

        var tokenExchange = await ExchangeCodeForTokensAsync(
            discovery,
            authCode,
            codeVerifier,
            dpopKey,
            cancellationToken);
        var idToken = await ValidateEncryptedJwtAsync(
            tokenExchange.IdToken,
            discovery,
            validateLifetime: true,
            cancellationToken);

        var returnedNonce = idToken.Principal.FindFirst("nonce")?.Value;
        if (!string.Equals(returnedNonce, nonce, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("The returned MyInfo nonce does not match the login session.");
        }

        var userInfoToken = await RequestUserInfoAsync(
            discovery,
            tokenExchange.AccessToken,
            dpopKey,
            cancellationToken);
        var validatedUserInfo = await ValidateEncryptedJwtAsync(
            userInfoToken,
            discovery,
            validateLifetime: false,
            cancellationToken);

        var personData = ParsePersonData(validatedUserInfo.PayloadJson);
        personData.NricFin ??= GetSubAccountIdentifier(idToken.PayloadJson);
        personData.Subject = idToken.Principal.FindFirst("sub")?.Value
            ?? validatedUserInfo.Principal.FindFirst("sub")?.Value;
        personData.VerifiedAtUtc = GetIssuedAt(idToken.Principal)
            ?? GetIssuedAt(validatedUserInfo.Principal)
            ?? DateTimeHelper.UtcOffsetNow;

        return personData;
    }

    private void EnsureConfigured()
    {
        if (!IsConfigured)
        {
            throw new InvalidOperationException("MyInfo is not fully configured.");
        }
    }

    private async Task<string> CreatePushedAuthorizationRequestAsync(
        MyInfoDiscoveryDocument discovery,
        string state,
        string nonce,
        string codeChallenge,
        EphemeralEcJwk dpopKey,
        CancellationToken cancellationToken)
    {
        var parEndpoint = discovery.PushedAuthorizationRequestEndpoint
            ?? throw new InvalidOperationException("MyInfo discovery document is missing the PAR endpoint.");
        var clientAssertion = CreateClientAssertion(discovery.Issuer);

        using var request = new HttpRequestMessage(HttpMethod.Post, parEndpoint)
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["response_type"] = "code",
                ["client_id"] = _clientId,
                ["redirect_uri"] = _redirectUri,
                ["scope"] = _scope,
                ["state"] = state,
                ["nonce"] = nonce,
                ["code_challenge"] = codeChallenge,
                ["code_challenge_method"] = "S256",
                ["client_assertion_type"] = ClientAssertionType,
                ["client_assertion"] = clientAssertion,
            }),
        };
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.TryAddWithoutValidation("DPoP", CreateDpopProof(HttpMethod.Post.Method, parEndpoint, dpopKey));

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var errorMessage = BuildOAuthErrorMessage(
                responseBody,
                "Singpass rejected the authorization request.");
            _logger.LogError(
                "MyInfo pushed authorization request failed with status {StatusCode}. ClientId {ClientId}, SigningKid {SigningKeyId}, Body: {Body}",
                (int)response.StatusCode,
                _clientId,
                _signingKeyId,
                responseBody);
            throw new InvalidOperationException(errorMessage);
        }

        using var document = JsonDocument.Parse(responseBody);
        var requestUri = document.RootElement.GetProperty("request_uri").GetString();
        if (string.IsNullOrWhiteSpace(requestUri))
        {
            throw new InvalidOperationException("MyInfo PAR response did not include a request_uri.");
        }

        return requestUri;
    }

    private async Task<TokenExchangeResult> ExchangeCodeForTokensAsync(
        MyInfoDiscoveryDocument discovery,
        string authCode,
        string codeVerifier,
        EphemeralEcJwk? dpopKey,
        CancellationToken cancellationToken)
    {
        var clientAssertion = CreateClientAssertion(discovery.Issuer);
        using var request = new HttpRequestMessage(HttpMethod.Post, discovery.TokenEndpoint)
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "authorization_code",
                ["code"] = authCode,
                ["redirect_uri"] = _redirectUri,
                ["client_assertion_type"] = ClientAssertionType,
                ["client_assertion"] = clientAssertion,
                ["code_verifier"] = codeVerifier,
            }),
        };

        if (dpopKey is not null)
        {
            request.Headers.TryAddWithoutValidation(
                "DPoP",
                CreateDpopProof(HttpMethod.Post.Method, discovery.TokenEndpoint, dpopKey));
        }

        using var tokenResponse = await _httpClient.SendAsync(request, cancellationToken);
        var tokenBody = await tokenResponse.Content.ReadAsStringAsync(cancellationToken);
        if (!tokenResponse.IsSuccessStatusCode)
        {
            var errorMessage = BuildOAuthErrorMessage(
                tokenBody,
                "Singpass rejected the token exchange request.");
            _logger.LogError(
                "MyInfo token exchange failed with status {StatusCode}. ClientId {ClientId}, SigningKid {SigningKeyId}, Body: {Body}",
                (int)tokenResponse.StatusCode,
                _clientId,
                _signingKeyId,
                tokenBody);
            throw new InvalidOperationException(errorMessage);
        }

        using var tokenDocument = JsonDocument.Parse(tokenBody);
        var accessToken = tokenDocument.RootElement.GetProperty("access_token").GetString();
        var idToken = tokenDocument.RootElement.GetProperty("id_token").GetString();
        var tokenType = tokenDocument.RootElement.TryGetProperty("token_type", out var tokenTypeElement)
            ? tokenTypeElement.GetString()
            : null;

        if (SupportsFapi(discovery) &&
            !string.IsNullOrWhiteSpace(tokenType) &&
            !string.Equals(tokenType, "DPoP", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("MyInfo token endpoint returned unexpected token_type '{TokenType}'.", tokenType);
        }

        if (string.IsNullOrWhiteSpace(accessToken) || string.IsNullOrWhiteSpace(idToken))
        {
            throw new InvalidOperationException("MyInfo token response was incomplete.");
        }

        return new TokenExchangeResult(accessToken, idToken);
    }

    private async Task<string> RequestUserInfoAsync(
        MyInfoDiscoveryDocument discovery,
        string accessToken,
        EphemeralEcJwk? dpopKey,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, discovery.UserInfoEndpoint);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/jwt"));

        if (dpopKey is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("DPoP", accessToken);
            request.Headers.TryAddWithoutValidation(
                "DPoP",
                CreateDpopProof(HttpMethod.Get.Method, discovery.UserInfoEndpoint, dpopKey, accessToken));
        }
        else
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "MyInfo userinfo request failed with status {StatusCode}: {Body}",
                (int)response.StatusCode,
                responseBody);

            if (responseBody.Contains("has yet to be provisioned", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "Your MyInfo profile has not been set up yet. Please visit the Singpass app or portal to complete your MyInfo profile before using this feature.");
            }

            throw new InvalidOperationException($"MyInfo data retrieval failed (status {(int)response.StatusCode}). Please try again later.");
        }

        return responseBody.Trim();
    }

    private async Task<ValidatedJwtResult> ValidateEncryptedJwtAsync(
        string token,
        MyInfoDiscoveryDocument discovery,
        bool validateLifetime,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        AppContext.SetSwitch(
            "Switch.Microsoft.IdentityModel.UseRfcDefinitionOfEpkAndKid", true);

        var jweInfo = LogJweHeader(token);

        var handler = new JsonWebTokenHandler
        {
            MapInboundClaims = false,
        };

        var encryptionKeys = CreateClientEncryptionSecurityKeys();

        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = discovery.Issuer,
            ValidateAudience = true,
            ValidAudience = _clientId,
            ValidateIssuerSigningKey = true,
            IssuerSigningKeys = await GetIssuerSigningKeysAsync(discovery.JwksUri, cancellationToken),
            TokenDecryptionKeyResolver = (token, securityToken, kid, parameters) =>
            {
                _logger.LogInformation(
                    "MyInfo JWT decryption key resolver invoked. JWE kid: {Kid}, returning {Count} key candidates",
                    kid,
                    encryptionKeys.Length);
                return encryptionKeys;
            },
            RequireSignedTokens = true,
            ValidateLifetime = validateLifetime,
            RequireExpirationTime = validateLifetime,
            ClockSkew = TimeSpan.FromMinutes(1),
        };

        var result = await handler.ValidateTokenAsync(token, validationParameters);
        cancellationToken.ThrowIfCancellationRequested();
        if (!result.IsValid)
        {
            if (result.Exception is SecurityTokenDecryptionFailedException)
            {
                _logger.LogWarning(
                    "Library JWE decryption failed for alg={Alg} enc={Enc}, attempting manual decryption",
                    jweInfo.Alg, jweInfo.Enc);
                return await ManualDecryptAndValidateAsync(
                    token, discovery, validateLifetime, handler, validationParameters);
            }

            throw new SecurityTokenException(
                "MyInfo JWT validation failed.",
                result.Exception);
        }

        return ExtractValidatedResult(result);
    }

    private async Task<ValidatedJwtResult> ManualDecryptAndValidateAsync(
        string jweToken,
        MyInfoDiscoveryDocument discovery,
        bool validateLifetime,
        JsonWebTokenHandler handler,
        TokenValidationParameters baseValidationParameters)
    {
        var parts = jweToken.Split('.');
        if (parts.Length != 5)
            throw new SecurityTokenException("Invalid JWE token structure.");

        var headerJson = Base64UrlEncoder.Decode(parts[0]);
        using var headerDoc = JsonDocument.Parse(headerJson);
        var header = headerDoc.RootElement;

        var alg = header.GetProperty("alg").GetString()!;
        var enc = header.GetProperty("enc").GetString()!;
        var epk = header.GetProperty("epk");

        var storedKey = LoadClientPrivateKey(_encryptionKeyId, expectedUse: "enc");
        using var ourPrivateKey = ECDiffieHellman.Create(new ECParameters
        {
            Curve = ResolveCurve(storedKey.Curve),
            Q = new ECPoint
            {
                X = Base64UrlEncoder.DecodeBytes(storedKey.X),
                Y = Base64UrlEncoder.DecodeBytes(storedKey.Y),
            },
            D = Base64UrlEncoder.DecodeBytes(storedKey.D),
        });

        using var ephemeralPublicKey = ECDiffieHellman.Create(new ECParameters
        {
            Curve = ResolveCurve(epk.GetProperty("crv").GetString()!),
            Q = new ECPoint
            {
                X = Base64UrlEncoder.DecodeBytes(epk.GetProperty("x").GetString()!),
                Y = Base64UrlEncoder.DecodeBytes(epk.GetProperty("y").GetString()!),
            },
        });

        var (cekSizeBytes, _) = alg switch
        {
            "ECDH-ES+A128KW" => (16, "A128KW"),
            "ECDH-ES+A192KW" => (24, "A192KW"),
            "ECDH-ES+A256KW" => (32, "A256KW"),
            _ => throw new NotSupportedException($"Unsupported JWE alg: {alg}"),
        };

        var algorithmId = Encoding.ASCII.GetBytes(alg);
        var apu = header.TryGetProperty("apu", out var apuEl) && apuEl.GetString() is string apuStr
            ? Base64UrlEncoder.DecodeBytes(apuStr) : [];
        var apv = header.TryGetProperty("apv", out var apvEl) && apvEl.GetString() is string apvStr
            ? Base64UrlEncoder.DecodeBytes(apvStr) : [];
        var suppPubInfo = BitConverter.GetBytes(System.Net.IPAddress.HostToNetworkOrder(cekSizeBytes * 8));

        var otherInfo = BuildConcatKdfOtherInfo(algorithmId, apu, apv, suppPubInfo);
        var sharedSecret = ourPrivateKey.DeriveRawSecretAgreement(ephemeralPublicKey.PublicKey);
        var kek = ConcatKdf(sharedSecret, cekSizeBytes, otherInfo);

        _logger.LogInformation(
            "Manual ECDH-ES: alg={Alg}, enc={Enc}, kekSize={KekSize}, sharedSecretLen={SharedSecretLen}, wrappedCekLen={WrappedCekLen}",
            alg, enc, cekSizeBytes, sharedSecret.Length, parts[1].Length);

        var wrappedCek = Base64UrlEncoder.DecodeBytes(parts[1]);
        var cek = AesKeyUnwrap(kek, wrappedCek);

        var iv = Base64UrlEncoder.DecodeBytes(parts[2]);
        var ciphertext = Base64UrlEncoder.DecodeBytes(parts[3]);
        var authTag = Base64UrlEncoder.DecodeBytes(parts[4]);
        var aad = Encoding.ASCII.GetBytes(parts[0]);

        var plaintext = enc switch
        {
            "A128GCM" or "A192GCM" or "A256GCM" => DecryptAesGcm(cek, iv, ciphertext, authTag, aad),
            "A128CBC-HS256" => DecryptAesCbcHmac(cek, iv, ciphertext, authTag, aad, 16),
            "A192CBC-HS384" => DecryptAesCbcHmac(cek, iv, ciphertext, authTag, aad, 24),
            "A256CBC-HS512" => DecryptAesCbcHmac(cek, iv, ciphertext, authTag, aad, 32),
            _ => throw new NotSupportedException($"Unsupported JWE enc: {enc}"),
        };

        var innerJws = Encoding.UTF8.GetString(plaintext);
        _logger.LogInformation("Manual JWE decryption succeeded, validating inner JWS");

        var jwsValidationParams = baseValidationParameters.Clone();
        jwsValidationParams.TokenDecryptionKeyResolver = null;
        jwsValidationParams.TokenDecryptionKey = null;

        var jwsResult = await handler.ValidateTokenAsync(innerJws, jwsValidationParams);
        if (!jwsResult.IsValid)
        {
            throw new SecurityTokenException(
                "MyInfo inner JWS validation failed after manual decryption.",
                jwsResult.Exception);
        }

        return ExtractValidatedResult(jwsResult);
    }

    private static ValidatedJwtResult ExtractValidatedResult(TokenValidationResult result)
    {
        var principal = new ClaimsPrincipal(result.ClaimsIdentity);
        var payloadJson = result.SecurityToken is Microsoft.IdentityModel.JsonWebTokens.JsonWebToken jwt
            ? Base64UrlEncoder.Decode(jwt.EncodedPayload)
            : throw new InvalidOperationException("Validated MyInfo token was not a JWT.");

        return new ValidatedJwtResult(principal, payloadJson);
    }

    private static byte[] BuildConcatKdfOtherInfo(
        byte[] algorithmId, byte[] apu, byte[] apv, byte[] suppPubInfo)
    {
        using var ms = new MemoryStream();
        ms.Write(IntToBytes(algorithmId.Length));
        ms.Write(algorithmId);
        ms.Write(IntToBytes(apu.Length));
        ms.Write(apu);
        ms.Write(IntToBytes(apv.Length));
        ms.Write(apv);
        ms.Write(suppPubInfo);
        return ms.ToArray();
    }

    private static byte[] IntToBytes(int value)
    {
        return BitConverter.GetBytes(System.Net.IPAddress.HostToNetworkOrder(value));
    }

    private static byte[] ConcatKdf(byte[] sharedSecret, int keySizeBytes, byte[] otherInfo)
    {
        var hashInput = new byte[4 + sharedSecret.Length + otherInfo.Length];
        hashInput[0] = 0; hashInput[1] = 0; hashInput[2] = 0; hashInput[3] = 1;
        Buffer.BlockCopy(sharedSecret, 0, hashInput, 4, sharedSecret.Length);
        Buffer.BlockCopy(otherInfo, 0, hashInput, 4 + sharedSecret.Length, otherInfo.Length);

        var hash = SHA256.HashData(hashInput);
        return hash[..keySizeBytes];
    }

    private static byte[] AesKeyUnwrap(byte[] kek, byte[] wrappedKey)
    {
        if (wrappedKey.Length < 16 || wrappedKey.Length % 8 != 0)
            throw new CryptographicException("Invalid wrapped key length.");

        int n = (wrappedKey.Length / 8) - 1;
        var a = new byte[8];
        Buffer.BlockCopy(wrappedKey, 0, a, 0, 8);
        var r = new byte[n * 8];
        Buffer.BlockCopy(wrappedKey, 8, r, 0, n * 8);

        using var aes = Aes.Create();
        aes.Key = kek;
        aes.Mode = CipherMode.ECB;
        aes.Padding = PaddingMode.None;
        using var decryptor = aes.CreateDecryptor();

        var block = new byte[16];
        var decrypted = new byte[16];

        for (int j = 5; j >= 0; j--)
        {
            for (int i = n; i >= 1; i--)
            {
                long t = (long)n * j + i;
                var tBytes = BitConverter.GetBytes(System.Net.IPAddress.HostToNetworkOrder(t));
                for (int k = 0; k < 8; k++)
                    block[k] = (byte)(a[k] ^ tBytes[k]);
                Buffer.BlockCopy(r, (i - 1) * 8, block, 8, 8);

                decryptor.TransformBlock(block, 0, 16, decrypted, 0);

                Buffer.BlockCopy(decrypted, 0, a, 0, 8);
                Buffer.BlockCopy(decrypted, 8, r, (i - 1) * 8, 8);
            }
        }

        ReadOnlySpan<byte> defaultIv = [0xA6, 0xA6, 0xA6, 0xA6, 0xA6, 0xA6, 0xA6, 0xA6];
        if (!a.AsSpan().SequenceEqual(defaultIv))
            throw new CryptographicException("AES Key Unwrap integrity check failed.");

        return r;
    }

    private static byte[] DecryptAesGcm(
        byte[] key, byte[] iv, byte[] ciphertext, byte[] authTag, byte[] aad)
    {
        var plaintext = new byte[ciphertext.Length];
        using var aesGcm = new AesGcm(key, authTag.Length);
        aesGcm.Decrypt(iv, ciphertext, authTag, plaintext, aad);
        return plaintext;
    }

    private static byte[] DecryptAesCbcHmac(
        byte[] key, byte[] iv, byte[] ciphertext, byte[] authTag, byte[] aad, int hashKeySize)
    {
        var macKey = key[..hashKeySize];
        var encKey = key[hashKeySize..];

        var al = BitConverter.GetBytes(System.Net.IPAddress.HostToNetworkOrder((long)aad.Length * 8));
        using var hmac = hashKeySize switch
        {
            16 => (HMAC)new HMACSHA256(macKey),
            24 => new HMACSHA384(macKey),
            32 => new HMACSHA512(macKey),
            _ => throw new NotSupportedException(),
        };
        var hmacInput = new byte[aad.Length + iv.Length + ciphertext.Length + al.Length];
        Buffer.BlockCopy(aad, 0, hmacInput, 0, aad.Length);
        Buffer.BlockCopy(iv, 0, hmacInput, aad.Length, iv.Length);
        Buffer.BlockCopy(ciphertext, 0, hmacInput, aad.Length + iv.Length, ciphertext.Length);
        Buffer.BlockCopy(al, 0, hmacInput, aad.Length + iv.Length + ciphertext.Length, al.Length);
        var computedTag = hmac.ComputeHash(hmacInput)[..hashKeySize];

        if (!CryptographicOperations.FixedTimeEquals(computedTag, authTag))
            throw new CryptographicException("JWE authentication tag verification failed.");

        using var aes = Aes.Create();
        aes.Key = encKey;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        aes.IV = iv;
        using var decryptor = aes.CreateDecryptor();
        return decryptor.TransformFinalBlock(ciphertext, 0, ciphertext.Length);
    }

    private (string Alg, string Enc) LogJweHeader(string token)
    {
        string alg = "unknown", enc = "unknown";
        try
        {
            var firstDot = token.IndexOf('.');
            if (firstDot > 0)
            {
                var headerJson = Base64UrlEncoder.Decode(token[..firstDot]);
                using var doc = JsonDocument.Parse(headerJson);
                alg = doc.RootElement.TryGetProperty("alg", out var algEl) ? algEl.GetString() ?? "unknown" : "unknown";
                enc = doc.RootElement.TryGetProperty("enc", out var encEl) ? encEl.GetString() ?? "unknown" : "unknown";
                var kid = doc.RootElement.TryGetProperty("kid", out var kidEl) ? kidEl.GetString() : "none";
                _logger.LogInformation(
                    "MyInfo JWE header: alg={Alg}, enc={Enc}, kid={Kid}, parts={Parts}",
                    alg, enc, kid, token.Count(c => c == '.') + 1);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not parse JWE header for diagnostics");
        }
        return (alg, enc);
    }

    private string CreateClientAssertion(string issuer)
    {
        var signingKey = CreateClientSigningSecurityKey();
        var now = DateTimeHelper.UtcOffsetNow;
        var header = new JwtHeader(new SigningCredentials(signingKey, SecurityAlgorithms.EcdsaSha256))
        {
            ["typ"] = "JWT",
            ["kid"] = signingKey.KeyId,
        };

        var payload = new JwtPayload
        {
            ["iss"] = _clientId,
            ["sub"] = _clientId,
            ["aud"] = issuer,
            ["iat"] = now.ToUnixTimeSeconds(),
            ["exp"] = now.AddMinutes(2).ToUnixTimeSeconds(),
            ["jti"] = Guid.NewGuid().ToString("N"),
        };

        var token = new JwtSecurityToken(header, payload);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private string CreateDpopProof(
        string method,
        string endpoint,
        EphemeralEcJwk dpopKey,
        string? accessToken = null)
    {
        var signingKey = CreateEphemeralSigningKey(dpopKey);
        var now = DateTimeHelper.UtcOffsetNow;
        var header = new JwtHeader(new SigningCredentials(signingKey, SecurityAlgorithms.EcdsaSha256))
        {
            ["typ"] = "dpop+jwt",
            ["jwk"] = new Dictionary<string, string>
            {
                ["kty"] = dpopKey.KeyType,
                ["crv"] = dpopKey.Curve,
                ["x"] = dpopKey.X,
                ["y"] = dpopKey.Y,
            },
        };

        var payload = new JwtPayload
        {
            ["jti"] = Guid.NewGuid().ToString(),
            ["htm"] = method.ToUpperInvariant(),
            ["htu"] = endpoint,
            ["iat"] = now.ToUnixTimeSeconds(),
            ["exp"] = now.AddMinutes(2).ToUnixTimeSeconds(),
        };

        if (!string.IsNullOrWhiteSpace(accessToken))
        {
            payload["ath"] = Base64UrlEncoder.Encode(
                SHA256.HashData(Encoding.ASCII.GetBytes(accessToken)));
        }

        var token = new JwtSecurityToken(header, payload);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private ECDsaSecurityKey CreateClientSigningSecurityKey()
    {
        var key = LoadClientPrivateKey(_signingKeyId, expectedUse: "sig");
        var ecdsa = ECDsa.Create(new ECParameters
        {
            Curve = ResolveCurve(key.Curve),
            Q = new ECPoint
            {
                X = Base64UrlEncoder.DecodeBytes(key.X),
                Y = Base64UrlEncoder.DecodeBytes(key.Y),
            },
            D = Base64UrlEncoder.DecodeBytes(key.D),
        });

        return new ECDsaSecurityKey(ecdsa)
        {
            KeyId = key.KeyId,
        };
    }

    private SecurityKey[] CreateClientEncryptionSecurityKeys()
    {
        var key = LoadClientPrivateKey(_encryptionKeyId, expectedUse: "enc");
        var ecParams = new ECParameters
        {
            Curve = ResolveCurve(key.Curve),
            Q = new ECPoint
            {
                X = Base64UrlEncoder.DecodeBytes(key.X),
                Y = Base64UrlEncoder.DecodeBytes(key.Y),
            },
            D = Base64UrlEncoder.DecodeBytes(key.D),
        };

        var ecdsa = ECDsa.Create(ecParams);
        var ecdsaKey = new ECDsaSecurityKey(ecdsa) { KeyId = key.KeyId };

        var jwk = new JsonWebKey
        {
            Kty = "EC",
            Crv = key.Curve,
            X = key.X,
            Y = key.Y,
            D = key.D,
            Kid = key.KeyId,
            Use = "enc",
            Alg = key.Alg ?? SecurityAlgorithms.EcdhEsA256kw,
        };

        return [ecdsaKey, jwk];
    }

    private ECDsaSecurityKey CreateEphemeralSigningKey(EphemeralEcJwk dpopKey)
    {
        var ecdsa = ECDsa.Create(new ECParameters
        {
            Curve = ResolveCurve(dpopKey.Curve),
            Q = new ECPoint
            {
                X = Base64UrlEncoder.DecodeBytes(dpopKey.X),
                Y = Base64UrlEncoder.DecodeBytes(dpopKey.Y),
            },
            D = Base64UrlEncoder.DecodeBytes(dpopKey.D),
        });

        return new ECDsaSecurityKey(ecdsa)
        {
            KeyId = dpopKey.KeyId,
        };
    }

    private EphemeralEcJwk CreateDpopKey()
    {
        using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var parameters = ecdsa.ExportParameters(true);

        return new EphemeralEcJwk(
            Guid.NewGuid().ToString("N"),
            "EC",
            "P-256",
            Base64UrlEncoder.Encode(parameters.Q.X ?? []),
            Base64UrlEncoder.Encode(parameters.Q.Y ?? []),
            Base64UrlEncoder.Encode(parameters.D ?? []));
    }

    private static EphemeralEcJwk LoadDpopPrivateKey(string serializedKey)
    {
        if (string.IsNullOrWhiteSpace(serializedKey))
        {
            throw new InvalidOperationException("The MyInfo DPoP key is missing from the login session.");
        }

        return JsonSerializer.Deserialize<EphemeralEcJwk>(serializedKey)
            ?? throw new InvalidOperationException("The MyInfo DPoP key could not be loaded.");
    }

    private StoredEcJwk LoadClientPrivateKey(string keyId, string expectedUse)
    {
        var keys = _memoryCache.GetOrCreate(
            $"myinfo:client-jwks:{_privateJwksPath}",
            entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = ClientKeyCacheLifetime;
                using var document = JsonDocument.Parse(File.ReadAllText(_privateJwksPath));
                var bundle = new Dictionary<string, StoredEcJwk>(StringComparer.Ordinal);
                foreach (var key in document.RootElement.GetProperty("keys").EnumerateArray())
                {
                    var storedKey = new StoredEcJwk(
                        key.GetProperty("kid").GetString() ?? string.Empty,
                        key.GetProperty("use").GetString() ?? string.Empty,
                        key.GetProperty("crv").GetString() ?? string.Empty,
                        key.GetProperty("x").GetString() ?? string.Empty,
                        key.GetProperty("y").GetString() ?? string.Empty,
                        key.TryGetProperty("d", out var dElement) ? dElement.GetString() : null,
                        key.TryGetProperty("alg", out var algElement) ? algElement.GetString() : null);
                    bundle[storedKey.KeyId] = storedKey;
                }

                return bundle;
            });

        if (keys is null || !keys.TryGetValue(keyId, out var key))
        {
            throw new InvalidOperationException($"MyInfo client key '{keyId}' was not found in the configured private JWKS.");
        }

        if (!string.Equals(key.Use, expectedUse, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"MyInfo client key '{keyId}' must be configured with use '{expectedUse}'.");
        }

        if (string.IsNullOrWhiteSpace(key.D))
        {
            throw new InvalidOperationException(
                $"MyInfo client key '{keyId}' is missing private key material.");
        }

        return key;
    }

    private async Task<IReadOnlyCollection<SecurityKey>> GetIssuerSigningKeysAsync(
        string jwksUri,
        CancellationToken cancellationToken)
    {
        var cacheKey = $"myinfo:issuer-jwks:{jwksUri}";
        if (_memoryCache.TryGetValue<IReadOnlyCollection<SecurityKey>>(cacheKey, out var cachedKeys) &&
            cachedKeys is not null)
        {
            return cachedKeys;
        }

        var responseBody = await _httpClient.GetStringAsync(jwksUri, cancellationToken);
        using var document = JsonDocument.Parse(responseBody);
        var keys = new List<SecurityKey>();
        foreach (var key in document.RootElement.GetProperty("keys").EnumerateArray())
        {
            var use = key.TryGetProperty("use", out var useElement) ? useElement.GetString() : null;
            if (!string.Equals(use, "sig", StringComparison.Ordinal))
            {
                continue;
            }

            var curve = key.GetProperty("crv").GetString() ?? string.Empty;
            var ecdsa = ECDsa.Create(new ECParameters
            {
                Curve = ResolveCurve(curve),
                Q = new ECPoint
                {
                    X = Base64UrlEncoder.DecodeBytes(key.GetProperty("x").GetString() ?? string.Empty),
                    Y = Base64UrlEncoder.DecodeBytes(key.GetProperty("y").GetString() ?? string.Empty),
                },
            });

            keys.Add(new ECDsaSecurityKey(ecdsa)
            {
                KeyId = key.GetProperty("kid").GetString(),
            });
        }

        _memoryCache.Set(cacheKey, keys, SigningKeyCacheLifetime);
        return keys;
    }

    private async Task<MyInfoDiscoveryDocument> GetDiscoveryDocumentAsync(CancellationToken cancellationToken)
    {
        var cacheKey = $"myinfo:discovery:{_discoveryUrl}";
        if (_memoryCache.TryGetValue<MyInfoDiscoveryDocument>(cacheKey, out var cachedDocument) &&
            cachedDocument is not null)
        {
            return cachedDocument;
        }

        using var response = await _httpClient.GetAsync(_discoveryUrl, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "MyInfo discovery request failed with status {StatusCode}: {Body}",
                (int)response.StatusCode,
                responseBody);
            throw new InvalidOperationException("MyInfo discovery endpoint is not reachable.");
        }

        var document = JsonSerializer.Deserialize<MyInfoDiscoveryDocument>(
            responseBody,
            JsonSerializerOptions.Web) ??
            throw new InvalidOperationException("MyInfo discovery response was empty.");

        _memoryCache.Set(cacheKey, document, DiscoveryCacheLifetime);
        return document;
    }

    private static MyInfoPersonData ParsePersonData(string payloadJson)
    {
        using var document = JsonDocument.Parse(payloadJson);
        var root = document.RootElement;
        if (root.TryGetProperty("person_info", out var personInfo))
        {
            root = personInfo;
        }

        return new MyInfoPersonData
        {
            Name = GetMyInfoValue(root, "name"),
            NricFin = GetMyInfoValue(root, "uinfin"),
            Sex = GetMyInfoValue(root, "sex"),
            Race = GetMyInfoValue(root, "race"),
            Nationality = GetMyInfoValue(root, "nationality"),
            DateOfBirth = ParseDate(GetMyInfoValue(root, "dob")),
            BirthCountry = GetMyInfoValue(root, "birthcountry"),
            ResidentialStatus = GetMyInfoValue(root, "residentialstatus"),
            MaritalStatus = GetMyInfoValue(root, "marital"),
            Email = GetMyInfoValue(root, "email"),
            MobileNumber = GetMyInfoValue(root, "mobileno"),
            PostalCode = GetNestedMyInfoValue(root, "regadd", "postal"),
            BlockNumber = GetNestedMyInfoValue(root, "regadd", "block"),
            StreetName = GetNestedMyInfoValue(root, "regadd", "street"),
            FloorNumber = GetNestedMyInfoValue(root, "regadd", "floor"),
            UnitNumber = GetNestedMyInfoValue(root, "regadd", "unit"),
            RegisteredAddress = BuildRegisteredAddress(root),
            HighestQualification = GetMyInfoValue(root, "hqualification"),
            Occupation = GetMyInfoValue(root, "occupation"),
            EmployerName = GetNestedMyInfoValue(root, "employment", "value"),
        };
    }

    private static string? GetSubAccountIdentifier(string payloadJson)
    {
        using var document = JsonDocument.Parse(payloadJson);
        if (!document.RootElement.TryGetProperty("sub_account", out var subAccount) ||
            subAccount.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        if (subAccount.TryGetProperty("uinfin", out var uinfin))
        {
            return ExtractValue(uinfin);
        }

        if (subAccount.TryGetProperty("foreign_id", out var foreignId))
        {
            return ExtractValue(foreignId);
        }

        return null;
    }

    private static string? BuildRegisteredAddress(JsonElement root)
    {
        if (!root.TryGetProperty("regadd", out _))
        {
            return null;
        }

        return string.Join(
            ", ",
            new[]
            {
                GetNestedMyInfoValue(root, "regadd", "block"),
                GetNestedMyInfoValue(root, "regadd", "street"),
                JoinAddressParts(
                    GetNestedMyInfoValue(root, "regadd", "floor"),
                    GetNestedMyInfoValue(root, "regadd", "unit")),
                GetNestedMyInfoValue(root, "regadd", "postal"),
            }.Where(value => !string.IsNullOrWhiteSpace(value)));
    }

    private static string? JoinAddressParts(string? floor, string? unit)
    {
        var combined = string.Join(
            "-",
            new[] { floor, unit }.Where(value => !string.IsNullOrWhiteSpace(value)));
        return string.IsNullOrWhiteSpace(combined) ? null : combined;
    }

    private static DateTime? ParseDate(string? value)
    {
        return DateTime.TryParse(value, out var parsed) ? parsed : null;
    }

    private static string? GetMyInfoValue(JsonElement root, string field)
    {
        if (!root.TryGetProperty(field, out var element))
        {
            return null;
        }

        return ExtractValue(element);
    }

    private static string? GetNestedMyInfoValue(JsonElement root, string field, string subField)
    {
        if (!root.TryGetProperty(field, out var element) || element.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        if (!element.TryGetProperty(subField, out var subElement))
        {
            return null;
        }

        return ExtractValue(subElement);
    }

    private static string? ExtractValue(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.ToString(),
            JsonValueKind.True => bool.TrueString,
            JsonValueKind.False => bool.FalseString,
            JsonValueKind.Object => ExtractValueFromObject(element),
            _ => null,
        };
    }

    private static string? ExtractValueFromObject(JsonElement element)
    {
        foreach (var propertyName in new[] { "value", "desc", "code" })
        {
            if (element.TryGetProperty(propertyName, out var property))
            {
                var value = ExtractValue(property);
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value;
                }
            }
        }

        return null;
    }

    private static DateTimeOffset? GetIssuedAt(ClaimsPrincipal principal)
    {
        var issuedAt = principal.FindFirst("iat")?.Value;
        if (!long.TryParse(issuedAt, out var unixSeconds))
        {
            return null;
        }

        return DateTimeOffset.FromUnixTimeSeconds(unixSeconds);
    }

    private static bool SupportsFapi(MyInfoDiscoveryDocument discovery)
    {
        return !string.IsNullOrWhiteSpace(discovery.PushedAuthorizationRequestEndpoint);
    }

    private string BuildOAuthErrorMessage(string responseBody, string fallbackMessage)
    {
        if (string.IsNullOrWhiteSpace(responseBody))
        {
            return fallbackMessage;
        }

        try
        {
            using var document = JsonDocument.Parse(responseBody);
            var root = document.RootElement;
            var error = root.TryGetProperty("error", out var errorElement)
                ? errorElement.GetString()
                : null;
            var description = root.TryGetProperty("error_description", out var descriptionElement)
                ? descriptionElement.GetString()
                : null;

            if (string.Equals(error, "invalid_client", StringComparison.Ordinal))
            {
                return "Singpass rejected the client assertion. Confirm that the registered App ID and JWKS match the current environment configuration.";
            }

            if (!string.IsNullOrWhiteSpace(description))
            {
                return description;
            }
        }
        catch (JsonException)
        {
        }

        return fallbackMessage;
    }

    private static string ResolveDiscoveryUrl(string? configuredDiscoveryUrl, string configuredBaseUrl)
    {
        if (!string.IsNullOrWhiteSpace(configuredDiscoveryUrl))
        {
            return configuredDiscoveryUrl;
        }

        var normalizedBaseUrl = NormalizeAuthorityBaseUrl(configuredBaseUrl);
        if (normalizedBaseUrl.EndsWith("/fapi", StringComparison.OrdinalIgnoreCase))
        {
            return $"{normalizedBaseUrl.TrimEnd('/')}/.well-known/openid-configuration";
        }

        return $"{normalizedBaseUrl.TrimEnd('/')}/fapi/.well-known/openid-configuration";
    }

    private static string NormalizeAuthorityBaseUrl(string configuredBaseUrl)
    {
        if (string.IsNullOrWhiteSpace(configuredBaseUrl))
        {
            return DefaultAuthority;
        }

        if (!Uri.TryCreate(configuredBaseUrl, UriKind.Absolute, out var uri))
        {
            return DefaultAuthority;
        }

        var baseAuthority = uri.Host.ToLowerInvariant() switch
        {
            "sandbox.api.myinfo.gov.sg" => "https://stg-id.singpass.gov.sg",
            "api.myinfo.gov.sg" => "https://id.singpass.gov.sg",
            _ => $"{uri.Scheme}://{uri.Authority}",
        };

        var normalizedPath = uri.AbsolutePath.TrimEnd('/');
        if (string.IsNullOrWhiteSpace(normalizedPath) || normalizedPath == "/")
        {
            return baseAuthority;
        }

        return $"{baseAuthority}{normalizedPath}";
    }

    private static string ResolveFilePath(string? configuredPath)
    {
        if (string.IsNullOrWhiteSpace(configuredPath))
        {
            return string.Empty;
        }

        return Path.IsPathRooted(configuredPath)
            ? configuredPath
            : Path.GetFullPath(configuredPath, AppContext.BaseDirectory);
    }

    private static string BuildScopeString(IConfiguration configuration)
    {
        var rawScopes = configuration["MyInfo:Scopes"];
        if (string.IsNullOrWhiteSpace(rawScopes))
        {
            rawScopes = configuration["MyInfo:Attributes"];
        }

        if (string.IsNullOrWhiteSpace(rawScopes))
        {
            rawScopes = DefaultScopeList;
        }

        var uniqueScopes = new HashSet<string>(StringComparer.Ordinal)
        {
            "openid",
        };

        foreach (var scope in rawScopes
                     .Replace(",", " ", StringComparison.Ordinal)
                     .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            uniqueScopes.Add(scope);
        }

        return string.Join(" ", uniqueScopes);
    }

    private static string BuildQueryString(IEnumerable<KeyValuePair<string, string>> values)
    {
        return string.Join(
            "&",
            values.Select(pair =>
                $"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(pair.Value)}"));
    }

    private static ECCurve ResolveCurve(string curve)
    {
        return curve switch
        {
            "P-256" => ECCurve.NamedCurves.nistP256,
            "P-384" => ECCurve.NamedCurves.nistP384,
            "P-521" => ECCurve.NamedCurves.nistP521,
            _ => throw new InvalidOperationException($"Unsupported MyInfo curve '{curve}'."),
        };
    }

    private sealed record MyInfoDiscoveryDocument(
        [property: JsonPropertyName("issuer")] string Issuer,
        [property: JsonPropertyName("authorization_endpoint")] string AuthorizationEndpoint,
        [property: JsonPropertyName("token_endpoint")] string TokenEndpoint,
        [property: JsonPropertyName("userinfo_endpoint")] string UserInfoEndpoint,
        [property: JsonPropertyName("jwks_uri")] string JwksUri,
        [property: JsonPropertyName("pushed_authorization_request_endpoint")] string? PushedAuthorizationRequestEndpoint);

    private sealed record StoredEcJwk(
        string KeyId,
        string Use,
        string Curve,
        string X,
        string Y,
        string? D,
        string? Alg);

    private sealed record EphemeralEcJwk(
        string KeyId,
        string KeyType,
        string Curve,
        string X,
        string Y,
        string D);

    private sealed record TokenExchangeResult(string AccessToken, string IdToken);

    private sealed record ValidatedJwtResult(ClaimsPrincipal Principal, string PayloadJson);
}

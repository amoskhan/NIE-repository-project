using System.Security.Cryptography;
using System.Text.Json;
using Jose;
using Microsoft.IdentityModel.Tokens;

namespace Auth.Tests.TestDoubles;

/// <summary>
/// An RSA key pair used to sign the inner JWS of a Portal SSO payload.
/// </summary>
/// <remarks>
/// Signing key pairs are deliberately per-harness for callback tests. Microsoft.IdentityModel keeps
/// a process-wide <c>CryptoProviderFactory</c> cache keyed by the signing key's material, and
/// <c>PortalSsoService</c> disposes the RSA instance it wraps, so reusing one public key across
/// validations makes later validations observe a disposed key.
/// </remarks>
internal sealed class SsoSigningKeyPair
{
    private SsoSigningKeyPair(string privateKeyPem, string publicKeyPem)
    {
        PrivateKeyPem = privateKeyPem;
        PublicKeyPem = publicKeyPem;
    }

    public static SsoSigningKeyPair Shared { get; } = Create();

    public string PrivateKeyPem { get; }

    public string PublicKeyPem { get; }

    public static SsoSigningKeyPair Create()
    {
        using var rsa = RSA.Create(2048);
        return new SsoSigningKeyPair(rsa.ExportPkcs8PrivateKeyPem(), rsa.ExportSubjectPublicKeyInfoPem());
    }
}

/// <summary>
/// Shared RSA material for the outer JWE. Encryption runs entirely inside jose-jwt, which keeps no
/// process-wide cache, so a single pair is safe to reuse across the whole run.
/// </summary>
internal static class SsoCryptoMaterial
{
    private static readonly Lazy<EncryptionKeys> Encryption =
        new(CreateEncryptionKeys, LazyThreadSafetyMode.ExecutionAndPublication);

    public static string DecryptionPrivateKeyPem => Encryption.Value.PrivateKeyPem;

    public static string DecryptionPublicKeyPem => Encryption.Value.PublicKeyPem;

    public static string UntrustedSigningPrivateKeyPem => UntrustedSigningKeys.Value.PrivateKeyPem;

    private static Lazy<SsoSigningKeyPair> UntrustedSigningKeys { get; } =
        new(SsoSigningKeyPair.Create, LazyThreadSafetyMode.ExecutionAndPublication);

    private static EncryptionKeys CreateEncryptionKeys()
    {
        using var rsa = RSA.Create(2048);
        return new EncryptionKeys(rsa.ExportPkcs8PrivateKeyPem(), rsa.ExportSubjectPublicKeyInfoPem());
    }

    private sealed record EncryptionKeys(string PrivateKeyPem, string PublicKeyPem);
}

/// <summary>
/// Builds the nested tokens the Portal SSO callback expects: an inner PS256 JWS wrapped in an
/// RSA-OAEP-256/A256GCM JWE.
/// </summary>
internal static class SsoTokenFactory
{
    public const string Issuer = "https://portal.nie.edu.sg";
    public const string Audience = "application-auth";
    public const string SourceSystemId = "portal";
    public const string SourceUrl = "https://portal.nie.edu.sg/launch";
    public const string ExchangeToken = "portal-exchange-token";

    public static Dictionary<string, object> Claims(string state, string nonce, string? jti = null)
    {
        var now = DateTimeOffset.UtcNow;
        return new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["iss"] = Issuer,
            ["aud"] = Audience,
            ["iat"] = now.ToUnixTimeSeconds(),
            ["nbf"] = now.AddMinutes(-1).ToUnixTimeSeconds(),
            ["exp"] = now.AddMinutes(5).ToUnixTimeSeconds(),
            ["jti"] = jti ?? Guid.NewGuid().ToString("N"),
            ["state"] = state,
            ["nonce"] = nonce,
            ["source_system"] = SourceSystemId,
            ["source_url"] = SourceUrl,
            ["exchange_token"] = ExchangeToken,
            ["preferred_username"] = "dev.ia",
            ["email"] = "dev.ia@nie.edu.sg",
            ["sub"] = "devia"
        };
    }

    public static string Encrypt(
        IDictionary<string, object> claims,
        string signingPrivateKeyPem,
        JwsAlgorithm signingAlgorithm = JwsAlgorithm.PS256)
    {
        using var signing = RSA.Create();
        signing.ImportFromPem(signingPrivateKeyPem);
        var innerToken = JWT.Encode(JsonSerializer.Serialize(claims), signing, signingAlgorithm);

        using var encryption = RSA.Create();
        encryption.ImportFromPem(SsoCryptoMaterial.DecryptionPublicKeyPem);
        return JWT.Encode(innerToken, encryption, JweAlgorithm.RSA_OAEP_256, JweEncryption.A256GCM);
    }

    public static string CompactTokenWithHeader(string alg, string enc)
    {
        var header = Base64UrlEncoder.Encode(JsonSerializer.Serialize(new { alg, enc }));
        return string.Join('.', header, RandomSegment(256), RandomSegment(12), RandomSegment(64), RandomSegment(16));
    }

    public static string UndecryptableToken() => CompactTokenWithHeader("RSA-OAEP-256", "A256GCM");

    private static string RandomSegment(int byteCount)
    {
        var bytes = new byte[byteCount];
        RandomNumberGenerator.Fill(bytes);
        return Base64UrlEncoder.Encode(bytes);
    }
}

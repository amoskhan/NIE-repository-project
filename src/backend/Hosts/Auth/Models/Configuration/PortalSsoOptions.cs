namespace Auth.Models;

public class PortalSsoOptions
{
    public const string SectionName = "PortalSso";

    public bool Enabled { get; set; }
    public string LaunchUrlTemplate { get; set; } = string.Empty;
    public string DefaultReturnUrl { get; set; } = string.Empty;
    public string CallbackUrl { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string SourceSystemClaim { get; set; } = "source_system";
    public string SourceSystemId { get; set; } = string.Empty;
    public string SourceUrlClaim { get; set; } = "source_url";
    public string ExchangeTokenClaim { get; set; } = "exchange_token";
    public string UsernameClaim { get; set; } = "preferred_username";
    public string EmailClaim { get; set; } = "email";
    public int StateTtlMinutes { get; set; } = 5;
    public int ReplayTtlMinutes { get; set; } = 15;
    public int FinalizePollIntervalMs { get; set; } = 1500;
    public string[] AllowedIpRanges { get; set; } = Array.Empty<string>();
    public string[] AllowedSourceUrls { get; set; } = Array.Empty<string>();
    public PortalSsoCryptoOptions Crypto { get; set; } = new();
    public PortalSsoExchangeApiOptions ExchangeApi { get; set; } = new();
}

public class PortalSsoCryptoOptions
{
    public string DecryptionPrivateKeyPath { get; set; } = string.Empty;
    public string DecryptionPrivateKeyPem { get; set; } = string.Empty;
    public string SigningPublicKeyPath { get; set; } = string.Empty;
    public string SigningPublicKeyPem { get; set; } = string.Empty;
    public string RequiredOuterAlg { get; set; } = "RSA-OAEP-256";
    public string RequiredEnc { get; set; } = "A256GCM";
    public string RequiredInnerAlg { get; set; } = "PS256";
}

public class PortalSsoExchangeApiOptions
{
    public string BaseUrl { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string SubscriptionHeaderName { get; set; } = "x-sso-api-key";
    public string SubscriptionKey { get; set; } = string.Empty;
    public string SourceHeaderName { get; set; } = "X-Source-System";
}

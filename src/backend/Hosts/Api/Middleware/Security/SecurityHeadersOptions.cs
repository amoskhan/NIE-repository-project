namespace Api.Middleware;

/// <summary>
/// Configurable security-response-header values. Bind from appsettings.json:SecurityHeaders.
/// Maintainers should review CSP per project before promoting to production — defaults are
/// intentionally strict and may need relaxing for third-party origins (Sentry CDN, OneSignal).
/// </summary>
public sealed class SecurityHeadersOptions
{
    public const string SectionName = "SecurityHeaders";

    public string ContentSecurityPolicy { get; set; } =
        "default-src 'self'; "
        + "script-src 'self'; "
        + "style-src 'self' 'unsafe-inline'; "
        + "img-src 'self' data:; "
        + "connect-src 'self'; "
        + "frame-ancestors 'none'; "
        + "base-uri 'self'; "
        + "form-action 'self'";

    public string StrictTransportSecurity { get; set; } =
        "max-age=31536000; includeSubDomains; preload";

    public string XFrameOptions { get; set; } = "DENY";

    public string XContentTypeOptions { get; set; } = "nosniff";

    public string ReferrerPolicy { get; set; } = "strict-origin-when-cross-origin";

    public string PermissionsPolicy { get; set; } =
        "accelerometer=(), camera=(), geolocation=(), microphone=(), payment=()";

    /// <summary>
    /// Allows the generated OpenAPI document to bypass CSP if a project-specific
    /// documentation viewer needs looser policies in non-production.
    /// </summary>
    public bool EnableForOpenApi { get; set; } = false;
}

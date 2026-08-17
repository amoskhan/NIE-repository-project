namespace Shared.Helpers;

/// <summary>
/// Validates outbound HTTP URLs against a configured allowlist before any request is sent.
/// Closes OWASP W-A10 / API7 (SSRF): any URL that comes from configuration, a database row,
/// or user input could otherwise be repointed at an internal address
/// (<c>http://169.254.169.254/</c>, <c>http://localhost:5432</c>) and turn the server into a proxy.
///
/// <para>
/// Route every outbound integration through this guard — a customer webhook, an OIDC issuer,
/// a push-notification endpoint, a file-fetch URL.
/// </para>
///
/// Usage:
/// <code>
/// // Allowlist comes from your own settings; there is no built-in default,
/// // so an unconfigured integration is refused rather than allowed.
/// var uri = SsrfGuard.Validate(webhook.Url, _settings.AllowedWebhookHosts, "Outbound webhook");
/// var response = await _httpClient.PostAsync(uri, content);
/// </code>
/// </summary>
public static class SsrfGuard
{
    /// <summary>
    /// Validates that <paramref name="url"/> parses as an absolute HTTPS URL and that its
    /// host matches one of <paramref name="allowedHosts"/>. Throws <see cref="InvalidOperationException"/>
    /// on any mismatch — never silently rewrites or downgrades.
    /// </summary>
    /// <param name="allowedHosts">Each entry is either an exact host (<c>hooks.example.com</c>)
    /// or a wildcard subdomain (<c>*.example.com</c>). Case-insensitive. The allowlist is empty
    /// by default: an empty or missing list denies everything, which is the safe failure mode.</param>
    /// <param name="contextLabel">A short label included in error messages so operators know
    /// which integration tripped the guard (e.g. "Outbound webhook").</param>
    public static Uri Validate(string? url, IReadOnlyCollection<string>? allowedHosts, string contextLabel)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new InvalidOperationException($"{contextLabel}: URL is empty.");

        if (allowedHosts == null || allowedHosts.Count == 0)
            throw new InvalidOperationException(
                $"{contextLabel}: no allowed hosts configured. Refusing to call '{url}'.");

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            throw new InvalidOperationException($"{contextLabel}: '{url}' is not an absolute URL.");

        if (uri.Scheme != Uri.UriSchemeHttps)
            throw new InvalidOperationException($"{contextLabel}: '{url}' must use HTTPS.");

        var host = uri.Host;
        if (!allowedHosts.Any(allowed => HostMatches(host, allowed)))
            throw new InvalidOperationException(
                $"{contextLabel}: host '{host}' is not in the allowlist [{string.Join(", ", allowedHosts)}].");

        return uri;
    }

    private static bool HostMatches(string host, string pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern))
            return false;

        if (pattern.StartsWith("*.", StringComparison.Ordinal))
        {
            // Wildcard: must end with the suffix AND have at least one extra label
            var suffix = pattern[1..]; // includes the leading dot
            return host.Length > suffix.Length
                   && host.EndsWith(suffix, StringComparison.OrdinalIgnoreCase);
        }

        return string.Equals(host, pattern, StringComparison.OrdinalIgnoreCase);
    }
}

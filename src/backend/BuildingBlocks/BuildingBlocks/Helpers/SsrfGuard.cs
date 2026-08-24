namespace BuildingBlocks.Helpers;

/// <summary>
/// Validates outbound HTTP URLs against a configured allowlist before any request is sent.
/// Closes OWASP W-A10 / API7 (SSRF) for integrations like MyInfo and Portal SSO whose
/// base URLs come from configuration and could be repointed at attacker-controlled hosts.
///
/// Usage:
/// <code>
/// var uri = SsrfGuard.Validate(_settings.BaseUrl, _settings.AllowedHosts, "MyInfo Person API");
/// var response = await _httpClient.GetAsync(uri);
/// </code>
/// </summary>
public static class SsrfGuard
{
    /// <summary>
    /// Validates that <paramref name="url"/> parses as an absolute HTTPS URL and that its
    /// host matches one of <paramref name="allowedHosts"/>. Throws <see cref="InvalidOperationException"/>
    /// on any mismatch — never silently rewrites or downgrades.
    /// </summary>
    /// <param name="allowedHosts">Each entry is either an exact host (<c>api.myinfo.gov.sg</c>)
    /// or a wildcard subdomain (<c>*.gov.sg</c>). Case-insensitive.</param>
    /// <param name="contextLabel">A short label included in error messages so operators know
    /// which integration tripped the guard (e.g. "MyInfo Token Endpoint").</param>
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

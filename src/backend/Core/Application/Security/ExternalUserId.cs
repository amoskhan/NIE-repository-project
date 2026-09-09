namespace Application.Security;

/// <summary>
/// Normalizes identifiers supplied by the institutional identity provider.
/// These identifiers are case-insensitive even when the persistence provider is not.
/// </summary>
public static class ExternalUserId
{
    public static string Normalize(string? userId) =>
        (userId ?? string.Empty).Trim().ToLowerInvariant();
}

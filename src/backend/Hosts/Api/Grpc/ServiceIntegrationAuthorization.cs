using System.Security.Claims;

namespace Api.Grpc;

public static class ServiceIntegrationAuthorization
{
    public static bool HasRequiredScope(
        ClaimsPrincipal principal,
        string requiredScope)
    {
        ArgumentNullException.ThrowIfNull(principal);
        if (string.IsNullOrWhiteSpace(requiredScope))
        {
            return false;
        }

        return principal
            .FindAll(claim => claim.Type is "scope" or "scp")
            .SelectMany(claim => claim.Value.Split(
                ' ',
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Contains(requiredScope, StringComparer.Ordinal);
    }
}

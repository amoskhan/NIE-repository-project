namespace API.Sessions;

/// <summary>
/// Kills live sessions from the Main API.
/// <para>
/// Sessions are minted by the Auth API and stored in Valkey under <c>session:{token}</c>. Both
/// APIs point at the same Valkey instance with the same <c>Valkey:InstanceName</c> prefix, which
/// is what lets the Main API delete a session it did not create.
/// </para>
/// </summary>
public interface ISessionRevocationService
{
    /// <summary>
    /// Deletes every stored session belonging to a login name, so the next request carrying one of
    /// those tokens is rejected by <c>SessionValidationMiddleware</c>.
    /// </summary>
    /// <returns>
    /// How many sessions were deleted, or null when Valkey could not be reached. Null means
    /// "unknown, sessions may still be live" and callers should surface that rather than
    /// reporting a clean revocation.
    /// </returns>
    Task<int?> RevokeUserSessionsAsync(string userId, CancellationToken cancellationToken = default);
}

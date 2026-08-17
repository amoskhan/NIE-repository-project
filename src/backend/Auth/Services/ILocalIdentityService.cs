using Auth.Models;

namespace Auth.Services;

/// <summary>
/// The application's own identity provider: it owns credentials for the
/// <c>UserAccounts</c> table and nothing else.
/// <para>
/// It does NOT resolve roles or permissions - those live in the Main API and are fetched by the
/// frontend after sign-in. It also does not create sessions; the controller hands the returned
/// <see cref="LoginResponse"/> to <see cref="IAuthSessionService"/> for that.
/// </para>
/// </summary>
public interface ILocalIdentityService
{
    /// <summary>
    /// Checks a login name and password against the local account store.
    /// </summary>
    /// <remarks>
    /// Enforces active/lockout state, counts failed attempts, stamps the last successful sign-in,
    /// and transparently upgrades the stored hash when the hashing parameters have moved on.
    /// The returned <see cref="LoginResponse.errorMessage"/> is deliberately generic on failure so
    /// callers cannot learn whether a given login name exists.
    /// </remarks>
    Task<LoginResponse> VerifyCredentialsAsync(string userId, string password, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new local account. Only permitted when <c>LocalIdentity:AllowSelfRegistration</c> is true.
    /// </summary>
    /// <returns><c>ok</c> is false with a human-readable <c>error</c> when registration is refused.</returns>
    Task<(bool ok, string? error)> RegisterAsync(
        string userId,
        string? name,
        string? email,
        string? department,
        string password,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Issues a single-use password-reset token for the account matching a login name or email.
    /// Only a SHA-256 hash of the token is stored; the raw value returned here is the only copy.
    /// </summary>
    /// <returns>
    /// The raw token, or null when no matching account exists. Callers MUST NOT expose the
    /// difference - the HTTP endpoint always answers 200.
    /// </returns>
    Task<string?> CreatePasswordResetTokenAsync(string userIdOrEmail, CancellationToken cancellationToken = default);

    /// <summary>
    /// Consumes a reset token and sets a new password.
    /// </summary>
    /// <remarks>
    /// The token identifies the account on its own - the caller does NOT name a user. The stored
    /// hash must match in constant time, must not have expired, and is cleared afterwards so the
    /// same link cannot be replayed.
    /// </remarks>
    Task<(bool ok, string? error)> ResetPasswordAsync(
        string rawToken,
        string newPassword,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Changes the password of a signed-in user after re-checking their current password.
    /// </summary>
    Task<(bool ok, string? error)> ChangePasswordAsync(
        string userId,
        string currentPassword,
        string newPassword,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves the local account behind an external (OIDC) sign-in, creating one on first use
    /// when the provider allows auto-provisioning. Used only by the optional ExternalIdp slot.
    /// </summary>
    Task<LoginResponse> ResolveExternalUserAsync(
        string provider,
        string subject,
        string? name,
        string? email,
        bool allowAutoProvision,
        CancellationToken cancellationToken = default);
}

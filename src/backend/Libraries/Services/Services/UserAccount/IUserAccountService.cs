using Shared.Dto;

namespace Services.Services;

/// <summary>
/// Administrative lifecycle for local accounts in the <c>UserAccounts</c> table: create one,
/// activate it, deactivate it.
/// <para>
/// The Auth API owns SIGN-IN against the same table (see <c>ILocalIdentityService</c>); this
/// service is the administrator-facing half and never verifies a password. Both write hashes with
/// the same <c>PasswordHasher&lt;UserAccount&gt;</c>, which is what keeps an account created here
/// signable-in over there.
/// </para>
/// </summary>
public interface IUserAccountService
{
    /// <summary>
    /// Returns every local account, ordered by login name. Credentials are not projected.
    /// </summary>
    Task<List<UserAccountDto>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns one account by primary key, or null when there is no such account.
    /// </summary>
    Task<UserAccountDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates an account with an administrator-issued password, flagged
    /// <c>MustChangePassword</c>. The account starts active.
    /// </summary>
    /// <returns>
    /// <c>Account</c> is null and <c>Error</c> carries a human-readable reason when the
    /// login name is taken or the password does not meet the policy.
    /// </returns>
    Task<(bool Ok, string? Error, UserAccountDto? Account)> RegisterAsync(
        RegisterUserAccountDto dto,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets whether an account may sign in. Returns null when there is no such account.
    /// </summary>
    /// <remarks>
    /// Deactivating stops FUTURE sign-ins. Sessions already in Valkey stay valid until they are
    /// revoked separately - the caller is responsible for that.
    /// </remarks>
    Task<UserAccountDto?> SetActiveAsync(int id, bool isActive, CancellationToken cancellationToken = default);
}

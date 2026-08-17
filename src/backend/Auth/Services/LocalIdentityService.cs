using System.Security.Cryptography;
using System.Text;
using Auth.Models;
using Data.Data;
using Domain.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Shared.Helpers;

namespace Auth.Services;

/// <summary>
/// The built-in identity provider. Credentials live in the <c>UserAccounts</c> table and are
/// hashed with ASP.NET Core's <see cref="PasswordHasher{TUser}"/> (PBKDF2-HMAC-SHA512,
/// per-password salt, iteration count embedded in the hash).
/// <para>
/// Replacing this with a real corporate IdP later means swapping this one class out - the HTTP
/// surface in <c>AuthController</c> and the Valkey session format stay exactly the same.
/// </para>
/// </summary>
public class LocalIdentityService : ILocalIdentityService
{
    /// <summary>
    /// Shown for every failed sign-in. Intentionally identical for "no such user", "wrong
    /// password" and "inactive account" so the endpoint cannot be used to enumerate accounts.
    /// </summary>
    private const string GenericLoginFailure = "Invalid user ID or password.";

    /// <summary>
    /// Shown for every rejected password reset. Identical for "no such token", "expired",
    /// "already used" and "inactive account" so a caller learns nothing from the wording.
    /// </summary>
    private const string InvalidResetToken = "The reset link is invalid or has expired.";

    private readonly MainDbContext _db;
    private readonly IPasswordHasher<UserAccount> _passwordHasher;
    private readonly LocalIdentityOptions _options;
    private readonly ILogger<LocalIdentityService> _logger;

    public LocalIdentityService(
        MainDbContext db,
        IPasswordHasher<UserAccount> passwordHasher,
        IOptions<LocalIdentityOptions> options,
        ILogger<LocalIdentityService> logger)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<LoginResponse> VerifyCredentialsAsync(
        string userId,
        string password,
        CancellationToken cancellationToken = default)
    {
        userId = userId?.Trim() ?? string.Empty;

        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(password))
            return Failure();

        var account = await _db.UserAccounts
            .FirstOrDefaultAsync(a => a.UserId == userId, cancellationToken);

        if (account is null)
        {
            // Hash a throwaway password so that "unknown user" costs roughly the same amount of
            // time as "wrong password". Without this, response timing leaks which names exist.
            _ = _passwordHasher.HashPassword(new UserAccount { UserId = userId }, password);
            return Failure();
        }

        if (!account.IsActive)
        {
            _logger.LogWarning("Sign-in refused for inactive account {UserId}.", account.UserId);
            return Failure();
        }

        var now = DateTimeHelper.Now;

        if (account.LockoutEndOn.HasValue && account.LockoutEndOn.Value > now)
        {
            // Trade-off: saying "locked" instead of the generic message tells a caller that this
            // account exists, but only after they have already made MaxFailedLoginAttempts guesses
            // against that exact name. Most frameworks make the same call, because silently
            // refusing a correct password is very confusing for real users. If you would rather
            // give nothing away, return Failure() here instead and rely on the log line.
            _logger.LogWarning(
                "Sign-in refused for locked-out account {UserId} until {LockoutEndOn}.",
                account.UserId,
                account.LockoutEndOn);
            return Failure("Account is temporarily locked. Please try again later.");
        }

        if (string.IsNullOrEmpty(account.PasswordHash))
        {
            // External-only account: it has no local password to check against.
            _logger.LogWarning("Sign-in refused for external-only account {UserId}.", account.UserId);
            return Failure();
        }

        var verification = _passwordHasher.VerifyHashedPassword(account, account.PasswordHash, password);

        if (verification == PasswordVerificationResult.Failed)
        {
            await RecordFailedAttemptAsync(account, now, cancellationToken);
            return Failure();
        }

        if (verification == PasswordVerificationResult.SuccessRehashNeeded)
        {
            // The password is correct but was hashed with older parameters - upgrade it in place
            // while we have the plaintext in hand.
            account.PasswordHash = _passwordHasher.HashPassword(account, password);
            _logger.LogInformation("Upgraded stored password hash for {UserId}.", account.UserId);
        }

        account.FailedLoginCount = 0;
        account.LockoutEndOn = null;
        account.LastLoginOn = now;
        await _db.SaveChangesAsync(cancellationToken);

        if (account.MustChangePassword)
        {
            // The account is flagged for a forced password change. The template still issues a
            // session; call POST /api/Auth/ChangePassword to clear the flag. Projects that need a
            // hard block can refuse the sign-in here instead.
            _logger.LogInformation("Account {UserId} is flagged MustChangePassword.", account.UserId);
        }

        return ToLoginResponse(account);
    }

    /// <inheritdoc />
    public async Task<(bool ok, string? error)> RegisterAsync(
        string userId,
        string? name,
        string? email,
        string? department,
        string password,
        CancellationToken cancellationToken = default)
    {
        if (!_options.AllowSelfRegistration)
            return (false, "Self-registration is disabled.");

        userId = userId?.Trim() ?? string.Empty;

        if (string.IsNullOrEmpty(userId))
            return (false, "A user ID is required.");

        if (userId.Length > 100)
            return (false, "User ID must be 100 characters or fewer.");

        var passwordError = ValidatePassword(password);
        if (passwordError is not null)
            return (false, passwordError);

        var taken = await _db.UserAccounts.AnyAsync(a => a.UserId == userId, cancellationToken);
        if (taken)
            return (false, "That user ID is already taken.");

        var account = new UserAccount
        {
            UserId = userId,
            Name = string.IsNullOrWhiteSpace(name) ? userId : name.Trim(),
            Email = email?.Trim(),
            Department = department?.Trim(),
            IsActive = true
        };
        account.PasswordHash = _passwordHasher.HashPassword(account, password);

        _db.UserAccounts.Add(account);
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Registered new local account {UserId}.", account.UserId);
        return (true, null);
    }

    /// <inheritdoc />
    public async Task<string?> CreatePasswordResetTokenAsync(
        string userIdOrEmail,
        CancellationToken cancellationToken = default)
    {
        userIdOrEmail = userIdOrEmail?.Trim() ?? string.Empty;

        if (string.IsNullOrEmpty(userIdOrEmail))
            return null;

        var account = await _db.UserAccounts.FirstOrDefaultAsync(
            a => a.UserId == userIdOrEmail || a.Email == userIdOrEmail,
            cancellationToken);

        // Returning null (rather than an error) keeps account existence secret; the controller
        // answers 200 either way.
        if (account is null || !account.IsActive)
            return null;

        // 256 bits of randomness, URL-safe. This raw value is the only copy - we store its hash.
        var rawToken = Base64UrlEncode(RandomNumberGenerator.GetBytes(32));

        account.PasswordResetTokenHash = HashToken(rawToken);
        account.PasswordResetExpiresOn = DateTimeHelper.Now.AddMinutes(_options.PasswordResetTokenTtlMinutes);
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Issued a password-reset token for {UserId}.", account.UserId);
        return rawToken;
    }

    /// <inheritdoc />
    public async Task<(bool ok, string? error)> ResetPasswordAsync(
        string rawToken,
        string newPassword,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(rawToken))
            return (false, InvalidResetToken);

        var passwordError = ValidatePassword(newPassword);
        if (passwordError is not null)
            return (false, passwordError);

        // The token IS the identity here: we look the account up by the hash we stored when the
        // token was issued. Nothing the caller sends names an account, so a reset cannot be aimed
        // at someone else's login by guessing their user ID.
        var candidateHash = HashToken(rawToken);

        var account = await _db.UserAccounts.FirstOrDefaultAsync(
            a => a.PasswordResetTokenHash == candidateHash,
            cancellationToken);

        if (account is null
            || !account.IsActive
            || string.IsNullOrEmpty(account.PasswordResetTokenHash)
            || !account.PasswordResetExpiresOn.HasValue
            || account.PasswordResetExpiresOn.Value <= DateTimeHelper.Now)
        {
            return (false, InvalidResetToken);
        }

        // The lookup above already matched exactly, so this cannot fail today. It stays because it
        // is the check that must hold: if the query is ever loosened, the constant-time comparison
        // is what keeps the token from being brute-forced one character at a time.
        if (!FixedTimeTokenEquals(account.PasswordResetTokenHash, candidateHash))
            return (false, InvalidResetToken);

        account.PasswordHash = _passwordHasher.HashPassword(account, newPassword);
        // Single use: clear the token so the same link cannot be replayed.
        account.PasswordResetTokenHash = null;
        account.PasswordResetExpiresOn = null;
        account.MustChangePassword = false;
        account.FailedLoginCount = 0;
        account.LockoutEndOn = null;
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Password reset completed for {UserId}.", account.UserId);
        return (true, null);
    }

    /// <inheritdoc />
    public async Task<(bool ok, string? error)> ChangePasswordAsync(
        string userId,
        string currentPassword,
        string newPassword,
        CancellationToken cancellationToken = default)
    {
        userId = userId?.Trim() ?? string.Empty;

        if (string.IsNullOrEmpty(userId))
            return (false, GenericLoginFailure);

        var passwordError = ValidatePassword(newPassword);
        if (passwordError is not null)
            return (false, passwordError);

        var account = await _db.UserAccounts.FirstOrDefaultAsync(a => a.UserId == userId, cancellationToken);

        if (account is null || !account.IsActive || string.IsNullOrEmpty(account.PasswordHash))
            return (false, GenericLoginFailure);

        var verification = _passwordHasher.VerifyHashedPassword(account, account.PasswordHash, currentPassword ?? string.Empty);
        if (verification == PasswordVerificationResult.Failed)
            return (false, "The current password is incorrect.");

        account.PasswordHash = _passwordHasher.HashPassword(account, newPassword);
        account.MustChangePassword = false;
        account.PasswordResetTokenHash = null;
        account.PasswordResetExpiresOn = null;
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Password changed for {UserId}.", account.UserId);
        return (true, null);
    }

    /// <inheritdoc />
    public async Task<LoginResponse> ResolveExternalUserAsync(
        string provider,
        string subject,
        string? name,
        string? email,
        bool allowAutoProvision,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(provider) || string.IsNullOrWhiteSpace(subject))
            return Failure("The external provider did not return a usable identity.");

        provider = provider.Trim();
        subject = subject.Trim();
        email = string.IsNullOrWhiteSpace(email) ? null : email.Trim();

        // 1. Already linked to this provider?
        var account = await _db.UserAccounts.FirstOrDefaultAsync(
            a => a.ExternalProvider == provider && a.ExternalSubject == subject,
            cancellationToken);

        // 2. Otherwise match an existing local account by email and link it on first use.
        if (account is null && email is not null)
        {
            account = await _db.UserAccounts.FirstOrDefaultAsync(a => a.Email == email, cancellationToken);

            if (account is not null)
            {
                account.ExternalProvider = provider;
                account.ExternalSubject = subject;
            }
        }

        // 3. Otherwise create an external-only account (no local password).
        if (account is null)
        {
            if (!allowAutoProvision)
                return Failure("No account exists for this external identity.");

            account = new UserAccount
            {
                UserId = await BuildUniqueExternalUserIdAsync(provider, subject, email, cancellationToken),
                Name = string.IsNullOrWhiteSpace(name) ? email ?? subject : name.Trim(),
                Email = email,
                PasswordHash = null,
                IsActive = true,
                ExternalProvider = provider,
                ExternalSubject = subject
            };

            _db.UserAccounts.Add(account);
            _logger.LogInformation(
                "Auto-provisioned account {UserId} from external provider {Provider}.",
                account.UserId,
                provider);
        }

        if (!account.IsActive)
            return Failure("This account has been deactivated.");

        if (!string.IsNullOrWhiteSpace(name))
            account.Name = name.Trim();

        account.LastLoginOn = DateTimeHelper.Now;
        await _db.SaveChangesAsync(cancellationToken);

        return ToLoginResponse(account);
    }

    /// <summary>
    /// Counts a bad password and applies the lockout once the configured threshold is reached.
    /// </summary>
    private async Task RecordFailedAttemptAsync(UserAccount account, DateTime now, CancellationToken cancellationToken)
    {
        account.FailedLoginCount++;

        if (_options.MaxFailedLoginAttempts > 0 && account.FailedLoginCount >= _options.MaxFailedLoginAttempts)
        {
            account.LockoutEndOn = now.AddMinutes(_options.LockoutMinutes);
            // Reset the counter so the user gets a full set of attempts once the lockout expires.
            account.FailedLoginCount = 0;

            _logger.LogWarning(
                "Account {UserId} locked out until {LockoutEndOn} after {Attempts} failed attempts.",
                account.UserId,
                account.LockoutEndOn,
                _options.MaxFailedLoginAttempts);
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Applies the configured password policy. Returns null when the password is acceptable.
    /// </summary>
    private string? ValidatePassword(string? password)
    {
        if (string.IsNullOrEmpty(password))
            return "A password is required.";

        if (password.Length < _options.MinPasswordLength)
            return $"Password must be at least {_options.MinPasswordLength} characters long.";

        return null;
    }

    /// <summary>
    /// Projects a stored account onto the login shape the session service already understands.
    /// </summary>
    private static LoginResponse ToLoginResponse(UserAccount account) => new()
    {
        isAuthenticated = true,
        userId = account.UserId,
        userName = account.UserId,
        fullName = account.Name ?? account.UserId,
        email = account.Email ?? string.Empty,
        department = account.Department ?? string.Empty,
        // Left null on purpose: AuthSessionService mints the session token.
        sessionToken = null
    };

    private static LoginResponse Failure(string message = GenericLoginFailure) => new()
    {
        isAuthenticated = false,
        errorMessage = message
    };

    /// <summary>
    /// SHA-256 of the raw reset token, hex encoded. Reset tokens are high-entropy random values,
    /// so a fast hash is appropriate here (unlike passwords, which need a slow KDF).
    /// </summary>
    private static string HashToken(string rawToken) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));

    /// <summary>
    /// Compares two token hashes without leaking how many characters matched.
    /// </summary>
    private static bool FixedTimeTokenEquals(string storedHash, string candidateHash) =>
        CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(storedHash),
            Encoding.UTF8.GetBytes(candidateHash));

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    /// <summary>
    /// Builds a readable login name for an auto-provisioned external account, making sure it does
    /// not collide with an account that already exists (UserId has a unique index).
    /// </summary>
    private async Task<string> BuildUniqueExternalUserIdAsync(
        string provider,
        string subject,
        string? email,
        CancellationToken cancellationToken)
    {
        var candidate = Truncate(
            !string.IsNullOrWhiteSpace(email) ? email : $"{provider.ToLowerInvariant()}:{subject}",
            100);

        if (!await _db.UserAccounts.AnyAsync(a => a.UserId == candidate, cancellationToken))
            return candidate;

        // Fall back to a provider-scoped id, which is unique by construction for this provider.
        var scoped = Truncate($"{provider.ToLowerInvariant()}:{subject}", 100);

        if (!await _db.UserAccounts.AnyAsync(a => a.UserId == scoped, cancellationToken))
            return scoped;

        return Truncate($"{provider.ToLowerInvariant()}:{Guid.NewGuid():N}", 100);
    }

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength];
}

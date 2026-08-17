using System.ComponentModel.DataAnnotations;

namespace Domain.Models;

/// <summary>
/// A local login account owned by this application's identity provider.
/// <para>
/// The Auth API authenticates against this table; roles and permissions are NOT stored here —
/// they live in <see cref="UserRole"/> / <see cref="Role"/> and are resolved by the Main API.
/// The link between the two is <see cref="UserId"/>, which must match <see cref="UserRole.UserId"/>.
/// </para>
/// <para>
/// An account may be local (has a <see cref="PasswordHash"/>), external-only
/// (<see cref="PasswordHash"/> is null and <see cref="ExternalProvider"/> is set), or both.
/// </para>
/// </summary>
public class UserAccount : TimestampedEntity
{
    /// <summary>
    /// The login name. Unique across the application and used as the identity key
    /// everywhere else in the system (audit logs, <see cref="UserRole.UserId"/>, sessions).
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string UserId { get; set; } = default!;

    /// <summary>
    /// The user's display name, shown in the UI and stored in the session.
    /// </summary>
    [MaxLength(200)]
    public string? Name { get; set; }

    /// <summary>
    /// The user's email address. Used for password-reset lookups and notifications.
    /// </summary>
    [MaxLength(256)]
    public string? Email { get; set; }

    /// <summary>
    /// The user's department or organisational unit.
    /// </summary>
    [MaxLength(200)]
    public string? Department { get; set; }

    /// <summary>
    /// The hashed password produced by <c>PasswordHasher&lt;UserAccount&gt;</c>.
    /// Null means the account has no local password and can only sign in through an external provider.
    /// Never contains a plaintext password.
    /// </summary>
    [MaxLength(500)]
    public string? PasswordHash { get; set; }

    /// <summary>
    /// Whether the account may sign in. Inactive accounts are rejected before the password is checked.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Whether the user must change their password on the next successful sign-in
    /// (for example after an administrator issued a temporary password).
    /// </summary>
    public bool MustChangePassword { get; set; } = false;

    /// <summary>
    /// Number of consecutive failed sign-in attempts. Reset to zero on a successful sign-in.
    /// </summary>
    public int FailedLoginCount { get; set; }

    /// <summary>
    /// When the current lockout expires. Null (or a past value) means the account is not locked out.
    /// </summary>
    public DateTime? LockoutEndOn { get; set; }

    /// <summary>
    /// When the user last signed in successfully.
    /// </summary>
    public DateTime? LastLoginOn { get; set; }

    /// <summary>
    /// The external identity provider key this account is linked to (for example "Google"),
    /// or null when the account is local-only.
    /// </summary>
    [MaxLength(50)]
    public string? ExternalProvider { get; set; }

    /// <summary>
    /// The stable subject identifier issued by <see cref="ExternalProvider"/> (the OIDC <c>sub</c> claim),
    /// or null when the account is local-only.
    /// </summary>
    [MaxLength(200)]
    public string? ExternalSubject { get; set; }

    /// <summary>
    /// SHA-256 hash of the outstanding password-reset token.
    /// The raw token is only ever returned to the requester — never stored.
    /// </summary>
    [MaxLength(200)]
    public string? PasswordResetTokenHash { get; set; }

    /// <summary>
    /// When the outstanding password-reset token stops being accepted.
    /// </summary>
    public DateTime? PasswordResetExpiresOn { get; set; }
}

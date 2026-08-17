namespace Auth.Models;

/// <summary>
/// Policy for the built-in (local) identity provider, bound from the <c>LocalIdentity</c>
/// configuration section. These are deliberately conservative defaults - tune them for your project.
/// </summary>
public class LocalIdentityOptions
{
    /// <summary>
    /// The configuration section this class binds to.
    /// </summary>
    public const string SectionName = "LocalIdentity";

    /// <summary>
    /// Smallest password accepted by Register / ResetPassword / ChangePassword.
    /// Length is the single most useful password rule; prefer raising this over adding
    /// character-class requirements.
    /// </summary>
    public int MinPasswordLength { get; set; } = 12;

    /// <summary>
    /// Consecutive failed sign-ins before the account is temporarily locked.
    /// Set to 0 to disable lockout entirely (not recommended).
    /// </summary>
    public int MaxFailedLoginAttempts { get; set; } = 5;

    /// <summary>
    /// How long an account stays locked once <see cref="MaxFailedLoginAttempts"/> is reached.
    /// </summary>
    public int LockoutMinutes { get; set; } = 15;

    /// <summary>
    /// How long a password-reset token remains valid after it is issued.
    /// </summary>
    public int PasswordResetTokenTtlMinutes { get; set; } = 30;

    /// <summary>
    /// Whether anonymous visitors may create their own account through
    /// <c>POST /api/Auth/Register</c>. Turn this off for internal applications.
    /// </summary>
    public bool AllowSelfRegistration { get; set; } = true;
}

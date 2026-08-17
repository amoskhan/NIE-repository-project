using System.ComponentModel.DataAnnotations;

namespace Auth.Models;

/// <summary>
/// Body of <c>POST /api/Auth/Register</c>.
/// Only accepted when <c>LocalIdentity:AllowSelfRegistration</c> is true.
/// </summary>
public class RegisterRequest
{
    /// <summary>The desired login name. Must be unique.</summary>
    [Required]
    [MaxLength(100)]
    public string UserId { get; set; } = default!;

    /// <summary>The display name shown in the UI.</summary>
    [MaxLength(200)]
    public string? FullName { get; set; }

    /// <summary>The user's email address, used for password resets.</summary>
    [EmailAddress]
    [MaxLength(256)]
    public string? Email { get; set; }

    /// <summary>The user's department or organisational unit.</summary>
    [MaxLength(200)]
    public string? Department { get; set; }

    /// <summary>The chosen password. Must satisfy <c>LocalIdentity:MinPasswordLength</c>.</summary>
    [Required]
    public string Password { get; set; } = default!;
}

/// <summary>
/// Body of <c>POST /api/Auth/ForgotPassword</c>.
/// The endpoint always answers 200 so it cannot be used to discover which accounts exist.
/// </summary>
public class ForgotPasswordRequest
{
    /// <summary>Either the login name or the email address of the account.</summary>
    [Required]
    public string UserIdOrEmail { get; set; } = default!;
}

/// <summary>
/// Body of <c>POST /api/Auth/ResetPassword</c>.
/// <para>
/// There is deliberately NO user ID here. The token is self-identifying: the account is
/// resolved from the stored hash of the token alone. Accepting a caller-supplied login name
/// would add an attacker-controlled input to the reset path for no benefit.
/// </para>
/// </summary>
public class ResetPasswordRequest
{
    /// <summary>The raw single-use token issued by ForgotPassword.</summary>
    [Required]
    public string Token { get; set; } = default!;

    /// <summary>The new password. Must satisfy <c>LocalIdentity:MinPasswordLength</c>.</summary>
    [Required]
    public string NewPassword { get; set; } = default!;
}

/// <summary>
/// Body of <c>POST /api/Auth/ChangePassword</c>. Requires a valid session
/// (<c>X-Session-Id</c> header) as well as the current password.
/// </summary>
public class ChangePasswordRequest
{
    /// <summary>The password currently on the account.</summary>
    [Required]
    public string CurrentPassword { get; set; } = default!;

    /// <summary>The replacement password. Must satisfy <c>LocalIdentity:MinPasswordLength</c>.</summary>
    [Required]
    public string NewPassword { get; set; } = default!;
}

/// <summary>
/// One entry of <c>GET /api/Auth/ExternalProviders</c>.
/// </summary>
public class ExternalProviderSummary
{
    /// <summary>The provider key to pass to <c>GET /api/Auth/ExternalStart?provider=</c>.</summary>
    public string Name { get; set; } = default!;

    /// <summary>Human-readable label for the sign-in button.</summary>
    public string DisplayName { get; set; } = default!;

    /// <summary>Relative URL the browser should be sent to in order to begin the flow.</summary>
    public string StartUrl { get; set; } = default!;
}

namespace Shared.Dto;

/// <summary>
/// A local account row as shown on the administration Users screen.
/// <para>
/// Credentials are never projected here - no password hash, no reset token.
/// </para>
/// </summary>
public class UserAccountDto
{
    /// <summary>Primary key of the account. This is the <c>id</c> that
    /// <c>ApproveUser</c> and <c>DeactivateUser</c> take.</summary>
    public int Id { get; set; }

    /// <summary>The login name. Also the identity key used by roles, sessions, and audit logs.</summary>
    public string UserId { get; set; } = default!;

    /// <summary>The display name shown in the UI.</summary>
    public string? FullName { get; set; }

    public string? Email { get; set; }

    public string? Department { get; set; }

    /// <summary>Whether the account may sign in. <c>ApproveUser</c> sets this true,
    /// <c>DeactivateUser</c> sets it false.</summary>
    public bool IsActive { get; set; }

    /// <summary>Set when an administrator issued the password, cleared once the user changes it.</summary>
    public bool MustChangePassword { get; set; }

    /// <summary>When the account is locked out until, or null when it is not locked.</summary>
    public DateTime? LockoutEndOn { get; set; }

    public DateTime? LastLoginOn { get; set; }
}

/// <summary>
/// Body of <c>POST /api/AccessControl/RegisterUser</c> - an administrator creating a local
/// account on someone else's behalf.
/// <para>
/// This is NOT self-registration: it is unaffected by <c>LocalIdentity:AllowSelfRegistration</c>
/// and is gated on the <c>api.access-control.users.manage</c> access function instead.
/// </para>
/// </summary>
public class RegisterUserAccountDto
{
    /// <summary>The login name to create. Must not already exist.</summary>
    public required string UserId { get; set; }

    /// <summary>The display name shown in the UI. Defaults to the login name when omitted.</summary>
    public string? FullName { get; set; }

    public string? Email { get; set; }

    public string? Department { get; set; }

    /// <summary>
    /// The password the user signs in with the first time. The account is created with
    /// <see cref="UserAccountDto.MustChangePassword"/> set, so this is a handover value only.
    /// </summary>
    public required string InitialPassword { get; set; }
}

/// <summary>
/// Response of <c>POST /api/AccessControl/DeactivateUser</c>.
/// <para>
/// Deactivating is two writes - the flag in PostgreSQL and the sessions in Valkey - and the second
/// one can fail on its own. This DTO reports that honestly instead of returning a bare account and
/// letting the caller assume the user is really locked out.
/// </para>
/// </summary>
public class DeactivateUserResultDto
{
    /// <summary>The account as it now stands.</summary>
    public UserAccountDto Account { get; set; } = default!;

    /// <summary>
    /// How many live sessions were deleted, or null when the session store could not be reached.
    /// Null means the user may still be signed in somewhere.
    /// </summary>
    public int? SessionsRevoked { get; set; }

    /// <summary>Set only when <see cref="SessionsRevoked"/> is null; safe to show to an administrator.</summary>
    public string? Warning { get; set; }
}

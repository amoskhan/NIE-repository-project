namespace Auth.Models;

/// <summary>
/// The short-lived, server-side half of an in-flight external sign-in.
/// Stored in Valkey under <c>extidp:state:{state}</c> and deleted the moment it is used, so a
/// callback can only ever be redeemed once (this is what stops CSRF and replay on the callback).
/// </summary>
public class ExternalLoginState
{
    /// <summary>Which configured provider this flow belongs to.</summary>
    public string Provider { get; set; } = default!;

    /// <summary>The PKCE code verifier; its SHA-256 challenge was sent to the provider.</summary>
    public string CodeVerifier { get; set; } = default!;

    /// <summary>
    /// The OIDC nonce sent on the authorization request. The id_token must echo it back,
    /// which ties the token to this particular sign-in attempt.
    /// </summary>
    public string Nonce { get; set; } = default!;

    /// <summary>The exact redirect_uri sent on the authorization request. Must be replayed at the token endpoint.</summary>
    public string RedirectUri { get; set; } = default!;

    /// <summary>Where to send the browser once the sign-in completes.</summary>
    public string? ReturnUrl { get; set; }

    /// <summary>When the flow was started (diagnostics only; Valkey enforces the real expiry).</summary>
    public DateTime StartedOn { get; set; }
}

/// <summary>
/// Outcome of an external sign-in callback.
/// </summary>
public class ExternalCallbackResult
{
    /// <summary>Whether a session was successfully minted.</summary>
    public bool Success { get; set; }

    /// <summary>A user-safe failure message when <see cref="Success"/> is false.</summary>
    public string? Error { get; set; }

    /// <summary>Where the browser should be sent next, when the flow supplied a return URL.</summary>
    public string? ReturnUrl { get; set; }

    /// <summary>The issued session - identical in shape to what POST /api/Auth/Login returns.</summary>
    public IssuedLoginResponse? Login { get; set; }
}

namespace Auth.Models;

/// <summary>
/// Body of <c>POST /api/Auth/Login</c>: <c>{ "userid": "...", "pd": "..." }</c>.
/// <para>
/// The lowercase names are part of the wire contract - the frontend and the Playwright API tests
/// both post exactly these two fields, so do not rename them.
/// </para>
/// <para>
/// Both properties are nullable on purpose: a missing or blank field should come back as a plain
/// 401 "invalid credentials" rather than a 400 model-binding error, which would tell an attacker
/// something about the request format.
/// </para>
/// </summary>
public class LoginRequest
{
    /// <summary>The login name (matches <c>UserAccount.UserId</c>).</summary>
    public string? userid { get; set; }

    /// <summary>The password, in plaintext over TLS. Never logged, never stored.</summary>
    public string? pd { get; set; }
}

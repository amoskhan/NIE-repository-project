using System.Text.Json;
using Auth.Models;
using Auth.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Caching.Distributed;
using Shared.Helpers;

namespace Auth.Controllers;

/// <summary>
/// The Auth API's entire HTTP surface.
/// <para>
/// Sign-in is handled by the application's own identity provider
/// (<see cref="ILocalIdentityService"/>, backed by the <c>UserAccounts</c> table). A successful
/// sign-in writes an <see cref="AuthSessionDto"/> to Valkey under <c>session:{token}</c>; callers
/// then send that token back as the <c>X-Session-Id</c> header on every request.
/// </para>
/// <para>
/// This API deliberately knows nothing about roles or permissions - the frontend fetches those
/// from the Main API after signing in.
/// </para>
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("api/[controller]/[action]")]
public class AuthController : ControllerBase
{
    /// <summary>
    /// Cookie names that may carry the session token, most specific first.
    /// Must stay in step with the frontend's cookie constants.
    /// </summary>
    private static readonly string[] SessionCookieNames =
    {
        "AppTemplate-SessionToken",
        "SessionToken",
        "SessionId"
    };

    private readonly IDistributedCache _cache;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthController> _logger;
    private readonly IWebHostEnvironment _environment;
    private readonly IAuthSessionService _authSessionService;
    private readonly ILocalIdentityService _localIdentityService;
    private readonly IExternalIdpService _externalIdpService;

    public AuthController(
        IDistributedCache cache,
        IWebHostEnvironment environment,
        IConfiguration configuration,
        ILogger<AuthController> logger,
        IAuthSessionService authSessionService,
        ILocalIdentityService localIdentityService,
        IExternalIdpService externalIdpService)
    {
        _cache = cache;
        _environment = environment;
        _configuration = configuration;
        _logger = logger;
        _authSessionService = authSessionService;
        _localIdentityService = localIdentityService;
        _externalIdpService = externalIdpService;
    }

    #region Sign in / sign out

    /// <summary>
    /// Signs a user in with a local user ID and password, and stores the session in Valkey.
    /// Does NOT resolve roles/permissions - the frontend fetches those from the Main API.
    /// </summary>
    /// <remarks>
    /// Request: <c>{ "userid": "...", "pd": "..." }</c>.
    /// Responds 200 with an <see cref="IssuedLoginResponse"/>, or 401 with a
    /// <see cref="LoginResponse"/> whose <c>isAuthenticated</c> is false.
    /// </remarks>
    [HttpPost]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        var result = await _localIdentityService.VerifyCredentialsAsync(
            req?.userid ?? string.Empty,
            req?.pd ?? string.Empty,
            HttpContext.RequestAborted);

        if (!result.isAuthenticated)
        {
            _logger.LogWarning("Login failed for user {UserId}.", req?.userid);
            return Unauthorized(result);
        }

        var issuedLogin = await _authSessionService.IssueSessionAsync(result, HttpContext.RequestAborted);

        _logger.LogInformation("Login success for user {UserId}.", result.userId);

        return Ok(issuedLogin);
    }

    /// <summary>
    /// Ends a session by deleting it from Valkey. Always answers 200 - signing out twice is not an error.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Logout([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] string? sessionToken)
    {
        sessionToken = GetSessionToken(sessionToken);
        if (!string.IsNullOrWhiteSpace(sessionToken))
            await _cache.RemoveAsync($"session:{sessionToken}");

        return Ok(new { success = true });
    }

    /// <summary>
    /// Checks whether the caller's session token is still valid.
    /// The token is read from the <c>X-Session-Id</c> header, the <c>sessionToken</c> query string,
    /// or a session cookie.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Verify()
    {
        var sessionToken = GetSessionToken();

        if (string.IsNullOrWhiteSpace(sessionToken))
            return Unauthorized(new { isValid = false });

        var dto = await ReadSessionAsync(sessionToken);
        if (dto == null)
            return Unauthorized(new { isValid = false });

        return Ok(new
        {
            isValid = true,
            userId = dto.UserId,
            userName = dto.Name
        });
    }

    /// <summary>
    /// Slides the session forward: the old token is retired and a brand-new one is issued with a
    /// fresh absolute expiry. Rotating the token on every renewal limits the damage a leaked token
    /// can do. Returns the new token as a JSON string.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Refresh([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] string? sessionToken)
    {
        sessionToken = GetSessionToken(sessionToken);

        if (string.IsNullOrWhiteSpace(sessionToken))
            return Unauthorized("Session not found or expired.");

        var dto = await ReadSessionAsync(sessionToken);
        if (dto == null)
            return Unauthorized("Session not found or expired.");

        var newSessionToken = Guid.NewGuid().ToString("N");
        dto.LastActive = DateTimeHelper.Now;

        await _cache.SetStringAsync(
            $"session:{newSessionToken}",
            JsonSerializer.Serialize(dto),
            BuildSessionCacheOptions(),
            HttpContext.RequestAborted);

        // Retire the previous token only after the replacement is safely stored.
        await _cache.RemoveAsync($"session:{sessionToken}", HttpContext.RequestAborted);

        return Ok(newSessionToken);
    }

    /// <summary>
    /// Returns the display profile held in the session. Roles and permissions are not included -
    /// ask the Main API for those.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> GetProfile([FromBody] string sessionToken)
    {
        var dto = await ReadSessionAsync(sessionToken);
        if (dto == null)
            return Unauthorized("Session not found or expired.");

        return Ok(new { dto.Name, dto.Email, dto.Department });
    }

    /// <summary>
    /// Creates a dev-only test session without checking a password. Session is stored in Valkey.
    /// The frontend must call the Main API for roles/permissions after redirect.
    /// Returns 404 outside the Development environment so it can never be reached in a deployment.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateTestSession([FromBody] CreateTestSessionRequest? req)
    {
        if (!_environment.IsDevelopment())
            return NotFound();

        var requestedUserId = string.IsNullOrWhiteSpace(req?.UserId) ? "alice" : req!.UserId!.Trim();
        var userName = string.IsNullOrWhiteSpace(req?.Name) ? requestedUserId : req!.Name!.Trim();
        var email = string.IsNullOrWhiteSpace(req?.Email) ? $"{requestedUserId}@example.edu" : req!.Email!.Trim();
        var department = string.IsNullOrWhiteSpace(req?.Department) ? "Digital Services" : req!.Department!.Trim();
        var sessionToken = Guid.NewGuid().ToString("N");

        var sessionDto = new AuthSessionDto
        {
            UserId = requestedUserId,
            LastActive = DateTimeHelper.Now,
            Name = userName,
            Email = email,
            Department = department
        };

        await _cache.SetStringAsync(
            $"session:{sessionToken}",
            JsonSerializer.Serialize(sessionDto),
            BuildSessionCacheOptions(),
            HttpContext.RequestAborted);

        return Ok(new CreateTestSessionResponse
        {
            Success = true,
            SessionToken = sessionToken,
            UserId = requestedUserId,
            UserName = userName,
            Email = email
        });
    }

    #endregion

    #region Account management

    /// <summary>
    /// Creates a local account. Only available when <c>LocalIdentity:AllowSelfRegistration</c> is true.
    /// Does not sign the new user in - the client should call Login afterwards.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Register([FromBody] RegisterRequest req)
    {
        var (ok, error) = await _localIdentityService.RegisterAsync(
            req.UserId,
            req.FullName,
            req.Email,
            req.Department,
            req.Password,
            HttpContext.RequestAborted);

        if (!ok)
            return BadRequest(new { success = false, message = error });

        return Ok(new { success = true });
    }

    /// <summary>
    /// Starts a password reset.
    /// </summary>
    /// <remarks>
    /// Always answers 200 with the same body, whether or not the account exists - otherwise this
    /// endpoint would be a way to discover valid user IDs and email addresses.
    /// <para>
    /// In Development the raw token is echoed back so students can complete the flow without an
    /// email server. In every other environment the token must be delivered out of band (email);
    /// wire that up where indicated below.
    /// </para>
    /// </remarks>
    [HttpPost]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest req)
    {
        var token = await _localIdentityService.CreatePasswordResetTokenAsync(
            req.UserIdOrEmail,
            HttpContext.RequestAborted);

        // TODO (project): email `token` to the account holder as a reset link.
        // Never return it from a deployed environment.
        if (_environment.IsDevelopment() && token is not null)
        {
            return Ok(new
            {
                success = true,
                message = "If that account exists, a password reset link has been sent.",
                developmentToken = token
            });
        }

        return Ok(new
        {
            success = true,
            message = "If that account exists, a password reset link has been sent."
        });
    }

    /// <summary>
    /// Completes a password reset using the single-use token issued by ForgotPassword.
    /// </summary>
    /// <remarks>
    /// The body carries only the token and the new password. The account is resolved from the
    /// token itself, so a caller can never point a reset at an account they do not hold a token for.
    /// </remarks>
    [HttpPost]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest req)
    {
        var (ok, error) = await _localIdentityService.ResetPasswordAsync(
            req.Token,
            req.NewPassword,
            HttpContext.RequestAborted);

        if (!ok)
            return BadRequest(new { success = false, message = error });

        return Ok(new { success = true });
    }

    /// <summary>
    /// Changes the signed-in user's password. Requires a live session AND the current password.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest req)
    {
        var sessionToken = GetSessionToken();
        if (string.IsNullOrWhiteSpace(sessionToken))
            return Unauthorized(new { success = false, message = "Session not found or expired." });

        var dto = await ReadSessionAsync(sessionToken);
        if (dto == null)
            return Unauthorized(new { success = false, message = "Session not found or expired." });

        var (ok, error) = await _localIdentityService.ChangePasswordAsync(
            dto.UserId,
            req.CurrentPassword,
            req.NewPassword,
            HttpContext.RequestAborted);

        if (!ok)
            return BadRequest(new { success = false, message = error });

        return Ok(new { success = true });
    }

    #endregion

    #region Optional external identity provider (disabled by default)

    /// <summary>
    /// Lists the external sign-in providers that are enabled and fully configured.
    /// Returns an empty array when the <c>ExternalIdp</c> slot is switched off, so a sign-in page
    /// can simply render one button per entry.
    /// </summary>
    [HttpGet]
    public IActionResult ExternalProviders() => Ok(_externalIdpService.GetEnabledProviders());

    /// <summary>
    /// Begins an external sign-in by redirecting the browser to the provider's authorization
    /// endpoint (authorization-code flow with PKCE).
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> ExternalStart([FromQuery] string? provider = null, [FromQuery] string? returnUrl = null)
    {
        if (!_externalIdpService.IsEnabled)
            return ExternalIdpDisabled();

        if (!IsSafeReturnUrl(returnUrl))
            return BadRequest(new { message = "The requested return URL is not allowed." });

        try
        {
            var callbackUrl = Url.ActionLink(nameof(ExternalCallback), "Auth")
                ?? throw new InvalidOperationException("Could not build the external callback URL.");

            var authorizationUrl = await _externalIdpService.BuildAuthorizationUrlAsync(
                provider ?? string.Empty,
                returnUrl,
                callbackUrl,
                HttpContext.RequestAborted);

            return Redirect(authorizationUrl);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "External sign-in could not be started for provider {Provider}.", provider);
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = ex.Message });
        }
    }

    /// <summary>
    /// The provider redirects the browser back here with a one-time authorization code.
    /// On success this mints exactly the same Valkey session a password sign-in would, then either
    /// redirects to the original return URL or returns the session as JSON.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> ExternalCallback(
        [FromQuery] string? code = null,
        [FromQuery] string? state = null,
        [FromQuery] string? error = null)
    {
        if (!_externalIdpService.IsEnabled)
            return ExternalIdpDisabled();

        if (!string.IsNullOrWhiteSpace(error))
        {
            _logger.LogWarning("External provider returned error {Error}.", error);
            return Unauthorized(new { message = "The external provider refused the sign-in." });
        }

        var result = await _externalIdpService.HandleCallbackAsync(code, state, HttpContext.RequestAborted);

        if (!result.Success || result.Login is null)
        {
            if (IsSafeReturnUrl(result.ReturnUrl) && !string.IsNullOrWhiteSpace(result.ReturnUrl))
                return Redirect(AppendQuery(result.ReturnUrl!, "error", result.Error ?? "Sign-in failed."));

            return Unauthorized(new { message = result.Error ?? "Sign-in failed." });
        }

        if (IsSafeReturnUrl(result.ReturnUrl) && !string.IsNullOrWhiteSpace(result.ReturnUrl))
        {
            var redirectUrl = AppendQuery(result.ReturnUrl!, "sessionToken", result.Login.sessionToken ?? string.Empty);
            redirectUrl = AppendQuery(redirectUrl, "userId", result.Login.userId ?? string.Empty);
            return Redirect(redirectUrl);
        }

        return Ok(result.Login);
    }

    private IActionResult ExternalIdpDisabled() =>
        StatusCode(StatusCodes.Status503ServiceUnavailable, new
        {
            message = "External sign-in is not enabled. Set ExternalIdp:Enabled and configure a provider "
                      + "in appsettings to switch it on, or sign in with a local account."
        });

    #endregion

    #region Helpers

    /// <summary>
    /// Session lifetime policy. Absolute (not sliding) expiry - Refresh is what extends a session,
    /// and it deliberately issues a new token when it does.
    /// </summary>
    private DistributedCacheEntryOptions BuildSessionCacheOptions() =>
        new DistributedCacheEntryOptions()
            .SetAbsoluteExpiration(TimeSpan.FromMinutes(Convert.ToInt32(_configuration["ValidSessionTimeInMins"])));

    /// <summary>
    /// Reads and deserializes a session from Valkey. Returns null when it is missing or unreadable.
    /// </summary>
    private async Task<AuthSessionDto?> ReadSessionAsync(string sessionToken)
    {
        if (string.IsNullOrWhiteSpace(sessionToken))
            return null;

        var dtoStr = await _cache.GetStringAsync($"session:{sessionToken}", HttpContext.RequestAborted);
        if (string.IsNullOrEmpty(dtoStr))
            return null;

        try
        {
            return JsonSerializer.Deserialize<AuthSessionDto>(dtoStr);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Stored session could not be deserialized.");
            return null;
        }
    }

    /// <summary>
    /// Finds the session token, preferring an explicit argument, then the <c>X-Session-Id</c>
    /// header, then the query string, then the known session cookies.
    /// </summary>
    private string? GetSessionToken(string? sessionToken = null)
    {
        if (!string.IsNullOrWhiteSpace(sessionToken))
            return sessionToken;

        var sessionHeader = Request.Headers["X-Session-Id"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(sessionHeader))
            return sessionHeader;

        var sessionQuery = Request.Query["sessionToken"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(sessionQuery))
            return sessionQuery;

        foreach (var cookieName in SessionCookieNames)
        {
            if (Request.Cookies.TryGetValue(cookieName, out var sessionCookie)
                && !string.IsNullOrWhiteSpace(sessionCookie))
                return sessionCookie;
        }

        return null;
    }

    /// <summary>
    /// Guards against open redirects: a return URL must either be relative, or point at an origin
    /// listed in <c>AllowedCORSOrigin</c>.
    /// </summary>
    private bool IsSafeReturnUrl(string? returnUrl)
    {
        if (string.IsNullOrWhiteSpace(returnUrl))
            return true;

        if (!Uri.TryCreate(returnUrl, UriKind.RelativeOrAbsolute, out var uri))
            return false;

        if (!uri.IsAbsoluteUri)
            return returnUrl.StartsWith('/') && !returnUrl.StartsWith("//", StringComparison.Ordinal);

        var allowedOrigins = _configuration.GetSection("AllowedCORSOrigin").Get<string[]>() ?? [];

        return allowedOrigins.Any(origin =>
            Uri.TryCreate(origin, UriKind.Absolute, out var allowed)
            && string.Equals(allowed.Scheme, uri.Scheme, StringComparison.OrdinalIgnoreCase)
            && string.Equals(allowed.Host, uri.Host, StringComparison.OrdinalIgnoreCase)
            && allowed.Port == uri.Port);
    }

    private static string AppendQuery(string url, string key, string value)
    {
        var separator = url.Contains('?', StringComparison.Ordinal) ? "&" : "?";
        return $"{url}{separator}{Uri.EscapeDataString(key)}={Uri.EscapeDataString(value)}";
    }

    #endregion
}

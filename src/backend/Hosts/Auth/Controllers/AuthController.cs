using System.Text.Json;
using Auth.Models;
using Auth.Services;
using BuildingBlocks.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.IdentityModel.Tokens;

namespace Auth.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/[controller]/[action]")]
public class AuthController : ControllerBase
{
    private static readonly string[] SessionCookieNames =
    {
        "Application-SessionToken",
        "SessionToken",
        "SessionId"
    };

    private readonly IHttpClientFactory _http;
    private readonly IDistributedCache _cache;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthController> _logger;
    private readonly IWebHostEnvironment _environment;
    private readonly IAuthSessionService _authSessionService;
    private readonly IPortalSsoService _portalSsoService;
    private readonly string _subKey;
    private readonly string _baseUrl;

    public AuthController(
        IHttpClientFactory http,
        IDistributedCache cache,
        IWebHostEnvironment environment,
        IConfiguration configuration,
        ILogger<AuthController> logger,
        IAuthSessionService authSessionService,
        IPortalSsoService portalSsoService)
    {
        _http = http;
        _cache = cache;
        _environment = environment;
        _configuration = configuration;
        _logger = logger;
        _authSessionService = authSessionService;
        _portalSsoService = portalSsoService;
        _subKey = configuration["NIEAuthApi:SubscriptionKey"]
            ?? throw new InvalidOperationException("NIEAuthApi:SubscriptionKey configuration is required.");
        _baseUrl = configuration["NIEAuthApi:BaseUrl"]
            ?? throw new InvalidOperationException("NIEAuthApi:BaseUrl configuration is required.");
    }

    /// <summary>
    /// Authenticates against the NIE IDP and stores the session in Valkey.
    /// Does NOT resolve roles/permissions — the frontend fetches those from the Main API.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        var client = _http.CreateClient();
        client.DefaultRequestHeaders.Add("x-nie-aws-api-gw-key", _subKey);

        var resp = await client.PostAsJsonAsync($"{_baseUrl}/LogInUser", new
        {
            userid = req.userid,
            pd = req.pd
        });
        var body = await resp.Content.ReadFromJsonAsync<LoginResponse>();

        if (body?.isAuthenticated != true)
        {
            _logger.LogWarning("Login failed for user {UserId}.", req.userid);
            return Unauthorized(body);
        }

        var issuedLogin = await _authSessionService.IssueSessionAsync(body, HttpContext.RequestAborted);

        _logger.LogInformation("Login success for user {UserId}.", body.userId);

        return Ok(issuedLogin);
    }

    [HttpGet]
    public async Task<IActionResult> SsoStart([FromQuery] string? returnUrl = null)
    {
        try
        {
            var callbackUrl = Url.ActionLink(nameof(SsoCallback), "Auth");
            var response = await _portalSsoService.StartAsync(returnUrl, callbackUrl, HttpContext.RequestAborted);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Portal SSO start is unavailable.");
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> SsoCallback([FromBody] SsoCallbackRequest request)
    {
        try
        {
            var result = await _portalSsoService.HandleCallbackAsync(request, HttpContext.Connection.RemoteIpAddress, HttpContext.RequestAborted);
            return Ok(result);
        }
        catch (SecurityTokenException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Portal SSO callback is unavailable.");
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = ex.Message });
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Portal SSO exchange failed.");
            return StatusCode(StatusCodes.Status502BadGateway, new { message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> SsoFinalize([FromQuery] string state)
    {
        try
        {
            var result = await _portalSsoService.FinalizeAsync(state, HttpContext.RequestAborted);
            return result.status switch
            {
                SsoStateStatus.Completed when result.login != null => Ok(result.login),
                SsoStateStatus.Failed => Unauthorized(new { message = result.message, status = result.status }),
                _ => Accepted(result)
            };
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Portal SSO finalize is unavailable.");
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = ex.Message });
        }
    }

    /// <summary>
    /// Creates a dev-only test session without IDP. Session is stored in Valkey.
    /// The frontend must call the Main API for roles/permissions after redirect.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateTestSession([FromBody] CreateTestSessionRequest? req)
    {
        if (!_environment.IsDevelopment())
            return NotFound();

        var requestedUserId = string.IsNullOrWhiteSpace(req?.UserId) ? "devia" : req!.UserId!.Trim();
        var userName = string.IsNullOrWhiteSpace(req?.Name) ? requestedUserId : req!.Name!.Trim();
        var email = string.IsNullOrWhiteSpace(req?.Email) ? $"{requestedUserId}@nie.edu.sg" : req!.Email!.Trim();
        var department = string.IsNullOrWhiteSpace(req?.Department) ? "Digital Solutions" : req!.Department!.Trim();
        var sessionToken = Guid.NewGuid().ToString("N");

        var sessionDto = new AuthSessionDto
        {
            UserId = requestedUserId,
            LastActive = DateTimeHelper.Now,
            Name = userName,
            Email = email,
            Department = department
        };

        var sessionCacheOptions = new DistributedCacheEntryOptions()
            .SetAbsoluteExpiration(TimeSpan.FromMinutes(Convert.ToInt32(_configuration["ValidSessionTimeInMins"])));

        await _cache.SetStringAsync(
            $"session:{sessionToken}",
            JsonSerializer.Serialize(sessionDto),
            sessionCacheOptions);

        return Ok(new CreateTestSessionResponse
        {
            Success = true,
            SessionToken = sessionToken,
            UserId = requestedUserId,
            UserName = userName,
            Email = email
        });
    }

    [HttpPost]
    public async Task<IActionResult> Refresh([FromBody] string sessionToken)
    {
        var client = _http.CreateClient();
        client.DefaultRequestHeaders.Add("x-nie-aws-api-gw-key", _subKey);
        client.DefaultRequestHeaders.Add("sessiontoken", sessionToken);

        var resp = await client.PostAsync($"{_baseUrl}/RefreshSession", null);
        var body = await resp.Content.ReadFromJsonAsync<RefreshResponseRoot>();
        if (body?.result?.authenticated == true)
        {
            var dtoStr = await _cache.GetStringAsync($"session:{sessionToken}");
            await _cache.RemoveAsync($"session:{sessionToken}");

            if (string.IsNullOrEmpty(dtoStr))
                return Unauthorized("Session not found or expired.");

            var dto = JsonSerializer.Deserialize<AuthSessionDto>(dtoStr);
            if (dto == null)
                return Unauthorized("Invalid session data.");

            dto.LastActive = DateTimeHelper.Now;
            var sessionCacheOptions = new DistributedCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(Convert.ToInt32(_configuration["ValidSessionTimeInMins"])));
            await _cache.SetStringAsync(
                $"session:{body.result.sessionToken}",
                JsonSerializer.Serialize(dto),
                sessionCacheOptions);
            return Ok(body.result.sessionToken);
        }

        return Unauthorized();
    }

    [HttpGet]
    public async Task<IActionResult> Verify()
    {
        var sessionToken = Request.Headers["X-Session-Id"].FirstOrDefault()
            ?? Request.Query["sessionToken"].FirstOrDefault()
            ?? Request.Cookies["SessionToken"];

        if (string.IsNullOrWhiteSpace(sessionToken))
            return Unauthorized(new { isValid = false });

        var dtoStr = await _cache.GetStringAsync($"session:{sessionToken}");
        if (string.IsNullOrWhiteSpace(dtoStr))
            return Unauthorized(new { isValid = false });

        var dto = JsonSerializer.Deserialize<AuthSessionDto>(dtoStr);
        if (dto == null)
            return Unauthorized(new { isValid = false });

        return Ok(new
        {
            isValid = true,
            userId = dto.UserId,
            userName = dto.Name
        });
    }

    [HttpPost]
    public async Task<IActionResult> Verify(string userId, string sessionToken)
    {
        var client = _http.CreateClient();
        client.DefaultRequestHeaders.Add("x-nie-aws-api-gw-key", _subKey);
        client.DefaultRequestHeaders.Add("UserId", userId);
        client.DefaultRequestHeaders.Add("sessionToken", sessionToken);

        var resp = await client.PostAsync($"{_baseUrl}/LogInUser", null);
        var body = await resp.Content.ReadFromJsonAsync<VerifyResponse>();
        return body?.success == true ? Ok() : Forbid();
    }
    [HttpPost]
    public async Task<IActionResult> Logout([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] string? sessionToken)
    {
        sessionToken = GetSessionToken(sessionToken);
        if (!string.IsNullOrWhiteSpace(sessionToken))
            await _cache.RemoveAsync($"session:{sessionToken}");

        return Ok(new { success = true });
    }


    [HttpPost]
    public async Task<IActionResult> GetProfile([FromBody] string sessionToken)
    {
        var dtoStr = await _cache.GetStringAsync($"session:{sessionToken}");
        if (string.IsNullOrEmpty(dtoStr))
            return Unauthorized("Session not found or expired.");

        var dto = JsonSerializer.Deserialize<AuthSessionDto>(dtoStr);
        if (dto == null)
            return Unauthorized("Invalid session data.");

        return Ok(new { dto.Name, dto.Email, dto.Department });
    }

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
}

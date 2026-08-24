using System.Text.Json;
using Api.Grpc;
using Application.Contracts;
using BuildingBlocks.Extensions;
using BuildingBlocks.Globals;
using BuildingBlocks.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Distributed;

namespace Api.Middleware;

/// <summary>
/// Session validation middleware used for authentication in this project.
/// All API's will be authenticated via this middleware.
/// NOTE: DO NOT CHANGE THIS MIDDLEWARE
/// </summary>
public class SessionValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SessionValidationMiddleware> _logger;
    private readonly IConfiguration _configuration;
    private readonly IDistributedCache _cache;
    private readonly HttpClient _httpClient;

    public SessionValidationMiddleware(
        RequestDelegate next,
        ILogger<SessionValidationMiddleware> logger,
        IConfiguration configuration,
        IDistributedCache cache,
        HttpClient httpClient)
    {
        _next = next;
        _logger = logger;
        _configuration = configuration;
        _cache = cache;
        _httpClient = httpClient;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var endpoint = context.GetEndpoint();
        if (endpoint?.Metadata.GetMetadata<ServiceIntegrationEndpointMetadata>() is not null)
        {
            await _next(context);
            return;
        }

        // Skip validation for certain paths
        var path = context.Request.Path.Value?.ToLower();
        if (ShouldSkipValidation(path))
        {
            await _next(context);
            return;
        }

        //Check if the endpoint allows anonymous access
        if (endpoint?.Metadata?.GetMetadata<AllowAnonymousAttribute>() != null)
        {
            await _next(context);
            return;
        }

        // Get session ID from header, query, or cookie
        var sessionId = GetSessionId(context);

        if (string.IsNullOrEmpty(sessionId))
        {
            await HandleUnauthorized(context, "Session ID is required");
            return;
        }

        // Validate session with auth service
        var isValid = await ValidateSessionAsync(context, sessionId);
        if (!isValid)
        {
            await HandleUnauthorized(context, "Invalid or expired session");
            return;
        }

        await _next(context);
    }

    private string? GetSessionId(HttpContext context)
    {
        // Cookie
        //var sessionCookie = context.Request.Cookies["SessionId"];
        //if (!string.IsNullOrEmpty(sessionCookie))
        //    return sessionCookie;

        var sessionHeader = context.Request.Headers["X-Session-Id"].FirstOrDefault();
        if (!string.IsNullOrEmpty(sessionHeader))
            return sessionHeader;

        return null;
    }

    private bool ShouldSkipValidation(string? path)
    {
        if (string.IsNullOrEmpty(path))
            return false;

        var skipPaths = new[]
        {
            "/openapi",
            "/health",
            "/favicon.ico",
            "/tickerq"
        };

        return skipPaths.Any(skipPath => path.StartsWith(skipPath));
    }

    private async Task<bool> ValidateSessionAsync(HttpContext context, string sessionId)
    {
        try
        {
            var authDtoStr = await _cache.GetStringAsync($"session:{sessionId}");
            if (string.IsNullOrEmpty(authDtoStr))
                return false;

            var authDto = JsonExtensions.Deserialize<AuthDto>(authDtoStr);
            if (authDto != null)
            {
                var validMins = Convert.ToInt32(_configuration["ValidSessionTimeInMins"]);
                var lastActiveSgt = NormalizeLastActiveToSingapore(authDto.LastActive);
                var expiry = lastActiveSgt.Add(TimeSpan.FromMinutes(validMins));
                if (DateTimeHelper.Now < expiry)
                {
                    context.Items[Constants.KeySessionUserId] = authDto.UserId;
                    context.Items[Constants.KeySessionUserName] = authDto.Name;
                    context.Items[Constants.KeySessionUserEmail] = authDto.Email;
                    context.Items[Constants.KeySessionSessionId] = sessionId;
                    context.Items[Constants.KeySessionUserDept] = authDto.Department;

                    return true;
                }

                await _cache.RemoveAsync($"session:{sessionId}");
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating session {SessionId}", sessionId);
            return false;
        }
    }

    /// <summary>
    /// Maps session payload to Singapore wall-clock for sliding-window comparison.
    /// Auth stores <see cref="DateTimeHelper.Now"/>; older Redis entries may still use UTC.
    /// </summary>
    private static DateTime NormalizeLastActiveToSingapore(DateTime lastActive)
    {
        return lastActive.Kind switch
        {
            DateTimeKind.Utc => DateTimeHelper.FromUtc(lastActive),
            DateTimeKind.Local => DateTimeHelper.FromUtc(lastActive.ToUniversalTime()),
            _ => DateTimeHelper.AsUnspecified(lastActive),
        };
    }

    private async Task HandleUnauthorized(HttpContext context, string message)
    {
        context.Response.StatusCode = 401;
        context.Response.ContentType = "application/json";

        var response = new
        {
            Success = false,
            Message = message,
            StatusCode = 401
        };

        var json = JsonSerializer.Serialize(response);
        await context.Response.WriteAsync(json);
    }
}

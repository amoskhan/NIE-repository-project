using System.Text.Json;
using Auth.Models;
using BuildingBlocks.Helpers;
using Microsoft.Extensions.Caching.Distributed;

namespace Auth.Services;

public class AuthSessionService : IAuthSessionService
{
    private readonly IDistributedCache _cache;
    private readonly DistributedCacheEntryOptions _sessionCacheOptions;

    public AuthSessionService(IDistributedCache cache, IConfiguration configuration)
    {
        _cache = cache;
        _sessionCacheOptions = new DistributedCacheEntryOptions()
            .SetAbsoluteExpiration(TimeSpan.FromMinutes(Convert.ToInt32(configuration["ValidSessionTimeInMins"])));
    }

    public async Task<IssuedLoginResponse> IssueSessionAsync(LoginResponse loginResponse, CancellationToken cancellationToken = default)
    {
        if (loginResponse.isAuthenticated != true)
            throw new InvalidOperationException("Cannot issue a session for an unauthenticated login response.");

        var sessionToken = string.IsNullOrWhiteSpace(loginResponse.sessionToken)
            ? Guid.NewGuid().ToString("N")
            : loginResponse.sessionToken;

        var sessionDto = new AuthSessionDto
        {
            UserId = loginResponse.userId ?? string.Empty,
            LastActive = DateTimeHelper.Now,
            Name = loginResponse.fullName ?? loginResponse.userName ?? string.Empty,
            Email = loginResponse.email ?? string.Empty,
            Department = loginResponse.department ?? string.Empty
        };

        await _cache.SetStringAsync(
            $"session:{sessionToken}",
            JsonSerializer.Serialize(sessionDto),
            _sessionCacheOptions,
            cancellationToken);

        return new IssuedLoginResponse
        {
            isAuthenticated = true,
            userId = loginResponse.userId,
            userName = loginResponse.userName ?? loginResponse.fullName,
            fullName = loginResponse.fullName,
            email = loginResponse.email,
            department = loginResponse.department,
            sessionToken = sessionToken
        };
    }
}

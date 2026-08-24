using Application.Abstractions.Identity;
using BuildingBlocks.Globals;
using Microsoft.AspNetCore.Http;

namespace Infrastructure.Identity;

/// <summary>
/// Provides access to current user context from HttpContext.
/// Used for audit fields (CreatedBy, UpdatedBy) in entities.
/// </summary>
public class UserContextService : IUserContextService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UserContextService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? UserId => _httpContextAccessor.HttpContext?.Items[Constants.KeySessionUserId]?.ToString();

    public string? UserName => _httpContextAccessor.HttpContext?.Items[Constants.KeySessionUserName]?.ToString();

    public string? UserEmail => _httpContextAccessor.HttpContext?.Items[Constants.KeySessionUserEmail]?.ToString();

    public string? SessionId => _httpContextAccessor.HttpContext?.Items[Constants.KeySessionSessionId]?.ToString();
}

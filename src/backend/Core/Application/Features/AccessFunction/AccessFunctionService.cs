using System.Text.Json;
using Application.Abstractions;
using Application.Contracts;
using Application.Security;
using Domain.Enums;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

namespace Application.Features;

/// <summary>
/// Reads access functions and evaluates user grants through role assignments.
/// </summary>
public class AccessFunctionService : IAccessFunctionService
{
    private readonly IApplicationDbContext _context;
    private readonly IDistributedCache? _cache;
    private const string UserAccessCachePrefix = "user_access_functions_v2_";
    private const string LegacyUserAccessCachePrefix = "user_access_functions_";

    public AccessFunctionService(IApplicationDbContext context, IDistributedCache? cache = null)
    {
        _context = context;
        _cache = cache;
    }

    /// <inheritdoc />
    public async Task<List<AccessFunctionDto>> GetAllAsync(EAccessFunctionType? type = null)
    {
        var query = _context.AccessFunctions
            .AsNoTracking()
            .Where(accessFunction => accessFunction.IsActive);

        if (type.HasValue)
        {
            query = query.Where(accessFunction => accessFunction.Type == type.Value);
        }

        return await query
            .OrderBy(accessFunction => accessFunction.Module)
            .ThenBy(accessFunction => accessFunction.DisplayOrder)
            .ProjectToType<AccessFunctionDto>()
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<List<string>> GetUserAccessFunctionCodesAsync(string userId)
    {
        var normalizedUserId = ExternalUserId.Normalize(userId);
        var cacheKey = $"{UserAccessCachePrefix}{normalizedUserId}";

        if (_cache != null)
        {
            var cached = await _cache.GetStringAsync(cacheKey);
            if (!string.IsNullOrWhiteSpace(cached))
            {
                var codes = JsonSerializer.Deserialize<List<string>>(cached);
                if (codes is { Count: > 0 })
                {
                    return codes;
                }
            }
        }

        var now = BuildingBlocks.Helpers.DateTimeHelper.Now;
        var activeAssignments = _context.UserRoles
            .AsNoTracking()
            .Where(userRole =>
                userRole.UserId.ToLower() == normalizedUserId &&
                userRole.IsActive &&
                userRole.Role.IsActive)
            .Where(userRole => userRole.ExpiresOn == null || userRole.ExpiresOn > now);
        var assignmentExpirations = await activeAssignments
            .Where(userRole => userRole.ExpiresOn != null)
            .Select(userRole => userRole.ExpiresOn!.Value)
            .ToListAsync();
        var codesFromDb = await activeAssignments
            .SelectMany(userRole => userRole.Role.RoleAccessFunctions)
            .Where(link => link.AccessFunction.IsActive)
            .Select(link => link.AccessFunction.Code)
            .Distinct()
            .OrderBy(code => code)
            .ToListAsync();

        if (_cache != null && codesFromDb.Count > 0)
        {
            var cacheLifetime = TimeSpan.FromMinutes(15);
            if (assignmentExpirations.Count > 0)
            {
                var timeUntilFirstExpiry = assignmentExpirations.Min() - now;
                if (timeUntilFirstExpiry < cacheLifetime)
                {
                    cacheLifetime = timeUntilFirstExpiry;
                }
            }

            if (cacheLifetime <= TimeSpan.Zero)
            {
                return codesFromDb;
            }

            await _cache.SetStringAsync(
                cacheKey,
                JsonSerializer.Serialize(codesFromDb),
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = cacheLifetime
                });
        }

        return codesFromDb;
    }

    /// <inheritdoc />
    public async Task<bool> HasAccessAsync(string userId, string accessFunctionCode)
    {
        var userCodes = await GetUserAccessFunctionCodesAsync(userId);
        return userCodes.Contains(accessFunctionCode, StringComparer.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public async Task InvalidateUsersAsync(IEnumerable<string> userIds)
    {
        if (_cache == null)
        {
            return;
        }

        foreach (var userId in userIds.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var normalizedUserId = ExternalUserId.Normalize(userId);
            await _cache.RemoveAsync($"{UserAccessCachePrefix}{normalizedUserId}");
            await _cache.RemoveAsync($"{LegacyUserAccessCachePrefix}{normalizedUserId}");
        }
    }
}

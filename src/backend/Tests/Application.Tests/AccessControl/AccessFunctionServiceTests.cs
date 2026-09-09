using System.Text;
using Application.Abstractions;
using Application.Features;
using Application.Tests.TestSupport;
using Domain.Enums;
using Domain.Models;
using Microsoft.Extensions.Caching.Distributed;
using NSubstitute;

namespace Application.Tests;

public sealed class AccessFunctionServiceTests
{
    [Fact]
    public async Task Access_lookup_matches_institutional_user_ids_without_regard_to_case()
    {
        var (context, _) = CreateContextWithDashboardGrant();
        var service = new AccessFunctionService(context);

        var grantedCodes = await service.GetUserAccessFunctionCodesAsync("NIE25");

        Assert.Contains("screen.dashboard.view", grantedCodes);
    }

    [Fact]
    public async Task Empty_cached_grants_are_rechecked_after_an_assignment_is_added()
    {
        var (context, _) = CreateContextWithDashboardGrant();
        var cache = Substitute.For<IDistributedCache>();
        cache.GetAsync(
                "user_access_functions_v2_nie25",
                Arg.Any<CancellationToken>())
            .Returns(Encoding.UTF8.GetBytes("[]"));
        var service = new AccessFunctionService(context, cache);

        var grantedCodes = await service.GetUserAccessFunctionCodesAsync("NIE25");

        Assert.Contains("screen.dashboard.view", grantedCodes);
    }

    [Fact]
    public async Task Cache_invalidation_uses_the_canonical_identity_key()
    {
        var cache = Substitute.For<IDistributedCache>();
        var context = Substitute.For<IApplicationDbContext>();
        var service = new AccessFunctionService(context, cache);

        await service.InvalidateUsersAsync([" NIE25 "]);

        await cache.Received(1).RemoveAsync(
            "user_access_functions_v2_nie25",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Cached_grants_expire_no_later_than_the_assignment()
    {
        var expiresOn = BuildingBlocks.Helpers.DateTimeHelper.Now.AddMinutes(2);
        var (context, userRoles) = CreateContextWithDashboardGrant();
        userRoles.Items.Single().ExpiresOn = expiresOn;
        var cache = Substitute.For<IDistributedCache>();
        var service = new AccessFunctionService(context, cache);

        await service.GetUserAccessFunctionCodesAsync("nie25");

        await cache.Received(1).SetAsync(
            "user_access_functions_v2_nie25",
            Arg.Any<byte[]>(),
            Arg.Is<DistributedCacheEntryOptions>(options => HasBoundedCacheLifetime(options)),
            Arg.Any<CancellationToken>());
    }

    private static bool HasBoundedCacheLifetime(DistributedCacheEntryOptions? options)
    {
        var cacheLifetime = options?.AbsoluteExpirationRelativeToNow;
        return cacheLifetime.HasValue &&
            cacheLifetime.GetValueOrDefault() > TimeSpan.Zero &&
            cacheLifetime.GetValueOrDefault() <= TimeSpan.FromMinutes(2);
    }

    private static (IApplicationDbContext Context, FakeDbSet<UserRole> UserRoles)
        CreateContextWithDashboardGrant()
    {
        const string accessCode = "screen.dashboard.view";
        var accessFunction = new AccessFunction
        {
            Code = accessCode,
            Name = "View dashboard",
            Module = "Dashboard",
            ResourceName = "dashboard",
            Type = EAccessFunctionType.Screen,
            IsActive = true,
        };
        var role = new Role
        {
            Code = "OPERATIONS_USER",
            Name = "Operations User",
            IsActive = true,
        };
        role.RoleAccessFunctions.Add(new RoleAccessFunction
        {
            Role = role,
            AccessFunction = accessFunction,
        });
        var userRoles = new FakeDbSet<UserRole>(new UserRole
        {
            UserId = "nie25",
            Role = role,
            IsActive = true,
        });
        var context = Substitute.For<IApplicationDbContext>();
        context.UserRoles.Returns(userRoles);
        return (context, userRoles);
    }
}

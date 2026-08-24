using Domain.Models;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Architecture.Tests;

public class AccessControlScopeTests
{
    [Fact]
    public void Application_access_uses_uuid_v7_compatible_keys_and_a_unique_scope_assignment()
    {
        Assert.Equal(typeof(Guid), typeof(ApplicationAccess).GetProperty(nameof(ApplicationAccess.Id))?.PropertyType);
        Assert.Equal(typeof(Guid), typeof(ApplicationAccess).GetProperty(nameof(ApplicationAccess.ApplicationId))?.PropertyType);
        Assert.Equal(typeof(Guid), typeof(ApplicationAccess).GetProperty(nameof(ApplicationAccess.RoleId))?.PropertyType);

        var options = new DbContextOptionsBuilder<MainDbContext>()
            .UseNpgsql("Host=localhost;Database=access_control_contract;Username=contract;Password=not-used")
            .Options;

        using var context = new MainDbContext(options);
        var entityType = context.Model.FindEntityType(typeof(ApplicationAccess));
        Assert.NotNull(entityType);

        var uniqueIndex = entityType.GetIndexes().SingleOrDefault(index =>
            index.IsUnique &&
            index.Properties.Select(property => property.Name).SequenceEqual(
                new[]
                {
                    nameof(ApplicationAccess.ApplicationId),
                    nameof(ApplicationAccess.UserId),
                    nameof(ApplicationAccess.RoleId)
                }));

        Assert.NotNull(uniqueIndex);
    }
}

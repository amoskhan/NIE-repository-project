using Application.Security;
using Domain.Identifiers;
using Domain.Models;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Architecture.Tests;

public class UuidV7IdentityTests
{
    [Fact]
    public void Base_entity_primary_key_is_a_guid()
    {
        var idProperty = typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id));

        Assert.NotNull(idProperty);
        Assert.Equal(typeof(Guid), idProperty.PropertyType);
    }

    [Fact]
    public void New_entities_receive_non_empty_uuid_v7_ids()
    {
        var entityTypes = typeof(BaseEntity).Assembly
            .GetTypes()
            .Where(type => !type.IsAbstract && typeof(BaseEntity).IsAssignableFrom(type))
            .OrderBy(type => type.FullName)
            .ToList();

        Assert.NotEmpty(entityTypes);

        foreach (var entityType in entityTypes)
        {
            var entity = Activator.CreateInstance(entityType);
            var id = Assert.IsType<Guid>(entityType.GetProperty(nameof(BaseEntity.Id))?.GetValue(entity));

            Assert.NotEqual(Guid.Empty, id);
            Assert.Equal(7, id.Version);
        }
    }

    [Fact]
    public void Every_ef_primary_key_uses_postgresql_uuid()
    {
        var options = new DbContextOptionsBuilder<MainDbContext>()
            .UseNpgsql("Host=localhost;Database=uuid_contract;Username=uuid_contract;Password=not-used")
            .Options;

        using var context = new MainDbContext(options);

        var violations = context.Model.GetEntityTypes()
            .SelectMany(entityType => entityType.FindPrimaryKey()?.Properties ?? [])
            .Where(property => property.ClrType != typeof(Guid) || property.GetColumnType() != "uuid")
            .Select(property => $"{property.DeclaringType.DisplayName()}.{property.Name}: {property.ClrType.Name}/{property.GetColumnType()}")
            .OrderBy(value => value)
            .ToList();

        Assert.Empty(violations);
    }

    [Fact]
    public void Audit_log_primary_key_is_a_guid()
    {
        var idProperty = typeof(AuditLog).GetProperty(nameof(AuditLog.Id));

        Assert.NotNull(idProperty);
        Assert.Equal(typeof(Guid), idProperty.PropertyType);
    }

    [Fact]
    public void Canonical_factory_and_fixed_system_role_ids_are_uuid_v7()
    {
        Assert.True(Uuid7.IsValid(Uuid7.New()));
        Assert.All(SystemRoleIds.All, id => Assert.True(Uuid7.IsValid(id)));
        Assert.All(SystemApplicationIds.All, id => Assert.True(Uuid7.IsValid(id)));
        Assert.Equal(SystemRoleIds.All.Count, SystemRoleIds.All.Distinct().Count());
        Assert.Equal(SystemApplicationIds.All.Count, SystemApplicationIds.All.Distinct().Count());
    }
}

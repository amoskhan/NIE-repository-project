using System.Collections;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Domain.Identifiers;
using Domain.Models;

namespace Domain.Tests;

/// <summary>
/// Identity and audit behaviour inherited by every persisted entity. The property
/// initialisers in <see cref="BaseEntity"/> and <see cref="TimestampedEntity"/> are what
/// make a brand-new object insertable without a database round trip, and what keep audit
/// columns out of API responses.
/// </summary>
public sealed class EntityIdentityTests
{
    private sealed class PlainEntity : BaseEntity;

    private sealed class AuditedEntity : TimestampedEntity;

    [Fact]
    public void A_new_entity_carries_a_client_generated_uuid_v7_key()
    {
        var entity = new PlainEntity();

        Assert.NotEqual(Guid.Empty, entity.Id);
        Assert.Equal(7, entity.Id.Version);
        Assert.True(Uuid7.IsValid(entity.Id));
    }

    [Fact]
    public void Each_construction_mints_a_fresh_key_that_the_persistence_layer_can_replace()
    {
        var first = new PlainEntity();
        var second = new PlainEntity();

        Assert.NotEqual(first.Id, second.Id);

        // EF Core overwrites Id when materialising an existing row; the initialiser must
        // not win over the stored value.
        var materialised = Guid.Parse("0199a0f0-0000-7000-8000-000000000001");
        second.Id = materialised;

        Assert.Equal(materialised, second.Id);
        Assert.True(Uuid7.IsValid(second.Id));
    }

    /// <summary>
    /// No entity overrides <see cref="object.Equals(object)"/>, so two objects holding the
    /// same key are still distinct instances. Code that de-duplicates tracked entities has
    /// to compare <c>Id</c> explicitly rather than the objects themselves.
    /// </summary>
    [Fact]
    public void Entities_compare_by_reference_not_by_key()
    {
        var id = Uuid7.New();
        var first = new PlainEntity { Id = id };
        var second = new PlainEntity { Id = id };

        Assert.NotSame(first, second);
        Assert.False(first.Equals(second));
        Assert.NotEqual(first, second);
        Assert.Equal(first.Id, second.Id);
    }

    [Fact]
    public void Audit_columns_stay_unset_until_the_save_interceptor_fills_them()
    {
        var entity = new AuditedEntity();

        Assert.Null(entity.CreatedOn);
        Assert.Null(entity.CreatedBy);
        Assert.Null(entity.UpdatedOn);
        Assert.Null(entity.UpdatedBy);
        Assert.True(Uuid7.IsValid(entity.Id));
    }

    [Fact]
    public void Every_audit_column_is_marked_json_ignore()
    {
        var unprotected = typeof(TimestampedEntity)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(property => property.GetCustomAttribute<JsonIgnoreAttribute>() is null)
            .Select(property => property.Name)
            .ToList();
        var declaredCount = typeof(TimestampedEntity)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Length;

        Assert.Equal(4, declaredCount);
        Assert.Empty(unprotected);
    }

    [Fact]
    public void Serialising_an_entity_leaks_neither_the_editor_nor_the_edit_time()
    {
        var vendor = new Vendor
        {
            Name = "Acme Supplies",
            Code = "ACME",
            CreatedBy = "kamaludemy",
            CreatedOn = DateTime.UtcNow,
            UpdatedBy = "another.user",
            UpdatedOn = DateTime.UtcNow
        };

        using var document = JsonDocument.Parse(JsonSerializer.Serialize(vendor));
        var root = document.RootElement;

        Assert.True(root.TryGetProperty(nameof(BaseEntity.Id), out _));
        Assert.True(root.TryGetProperty(nameof(Vendor.Name), out _));
        Assert.False(root.TryGetProperty(nameof(TimestampedEntity.CreatedOn), out _));
        Assert.False(root.TryGetProperty(nameof(TimestampedEntity.CreatedBy), out _));
        Assert.False(root.TryGetProperty(nameof(TimestampedEntity.UpdatedOn), out _));
        Assert.False(root.TryGetProperty(nameof(TimestampedEntity.UpdatedBy), out _));
    }

    /// <summary>
    /// Covers entities that opt out of <see cref="BaseEntity"/> and declare their own key,
    /// such as <see cref="AuditLog"/>: they must still hand out a UUIDv7 or their inserts
    /// land in random index pages.
    /// </summary>
    [Fact]
    public void Every_domain_entity_gets_a_uuid_v7_key_without_touching_the_database()
    {
        var offenders = new List<string>();
        var covered = new List<string>();

        foreach (var type in DomainTypes.Entities)
        {
            var idProperty = type.GetProperty("Id", BindingFlags.Public | BindingFlags.Instance);
            if (idProperty is null || idProperty.PropertyType != typeof(Guid))
            {
                continue;
            }

            covered.Add(type.Name);
            var id = (Guid)idProperty.GetValue(DomainTypes.CreateInstance(type))!;
            if (!Uuid7.IsValid(id))
            {
                offenders.Add(type.Name);
            }
        }

        Assert.NotEmpty(covered);
        Assert.Contains(nameof(AuditLog), covered);
        Assert.Empty(offenders);
    }

    /// <summary>
    /// Navigation collections are dereferenced before any load happens — the approval flow
    /// does <c>po.Approvals.Add(...)</c> on a freshly constructed order — so a nav property
    /// left null is a NullReferenceException waiting for the first create request.
    /// </summary>
    [Fact]
    public void Every_navigation_collection_starts_empty_rather_than_null()
    {
        var offenders = new List<string>();
        var covered = new List<string>();

        foreach (var type in DomainTypes.Entities)
        {
            var instance = DomainTypes.CreateInstance(type);

            foreach (var property in DomainTypes.ReadableProperties(type))
            {
                if (property.PropertyType == typeof(string)
                    || !typeof(IEnumerable).IsAssignableFrom(property.PropertyType))
                {
                    continue;
                }

                var member = type.Name + "." + property.Name;
                covered.Add(member);

                if (property.GetValue(instance) is not IEnumerable collection
                    || collection.GetEnumerator().MoveNext())
                {
                    offenders.Add(member);
                }
            }
        }

        Assert.NotEmpty(covered);
        Assert.Empty(offenders);
    }
}

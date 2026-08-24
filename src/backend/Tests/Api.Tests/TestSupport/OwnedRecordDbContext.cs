using Domain.Abstractions;
using Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Api.Tests.TestSupport;

/// <summary>
/// Owned aggregate root used with <c>BaseController.EnsureOwnedAsync</c>, which requires
/// a <see cref="BaseEntity"/> with a GUID key.
/// </summary>
internal sealed class SampleOwnedEntity : BaseEntity, IOwnedEntity
{
    public string OwnerUserId { get; set; } = string.Empty;
}

/// <summary>
/// Minimal owned entity used to close <c>OwnedEntityActionFilter&lt;TEntity&gt;</c> without
/// depending on the production model.
/// </summary>
internal sealed class OwnedRecord : IOwnedEntity
{
    public int Id { get; set; }

    public string OwnerUserId { get; set; } = string.Empty;
}

/// <summary>
/// Provider-configured but never connected. Records are put into the change tracker so
/// <c>DbSet.FindAsync</c> resolves them from the identity map instead of the database.
/// </summary>
internal sealed class OwnedRecordDbContext : DbContext
{
    public OwnedRecordDbContext(DbContextOptions<OwnedRecordDbContext> options)
        : base(options)
    {
    }

    public DbSet<OwnedRecord> OwnedRecords => Set<OwnedRecord>();

    public static OwnedRecordDbContext Create() =>
        new(new DbContextOptionsBuilder<OwnedRecordDbContext>()
            .UseNpgsql("Host=localhost;Database=api_unit_tests;Username=api_unit_tests;Password=api_unit_tests")
            .Options);
}

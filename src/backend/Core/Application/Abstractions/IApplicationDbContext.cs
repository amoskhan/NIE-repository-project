using Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Application.Abstractions;

/// <summary>
/// Persistence boundary used by application use cases. Infrastructure owns the
/// concrete EF Core context and registers it through dependency injection.
/// </summary>
public interface IApplicationDbContext
{
    DbSet<AccessFunction> AccessFunctions { get; }

    DbSet<Domain.Models.Application> Applications { get; }

    DbSet<ApplicationAccess> ApplicationAccesses { get; }

    DbSet<AuditLog> AuditLogs { get; }

    DbSet<IntegrationInboxMessage> IntegrationInboxMessages { get; }

    DbSet<IntegrationOutboxMessage> IntegrationOutboxMessages { get; }

    DbSet<NotificationOutbox> NotificationOutboxes { get; }

    DbSet<Role> Roles { get; }

    DbSet<RoleAccessFunction> RoleAccessFunctions { get; }

    DbSet<UserContactProfile> UserContactProfiles { get; }

    DbSet<UserDataTablePreference> UserDataTablePreferences { get; }

    DbSet<UserRole> UserRoles { get; }

    DbSet<WorkflowStateLog> WorkflowStateLogs { get; }

    DbSet<WorkflowTransition> WorkflowTransitions { get; }

    DbSet<TEntity> Set<TEntity>() where TEntity : class;

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

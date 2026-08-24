using System.Text.Json;
using Application.Abstractions;
using Application.Abstractions.Identity;
using Application.Security;
using Domain.Enums;
using Domain.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Infrastructure.Persistence;

/// <summary>
/// The main application DbContext.
/// This class is responsible for interacting with the database and managing the entities.
/// </summary>
public class MainDbContext : DbContext, IApplicationDbContext
{
    private readonly IUserContextService? _userContextService;
    private readonly IHttpContextAccessor? _httpContextAccessor;

    public MainDbContext(DbContextOptions<MainDbContext> options)
        : base(options)
    { }

    public MainDbContext(
        DbContextOptions<MainDbContext> options,
        IUserContextService userContextService,
        IHttpContextAccessor? httpContextAccessor = null)
        : base(options)
    {
        _userContextService = userContextService;
        _httpContextAccessor = httpContextAccessor;
    }

    public DbSet<Document> Documents { get; set; } = default!;

    // === SAMPLE: procurement (reference vertical; remove only after approved replacement) ===
    // Entity types live in src/backend/Core/Domain/Models/Samples/Procurement/.
    public DbSet<Vendor> Vendors { get; set; } = default!;
    public DbSet<CatalogItem> CatalogItems { get; set; } = default!;
    public DbSet<PurchaseOrder> PurchaseOrders { get; set; } = default!;
    public DbSet<PurchaseOrderLine> PurchaseOrderLines { get; set; } = default!;
    public DbSet<PurchaseOrderApproval> PurchaseOrderApprovals { get; set; } = default!;
    public DbSet<PurchaseOrderDocument> PurchaseOrderDocuments { get; set; } = default!;
    // === END SAMPLE ===

    // Code tables
    public DbSet<Code> Codes { get; set; } = default!;

    // Audit and Security
    public DbSet<AuditLog> AuditLogs { get; set; } = default!;
    public DbSet<AccessFunction> AccessFunctions { get; set; } = default!;
    public DbSet<Role> Roles { get; set; } = default!;
    public DbSet<UserRole> UserRoles { get; set; } = default!;
    public DbSet<RoleAccessFunction> RoleAccessFunctions { get; set; } = default!;
    public DbSet<Domain.Models.Application> Applications { get; set; } = default!;
    public DbSet<ApplicationAccess> ApplicationAccesses { get; set; } = default!;
    public DbSet<UserContactProfile> UserContactProfiles { get; set; } = default!;
    public DbSet<UserDataTablePreference> UserDataTablePreferences { get; set; } = default!;

    // Service integration reliability
    public DbSet<IntegrationInboxMessage> IntegrationInboxMessages { get; set; } = default!;
    public DbSet<IntegrationOutboxMessage> IntegrationOutboxMessages { get; set; } = default!;

    // Notification orchestration
    public DbSet<Notification> Notifications { get; set; } = default!;
    public DbSet<NotificationPolicy> NotificationPolicies { get; set; } = default!;
    public DbSet<NotificationTemplate> NotificationTemplates { get; set; } = default!;
    public DbSet<NotificationOutbox> NotificationOutboxes { get; set; } = default!;
    public DbSet<NotificationDelivery> NotificationDeliveries { get; set; } = default!;
    public DbSet<UserNotificationPreference> UserNotificationPreferences { get; set; } = default!;

    // Workflow
    public DbSet<WorkflowTransition> WorkflowTransitions { get; set; } = default!;
    public DbSet<WorkflowStateLog> WorkflowStateLogs { get; set; } = default!;

    // AI Chat
    public DbSet<ChatConversation> ChatConversations { get; set; } = default!;
    public DbSet<ChatMessage> ChatMessages { get; set; } = default!;

    // ChatEmbeddings is mapped only when the pgvector extension is installed.
    // To enable RAG: install the pgvector OS package on PostgreSQL, then
    //   1. re-add `public DbSet<ChatEmbedding> ChatEmbeddings { get; set; }` here,
    //   2. re-enable the `ChatEmbedding` block + `HasPostgresExtension("vector")`
    //      + `.UseVector()` (in Program.cs), and
    //   3. `dotnet ef migrations add EnableRagEmbeddings`.

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<DateTime>()
            .HaveColumnType("timestamp without time zone")
            .HaveConversion<UnspecifiedDateTimeConverter>();

        configurationBuilder.Properties<DateTime?>()
            .HaveColumnType("timestamp without time zone")
            .HaveConversion<NullableUnspecifiedDateTimeConverter>();
    }

    #region Override SaveChanges

    public override int SaveChanges()
    {
        return SaveChanges(acceptAllChangesOnSuccess: true);
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        var auditEntries = OnBeforeSaveChanges();
        UpdateTimestamps();
        var result = base.SaveChanges(acceptAllChangesOnSuccess);
        if (AddAuditLogs(auditEntries))
        {
            base.SaveChanges();
        }
        return result;
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await SaveChangesAsync(acceptAllChangesOnSuccess: true, cancellationToken);
    }

    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        var auditEntries = OnBeforeSaveChanges();
        UpdateTimestamps();
        var result = await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        if (AddAuditLogs(auditEntries))
        {
            await base.SaveChangesAsync(cancellationToken);
        }
        return result;
    }

    /// <summary>
    /// Captures changes before saving and creates audit entries.
    /// </summary>
    private List<AuditEntry> OnBeforeSaveChanges()
    {
        ChangeTracker.DetectChanges();
        var auditEntries = new List<AuditEntry>();

        foreach (var entry in ChangeTracker.Entries())
        {
            if (ShouldSkipAudit(entry))
                continue;

            var auditEntry = new AuditEntry(entry)
            {
                EntityName = entry.Entity.GetType().Name,
                Category = ResolveAuditCategory(entry.Entity.GetType()),
                UserId = GetCurrentUserId(),
                UserName = _userContextService?.UserName,
                IpAddress = GetClientIpAddress(),
                UserAgent = GetUserAgent(),
                CorrelationId = GetCorrelationId(),
                SessionId = _userContextService?.SessionId,
                RequestMethod = _httpContextAccessor?.HttpContext?.Request?.Method,
                RequestUrl = _httpContextAccessor?.HttpContext?.Request?.Path.Value
            };

            switch (entry.State)
            {
                case EntityState.Added:
                    auditEntry.Action = EAuditAction.Create;
                    foreach (var property in entry.Properties)
                    {
                        if (!ShouldAuditProperty(property))
                            continue;

                        if (property.Metadata.IsPrimaryKey())
                        {
                            auditEntry.EntityId = property.CurrentValue?.ToString();

                            if (property.IsTemporary)
                            {
                                auditEntry.HasTemporaryProperties = true;
                                auditEntry.TemporaryProperties.Add(property);
                            }
                        }

                        auditEntry.NewValues[property.Metadata.Name] = property.CurrentValue;
                    }
                    break;

                case EntityState.Deleted:
                    auditEntry.Action = EAuditAction.Delete;
                    foreach (var property in entry.Properties)
                    {
                        if (!ShouldAuditProperty(property))
                            continue;

                        if (property.Metadata.IsPrimaryKey())
                        {
                            auditEntry.EntityId = property.CurrentValue?.ToString();
                        }
                        auditEntry.OldValues[property.Metadata.Name] = property.OriginalValue;
                    }
                    break;

                case EntityState.Modified:
                    auditEntry.Action = EAuditAction.Update;
                    foreach (var property in entry.Properties)
                    {
                        if (!ShouldAuditProperty(property))
                            continue;

                        if (property.Metadata.IsPrimaryKey())
                        {
                            auditEntry.EntityId = property.CurrentValue?.ToString();
                            continue;
                        }

                        if (property.IsModified && !Equals(property.OriginalValue, property.CurrentValue))
                        {
                            auditEntry.ChangedProperties.Add(property.Metadata.Name);
                            auditEntry.OldValues[property.Metadata.Name] = property.OriginalValue;
                            auditEntry.NewValues[property.Metadata.Name] = property.CurrentValue;
                        }
                    }
                    // Only audit if there are actual changes
                    if (auditEntry.ChangedProperties.Count == 0)
                        continue;
                    break;

                default:
                    continue;
            }

            auditEntries.Add(auditEntry);
        }

        return auditEntries;
    }

    /// <summary>
    /// Adds audit entries after the main save has resolved generated keys.
    /// </summary>
    private bool AddAuditLogs(List<AuditEntry> auditEntries)
    {
        if (auditEntries.Count == 0)
            return false;

        foreach (var auditEntry in auditEntries)
        {
            // Get the final primary key for newly created entities
            if (auditEntry.HasTemporaryProperties)
            {
                foreach (var prop in auditEntry.TemporaryProperties)
                {
                    if (prop.Metadata.IsPrimaryKey())
                    {
                        auditEntry.EntityId = prop.CurrentValue?.ToString();
                    }
                    auditEntry.NewValues[prop.Metadata.Name] = prop.CurrentValue;
                }
            }

            AuditLogs.Add(auditEntry.ToAuditLog());
        }

        return true;
    }

    private string? GetClientIpAddress()
    {
        return _httpContextAccessor?.HttpContext?.Connection?.RemoteIpAddress?.ToString();
    }

    private string? GetUserAgent()
    {
        return _httpContextAccessor?.HttpContext?.Request?.Headers["User-Agent"].ToString();
    }

    private string? GetCorrelationId()
    {
        return _httpContextAccessor?.HttpContext?.TraceIdentifier;
    }

    private static bool ShouldSkipAudit(EntityEntry entry)
    {
        return entry.Entity is AuditLog
            || entry.State == EntityState.Detached
            || entry.State == EntityState.Unchanged
            || entry.Entity is not TimestampedEntity;
    }

    private static bool ShouldAuditProperty(PropertyEntry property)
    {
        if (property.Metadata.IsShadowProperty())
            return false;

        if (property.EntityEntry.Entity is UserDataTablePreference &&
            property.Metadata.Name == nameof(UserDataTablePreference.PreferencesJson))
        {
            return false;
        }

        return property.Metadata.Name is not nameof(TimestampedEntity.CreatedOn)
            and not nameof(TimestampedEntity.CreatedBy)
            and not nameof(TimestampedEntity.UpdatedOn)
            and not nameof(TimestampedEntity.UpdatedBy);
    }

    private static EAuditCategory ResolveAuditCategory(Type entityType)
    {
        return entityType == typeof(AccessFunction)
               || entityType == typeof(Role)
               || entityType == typeof(RoleAccessFunction)
               || entityType == typeof(UserRole)
            ? EAuditCategory.AccessControl
            : EAuditCategory.Data;
    }

    /// <summary>
    /// Updates the CreatedOn and UpdatedOn timestamps for entities that implement TimestampedEntity.
    /// Also sets CreatedBy and UpdatedBy from the current user context.
    /// </summary>
    private void UpdateTimestamps()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.Entity is TimestampedEntity && (e.State == EntityState.Added || e.State == EntityState.Modified));

        var now = BuildingBlocks.Helpers.DateTimeHelper.Now;
        var currentUserId = GetCurrentUserId();

        foreach (var entityEntry in entries)
        {
            var entity = (TimestampedEntity)entityEntry.Entity;

            if (entityEntry.State == EntityState.Added)
            {
                entity.CreatedOn = now;
                entity.CreatedBy = currentUserId;
            }
            else
            {
                // Don't update CreatedOn or CreatedBy for modified entities
                entityEntry.Property("CreatedOn").IsModified = false;
                entityEntry.Property("CreatedBy").IsModified = false;
            }

            entity.UpdatedOn = now;
            entity.UpdatedBy = currentUserId;
        }
    }

    private string? GetCurrentUserId()
    {
        return _userContextService?.UserId;
    }

    #endregion

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // AuditLog configuration
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.EntityName);
            entity.HasIndex(e => e.EntityId);
            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.Category);
            entity.HasIndex(e => e.Severity);
            entity.HasIndex(e => e.Action);
            entity.HasIndex(e => e.SessionId);
            entity.HasIndex(e => new { e.EntityName, e.EntityId });
            entity.HasIndex(e => new { e.Category, e.Timestamp });
            entity.HasIndex(e => new { e.Severity, e.Timestamp });
            entity.Property(e => e.OldValues).HasColumnType("jsonb");
            entity.Property(e => e.NewValues).HasColumnType("jsonb");
            entity.Property(e => e.ChangedProperties).HasColumnType("jsonb");
            entity.Property(e => e.AdditionalData).HasColumnType("jsonb");
        });

        // Role configuration
        modelBuilder.Entity<AccessFunction>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Code).IsUnique();
            entity.HasIndex(e => new { e.Type, e.Module, e.DisplayOrder });
            entity.Property(e => e.Code).HasMaxLength(120).IsRequired();
            entity.Property(e => e.Name).HasMaxLength(120).IsRequired();
            entity.Property(e => e.Module).HasMaxLength(80).IsRequired();
            entity.Property(e => e.ResourceName).HasMaxLength(120).IsRequired();
            entity.Property(e => e.Route).HasMaxLength(200);
            entity.Property(e => e.HttpMethod).HasMaxLength(20);
            entity.Property(e => e.Description).HasMaxLength(500);
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Code).IsUnique();
            entity.HasIndex(e => e.Name).IsUnique();
            entity.Property(e => e.Code).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(500);
        });

        // UserRole configuration
        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.UserId, e.RoleId }).IsUnique();
            entity.HasIndex(e => e.UserId);
            entity.HasOne(e => e.Role)
                  .WithMany(r => r.UserRoles)
                  .HasForeignKey(e => e.RoleId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // RoleAccessFunction configuration
        modelBuilder.Entity<RoleAccessFunction>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.RoleId, e.AccessFunctionId }).IsUnique();
            entity.HasOne(e => e.Role)
                  .WithMany(r => r.RoleAccessFunctions)
                  .HasForeignKey(e => e.RoleId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.AccessFunction)
                  .WithMany(accessFunction => accessFunction.RoleAccessFunctions)
                  .HasForeignKey(e => e.AccessFunctionId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Domain.Models.Application>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ProjectKey).IsUnique();
            entity.Property(e => e.Name).HasMaxLength(120).IsRequired();
            entity.Property(e => e.ProjectKey).HasMaxLength(120).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Repository).HasMaxLength(500);
            entity.Property(e => e.Branch).HasMaxLength(200);
        });

        modelBuilder.Entity<ApplicationAccess>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.ApplicationId, e.UserId, e.RoleId }).IsUnique();
            entity.HasIndex(e => e.UserId);
            entity.Property(e => e.UserId).HasMaxLength(256).IsRequired();
            entity.Property(e => e.AssignedBy).HasMaxLength(256);
            entity.HasOne(e => e.Application)
                .WithMany(application => application.AccessAssignments)
                .HasForeignKey(e => e.ApplicationId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Role)
                .WithMany(role => role.ApplicationAccesses)
                .HasForeignKey(e => e.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UserContactProfile>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserId).IsUnique();
            entity.Property(e => e.UserId).HasMaxLength(256).IsRequired();
            entity.Property(e => e.DisplayName).HasMaxLength(200);
            entity.Property(e => e.Email).HasMaxLength(254);
            entity.Property(e => e.Department).HasMaxLength(120);
            entity.Property(e => e.DepartmentDescription).HasMaxLength(300);
            entity.Property(e => e.Designation).HasMaxLength(200);
            entity.Property(e => e.Title).HasMaxLength(100);
            entity.Property(e => e.Source).HasMaxLength(100).IsRequired();
        });

        modelBuilder.Entity<IntegrationInboxMessage>(entity =>
        {
            entity.HasKey(message => message.Id);
            entity.HasIndex(message => new { message.MessageId, message.Consumer }).IsUnique();
            entity.HasIndex(message => message.ProcessedAtUtc);
            entity.Property(message => message.Consumer).HasMaxLength(200).IsRequired();
            entity.Property(message => message.EventName).HasMaxLength(200).IsRequired();
        });

        modelBuilder.Entity<IntegrationOutboxMessage>(entity =>
        {
            entity.HasKey(message => message.Id);
            entity.HasIndex(message => message.MessageId).IsUnique();
            entity.HasIndex(message => new
            {
                message.PublishedAtUtc,
                message.DeadLetteredAtUtc,
                message.AvailableAtUtc,
            });
            entity.Property(message => message.EventName).HasMaxLength(200).IsRequired();
            entity.Property(message => message.Producer).HasMaxLength(100).IsRequired();
            entity.Property(message => message.CorrelationId).HasMaxLength(100).IsRequired();
            entity.Property(message => message.CausationId).HasMaxLength(100);
            entity.Property(message => message.Payload).HasColumnType("jsonb").IsRequired();
            entity.Property(message => message.LastFailureCode).HasMaxLength(200);
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(item => item.Id);
            entity.HasIndex(item => new { item.RecipientUserId, item.CreatedOn });
            entity.HasIndex(item => item.DedupeKey).IsUnique();
            entity.Property(item => item.RecipientUserId).HasMaxLength(200).IsRequired();
            entity.Property(item => item.Title).HasMaxLength(240).IsRequired();
            entity.Property(item => item.Type).HasMaxLength(80).IsRequired();
            entity.Property(item => item.DedupeKey).HasMaxLength(300);
        });

        modelBuilder.Entity<NotificationPolicy>(entity =>
        {
            entity.HasKey(item => item.Id);
            entity.HasIndex(item => item.EventKey).IsUnique();
            entity.Property(item => item.EventKey).HasMaxLength(160).IsRequired();
            entity.Property(item => item.DisplayName).HasMaxLength(200).IsRequired();
            entity.Property(item => item.Description).HasMaxLength(800).IsRequired();
            entity.Property(item => item.Category).HasMaxLength(120).IsRequired();
        });

        modelBuilder.Entity<NotificationTemplate>(entity =>
        {
            entity.HasKey(item => item.Id);
            entity.HasIndex(item => new { item.EventKey, item.Channel, item.Version }).IsUnique();
            entity.HasIndex(item => new { item.EventKey, item.Channel, item.IsPublished });
            entity.Property(item => item.EventKey).HasMaxLength(160).IsRequired();
            entity.Property(item => item.Channel).HasMaxLength(40).IsRequired();
            entity.Property(item => item.Subject).HasMaxLength(240).IsRequired();
            entity.Property(item => item.Content).HasMaxLength(20_000).IsRequired();
        });

        modelBuilder.Entity<NotificationOutbox>(entity =>
        {
            entity.HasKey(item => item.Id);
            entity.HasIndex(item => item.DedupeKey).IsUnique();
            entity.HasIndex(item => new { item.Status, item.NextAttemptOn });
            entity.Property(item => item.EventKey).HasMaxLength(160).IsRequired();
            entity.Property(item => item.CorrelationKey).HasMaxLength(200).IsRequired();
            entity.Property(item => item.PayloadJson).HasColumnType("jsonb");
            entity.Property(item => item.DedupeKey).HasMaxLength(300).IsRequired();
            entity.HasMany(item => item.Deliveries)
                .WithOne(item => item.NotificationOutbox)
                .HasForeignKey(item => item.NotificationOutboxId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<NotificationDelivery>(entity =>
        {
            entity.HasKey(item => item.Id);
            entity.HasIndex(item => new { item.Status, item.NextAttemptOn });
            entity.HasIndex(item => new { item.NotificationOutboxId, item.RecipientUserId, item.Channel }).IsUnique();
            entity.Property(item => item.RecipientUserId).HasMaxLength(200).IsRequired();
            entity.Property(item => item.RecipientEmail).HasMaxLength(320);
            entity.Property(item => item.Channel).HasMaxLength(40).IsRequired();
            entity.Property(item => item.Status).HasMaxLength(40).IsRequired();
        });

        modelBuilder.Entity<UserNotificationPreference>(entity =>
        {
            entity.HasKey(item => item.Id);
            entity.HasIndex(item => item.UserId).IsUnique();
            entity.Property(item => item.UserId).HasMaxLength(200).IsRequired();
        });

        modelBuilder.Entity<UserDataTablePreference>(entity =>
        {
            entity.HasKey(item => item.Id);
            entity.HasIndex(item => new { item.ApplicationId, item.UserId, item.TableKey }).IsUnique();
            entity.HasIndex(item => new { item.ApplicationId, item.UserId });
            entity.Property(item => item.UserId).HasMaxLength(256).IsRequired();
            entity.Property(item => item.TableKey).HasMaxLength(160).IsRequired();
            entity.Property(item => item.PreferencesJson).HasColumnType("jsonb").IsRequired();
            entity.Property(item => item.Revision).IsConcurrencyToken();
            entity.HasOne<Domain.Models.Application>()
                .WithMany()
                .HasForeignKey(item => item.ApplicationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Document configuration (polymorphic owner; no hard FK from this layer)
        modelBuilder.Entity<Document>()
            .HasIndex(e => new { e.OwnerType, e.OwnerId });

        // === SAMPLE: procurement relationships (reference vertical; remove only after approved replacement) ===
        modelBuilder.Entity<Vendor>()
            .HasMany(v => v.CatalogItems)
            .WithOne(c => c.Vendor)
            .HasForeignKey(c => c.VendorId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Vendor>()
            .HasIndex(v => v.Code).IsUnique();

        modelBuilder.Entity<PurchaseOrder>()
            .HasMany(po => po.Lines)
            .WithOne(l => l.PurchaseOrder)
            .HasForeignKey(l => l.PurchaseOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PurchaseOrder>()
            .HasMany(po => po.Approvals)
            .WithOne(a => a.PurchaseOrder)
            .HasForeignKey(a => a.PurchaseOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PurchaseOrder>()
            .HasMany(po => po.Documents)
            .WithOne(d => d.PurchaseOrder)
            .HasForeignKey(d => d.PurchaseOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PurchaseOrder>()
            .HasOne(po => po.Vendor)
            .WithMany(v => v.PurchaseOrders)
            .HasForeignKey(po => po.VendorId);

        modelBuilder.Entity<PurchaseOrder>()
            .HasIndex(po => po.PoNumber).IsUnique();

        modelBuilder.Entity<PurchaseOrder>()
            .Property(po => po.TotalAmount).HasColumnType("decimal(18,2)");

        modelBuilder.Entity<PurchaseOrder>()
            .Property(po => po.WorkflowState)
            .HasMaxLength(50)
            .HasDefaultValue(EWorkflowState.Draft.ToString());

        // Persist EApprovalStage as its string name for readability + safe forward-compat
        modelBuilder.Entity<PurchaseOrderApproval>()
            .Property(a => a.ApprovalStage)
            .HasConversion<string>();

        modelBuilder.Entity<PurchaseOrderLine>()
            .Property(l => l.UnitPrice).HasColumnType("decimal(18,2)");

        modelBuilder.Entity<PurchaseOrderLine>()
            .Property(l => l.LineTotal).HasColumnType("decimal(18,2)");

        modelBuilder.Entity<CatalogItem>()
            .Property(c => c.UnitPrice).HasColumnType("decimal(18,2)");
        // === END SAMPLE ===

        // Code table configuration
        modelBuilder.Entity<Code>()
            .HasKey(e => e.Id);

        modelBuilder.Entity<Code>()
            .HasIndex(c => new { c.Type, c.Name })
            .IsUnique();

        // Workflow configuration
        modelBuilder.Entity<WorkflowTransition>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.FromState, e.ToState, e.RequiredRole }).IsUnique();
            entity.Property(e => e.FromState).HasMaxLength(50).IsRequired();
            entity.Property(e => e.ToState).HasMaxLength(50).IsRequired();
            entity.Property(e => e.RequiredRole).HasMaxLength(50).IsRequired();
            entity.Property(e => e.DisplayLabel).HasMaxLength(200);
            entity.Property(e => e.UiConditions).HasMaxLength(500);
        });

        modelBuilder.Entity<WorkflowStateLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.OwnerType, e.OwnerId });
            entity.HasIndex(e => e.TransitionedAt);
            entity.Property(e => e.FromState).HasMaxLength(50).IsRequired();
            entity.Property(e => e.ToState).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Remarks).HasMaxLength(1000);
            entity.Property(e => e.PerformedByUserId).HasMaxLength(100);
            entity.Property(e => e.PerformedByName).HasMaxLength(200);
            entity.Property(e => e.PerformedByRole).HasMaxLength(100);
            entity.Property(e => e.IpAddress).HasMaxLength(200);
            entity.Property(e => e.OwnerType).HasMaxLength(100).IsRequired();
        });

        // AI Chat configuration
        modelBuilder.Entity<ChatConversation>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.UserId, e.LastMessageAt });
            entity.HasIndex(e => new { e.UserId, e.Source });
            entity.Property(e => e.Title).HasMaxLength(200).IsRequired();
            entity.Property(e => e.UserId).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Source).HasMaxLength(50);
            entity.HasMany(e => e.Messages)
                .WithOne(m => m.Conversation)
                .HasForeignKey(m => m.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ChatMessage>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ConversationId);
            entity.Property(e => e.Role).HasMaxLength(20).IsRequired();
            entity.Property(e => e.Content).IsRequired();
            entity.Property(e => e.Metadata).HasColumnType("jsonb");
        });

        // ChatEmbedding mapping is intentionally omitted by default — it requires
        // the `vector` Postgres extension. To enable RAG, install pgvector then
        // restore both `modelBuilder.HasPostgresExtension("vector")` and the
        // ChatEmbedding entity block (see the README in Libraries/AI/Services/Rag).

        // Initial data is seeded from MainDbContextSeeder via EF Core UseSeeding/UseAsyncSeeding.
    }
}

/// <summary>
/// Helper class for building audit log entries from entity changes.
/// </summary>
internal class AuditEntry
{
    public AuditEntry(EntityEntry entry)
    {
        Entry = entry;
    }

    public EntityEntry Entry { get; }
    public string EntityName { get; set; } = default!;
    public string? EntityId { get; set; }
    public EAuditAction Action { get; set; }
    public EAuditCategory Category { get; set; }
    public Dictionary<string, object?> OldValues { get; } = new();
    public Dictionary<string, object?> NewValues { get; } = new();
    public List<string> ChangedProperties { get; } = new();
    public string? UserId { get; set; }
    public string? UserName { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? CorrelationId { get; set; }
    public string? SessionId { get; set; }
    public string? RequestMethod { get; set; }
    public string? RequestUrl { get; set; }
    public bool HasTemporaryProperties { get; set; }
    public List<PropertyEntry> TemporaryProperties { get; } = new();

    public AuditLog ToAuditLog()
    {
        return new AuditLog
        {
            EntityName = EntityName,
            EntityId = EntityId,
            Action = Action,
            Category = Category,
            Severity = Action == EAuditAction.Delete ? EAuditSeverity.Warning : EAuditSeverity.Info,
            OldValues = OldValues.Count > 0 ? JsonSerializer.Serialize(OldValues) : null,
            NewValues = NewValues.Count > 0 ? JsonSerializer.Serialize(NewValues) : null,
            ChangedProperties = ChangedProperties.Count > 0 ? JsonSerializer.Serialize(ChangedProperties) : null,
            UserId = UserId,
            UserName = UserName,
            Timestamp = BuildingBlocks.Helpers.DateTimeHelper.Now,
            IpAddress = IpAddress,
            UserAgent = UserAgent,
            CorrelationId = CorrelationId,
            SessionId = SessionId,
            RequestMethod = RequestMethod,
            RequestUrl = RequestUrl,
            Outcome = "Success"
        };
    }
}

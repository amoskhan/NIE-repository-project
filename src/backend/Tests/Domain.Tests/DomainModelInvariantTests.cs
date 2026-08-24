using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text.Json;
using Domain.Enums;
using Domain.Models;

namespace Domain.Tests;

/// <summary>
/// Property initialisers on the entities are the seed state of every insert: they decide
/// which workflow state a row starts in, whether a background worker will pick it up, and
/// whether a record is visible. These tests pin the seeds that carry meaning and the
/// cross-checks between a seed and the column it is written to.
/// </summary>
public sealed class DomainModelInvariantTests
{
    [Fact]
    public void A_new_purchase_order_is_a_draft_in_both_the_status_and_the_workflow_column()
    {
        var order = new PurchaseOrder();

        Assert.Equal(EPurchaseOrderStatus.Draft, order.Status);
        Assert.Equal(default(EPurchaseOrderStatus), order.Status);
        Assert.Equal("Draft", order.WorkflowState);
        Assert.Equal(
            EWorkflowState.Draft,
            Enum.Parse<EWorkflowState>(order.WorkflowState, ignoreCase: false));
        Assert.Empty(order.Lines);
        Assert.Empty(order.Approvals);
        Assert.Empty(order.Documents);
    }

    /// <summary>
    /// <c>WorkflowState</c> is a string column, so a state name longer than the declared
    /// width is a truncation (or a Postgres error) rather than a compile failure.
    /// </summary>
    [Fact]
    public void Every_workflow_state_name_fits_every_column_that_stores_it()
    {
        var widths = new[]
        {
            MaxLengthOf<PurchaseOrder>(nameof(PurchaseOrder.WorkflowState)),
            MaxLengthOf<WorkflowStateLog>(nameof(WorkflowStateLog.FromState)),
            MaxLengthOf<WorkflowStateLog>(nameof(WorkflowStateLog.ToState)),
            MaxLengthOf<WorkflowTransition>(nameof(WorkflowTransition.FromState)),
            MaxLengthOf<WorkflowTransition>(nameof(WorkflowTransition.ToState))
        };
        var narrowest = widths.Min();
        var tooLong = Enum.GetNames<EWorkflowState>()
            .Where(name => name.Length > narrowest)
            .ToList();

        Assert.Equal(50, narrowest);
        Assert.Empty(tooLong);
    }

    [Fact]
    public void Every_role_name_fits_the_workflow_transition_role_column()
    {
        var width = MaxLengthOf<WorkflowTransition>(nameof(WorkflowTransition.RequiredRole));
        var tooLong = Enum.GetNames<ERole>()
            .Where(name => name.Length > width)
            .ToList();

        Assert.Empty(tooLong);
    }

    /// <summary>
    /// A seeded default that overflows its own column fails on the very first insert.
    /// Reflection-driven so a new entity with a chatty default is caught here.
    /// </summary>
    [Fact]
    public void Every_seeded_string_default_fits_the_column_it_is_written_to()
    {
        var offenders = new List<string>();
        var covered = new List<string>();

        foreach (var type in DomainTypes.Entities)
        {
            var instance = DomainTypes.CreateInstance(type);

            foreach (var property in DomainTypes.ReadableProperties(type))
            {
                if (property.PropertyType != typeof(string))
                {
                    continue;
                }

                var maxLength = property.GetCustomAttribute<MaxLengthAttribute>();
                if (maxLength is null || maxLength.Length <= 0)
                {
                    continue;
                }

                if (property.GetValue(instance) is not string seeded || seeded.Length == 0)
                {
                    continue;
                }

                var member = type.Name + "." + property.Name;
                covered.Add(member);

                if (seeded.Length > maxLength.Length)
                {
                    offenders.Add(member);
                }
            }
        }

        Assert.NotEmpty(covered);
        Assert.Empty(offenders);
    }

    /// <summary>
    /// Npgsql maps <c>DateTime</c> to <c>timestamptz</c> and throws on a non-UTC kind, so a
    /// default of <c>DateTime.Now</c> anywhere in the model breaks inserts at runtime.
    /// </summary>
    [Fact]
    public void Every_seeded_timestamp_is_utc_so_timestamptz_inserts_succeed()
    {
        var offenders = new List<string>();
        var covered = new List<string>();

        foreach (var type in DomainTypes.Entities)
        {
            var instance = DomainTypes.CreateInstance(type);

            foreach (var property in DomainTypes.ReadableProperties(type))
            {
                if (property.PropertyType != typeof(DateTime))
                {
                    continue;
                }

                if (property.GetValue(instance) is not DateTime seeded
                    || seeded == default)
                {
                    continue;
                }

                var member = type.Name + "." + property.Name;
                covered.Add(member);

                if (seeded.Kind != DateTimeKind.Utc)
                {
                    offenders.Add(member);
                }
            }
        }

        Assert.NotEmpty(covered);
        Assert.Empty(offenders);
    }

    [Fact]
    public void Conversation_and_workflow_log_stamp_the_current_utc_instant()
    {
        var before = DateTime.UtcNow.AddMinutes(-1);
        var conversation = new ChatConversation();
        var log = new WorkflowStateLog();
        var after = DateTime.UtcNow.AddMinutes(1);

        Assert.Equal(DateTimeKind.Utc, conversation.LastMessageAt.Kind);
        Assert.InRange(conversation.LastMessageAt, before, after);
        Assert.Equal(0, conversation.MessageCount);

        Assert.Equal(DateTimeKind.Utc, log.TransitionedAt.Kind);
        Assert.InRange(log.TransitionedAt, before, after);
        Assert.False(log.NotificationSent);
        Assert.Null(log.NotificationSentAt);
    }

    /// <summary>
    /// The stored payload is round-tripped through <c>JsonDocument</c> by the preferences
    /// API, so the seeded default has to be parseable — an empty string or <c>null</c>
    /// would throw before normalisation ever runs.
    /// </summary>
    [Fact]
    public void A_new_table_preference_holds_a_parseable_empty_payload()
    {
        var preference = new UserDataTablePreference();

        Assert.Equal(1, preference.DefinitionVersion);
        Assert.Equal(1, preference.Revision);
        Assert.Equal(string.Empty, preference.UserId);
        Assert.Equal(string.Empty, preference.TableKey);

        using var document = JsonDocument.Parse(preference.PreferencesJson);
        var propertyCount = document.RootElement.EnumerateObject().Count();

        Assert.Equal(JsonValueKind.Object, document.RootElement.ValueKind);
        Assert.Equal(0, propertyCount);
    }

    /// <summary>
    /// Both outboxes are drained by pollers that select on status, attempt count and lock
    /// state. A row must therefore be born claimable and unclaimed.
    /// </summary>
    [Fact]
    public void Outbox_rows_are_born_pending_unattempted_and_unlocked()
    {
        var notificationOutbox = new NotificationOutbox();
        var delivery = new NotificationDelivery();
        var integrationOutbox = new IntegrationOutboxMessage();

        Assert.Equal("Pending", notificationOutbox.Status);
        Assert.Equal(0, notificationOutbox.Attempts);
        Assert.Null(notificationOutbox.NextAttemptOn);
        Assert.Null(notificationOutbox.ProcessedOn);
        Assert.Null(notificationOutbox.LastError);
        Assert.Empty(notificationOutbox.Deliveries);

        Assert.Equal("Pending", delivery.Status);
        Assert.Equal(0, delivery.Attempts);
        Assert.Null(delivery.SentOn);
        Assert.Null(delivery.ProviderMessageId);

        Assert.Equal(0, integrationOutbox.AttemptCount);
        Assert.Null(integrationOutbox.PublishedAtUtc);
        Assert.Null(integrationOutbox.DeadLetteredAtUtc);
        Assert.Null(integrationOutbox.LockToken);
        Assert.Null(integrationOutbox.LockExpiresAtUtc);
        Assert.Null(integrationOutbox.LastFailureCode);
        Assert.Equal(string.Empty, integrationOutbox.Payload);
        Assert.Equal(string.Empty, integrationOutbox.CorrelationId);
    }

    /// <summary>
    /// Soft-delete and visibility queries filter on <c>IsActive</c>. A record created with
    /// the flag defaulted to <see langword="false"/> would be invisible the moment it is
    /// saved, so the default is asserted across the whole model at once.
    /// </summary>
    [Fact]
    public void Records_are_visible_by_default_wherever_an_is_active_flag_exists()
    {
        var offenders = new List<string>();
        var covered = new List<string>();

        foreach (var type in DomainTypes.Entities)
        {
            var property = type.GetProperty("IsActive", BindingFlags.Public | BindingFlags.Instance);
            if (property is null || property.PropertyType != typeof(bool))
            {
                continue;
            }

            covered.Add(type.Name);
            var isActive = (bool)property.GetValue(DomainTypes.CreateInstance(type))!;
            if (!isActive)
            {
                offenders.Add(type.Name);
            }
        }

        Assert.NotEmpty(covered);
        Assert.Empty(offenders);
    }

    /// <summary>
    /// The two "system owned" flags default in opposite directions on purpose: a new role
    /// is deletable, a new access function is not.
    /// </summary>
    [Fact]
    public void System_ownership_defaults_differ_between_roles_and_access_functions()
    {
        Assert.False(new Role().IsSystemRole);
        Assert.True(new AccessFunction().IsSystemFunction);
    }

    [Fact]
    public void A_directory_backed_contact_profile_is_attributed_to_nie_by_default()
    {
        var profile = new UserContactProfile();
        var width = MaxLengthOf<UserContactProfile>(nameof(UserContactProfile.Source));

        Assert.Equal("NIE", profile.Source);
        Assert.True(profile.Source.Length <= width);
        Assert.Equal(default(DateTime), profile.LastVerifiedOn);
    }

    private static int MaxLengthOf<TEntity>(string propertyName)
    {
        var property = typeof(TEntity)
            .GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException(
                typeof(TEntity).Name + " has no property named " + propertyName);

        var maxLength = property.GetCustomAttribute<MaxLengthAttribute>()
            ?? throw new InvalidOperationException(
                typeof(TEntity).Name + "." + propertyName + " has no [MaxLength]");

        return maxLength.Length;
    }
}

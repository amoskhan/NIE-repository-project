using System.Text.RegularExpressions;
using Application.Features.Notifications;

namespace Application.Tests;

public sealed partial class NotificationEventCatalogTests
{
    [GeneratedRegex(@"\{([A-Za-z][A-Za-z0-9]*)\}")]
    private static partial Regex PlaceholderRegex();

    [Theory]
    [InlineData("procurement.purchase-order.submitted")]
    [InlineData("PROCUREMENT.PURCHASE-ORDER.SUBMITTED")]
    [InlineData("Procurement.Purchase-Order.Submitted")]
    public void Find_matches_an_event_key_without_regard_to_case(string eventKey)
    {
        var definition = NotificationEventCatalog.Find(eventKey);

        Assert.NotNull(definition);
        Assert.Equal(NotificationEventKeys.PurchaseOrderSubmitted, definition.EventKey);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("procurement.purchase-order")]
    [InlineData("unknown.event")]
    public void Find_returns_nothing_for_an_unregistered_event_key(string eventKey)
    {
        Assert.Null(NotificationEventCatalog.Find(eventKey));
    }

    [Fact]
    public void Registers_every_event_key_exactly_once()
    {
        var keys = NotificationEventCatalog.Events.Select(item => item.EventKey).ToList();

        Assert.Equal(keys.Count, keys.Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }

    [Fact]
    public void Whitelists_every_placeholder_used_by_a_default_template()
    {
        var allowed = NotificationEventCatalog.AllowedPlaceholders.ToHashSet(StringComparer.Ordinal);

        var unknown = NotificationEventCatalog.Events
            .SelectMany(definition =>
                PlaceholderRegex()
                    .Matches($"{definition.DefaultSubject}\n{definition.DefaultContent}")
                    .Select(match => $"{definition.EventKey}:{match.Groups[1].Value}"))
            .Where(entry => !allowed.Contains(entry.Split(':')[1]))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        Assert.Empty(unknown);
    }

    [Fact]
    public void Ships_default_templates_that_the_renderer_accepts()
    {
        var renderer = new NotificationTemplateRenderer();

        var rejected = NotificationEventCatalog.Events
            .Where(definition => renderer.Validate(definition.DefaultSubject, definition.DefaultContent).Count > 0)
            .Select(definition => definition.EventKey)
            .ToList();

        Assert.Empty(rejected);
    }

    [Fact]
    public void Offers_reminder_configuration_only_for_the_time_based_events()
    {
        var reminderEvents = NotificationEventCatalog.Events
            .Where(definition => definition.SupportsReminderConfiguration)
            .Select(definition => definition.EventKey)
            .ToList();

        Assert.Equal(
            [NotificationEventKeys.ApprovalOverdue, NotificationEventKeys.DeliveryReminder],
            reminderEvents);
    }

    [Fact]
    public void Enables_at_least_one_delivery_channel_for_every_event()
    {
        var silent = NotificationEventCatalog.Events
            .Where(definition =>
                !definition.InAppEnabled && !definition.EmailEnabled && !definition.PushEnabled)
            .Select(definition => definition.EventKey)
            .ToList();

        Assert.Empty(silent);
    }

    [Fact]
    public void Describes_every_event_with_a_display_name_category_and_subject()
    {
        var incomplete = NotificationEventCatalog.Events
            .Where(definition =>
                string.IsNullOrWhiteSpace(definition.DisplayName) ||
                string.IsNullOrWhiteSpace(definition.Category) ||
                string.IsNullOrWhiteSpace(definition.Description) ||
                string.IsNullOrWhiteSpace(definition.DefaultSubject) ||
                string.IsNullOrWhiteSpace(definition.DefaultContent))
            .Select(definition => definition.EventKey)
            .ToList();

        Assert.Empty(incomplete);
    }

    [Fact]
    public void Lists_each_allowed_placeholder_only_once()
    {
        var placeholders = NotificationEventCatalog.AllowedPlaceholders;

        Assert.Equal(placeholders.Count, placeholders.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void Keeps_the_cancelled_event_free_of_line_items_and_call_to_action()
    {
        var definition = NotificationEventCatalog.Find(NotificationEventKeys.PurchaseOrderCancelled);

        Assert.NotNull(definition);
        Assert.DoesNotContain("{LineItemsTable}", definition.DefaultContent, StringComparison.Ordinal);
        Assert.DoesNotContain("{ActionUrl}", definition.DefaultContent, StringComparison.Ordinal);
    }

    [Fact]
    public void Keeps_a_call_to_action_on_the_approval_task_events()
    {
        var definition = NotificationEventCatalog.Find(NotificationEventKeys.ManagerApprovalRequired);

        Assert.NotNull(definition);
        Assert.Contains("{ActionUrl}", definition.DefaultContent, StringComparison.Ordinal);
        Assert.Contains("{LineItemsTable}", definition.DefaultContent, StringComparison.Ordinal);
    }
}

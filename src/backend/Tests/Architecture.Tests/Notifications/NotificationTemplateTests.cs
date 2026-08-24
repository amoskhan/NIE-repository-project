using Application.Contracts;
using Application.Features.Notifications;
using Domain.Models;

namespace Architecture.Tests;

public class NotificationTemplateTests
{
    private static readonly string SourceBrand = string.Concat("Code", " Sentinel");

    [Fact]
    public void Validator_rejects_unknown_placeholders_and_unsafe_markup()
    {
        var renderer = new NotificationTemplateRenderer();

        var errors = renderer.Validate(
            "Status for {UnknownValue}",
            "<p>Hello</p><script>alert('unsafe')</script>");

        Assert.Contains(errors, error => error.Contains("Unknown placeholders", StringComparison.Ordinal));
        Assert.Contains(errors, error => error.Contains("unsafe HTML", StringComparison.Ordinal));
    }

    [Fact]
    public void Renderer_encodes_untrusted_procurement_values_and_keeps_owned_line_item_markup()
    {
        var renderer = new NotificationTemplateRenderer();
        var payload = new PurchaseOrderNotificationPayload
        {
            PurchaseOrderId = Guid.CreateVersion7(),
            ApplicationId = Guid.CreateVersion7(),
            ApplicationName = "NIE Procurement <script>",
            PurchaseOrderNumber = "PO-2026-00001",
            RequestedBy = "requester@example.edu.sg",
            VendorName = "Vendor <unsafe>",
            TotalAmount = 1250.50m,
            CurrentStage = "Manager Review",
            ActorUserId = "actor@example.edu.sg",
            SubmittedOn = new DateTime(2026, 8, 5, 9, 30, 0, DateTimeKind.Utc),
            ExpectedDeliveryDate = new DateTime(2026, 8, 20),
            DeliveryAddress = "1 Nanyang Walk <unsafe>",
            LineItems =
            [
                new PurchaseOrderLineNotificationDto
                {
                    LineNumber = 1,
                    ItemName = "Laptop <unsafe>",
                    Quantity = 2,
                    UnitOfMeasure = "Each",
                    UnitPrice = 625.25m,
                    LineTotal = 1250.50m
                }
            ]
        };

        var rendered = renderer.Render(
            "Review {PurchaseOrderNumber} from {VendorName}",
            "<p>Hello {RecipientName}</p><p>{DeliveryAddress}</p>{LineItemsTable}",
            payload,
            "Admin <admin@example.edu.sg>",
            "Requester",
            "Approver",
            "https://example.edu.sg/purchase-order/1");

        Assert.Contains("PO-2026-00001", rendered.Subject, StringComparison.Ordinal);
        Assert.Contains("Vendor &lt;unsafe&gt;", rendered.Subject, StringComparison.Ordinal);
        Assert.Contains("Admin &lt;admin@example.edu.sg&gt;", rendered.Content, StringComparison.Ordinal);
        Assert.Contains("1 Nanyang Walk &lt;unsafe&gt;", rendered.Content, StringComparison.Ordinal);
        Assert.Contains("<table role=\"presentation\"", rendered.Content, StringComparison.Ordinal);
        Assert.Contains("Laptop &lt;unsafe&gt;", rendered.Content, StringComparison.Ordinal);
        Assert.Contains("SGD 1,250.50", rendered.Content, StringComparison.Ordinal);
        Assert.DoesNotContain(SourceBrand, rendered.Content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Seeded_templates_use_procurement_events_content_and_placeholders()
    {
        Assert.NotEmpty(NotificationEventCatalog.Events);
        Assert.Contains(
            NotificationEventCatalog.Events,
            definition => definition.EventKey == NotificationEventKeys.ManagerApprovalRequired);
        Assert.Contains(
            NotificationEventCatalog.Events,
            definition => definition.EventKey == NotificationEventKeys.FinanceApprovalRequired);
        Assert.Contains(
            NotificationEventCatalog.Events,
            definition => definition.EventKey == NotificationEventKeys.ProcurementApprovalRequired);
        Assert.Contains("PurchaseOrderNumber", NotificationEventCatalog.AllowedPlaceholders);
        Assert.Contains("VendorName", NotificationEventCatalog.AllowedPlaceholders);
        Assert.Contains("TotalAmount", NotificationEventCatalog.AllowedPlaceholders);
        Assert.Contains("LineItemsTable", NotificationEventCatalog.AllowedPlaceholders);
        Assert.DoesNotContain("FindingCount", NotificationEventCatalog.AllowedPlaceholders);
        Assert.DoesNotContain("RemediationType", NotificationEventCatalog.AllowedPlaceholders);
        Assert.DoesNotContain("FindingsTable", NotificationEventCatalog.AllowedPlaceholders);

        Assert.All(NotificationEventCatalog.Events, definition =>
        {
            Assert.DoesNotContain(SourceBrand, definition.DefaultSubject, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(SourceBrand, definition.DefaultContent, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("remediation", definition.EventKey, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("remediation", definition.DefaultSubject, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("remediation", definition.DefaultContent, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("finding", definition.DefaultContent, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("no-fix", definition.DefaultContent, StringComparison.OrdinalIgnoreCase);
        });
        Assert.Contains(
            NotificationEventCatalog.Events,
            definition =>
                definition.DefaultContent.Contains("NIE Template", StringComparison.Ordinal) &&
                definition.DefaultContent.Contains("{PurchaseOrderNumber}", StringComparison.Ordinal));
    }

    [Fact]
    public void Reminder_timing_is_optional_and_available_only_for_reminder_events()
    {
        var configurableEvents = NotificationEventCatalog.Events
            .Where(definition => definition.SupportsReminderConfiguration)
            .Select(definition => definition.EventKey)
            .ToArray();

        Assert.Equal(
            [NotificationEventKeys.ApprovalOverdue, NotificationEventKeys.DeliveryReminder],
            configurableEvents);
        Assert.Null(new NotificationPolicy().ReminderAfterHours);
        Assert.Null(new NotificationPolicy().EscalationAfterHours);

        var reminderDefinition = NotificationEventCatalog.Find(NotificationEventKeys.ApprovalOverdue)!;
        var transactionalDefinition = NotificationEventCatalog.Find(NotificationEventKeys.PurchaseOrderSubmitted)!;

        Assert.Empty(NotificationPolicyTimingRules.Validate(reminderDefinition, null, null));
        Assert.Empty(NotificationPolicyTimingRules.Validate(reminderDefinition, 24, null));
        Assert.Empty(NotificationPolicyTimingRules.Validate(reminderDefinition, null, 72));
        Assert.Empty(NotificationPolicyTimingRules.Validate(reminderDefinition, 24, 72));
        Assert.Empty(NotificationPolicyTimingRules.Validate(transactionalDefinition, null, null));
        Assert.NotEmpty(NotificationPolicyTimingRules.Validate(transactionalDefinition, 24, 72));
        Assert.NotEmpty(NotificationPolicyTimingRules.Validate(reminderDefinition, 0, null));
        Assert.NotEmpty(NotificationPolicyTimingRules.Validate(reminderDefinition, null, 2161));
        Assert.NotEmpty(NotificationPolicyTimingRules.Validate(reminderDefinition, 72, 24));
    }

    [Fact]
    public void Generic_renderer_encodes_domain_values_and_accepts_owned_html_only_explicitly()
    {
        var renderer = new NotificationTemplateRenderer();

        var rendered = renderer.Render(
            "Purchase order {PurchaseOrderNumber}",
            "<p>{RecipientName}</p>{LineItemsTable}",
            new Dictionary<string, string>
            {
                ["PurchaseOrderNumber"] = "PO-2026-00001 <unsafe>",
                ["RecipientName"] = "Approver <unsafe>",
            },
            new Dictionary<string, string>
            {
                ["LineItemsTable"] = "<table><tr><td>Owned summary</td></tr></table>",
            });

        Assert.Contains("PO-2026-00001 &lt;unsafe&gt;", rendered.Subject, StringComparison.Ordinal);
        Assert.Contains("Approver &lt;unsafe&gt;", rendered.Content, StringComparison.Ordinal);
        Assert.Contains("<table><tr><td>Owned summary</td></tr></table>", rendered.Content, StringComparison.Ordinal);
    }
}

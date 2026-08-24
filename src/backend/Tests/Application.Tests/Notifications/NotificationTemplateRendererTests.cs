using Application.Contracts;
using Application.Features.Notifications;

namespace Application.Tests;

public sealed class NotificationTemplateRendererTests
{
    private static readonly NotificationTemplateRenderer Renderer = new();

    private static PurchaseOrderNotificationPayload CreatePayload(
        int lineItemCount = 0,
        string vendorName = "Acme Supplies") =>
        new()
        {
            PurchaseOrderId = Guid.Parse("0199a1a2-0000-7000-8000-000000000001"),
            ApplicationName = "NIE Template",
            PurchaseOrderNumber = "PO-2026-0001",
            VendorName = vendorName,
            TotalAmount = 1234.5m,
            CurrentStage = "Manager Review",
            SubmittedOn = new DateTime(2026, 2, 3, 9, 30, 0, DateTimeKind.Utc),
            LineItems = [.. Enumerable.Range(1, lineItemCount).Select(number =>
                new PurchaseOrderLineNotificationDto
                {
                    LineNumber = number,
                    ItemName = $"Item {number}",
                    Quantity = number,
                    UnitOfMeasure = "box",
                    UnitPrice = 10m,
                    LineTotal = 10m * number,
                })],
        };

    [Fact]
    public void Validate_accepts_a_template_that_uses_only_whitelisted_placeholders()
    {
        var errors = Renderer.Validate(
            "Purchase order {PurchaseOrderNumber}",
            "<p>Dear {RecipientName}, the total is {TotalAmount}.</p>");

        Assert.Empty(errors);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_rejects_a_blank_subject(string subject)
    {
        var errors = Renderer.Validate(subject, "<p>Body</p>");

        Assert.Contains("The subject is required and cannot exceed 240 characters.", errors);
    }

    [Theory]
    [InlineData(240, false)]
    [InlineData(241, true)]
    public void Validate_bounds_the_subject_at_two_hundred_and_forty_characters(int length, bool expectError)
    {
        var errors = Renderer.Validate(new string('s', length), "<p>Body</p>");

        Assert.Equal(expectError, errors.Count > 0);
    }

    [Theory]
    [InlineData("")]
    [InlineData("     ")]
    public void Validate_rejects_blank_content(string content)
    {
        var errors = Renderer.Validate("Subject", content);

        Assert.Contains("The content is required and cannot exceed 20,000 characters.", errors);
    }

    [Theory]
    [InlineData(20000, false)]
    [InlineData(20001, true)]
    public void Validate_bounds_the_content_at_twenty_thousand_characters(int length, bool expectError)
    {
        var errors = Renderer.Validate("Subject", new string('c', length));

        Assert.Equal(expectError, errors.Count > 0);
    }

    [Fact]
    public void Validate_reports_unknown_placeholders_once_in_alphabetical_order()
    {
        var errors = Renderer.Validate(
            "Hello {Zebra}",
            "<p>{Alpha} {Zebra} {RecipientName} {Alpha}</p>");

        Assert.Contains("Unknown placeholders: Alpha, Zebra.", errors);
    }

    [Fact]
    public void Validate_treats_placeholder_names_as_case_sensitive()
    {
        var errors = Renderer.Validate("Subject", "<p>{recipientName}</p>");

        Assert.Contains("Unknown placeholders: recipientName.", errors);
    }

    [Fact]
    public void Validate_ignores_tokens_that_are_not_placeholder_shaped()
    {
        var errors = Renderer.Validate("Subject", "<p>{ spaced } {9lives} {}</p>");

        Assert.Empty(errors);
    }

    [Theory]
    [InlineData("<script>alert(1)</script>")]
    [InlineData("<IFRAME src='x'></IFRAME>")]
    [InlineData("<img src='x'>")]
    [InlineData("<form action='x'></form>")]
    [InlineData("<style>p{color:red}</style>")]
    [InlineData("<div onclick='steal()'>hi</div>")]
    [InlineData("<div ONMOUSEOVER = 'steal()'>hi</div>")]
    [InlineData("<a href='javascript:alert(1)'>go</a>")]
    [InlineData("<div style='background:URL(http://evil)'>hi</div>")]
    public void Validate_rejects_unsafe_markup_in_the_content(string content)
    {
        var errors = Renderer.Validate("Subject", content);

        Assert.Contains("The template contains unsupported or unsafe HTML.", errors);
    }

    [Fact]
    public void Validate_reports_every_independent_problem_together()
    {
        var errors = Renderer.Validate(string.Empty, "<script>{Nope}</script>");

        Assert.Equal(3, errors.Count);
    }

    [Fact]
    public void Render_html_encodes_every_untrusted_value()
    {
        var (subject, content) = Renderer.Render(
            "PO for {VendorName}",
            "<p>Dear {RecipientName}, raised by {RequesterName}.</p>",
            CreatePayload(vendorName: "Acme & <b>Co</b>"),
            recipientName: "O'Brien <admin>",
            requesterName: "Tan & Sons",
            decisionByName: "Lee",
            actionUrl: "https://example.test/po?a=1&b=2");

        Assert.Equal("PO for Acme &amp; &lt;b&gt;Co&lt;/b&gt;", subject);
        Assert.Contains("&lt;admin&gt;", content, StringComparison.Ordinal);
        Assert.Contains("Tan &amp; Sons", content, StringComparison.Ordinal);
        Assert.DoesNotContain("<b>Co</b>", content, StringComparison.Ordinal);
    }

    [Fact]
    public void Render_formats_the_total_as_singapore_dollars()
    {
        var (_, content) = Renderer.Render(
            "Subject",
            "<p>{TotalAmount}</p>",
            CreatePayload(),
            "Recipient",
            "Requester",
            "Approver",
            "https://example.test");

        Assert.Equal("<p>SGD 1,234.50</p>", content);
    }

    [Fact]
    public void Render_substitutes_a_placeholder_for_every_missing_optional_value()
    {
        var (_, content) = Renderer.Render(
            "Subject",
            "<p>{ExpectedDeliveryDate}|{DeliveryAddress}|{DueOn}|{Decision}|{DecisionComment}</p>",
            CreatePayload(),
            "Recipient",
            "Requester",
            "Approver",
            "https://example.test");

        Assert.Equal("<p>Not set|Not set|Not set||</p>", content);
    }

    [Fact]
    public void Render_produces_no_line_item_table_when_the_order_has_no_lines()
    {
        var (_, content) = Renderer.Render(
            "Subject",
            "<div>{LineItemsTable}</div>",
            CreatePayload(lineItemCount: 0),
            "Recipient",
            "Requester",
            "Approver",
            "https://example.test");

        Assert.Equal("<div></div>", content);
    }

    [Fact]
    public void Render_lists_line_items_when_the_order_has_lines()
    {
        var (_, content) = Renderer.Render(
            "Subject",
            "<div>{LineItemsTable}</div>",
            CreatePayload(lineItemCount: 2),
            "Recipient",
            "Requester",
            "Approver",
            "https://example.test");

        Assert.Contains("Item 1</strong>", content, StringComparison.Ordinal);
        Assert.Contains("Item 2</strong>", content, StringComparison.Ordinal);
        Assert.DoesNotContain("line items.", content, StringComparison.Ordinal);
    }

    [Fact]
    public void Render_truncates_the_line_item_table_after_one_hundred_rows()
    {
        var (_, content) = Renderer.Render(
            "Subject",
            "<div>{LineItemsTable}</div>",
            CreatePayload(lineItemCount: 105),
            "Recipient",
            "Requester",
            "Approver",
            "https://example.test");

        Assert.Contains("Item 100</strong>", content, StringComparison.Ordinal);
        Assert.DoesNotContain("Item 101</strong>", content, StringComparison.Ordinal);
        Assert.Contains("remaining 5 line items.", content, StringComparison.Ordinal);
    }

    [Fact]
    public void Render_encodes_line_item_names()
    {
        var payload = CreatePayload(lineItemCount: 1);
        payload.LineItems[0].ItemName = "<b>Widget</b>";

        var (_, content) = Renderer.Render(
            "Subject",
            "<div>{LineItemsTable}</div>",
            payload,
            "Recipient",
            "Requester",
            "Approver",
            "https://example.test");

        Assert.Contains("&lt;b&gt;Widget&lt;/b&gt;", content, StringComparison.Ordinal);
    }

    [Fact]
    public void Render_rejects_a_null_value_map()
    {
        Assert.Throws<ArgumentNullException>(() =>
        {
            _ = Renderer.Render("Subject", "Content", null!);
        });
    }

    [Fact]
    public void Render_rejects_a_value_whose_placeholder_is_not_whitelisted()
    {
        var values = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["RecipientName"] = "Ann",
            ["SecretToken"] = "abc",
        };

        var exception = Assert.Throws<ArgumentException>(() =>
        {
            _ = Renderer.Render("Subject", "Content", values);
        });

        Assert.Equal("values", exception.ParamName);
    }

    [Fact]
    public void Render_rejects_owned_html_whose_placeholder_is_not_whitelisted()
    {
        var owned = new Dictionary<string, string>(StringComparer.Ordinal) { ["RawBlock"] = "<p>ok</p>" };

        var exception = Assert.Throws<ArgumentException>(() =>
        {
            _ = Renderer.Render("Subject", "Content", new Dictionary<string, string>(StringComparer.Ordinal), owned);
        });

        Assert.Equal("ownedHtmlValues", exception.ParamName);
    }

    [Theory]
    [InlineData("<script>alert(1)</script>")]
    [InlineData("<img src='x'>")]
    [InlineData("<div onerror='x'>y</div>")]
    [InlineData("<a href='JavaScript:alert(1)'>y</a>")]
    [InlineData("<div style='background:url(x)'>y</div>")]
    public void Render_rejects_owned_html_that_carries_unsafe_markup(string html)
    {
        var owned = new Dictionary<string, string>(StringComparer.Ordinal) { ["LineItemsTable"] = html };

        var exception = Assert.Throws<ArgumentException>(() =>
        {
            _ = Renderer.Render("Subject", "{LineItemsTable}", new Dictionary<string, string>(StringComparer.Ordinal), owned);
        });

        Assert.Equal("ownedHtmlValues", exception.ParamName);
    }

    [Fact]
    public void Render_encodes_plain_values_but_preserves_owned_html()
    {
        var values = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["RecipientName"] = "<b>Ann</b>",
        };
        var owned = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["LineItemsTable"] = "<table><tr><td>ok</td></tr></table>",
        };

        var (_, content) = Renderer.Render(
            "Subject",
            "<p>{RecipientName}</p>{LineItemsTable}",
            values,
            owned);

        Assert.Equal("<p>&lt;b&gt;Ann&lt;/b&gt;</p><table><tr><td>ok</td></tr></table>", content);
    }

    [Fact]
    public void Render_lets_owned_html_replace_an_encoded_value_for_the_same_placeholder()
    {
        var values = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["LineItemsTable"] = "<b>encoded</b>",
        };
        var owned = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["LineItemsTable"] = "<b>raw</b>",
        };

        var (_, content) = Renderer.Render("Subject", "{LineItemsTable}", values, owned);

        Assert.Equal("<b>raw</b>", content);
    }

    [Fact]
    public void Render_leaves_a_placeholder_untouched_when_no_value_is_supplied()
    {
        var values = new Dictionary<string, string>(StringComparer.Ordinal) { ["VendorName"] = "Acme" };

        var (subject, content) = Renderer.Render("{VendorName}", "{VendorName} {DueOn}", values);

        Assert.Equal("Acme", subject);
        Assert.Equal("Acme {DueOn}", content);
    }

    [Fact]
    public void Render_accepts_an_empty_value_map()
    {
        var (subject, content) = Renderer.Render(
            "Subject",
            "Content",
            new Dictionary<string, string>(StringComparer.Ordinal));

        Assert.Equal("Subject", subject);
        Assert.Equal("Content", content);
    }
}

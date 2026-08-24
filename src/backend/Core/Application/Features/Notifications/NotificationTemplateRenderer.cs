using System.Globalization;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Application.Contracts;
using Application.Features.Notifications;

namespace Application.Features.Notifications;

public sealed partial class NotificationTemplateRenderer : INotificationTemplateRenderer
{
    private static readonly HashSet<string> AllowedPlaceholders =
        NotificationEventCatalog.AllowedPlaceholders.ToHashSet(StringComparer.Ordinal);

    public IReadOnlyList<string> Validate(string subject, string content)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(subject) || subject.Length > 240)
        {
            errors.Add("The subject is required and cannot exceed 240 characters.");
        }

        if (string.IsNullOrWhiteSpace(content) || content.Length > 20000)
        {
            errors.Add("The content is required and cannot exceed 20,000 characters.");
        }

        var unknown = PlaceholderRegex().Matches($"{subject}\n{content}")
            .Select(match => match.Groups[1].Value)
            .Where(token => !AllowedPlaceholders.Contains(token))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(token => token)
            .ToList();
        if (unknown.Count > 0)
        {
            errors.Add($"Unknown placeholders: {string.Join(", ", unknown)}.");
        }

        if (DangerousMarkupRegex().IsMatch(content) ||
            EventAttributeRegex().IsMatch(content) ||
            content.Contains("javascript:", StringComparison.OrdinalIgnoreCase) ||
            content.Contains("url(", StringComparison.OrdinalIgnoreCase))
        {
            errors.Add("The template contains unsupported or unsafe HTML.");
        }

        return errors;
    }

    public (string Subject, string Content) Render(
        string subject,
        string content,
        PurchaseOrderNotificationPayload payload,
        string recipientName,
        string requesterName,
        string decisionByName,
        string actionUrl)
    {
        var replacements = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["RecipientName"] = Encode(recipientName),
            ["ApplicationName"] = Encode(payload.ApplicationName),
            ["PurchaseOrderId"] = Encode(payload.PurchaseOrderId.ToString()),
            ["PurchaseOrderNumber"] = Encode(payload.PurchaseOrderNumber),
            ["RequesterName"] = Encode(requesterName),
            ["VendorName"] = Encode(payload.VendorName),
            ["TotalAmount"] = FormatCurrency(payload.TotalAmount),
            ["CurrentStage"] = Encode(payload.CurrentStage),
            ["Decision"] = Encode(payload.Decision ?? string.Empty),
            ["DecisionBy"] = Encode(decisionByName),
            ["DecisionComment"] = Encode(payload.DecisionComment ?? string.Empty),
            ["SubmittedOn"] = Encode(payload.SubmittedOn.ToString("dd MMM yyyy, h:mm tt")),
            ["ExpectedDeliveryDate"] = Encode(payload.ExpectedDeliveryDate?.ToString("dd MMM yyyy") ?? "Not set"),
            ["DeliveryAddress"] = Encode(payload.DeliveryAddress ?? "Not set"),
            ["DueOn"] = Encode(payload.DueOn?.ToString("dd MMM yyyy, h:mm tt") ?? "Not set"),
            ["ActionUrl"] = Encode(actionUrl),
            ["LineItemsTable"] = BuildLineItemsTable(payload),
        };

        return (
            Replace(subject, replacements),
            Replace(content, replacements));
    }

    public (string Subject, string Content) Render(
        string subject,
        string content,
        IReadOnlyDictionary<string, string> values,
        IReadOnlyDictionary<string, string>? ownedHtmlValues = null)
    {
        ArgumentNullException.ThrowIfNull(values);
        var replacements = values.ToDictionary(
            pair => pair.Key,
            pair => Encode(pair.Value),
            StringComparer.Ordinal);

        foreach (var key in replacements.Keys)
        {
            if (!AllowedPlaceholders.Contains(key))
            {
                throw new ArgumentException($"Unknown placeholder: {key}.", nameof(values));
            }
        }

        foreach (var (key, value) in ownedHtmlValues ??
                 new Dictionary<string, string>(StringComparer.Ordinal))
        {
            if (!AllowedPlaceholders.Contains(key))
            {
                throw new ArgumentException($"Unknown owned HTML placeholder: {key}.", nameof(ownedHtmlValues));
            }

            if (DangerousMarkupRegex().IsMatch(value) ||
                EventAttributeRegex().IsMatch(value) ||
                value.Contains("javascript:", StringComparison.OrdinalIgnoreCase) ||
                value.Contains("url(", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Owned HTML contains unsupported or unsafe markup.", nameof(ownedHtmlValues));
            }

            replacements[key] = value;
        }

        return (Replace(subject, replacements), Replace(content, replacements));
    }

    private static string Replace(
        string input,
        IReadOnlyDictionary<string, string> replacements)
    {
        var result = input;
        foreach (var (key, value) in replacements)
        {
            result = result.Replace($"{{{key}}}", value, StringComparison.Ordinal);
        }

        return result;
    }

    private static string BuildLineItemsTable(PurchaseOrderNotificationPayload payload)
    {
        if (payload.LineItems.Count == 0)
        {
            return string.Empty;
        }

        var rows = new StringBuilder();
        foreach (var lineItem in payload.LineItems.Take(100))
        {
            rows.Append("<tr><td style=\"padding:12px 14px;border-bottom:1px solid #e5e9ef;color:#344054;font-size:13px;line-height:19px;word-break:break-word\">")
                .Append("<strong style=\"display:block;color:#101828;font-weight:600\">")
                .Append(Encode(lineItem.ItemName))
                .Append("</strong>")
                .Append("<span style=\"color:#667085\">Line ")
                .Append(lineItem.LineNumber)
                .Append("</span></td><td style=\"padding:12px 14px;border-bottom:1px solid #e5e9ef;color:#344054;font-size:13px;line-height:19px;text-align:right\">")
                .Append(lineItem.Quantity)
                .Append(string.IsNullOrWhiteSpace(lineItem.UnitOfMeasure)
                    ? string.Empty
                    : $" {Encode(lineItem.UnitOfMeasure)}")
                .Append("</td><td style=\"padding:12px 14px;border-bottom:1px solid #e5e9ef;color:#344054;font-size:13px;line-height:19px;text-align:right\">")
                .Append(FormatCurrency(lineItem.UnitPrice))
                .Append("</td><td style=\"padding:12px 14px;border-bottom:1px solid #e5e9ef;color:#101828;font-size:13px;font-weight:600;line-height:19px;text-align:right\">")
                .Append(FormatCurrency(lineItem.LineTotal))
                .Append("</td></tr>");
        }

        if (payload.LineItems.Count > 100)
        {
            rows.Append("<tr><td colspan=\"4\" style=\"padding:12px 14px;color:#667085;font-size:12px;line-height:18px\">")
                .Append("Open NIE Template to view the remaining ")
                .Append(payload.LineItems.Count - 100)
                .Append(" line items.</td></tr>");
        }

        return
            "<table role=\"presentation\" style=\"width:100%;table-layout:fixed;border-collapse:separate;border-spacing:0;border:1px solid #dfe4eb;border-radius:8px;overflow:hidden\">" +
            "<thead><tr>" +
            "<th width=\"46%\" align=\"left\" style=\"width:46%;padding:10px 14px;background:#f8fafc;border-bottom:1px solid #dfe4eb;color:#475467;font-size:11px;font-weight:700;letter-spacing:.04em;text-transform:uppercase\">Item</th>" +
            "<th width=\"14%\" align=\"right\" style=\"width:14%;padding:10px 14px;background:#f8fafc;border-bottom:1px solid #dfe4eb;color:#475467;font-size:11px;font-weight:700;letter-spacing:.04em;text-transform:uppercase\">Qty</th>" +
            "<th width=\"20%\" align=\"right\" style=\"width:20%;padding:10px 14px;background:#f8fafc;border-bottom:1px solid #dfe4eb;color:#475467;font-size:11px;font-weight:700;letter-spacing:.04em;text-transform:uppercase\">Unit price</th>" +
            "<th width=\"20%\" align=\"right\" style=\"width:20%;padding:10px 14px;background:#f8fafc;border-bottom:1px solid #dfe4eb;color:#475467;font-size:11px;font-weight:700;letter-spacing:.04em;text-transform:uppercase\">Amount</th>" +
            "</tr></thead><tbody>" +
            rows +
            "</tbody></table>";
    }

    private static string FormatCurrency(decimal amount) =>
        $"SGD {amount.ToString("N2", CultureInfo.GetCultureInfo("en-SG"))}";

    private static string Encode(string value) => WebUtility.HtmlEncode(value);

    [GeneratedRegex(@"\{([A-Za-z][A-Za-z0-9]*)\}")]
    private static partial Regex PlaceholderRegex();

    [GeneratedRegex(@"<\s*/?\s*(script|iframe|object|embed|form|style|link|meta|img|svg|video|audio|source|picture)\b", RegexOptions.IgnoreCase)]
    private static partial Regex DangerousMarkupRegex();

    [GeneratedRegex(@"\son[a-z]+\s*=", RegexOptions.IgnoreCase)]
    private static partial Regex EventAttributeRegex();
}

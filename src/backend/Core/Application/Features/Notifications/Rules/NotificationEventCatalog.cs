namespace Application.Features.Notifications;

public static class NotificationChannels
{
    public const string InApp = "InApp";
    public const string Email = "Email";
    public const string Push = "Push";
}

public static class NotificationDeliveryStatuses
{
    public const string Pending = "Pending";
    public const string Retry = "Retry";
    public const string Sent = "Sent";
    public const string Skipped = "Skipped";
    public const string Failed = "Failed";
}

public static class NotificationOutboxStatuses
{
    public const string Pending = "Pending";
    public const string Processing = "Processing";
    public const string Retry = "Retry";
    public const string Processed = "Processed";
    public const string Failed = "Failed";
}

public static class NotificationEventKeys
{
    public const string PurchaseOrderSubmitted = "procurement.purchase-order.submitted";
    public const string ManagerApprovalRequired = "procurement.purchase-order.approval.manager";
    public const string FinanceApprovalRequired = "procurement.purchase-order.approval.finance";
    public const string ProcurementApprovalRequired = "procurement.purchase-order.approval.procurement";
    public const string PurchaseOrderApproved = "procurement.purchase-order.approved";
    public const string PurchaseOrderRejected = "procurement.purchase-order.rejected";
    public const string PurchaseOrderCancelled = "procurement.purchase-order.cancelled";
    public const string ApprovalOverdue = "procurement.purchase-order.approval.overdue";
    public const string DeliveryReminder = "procurement.purchase-order.delivery.reminder";
    public const string VendorUpdated = "procurement.vendor.updated";
    public const string CatalogRefreshed = "procurement.catalog.refreshed";
}

public sealed record NotificationEventDefinition(
    string EventKey,
    string DisplayName,
    string Description,
    string Category,
    bool InAppEnabled,
    bool EmailEnabled,
    bool PushEnabled,
    string DefaultSubject,
    string DefaultContent)
{
    public bool SupportsReminderConfiguration { get; init; }
}

public static class NotificationEventCatalog
{
    public static IReadOnlyList<string> AllowedPlaceholders { get; } =
    [
        "RecipientName", "ApplicationName", "PurchaseOrderId", "PurchaseOrderNumber",
        "RequesterName", "VendorName", "TotalAmount", "CurrentStage", "Decision",
        "DecisionBy", "DecisionComment", "SubmittedOn", "ExpectedDeliveryDate",
        "DeliveryAddress", "DueOn", "ActionUrl", "LineItemsTable"
    ];

    private const string Greeting =
        """<p style="margin:0 0 22px;color:#344054">Dear {RecipientName},</p>""";

    private const string PurchaseOrderDetails =
        """
        <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="margin:24px 0;border:1px solid #dfe4eb;border-radius:8px">
          <tr>
            <td colspan="2" style="padding:11px 14px;background:#f8fafc;border-bottom:1px solid #dfe4eb;color:#475467;font-size:11px;font-weight:700;letter-spacing:.04em;text-transform:uppercase">Purchase order details</td>
          </tr>
          <tr>
            <td style="width:38%;padding:10px 14px;border-bottom:1px solid #eef1f5;color:#667085;font-size:13px">Purchase order</td>
            <td style="padding:10px 14px;border-bottom:1px solid #eef1f5;color:#101828;font-size:13px;font-weight:600">{PurchaseOrderNumber}</td>
          </tr>
          <tr>
            <td style="padding:10px 14px;border-bottom:1px solid #eef1f5;color:#667085;font-size:13px">Requested by</td>
            <td style="padding:10px 14px;border-bottom:1px solid #eef1f5;color:#101828;font-size:13px">{RequesterName}</td>
          </tr>
          <tr>
            <td style="padding:10px 14px;border-bottom:1px solid #eef1f5;color:#667085;font-size:13px">Vendor</td>
            <td style="padding:10px 14px;border-bottom:1px solid #eef1f5;color:#101828;font-size:13px">{VendorName}</td>
          </tr>
          <tr>
            <td style="padding:10px 14px;border-bottom:1px solid #eef1f5;color:#667085;font-size:13px">Total amount</td>
            <td style="padding:10px 14px;border-bottom:1px solid #eef1f5;color:#101828;font-size:13px;font-weight:600">{TotalAmount}</td>
          </tr>
          <tr>
            <td style="padding:10px 14px;border-bottom:1px solid #eef1f5;color:#667085;font-size:13px">Current stage</td>
            <td style="padding:10px 14px;border-bottom:1px solid #eef1f5;color:#101828;font-size:13px">{CurrentStage}</td>
          </tr>
          <tr>
            <td style="padding:10px 14px;border-bottom:1px solid #eef1f5;color:#667085;font-size:13px">Submitted</td>
            <td style="padding:10px 14px;border-bottom:1px solid #eef1f5;color:#101828;font-size:13px">{SubmittedOn}</td>
          </tr>
          <tr>
            <td style="padding:10px 14px;border-bottom:1px solid #eef1f5;color:#667085;font-size:13px">Expected delivery</td>
            <td style="padding:10px 14px;border-bottom:1px solid #eef1f5;color:#101828;font-size:13px">{ExpectedDeliveryDate}</td>
          </tr>
          <tr>
            <td style="padding:10px 14px;color:#667085;font-size:13px">Delivery address</td>
            <td style="padding:10px 14px;color:#101828;font-size:13px">{DeliveryAddress}</td>
          </tr>
        </table>
        """;

    private const string LineItems =
        """
        <h2 style="margin:28px 0 10px;color:#101828;font-size:15px;line-height:22px">Line items</h2>
        {LineItemsTable}
        """;

    private const string PurchaseOrderAction =
        """
        <table role="presentation" cellpadding="0" cellspacing="0" border="0" style="margin:28px 0 0">
          <tr>
            <td style="border-radius:6px;background:#0055a5">
              <a href="{ActionUrl}" style="display:inline-block;padding:11px 18px;color:#ffffff;font-size:14px;font-weight:700;line-height:20px;text-decoration:none">Open purchase order</a>
            </td>
          </tr>
        </table>
        <p style="margin:14px 0 0;color:#667085;font-size:12px;line-height:18px">Reference: purchase order {PurchaseOrderNumber}</p>
        """;

    private const string GeneralAction =
        """
        <table role="presentation" cellpadding="0" cellspacing="0" border="0" style="margin:28px 0 0">
          <tr>
            <td style="border-radius:6px;background:#0055a5">
              <a href="{ActionUrl}" style="display:inline-block;padding:11px 18px;color:#ffffff;font-size:14px;font-weight:700;line-height:20px;text-decoration:none">Open NIE Template</a>
            </td>
          </tr>
        </table>
        """;

    private const string SignOff =
        """
        <p style="margin:28px 0 0;color:#475467">Regards,<br><strong style="color:#344054">NIE Template</strong></p>
        """;

    private static string ComposePurchaseOrder(
        string heading,
        string message,
        bool showLineItems = true,
        bool showAction = true)
    {
        return Greeting +
            $"<h1 style=\"margin:0 0 12px;color:#101828;font-size:22px;line-height:30px;font-weight:700\">{heading}</h1>" +
            $"<p style=\"margin:0;color:#475467;line-height:22px\">{message}</p>" +
            PurchaseOrderDetails +
            (showLineItems ? LineItems : string.Empty) +
            (showAction ? PurchaseOrderAction : string.Empty) +
            SignOff;
    }

    private static string ComposeGeneral(string heading, string message)
    {
        return Greeting +
            $"<h1 style=\"margin:0 0 12px;color:#101828;font-size:22px;line-height:30px;font-weight:700\">{heading}</h1>" +
            $"<p style=\"margin:0;color:#475467;line-height:22px\">{message}</p>" +
            GeneralAction +
            SignOff;
    }

    public static IReadOnlyList<NotificationEventDefinition> Events { get; } =
    [
        new(NotificationEventKeys.PurchaseOrderSubmitted,
            "Purchase order submitted",
            "Confirms to the requester that a purchase order entered the approval workflow.",
            "Order updates", true, true, false,
            "Purchase order submitted — {PurchaseOrderNumber}",
            ComposePurchaseOrder(
                "Purchase order submitted",
                "Your purchase order has been submitted successfully and is awaiting Manager Review.")),
        new(NotificationEventKeys.ManagerApprovalRequired,
            "Manager approval required",
            "A submitted purchase order is ready for Manager Review.",
            "Approval tasks", true, true, true,
            "Manager approval required — {PurchaseOrderNumber}",
            ComposePurchaseOrder(
                "Manager approval required",
                "{RequesterName} submitted this purchase order. Please review the business need, vendor, line items, delivery details, and total amount before recording your decision.")),
        new(NotificationEventKeys.FinanceApprovalRequired,
            "Finance approval required",
            "Manager Review passed and the purchase order is ready for Finance Review.",
            "Approval tasks", true, true, true,
            "Finance approval required — {PurchaseOrderNumber}",
            ComposePurchaseOrder(
                "Finance approval required",
                "Manager Review was approved by {DecisionBy}. Please verify the budget, account treatment, and financial controls before recording your decision.")),
        new(NotificationEventKeys.ProcurementApprovalRequired,
            "Procurement approval required",
            "Finance Review passed and the purchase order is ready for Procurement Review.",
            "Approval tasks", true, true, true,
            "Procurement approval required — {PurchaseOrderNumber}",
            ComposePurchaseOrder(
                "Procurement approval required",
                "Finance Review was approved by {DecisionBy}. Please complete the sourcing, vendor, and procurement compliance review before recording the final decision.")),
        new(NotificationEventKeys.PurchaseOrderApproved,
            "Purchase order approved",
            "All required approval stages passed.",
            "Approval decisions", true, true, true,
            "Purchase order approved — {PurchaseOrderNumber}",
            ComposePurchaseOrder(
                "Purchase order approved",
                "All required approval stages are complete. The purchase order was approved by {DecisionBy} and is ready for the next procurement action.")),
        new(NotificationEventKeys.PurchaseOrderRejected,
            "Purchase order rejected",
            "An approver rejected a purchase order.",
            "Approval decisions", true, true, true,
            "Purchase order rejected — {PurchaseOrderNumber}",
            ComposePurchaseOrder(
                "Purchase order rejected",
                "The purchase order was rejected during {CurrentStage} by {DecisionBy}.<br><br><strong style=\"color:#344054\">Decision comments</strong><br>{DecisionComment}")),
        new(NotificationEventKeys.PurchaseOrderCancelled,
            "Purchase order cancelled",
            "A purchase order was cancelled before completion.",
            "Order updates", true, true, false,
            "Purchase order cancelled — {PurchaseOrderNumber}",
            ComposePurchaseOrder(
                "Purchase order cancelled",
                "The purchase order was cancelled by {DecisionBy}.<br><br><strong style=\"color:#344054\">Comments</strong><br>{DecisionComment}",
                showLineItems: false,
                showAction: false)),
        new(NotificationEventKeys.ApprovalOverdue,
            "Purchase order approval overdue",
            "A purchase order remained in an approval stage beyond its configured service level.",
            "Approval reminders", true, true, true,
            "Approval overdue — {PurchaseOrderNumber}",
            ComposePurchaseOrder(
                "Purchase order approval is overdue",
                "This purchase order has remained at {CurrentStage} beyond the review due date of {DueOn}. Please review it as soon as practical."))
        {
            SupportsReminderConfiguration = true,
        },
        new(NotificationEventKeys.DeliveryReminder,
            "Expected delivery reminder",
            "Reminds the requester that an approved purchase order is approaching its expected delivery date.",
            "Order updates", true, true, false,
            "Expected delivery approaching — {PurchaseOrderNumber}",
            ComposePurchaseOrder(
                "Expected delivery approaching",
                "The expected delivery date is {ExpectedDeliveryDate}. Please confirm receipt, record any delivery issue, and retain the supporting documents.",
                showLineItems: true,
                showAction: true))
        {
            SupportsReminderConfiguration = true,
        },
        new(NotificationEventKeys.VendorUpdated,
            "Vendor profile updated",
            "A vendor profile or availability status changed.",
            "Catalog & vendors", true, true, false,
            "Vendor profile updated — {VendorName}",
            ComposeGeneral(
                "Vendor profile updated",
                "The profile for {VendorName} was updated by {DecisionBy}. Open NIE Template to review the current vendor details and availability.")),
        new(NotificationEventKeys.CatalogRefreshed,
            "Procurement catalog refreshed",
            "Catalog items, prices, or availability were refreshed.",
            "Catalog & vendors", true, true, false,
            "Procurement catalog refreshed — {ApplicationName}",
            ComposeGeneral(
                "Procurement catalog refreshed",
                "The procurement catalog was refreshed by {DecisionBy}. Open NIE Template to review current items, prices, vendors, and availability."))
    ];

    public static NotificationEventDefinition? Find(string eventKey) =>
        Events.FirstOrDefault(item =>
            item.EventKey.Equals(eventKey, StringComparison.OrdinalIgnoreCase));
}

namespace Application.Contracts;

public sealed class NotificationPolicyDto
{
    public Guid Id { get; set; }
    public string EventKey { get; set; } = default!;
    public string DisplayName { get; set; } = default!;
    public string Description { get; set; } = default!;
    public string Category { get; set; } = default!;
    public bool InAppEnabled { get; set; }
    public bool EmailEnabled { get; set; }
    public bool PushEnabled { get; set; }
    public bool IsActive { get; set; }
    public bool SupportsReminderConfiguration { get; set; }
    public int? ReminderAfterHours { get; set; }
    public int? EscalationAfterHours { get; set; }
}

public sealed class UpdateNotificationPolicyDto
{
    public bool InAppEnabled { get; set; }
    public bool EmailEnabled { get; set; }
    public bool PushEnabled { get; set; }
    public bool IsActive { get; set; }
    public int? ReminderAfterHours { get; set; }
    public int? EscalationAfterHours { get; set; }
}

public sealed class NotificationTemplateDto
{
    public Guid Id { get; set; }
    public string EventKey { get; set; } = default!;
    public string Channel { get; set; } = default!;
    public int Version { get; set; }
    public string Subject { get; set; } = default!;
    public string Content { get; set; } = default!;
    public bool IsPublished { get; set; }
    public string? PublishedBy { get; set; }
    public DateTime? PublishedOn { get; set; }
}

public sealed class SaveNotificationTemplateDto
{
    public string EventKey { get; set; } = default!;
    public string Subject { get; set; } = default!;
    public string Content { get; set; } = default!;
    public bool Publish { get; set; } = true;
}

public sealed class NotificationDeliveryDto
{
    public Guid Id { get; set; }
    public string EventKey { get; set; } = default!;
    public string CorrelationKey { get; set; } = default!;
    public string RecipientUserId { get; set; } = default!;
    public string? RecipientName { get; set; }
    public string? RecipientEmail { get; set; }
    public string Channel { get; set; } = default!;
    public string Status { get; set; } = default!;
    public int Attempts { get; set; }
    public DateTime? SentOn { get; set; }
    public DateTime? NextAttemptOn { get; set; }
    public string? LastError { get; set; }
    public DateTime? CreatedOn { get; set; }
}

public sealed class NotificationChannelHealthDto
{
    public bool EmailConfigured { get; set; }
    public bool PushNotificationsConfigured { get; set; }
    public bool RealtimeConfigured { get; set; } = true;
}

public sealed class NotificationAdministrationOverviewDto
{
    public List<NotificationPolicyDto> Policies { get; set; } = [];
    public List<NotificationTemplateDto> Templates { get; set; } = [];
    public List<NotificationDeliveryDto> RecentDeliveries { get; set; } = [];
    public Dictionary<string, int> DeliveryStatusCounts { get; set; } = [];
    public NotificationChannelHealthDto ChannelHealth { get; set; } = new();
    public IReadOnlyList<string> AllowedPlaceholders { get; set; } = [];
}

public sealed class UserNotificationPreferencesDto
{
    public bool DesktopAlerts { get; set; } = true;
    public bool ApprovalTasksPush { get; set; } = true;
    public bool ApprovalDecisionsPush { get; set; } = true;
    public bool OrderUpdatesPush { get; set; } = true;
    public bool SystemAlertsPush { get; set; } = true;
}

public sealed class TestNotificationDto
{
    public string Channel { get; set; } = default!;
    public string? Email { get; set; }
}

public sealed class PurchaseOrderNotificationPayload
{
    public Guid PurchaseOrderId { get; set; }
    public Guid ApplicationId { get; set; }
    public string ApplicationName { get; set; } = default!;
    public string PurchaseOrderNumber { get; set; } = default!;
    public string RequestedBy { get; set; } = default!;
    public string VendorName { get; set; } = default!;
    public decimal TotalAmount { get; set; }
    public string CurrentStage { get; set; } = default!;
    public string ActorUserId { get; set; } = default!;
    public string? Decision { get; set; }
    public string? DecisionComment { get; set; }
    public DateTime SubmittedOn { get; set; }
    public DateTime? ExpectedDeliveryDate { get; set; }
    public string? DeliveryAddress { get; set; }
    public DateTime? DueOn { get; set; }
    public List<PurchaseOrderLineNotificationDto> LineItems { get; set; } = [];
}

public sealed class PurchaseOrderLineNotificationDto
{
    public int LineNumber { get; set; }
    public string ItemName { get; set; } = default!;
    public int Quantity { get; set; }
    public string? UnitOfMeasure { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
}

public sealed class NotificationRecipientDto
{
    public string UserId { get; set; } = default!;
    public string? Name { get; set; }
    public string? Email { get; set; }
    public bool PushEnabled { get; set; } = true;
}

public sealed class NotificationEnqueueRequest
{
    public string EventKey { get; set; } = default!;
    public string CorrelationKey { get; set; } = default!;
    public Guid? ApplicationId { get; set; }
    public string ActorUserId { get; set; } = default!;
    public string DedupeKey { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string Message { get; set; } = default!;
    public string Type { get; set; } = "SystemAlert";
    public string? Link { get; set; }
    public Dictionary<string, string> TemplateValues { get; set; } = new(StringComparer.Ordinal);
    public List<NotificationRecipientDto> Recipients { get; set; } = [];
}

public sealed class NotificationOutboxPayloadDto
{
    public string Title { get; set; } = default!;
    public string Message { get; set; } = default!;
    public string Type { get; set; } = default!;
    public string? Link { get; set; }
    public Dictionary<string, string> TemplateValues { get; set; } = new(StringComparer.Ordinal);
    public List<NotificationRecipientDto> Recipients { get; set; } = [];
}

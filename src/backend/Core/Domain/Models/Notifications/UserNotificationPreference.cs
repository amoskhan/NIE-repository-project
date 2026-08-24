namespace Domain.Models;

/// <summary>
/// Server-backed user preferences. Workflow in-app messages remain mandatory;
/// approval-task email is policy-mandatory; these preferences govern
/// supplementary browser/push behaviour.
/// </summary>
public class UserNotificationPreference : TimestampedEntity
{
    public string UserId { get; set; } = default!;
    public bool DesktopAlerts { get; set; } = true;
    public bool ApprovalTasksPush { get; set; } = true;
    public bool ApprovalDecisionsPush { get; set; } = true;
    public bool OrderUpdatesPush { get; set; } = true;
    public bool SystemAlertsPush { get; set; } = true;
}

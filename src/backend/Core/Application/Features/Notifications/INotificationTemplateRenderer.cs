using Application.Contracts;

namespace Application.Features.Notifications;

public interface INotificationTemplateRenderer
{
    IReadOnlyList<string> Validate(string subject, string content);

    (string Subject, string Content) Render(
        string subject,
        string content,
        PurchaseOrderNotificationPayload payload,
        string recipientName,
        string requesterName,
        string decisionByName,
        string actionUrl);

    (string Subject, string Content) Render(
        string subject,
        string content,
        IReadOnlyDictionary<string, string> values,
        IReadOnlyDictionary<string, string>? ownedHtmlValues = null);
}

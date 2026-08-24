namespace Application.Features.PushNotification;

public interface IPushNotificationService
{
    /// <summary>
    /// Send a push notification to specific user(s) by their external user IDs.
    /// </summary>
    Task SendToUsersAsync(IEnumerable<string> externalUserIds, string title, string message, string? url = null, IDictionary<string, string>? data = null);

    /// <summary>
    /// Send a push notification to all subscribed users.
    /// </summary>
    Task SendToAllAsync(string title, string message, string? url = null, IDictionary<string, string>? data = null);

    /// <summary>
    /// Send a push notification to users matching specific tags/segments.
    /// </summary>
    Task SendToSegmentAsync(string segment, string title, string message, string? url = null, IDictionary<string, string>? data = null);
}

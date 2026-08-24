using Application.Contracts;

namespace Application.Features.Notifications;

public interface INotificationOutboxService
{
    /// <summary>
    /// Adds an idempotent notification event to the caller's DbContext transaction.
    /// The caller remains responsible for committing the domain state and outbox row together.
    /// </summary>
    Task<bool> EnqueueAsync(
        NotificationEnqueueRequest request,
        CancellationToken cancellationToken = default);
}

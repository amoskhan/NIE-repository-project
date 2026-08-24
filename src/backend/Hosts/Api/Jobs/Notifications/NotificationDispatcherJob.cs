using System.Text.Json;
using Api.Hubs;
using Application.Contracts;
using Application.Features.Email;
using Application.Features.Notifications;
using Application.Features.PushNotification;
using BuildingBlocks.Helpers;
using Domain.Models;
using Infrastructure.Options;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TickerQ.Utilities.Base;

namespace Api.Jobs;

/// <summary>Dispatches durable notification events with isolated channel retries.</summary>
public sealed class NotificationDispatcherJob
{
    private static readonly int[] RetryMinutes = [1, 5, 15, 60, 240];

    private readonly MainDbContext _context;
    private readonly IEmailService _email;
    private readonly IPushNotificationService _push;
    private readonly INotificationTemplateRenderer _renderer;
    private readonly IHubContext<NotificationHub> _hub;
    private readonly EmailSettings _emailSettings;
    private readonly OneSignalSettings _oneSignalSettings;
    private readonly NotificationSettings _settings;
    private readonly ILogger<NotificationDispatcherJob> _logger;

    public NotificationDispatcherJob(
        MainDbContext context,
        IEmailService email,
        IPushNotificationService push,
        INotificationTemplateRenderer renderer,
        IHubContext<NotificationHub> hub,
        IOptions<EmailSettings> emailSettings,
        IOptions<OneSignalSettings> oneSignalSettings,
        IOptions<NotificationSettings> settings,
        ILogger<NotificationDispatcherJob> logger)
    {
        _context = context;
        _email = email;
        _push = push;
        _renderer = renderer;
        _hub = hub;
        _emailSettings = emailSettings.Value;
        _oneSignalSettings = oneSignalSettings.Value;
        _settings = settings.Value;
        _logger = logger;
    }

    [TickerFunction("NotificationOutboxDispatcher", cronExpression: "0 * * * * *")]
    public async Task ExecuteAsync(
        TickerFunctionContext context,
        CancellationToken cancellationToken)
    {
        context.CronOccurrenceOperations?.SkipIfAlreadyRunning();
        var now = DateTimeHelper.Now;
        var items = await _context.NotificationOutboxes
            .Include(item => item.Deliveries)
            .Where(item =>
                (item.Status == NotificationOutboxStatuses.Pending ||
                 item.Status == NotificationOutboxStatuses.Retry ||
                 item.Status == NotificationOutboxStatuses.Processing) &&
                (item.NextAttemptOn == null || item.NextAttemptOn <= now))
            .OrderBy(item => item.OccurredOn)
            .Take(_settings.DispatchBatchSize)
            .ToListAsync(cancellationToken);

        foreach (var item in items)
        {
            await ProcessAsync(item, cancellationToken);
        }
    }

    private async Task ProcessAsync(
        NotificationOutbox item,
        CancellationToken cancellationToken)
    {
        var policy = await _context.NotificationPolicies
            .AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.EventKey == item.EventKey, cancellationToken);
        if (policy is null || !policy.IsActive)
        {
            item.Status = NotificationOutboxStatuses.Processed;
            item.ProcessedOn = DateTimeHelper.Now;
            item.LastError = policy is null
                ? "No notification policy exists for this event."
                : "Notification policy is inactive.";
            await _context.SaveChangesAsync(cancellationToken);
            return;
        }

        NotificationOutboxPayloadDto? payload;
        try
        {
            payload = JsonSerializer.Deserialize<NotificationOutboxPayloadDto>(item.PayloadJson);
        }
        catch (JsonException exception)
        {
            await MarkOutboxFailedAsync(item, "Notification payload is invalid.", cancellationToken);
            _logger.LogError(exception, "Invalid payload for notification outbox {OutboxId}", item.Id);
            return;
        }

        if (payload is null || payload.Recipients.Count == 0)
        {
            await MarkOutboxFailedAsync(item, "Notification payload has no recipients.", cancellationToken);
            return;
        }

        item.Status = NotificationOutboxStatuses.Processing;
        item.Attempts++;
        EnsureDeliveries(item, policy, payload);
        await _context.SaveChangesAsync(cancellationToken);

        foreach (var delivery in item.Deliveries
                     .Where(candidate => candidate.Status is NotificationDeliveryStatuses.Pending or NotificationDeliveryStatuses.Retry)
                     .Where(candidate => candidate.NextAttemptOn == null || candidate.NextAttemptOn <= DateTimeHelper.Now)
                     .OrderBy(candidate => candidate.CreatedOn))
        {
            await DeliverAsync(item, delivery, payload, cancellationToken);
        }

        var waiting = item.Deliveries
            .Where(candidate => candidate.Status is NotificationDeliveryStatuses.Pending or NotificationDeliveryStatuses.Retry)
            .OrderBy(candidate => candidate.NextAttemptOn)
            .ToList();
        item.Status = waiting.Count == 0
            ? NotificationOutboxStatuses.Processed
            : NotificationOutboxStatuses.Retry;
        item.NextAttemptOn = waiting.FirstOrDefault()?.NextAttemptOn;
        item.ProcessedOn = waiting.Count == 0 ? DateTimeHelper.Now : null;
        item.LastError = waiting.Count == 0
            ? null
            : "One or more channel deliveries are waiting to retry.";
        await _context.SaveChangesAsync(cancellationToken);
    }

    private static void EnsureDeliveries(
        NotificationOutbox item,
        NotificationPolicy policy,
        NotificationOutboxPayloadDto payload)
    {
        if (item.Deliveries.Count > 0)
        {
            return;
        }

        foreach (var recipient in payload.Recipients
                     .GroupBy(candidate => candidate.UserId, StringComparer.Ordinal)
                     .Select(group => group.First()))
        {
            if (policy.InAppEnabled)
            {
                item.Deliveries.Add(CreateDelivery(recipient, NotificationChannels.InApp));
            }
            if (policy.EmailEnabled)
            {
                item.Deliveries.Add(CreateDelivery(recipient, NotificationChannels.Email));
            }
            if (policy.PushEnabled && recipient.PushEnabled)
            {
                item.Deliveries.Add(CreateDelivery(recipient, NotificationChannels.Push));
            }
        }
    }

    private async Task DeliverAsync(
        NotificationOutbox item,
        NotificationDelivery delivery,
        NotificationOutboxPayloadDto payload,
        CancellationToken cancellationToken)
    {
        delivery.Attempts++;
        try
        {
            switch (delivery.Channel)
            {
                case NotificationChannels.InApp:
                    await DeliverInAppAsync(item, delivery, payload, cancellationToken);
                    break;
                case NotificationChannels.Email:
                    await DeliverEmailAsync(item, delivery, payload, cancellationToken);
                    break;
                case NotificationChannels.Push:
                    await DeliverPushAsync(delivery, payload);
                    break;
                default:
                    MarkSkipped(delivery, "Unsupported notification channel.");
                    break;
            }
        }
        catch (Exception exception)
        {
            ScheduleRetry(delivery);
            _logger.LogError(
                exception,
                "Notification delivery {DeliveryId} failed on {Channel}",
                delivery.Id,
                delivery.Channel);
        }
        finally
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task DeliverInAppAsync(
        NotificationOutbox item,
        NotificationDelivery delivery,
        NotificationOutboxPayloadDto payload,
        CancellationToken cancellationToken)
    {
        var dedupeKey = $"{item.DedupeKey}:{delivery.RecipientUserId}:{NotificationChannels.InApp}";
        var notification = await _context.Notifications
            .SingleOrDefaultAsync(candidate => candidate.DedupeKey == dedupeKey, cancellationToken);
        if (notification is null)
        {
            notification = new Notification
            {
                RecipientUserId = delivery.RecipientUserId,
                RecipientName = delivery.RecipientName,
                Title = payload.Title,
                Message = payload.Message,
                Type = payload.Type,
                Link = payload.Link,
                EventKey = item.EventKey,
                CorrelationKey = item.CorrelationKey,
                DedupeKey = dedupeKey,
                CreatedOn = DateTimeHelper.Now,
            };
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync(cancellationToken);
        }

        MarkSent(delivery);
        await _hub.Clients
            .Group(NotificationHub.UserGroup(delivery.RecipientUserId))
            .SendAsync("ReceiveNotification", new
            {
                notification.Id,
                recipientType = "User",
                notification.RecipientUserId,
                notification.RecipientName,
                notification.Title,
                notification.Message,
                notification.Type,
                notification.IsRead,
                notification.ReadAt,
                notification.Link,
                notification.EventKey,
                notification.CorrelationKey,
                notification.CreatedOn,
            }, cancellationToken);
    }

    private async Task DeliverEmailAsync(
        NotificationOutbox item,
        NotificationDelivery delivery,
        NotificationOutboxPayloadDto payload,
        CancellationToken cancellationToken)
    {
        if (!IsEmailConfigured())
        {
            MarkSkipped(delivery, "SMTP is not configured.");
            return;
        }
        if (string.IsNullOrWhiteSpace(delivery.RecipientEmail))
        {
            MarkSkipped(delivery, "The recipient has no email address.");
            return;
        }

        var template = await _context.NotificationTemplates
            .AsNoTracking()
            .Where(candidate =>
                candidate.EventKey == item.EventKey &&
                candidate.Channel == NotificationChannels.Email &&
                candidate.IsPublished)
            .OrderByDescending(candidate => candidate.Version)
            .FirstOrDefaultAsync(cancellationToken);
        if (template is null)
        {
            MarkSkipped(delivery, "No published email template exists.");
            return;
        }

        var values = NotificationEventCatalog.AllowedPlaceholders
            .ToDictionary(key => key, _ => string.Empty, StringComparer.Ordinal);
        foreach (var (key, value) in payload.TemplateValues)
        {
            if (values.ContainsKey(key))
            {
                values[key] = value;
            }
        }
        values["RecipientName"] = delivery.RecipientName ?? delivery.RecipientUserId;
        values["ActionUrl"] = payload.Link ?? string.Empty;

        var rendered = _renderer.Render(template.Subject, template.Content, values);
        await _email.SendBaseTemplatedEmailAsync(
            delivery.RecipientEmail,
            rendered.Subject,
            rendered.Content,
            delivery.RecipientName);
        MarkSent(delivery);
    }

    private async Task DeliverPushAsync(
        NotificationDelivery delivery,
        NotificationOutboxPayloadDto payload)
    {
        if (string.IsNullOrWhiteSpace(_oneSignalSettings.AppId) ||
            string.IsNullOrWhiteSpace(_oneSignalSettings.RestApiKey))
        {
            MarkSkipped(delivery, "Push notifications are not configured.");
            return;
        }

        await _push.SendToUsersAsync(
            [delivery.RecipientUserId],
            payload.Title,
            payload.Message,
            payload.Link);
        MarkSent(delivery);
    }

    private void ScheduleRetry(NotificationDelivery delivery)
    {
        if (delivery.Attempts >= _settings.MaxDeliveryAttempts)
        {
            delivery.Status = NotificationDeliveryStatuses.Failed;
            delivery.NextAttemptOn = null;
            delivery.LastError = "Delivery failed after the configured maximum attempts.";
            return;
        }

        delivery.Status = NotificationDeliveryStatuses.Retry;
        delivery.NextAttemptOn = DateTimeHelper.Now.AddMinutes(
            RetryMinutes[Math.Min(delivery.Attempts - 1, RetryMinutes.Length - 1)]);
        delivery.LastError = "Delivery attempt failed and is queued for retry.";
    }

    private static NotificationDelivery CreateDelivery(
        NotificationRecipientDto recipient,
        string channel) =>
        new()
        {
            RecipientUserId = recipient.UserId,
            RecipientName = recipient.Name,
            RecipientEmail = recipient.Email,
            Channel = channel,
            Status = NotificationDeliveryStatuses.Pending,
            NextAttemptOn = DateTimeHelper.Now,
        };

    private static void MarkSent(NotificationDelivery delivery)
    {
        delivery.Status = NotificationDeliveryStatuses.Sent;
        delivery.SentOn = DateTimeHelper.Now;
        delivery.NextAttemptOn = null;
        delivery.LastError = null;
    }

    private static void MarkSkipped(NotificationDelivery delivery, string reason)
    {
        delivery.Status = NotificationDeliveryStatuses.Skipped;
        delivery.NextAttemptOn = null;
        delivery.LastError = reason;
    }

    private async Task MarkOutboxFailedAsync(
        NotificationOutbox item,
        string reason,
        CancellationToken cancellationToken)
    {
        item.Status = NotificationOutboxStatuses.Failed;
        item.ProcessedOn = DateTimeHelper.Now;
        item.NextAttemptOn = null;
        item.LastError = reason;
        await _context.SaveChangesAsync(cancellationToken);
    }

    private bool IsEmailConfigured() =>
        !string.IsNullOrWhiteSpace(_emailSettings.SmtpHost) &&
        (!string.IsNullOrWhiteSpace(_emailSettings.SenderEmail) ||
         !string.IsNullOrWhiteSpace(_emailSettings.AppName));
}

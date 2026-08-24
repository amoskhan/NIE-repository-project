using System.Text.Json;
using Application.Abstractions;
using Application.Contracts;
using Application.Features.Notifications;
using BuildingBlocks.Helpers;
using Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Notifications;

public sealed class NotificationOutboxService : INotificationOutboxService
{
    private readonly IApplicationDbContext _context;

    public NotificationOutboxService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> EnqueueAsync(
        NotificationEnqueueRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.EventKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.CorrelationKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.ActorUserId);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.DedupeKey);

        if (NotificationEventCatalog.Find(request.EventKey) is null)
        {
            throw new ArgumentException("The notification event is not registered.", nameof(request));
        }

        if (request.Recipients.Count == 0 ||
            request.Recipients.Any(recipient => string.IsNullOrWhiteSpace(recipient.UserId)))
        {
            throw new ArgumentException("At least one recipient with a user identifier is required.", nameof(request));
        }

        var duplicateInUnitOfWork = _context.NotificationOutboxes.Local
            .Any(item => item.DedupeKey == request.DedupeKey);
        var duplicateInDatabase = await _context.NotificationOutboxes
            .AsNoTracking()
            .AnyAsync(item => item.DedupeKey == request.DedupeKey, cancellationToken);
        if (duplicateInUnitOfWork || duplicateInDatabase)
        {
            return false;
        }

        var payload = new NotificationOutboxPayloadDto
        {
            Title = request.Title,
            Message = request.Message,
            Type = request.Type,
            Link = request.Link,
            TemplateValues = new Dictionary<string, string>(request.TemplateValues, StringComparer.Ordinal),
            Recipients = request.Recipients
                .GroupBy(recipient => recipient.UserId, StringComparer.Ordinal)
                .Select(group => group.First())
                .ToList(),
        };

        _context.NotificationOutboxes.Add(new NotificationOutbox
        {
            EventKey = request.EventKey,
            CorrelationKey = request.CorrelationKey,
            ApplicationId = request.ApplicationId,
            ActorUserId = request.ActorUserId,
            PayloadJson = JsonSerializer.Serialize(payload),
            Status = NotificationOutboxStatuses.Pending,
            OccurredOn = DateTimeHelper.Now,
            NextAttemptOn = DateTimeHelper.Now,
            DedupeKey = request.DedupeKey,
        });
        return true;
    }
}

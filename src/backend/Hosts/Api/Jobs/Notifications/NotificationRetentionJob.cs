using Application.Features.Notifications;
using BuildingBlocks.Helpers;
using Infrastructure.Options;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TickerQ.Utilities.Base;

namespace Api.Jobs;

/// <summary>Removes notification delivery metadata after the approved retention window.</summary>
public sealed class NotificationRetentionJob
{
    private readonly MainDbContext _context;
    private readonly NotificationSettings _settings;

    public NotificationRetentionJob(
        MainDbContext context,
        IOptions<NotificationSettings> settings)
    {
        _context = context;
        _settings = settings.Value;
    }

    [TickerFunction("NotificationRetention", cronExpression: "0 15 2 * * *")]
    public async Task ExecuteAsync(
        TickerFunctionContext context,
        CancellationToken cancellationToken)
    {
        context.CronOccurrenceOperations?.SkipIfAlreadyRunning();
        var cutoff = DateTimeHelper.Now.AddDays(-_settings.RetentionDays);

        await _context.NotificationOutboxes
            .Where(item =>
                item.OccurredOn < cutoff &&
                (item.Status == NotificationOutboxStatuses.Processed ||
                 item.Status == NotificationOutboxStatuses.Failed))
            .ExecuteDeleteAsync(cancellationToken);

        await _context.Notifications
            .Where(item => item.CreatedOn < cutoff)
            .ExecuteDeleteAsync(cancellationToken);
    }
}

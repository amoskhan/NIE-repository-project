using Application.Integration;
using Infrastructure.Integration.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Integration.RabbitMq;

public sealed class IntegrationMessageRetentionWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<ServiceIntegrationOptions> options,
    ILogger<IntegrationMessageRetentionWorker> logger) : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly ServiceIntegrationOptions _options = options.Value;
    private readonly ILogger<IntegrationMessageRetentionWorker> _logger = logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled || !_options.RabbitMq.Enabled)
        {
            return;
        }

        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(_options.Outbox.RetentionSweepMinutes));
        do
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var store = scope.ServiceProvider.GetRequiredService<IIntegrationMessageRetentionStore>();
                var now = DateTimeOffset.UtcNow;
                var result = await store.PruneAsync(
                    now.AddDays(-_options.Outbox.PublishedRetentionDays),
                    now.AddDays(-_options.Outbox.InboxRetentionDays),
                    _options.Outbox.RetentionBatchSize,
                    stoppingToken);
                if (result.PublishedOutboxMessagesDeleted > 0 || result.InboxReceiptsDeleted > 0)
                {
                    _logger.LogInformation(
                        "Pruned {OutboxCount} published integration outbox rows and {InboxCount} inbox receipts",
                        result.PublishedOutboxMessagesDeleted,
                        result.InboxReceiptsDeleted);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Integration message-retention sweep failed");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}

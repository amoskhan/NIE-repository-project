using Application.Integration;
using Infrastructure.Integration.Observability;
using Infrastructure.Integration.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Integration.RabbitMq;

public sealed class IntegrationOutboxPublisherWorker(
    IServiceScopeFactory scopeFactory,
    IIntegrationEventTransport transport,
    IOptions<ServiceIntegrationOptions> options,
    ILogger<IntegrationOutboxPublisherWorker> logger) : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly IIntegrationEventTransport _transport = transport;
    private readonly ServiceIntegrationOptions _options = options.Value;
    private readonly ILogger<IntegrationOutboxPublisherWorker> _logger = logger;
    private DateTimeOffset _nextMetricsSampleUtc = DateTimeOffset.MinValue;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled || !_options.RabbitMq.Enabled)
        {
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var publishedAny = await PublishBatchAsync(stoppingToken);
                if (!publishedAny)
                {
                    await Task.Delay(_options.Outbox.PollIntervalMilliseconds, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Integration outbox polling failed; the batch will be retried");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    private async Task<bool> PublishBatchAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<IIntegrationOutboxStore>();
        if (DateTimeOffset.UtcNow >= _nextMetricsSampleUtc)
        {
            ServiceIntegrationTelemetry.SetOutboxStatistics(
                await store.GetPendingStatisticsAsync(cancellationToken));
            _nextMetricsSampleUtc = DateTimeOffset.UtcNow.AddSeconds(
                _options.Outbox.MetricsSampleSeconds);
        }

        var messages = await store.ClaimAsync(
            _options.Outbox.BatchSize,
            TimeSpan.FromSeconds(_options.Outbox.LeaseSeconds),
            cancellationToken);

        foreach (var message in messages)
        {
            try
            {
                await _transport.PublishAsync(message.Envelope, cancellationToken);
                await store.MarkPublishedAsync(
                    message.OutboxId,
                    message.LockToken,
                    cancellationToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                _logger.LogWarning(
                    exception,
                    "Integration outbox publication failed for message {MessageId} and event {EventName}",
                    message.Envelope.MessageId,
                    message.Envelope.EventName);
                var delaySeconds = Math.Min(300, Math.Pow(2, Math.Min(message.AttemptCount, 8)));
                await store.MarkFailedAsync(
                    message.OutboxId,
                    message.LockToken,
                    exception.GetType().Name,
                    _options.Outbox.MaximumAttempts,
                    TimeSpan.FromSeconds(delaySeconds),
                    cancellationToken);
            }
        }

        return messages.Count > 0;
    }
}

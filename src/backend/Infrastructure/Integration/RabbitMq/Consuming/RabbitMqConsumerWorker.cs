using System.Diagnostics;
using Application.Integration;
using Contracts.Integration;
using Infrastructure.Integration.Observability;
using Infrastructure.Integration.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Infrastructure.Integration.RabbitMq;

public sealed class RabbitMqConsumerWorker(
    IRabbitMqConnectionProvider connectionProvider,
    IServiceScopeFactory scopeFactory,
    IOptions<ServiceIntegrationOptions> options,
    RabbitMqSubscriptionState subscriptionState,
    ILogger<RabbitMqConsumerWorker> logger) : BackgroundService
{
    private readonly IRabbitMqConnectionProvider _connectionProvider = connectionProvider;
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly ServiceIntegrationOptions _options = options.Value;
    private readonly RabbitMqSubscriptionState _subscriptionState = subscriptionState;
    private readonly ILogger<RabbitMqConsumerWorker> _logger = logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled || !_options.RabbitMq.Enabled)
        {
            return;
        }

        _subscriptionState.MarkUnavailable("RabbitMQ subscriptions are starting.");
        var failureCount = 0;
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunSubscriptionsAsync(stoppingToken);
                failureCount = 0;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                _subscriptionState.MarkUnavailable("RabbitMQ subscriptions are unavailable and will be retried.");
                failureCount++;
                var delay = TimeSpan.FromSeconds(Math.Min(30, Math.Pow(2, Math.Min(failureCount, 5))));
                _logger.LogError(
                    exception,
                    "RabbitMQ subscriptions are unavailable; retrying in {RetryDelaySeconds} seconds",
                    delay.TotalSeconds);
                await Task.Delay(delay, stoppingToken);
            }
        }
    }

    private async Task RunSubscriptionsAsync(CancellationToken stoppingToken)
    {
        var channels = new List<IChannel>();
        var subscriptionEnded = new TaskCompletionSource<string>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        try
        {
            var connection = await _connectionProvider.GetConnectionAsync(stoppingToken);
            foreach (var contract in IntegrationContractCatalog.Subscribed)
            {
                var channel = await connection.CreateChannelAsync(
                    new CreateChannelOptions(
                        publisherConfirmationsEnabled: true,
                        publisherConfirmationTrackingEnabled: true),
                    stoppingToken);
                channels.Add(channel);

                var topology = RabbitMqTopologyPlan.Create(_options.RabbitMq, contract);
                await RabbitMqTopologyProvisioner.ApplyAsync(channel, topology, stoppingToken);
                await channel.BasicQosAsync(
                    prefetchSize: 0,
                    prefetchCount: _options.RabbitMq.PrefetchCount,
                    global: false,
                    cancellationToken: stoppingToken);

                var consumer = new AsyncEventingBasicConsumer(channel);
                consumer.ReceivedAsync += (_, delivery) =>
                    HandleDeliveryAsync(channel, topology, contract, delivery, stoppingToken);
                consumer.ShutdownAsync += (_, _) =>
                {
                    subscriptionEnded.TrySetResult("A RabbitMQ consumer channel shut down.");
                    return Task.CompletedTask;
                };
                consumer.UnregisteredAsync += (_, _) =>
                {
                    subscriptionEnded.TrySetResult("A RabbitMQ consumer was cancelled by the server.");
                    return Task.CompletedTask;
                };
                await channel.BasicConsumeAsync(
                    topology.MainQueue.Name,
                    autoAck: false,
                    consumer,
                    stoppingToken);

                _logger.LogInformation(
                    "RabbitMQ subscription started for {EventName} version {EventVersion} on queue {QueueName}",
                    contract.Name,
                    contract.Version,
                    topology.MainQueue.Name);
            }

            _subscriptionState.MarkReady(channels.Count);
            if (channels.Count == 0)
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
            }

            var endedReason = await subscriptionEnded.Task.WaitAsync(stoppingToken);
            _subscriptionState.MarkUnavailable(endedReason);
            throw new InvalidOperationException(endedReason);
        }
        finally
        {
            _subscriptionState.MarkUnavailable("RabbitMQ subscriptions are not active.");
            foreach (var channel in channels)
            {
                await channel.DisposeAsync();
            }
        }
    }

    private async Task HandleDeliveryAsync(
        IChannel channel,
        RabbitMqSubscriptionTopology topology,
        IntegrationContractDescriptor contract,
        BasicDeliverEventArgs delivery,
        CancellationToken stoppingToken)
    {
        // RabbitMQ.Client owns delivery memory after the callback; copy before awaiting.
        var body = delivery.Body.Length <= _options.RabbitMq.MaximumMessageBytes
            ? delivery.Body.ToArray()
            : [];
        var completedRetries = RabbitMqRetryHeader.ReadCompletedRetries(delivery.BasicProperties.Headers);
        var started = Stopwatch.GetTimestamp();
        Activity? activity = null;

        try
        {
            if (delivery.Body.Length > _options.RabbitMq.MaximumMessageBytes)
            {
                throw new PermanentIntegrationEventException(
                    "The RabbitMQ delivery exceeds the configured message-size limit.");
            }

            if (!string.Equals(
                    delivery.RoutingKey,
                    IntegrationContractRoutingKey.Create(contract.Name, contract.Version),
                    StringComparison.Ordinal))
            {
                throw new PermanentIntegrationEventException(
                    "The RabbitMQ routing key does not match the subscribed contract.");
            }

            var envelope = RabbitMqDeliveryEnvelopeParser.Parse(
                body,
                contract,
                _options.RabbitMq.MaximumMessageBytes);
            var parentContext = ActivityContext.TryParse(
                envelope.TraceParent,
                envelope.TraceState,
                out var parsedParent)
                ? parsedParent
                : default;
            activity = ServiceIntegrationTelemetry.ActivitySource.StartActivity(
                "rabbitmq consume",
                ActivityKind.Consumer,
                parentContext);
            activity?.SetTag("messaging.system", "rabbitmq");
            activity?.SetTag("messaging.destination.name", topology.MainQueue.Name);
            activity?.SetTag("messaging.operation.type", "receive");
            activity?.SetTag("messaging.message.id", envelope.MessageId.ToString("D"));
            activity?.SetTag("messaging.message.type", envelope.EventName);

            using var scope = _scopeFactory.CreateScope();
            var processor = scope.ServiceProvider.GetRequiredService<IIntegrationEventProcessor>();
            var result = await processor.ProcessAsync(
                envelope,
                topology.MainQueue.Name,
                stoppingToken);

            await channel.BasicAckAsync(delivery.DeliveryTag, multiple: false, stoppingToken);
            ServiceIntegrationTelemetry.ConsumedEvents.Add(
                1,
                new KeyValuePair<string, object?>("event.name", contract.Name),
                new KeyValuePair<string, object?>("processing.result", result.ToString()));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            await channel.BasicNackAsync(delivery.DeliveryTag, multiple: false, requeue: true, CancellationToken.None);
        }
        catch (Exception exception)
        {
            await HandleFailureAsync(
                channel,
                topology,
                contract,
                delivery,
                body,
                completedRetries,
                exception,
                stoppingToken);
        }
        finally
        {
            activity?.Dispose();
            ServiceIntegrationTelemetry.ConsumeDurationMilliseconds.Record(
                Stopwatch.GetElapsedTime(started).TotalMilliseconds,
                new KeyValuePair<string, object?>("event.name", contract.Name));
        }
    }

    private async Task HandleFailureAsync(
        IChannel channel,
        RabbitMqSubscriptionTopology topology,
        IntegrationContractDescriptor contract,
        BasicDeliverEventArgs delivery,
        byte[] body,
        int completedRetries,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var decision = RabbitMqDeliveryPolicy.Decide(
            exception,
            completedRetries,
            _options.RabbitMq.MaximumDeliveryAttempts);
        if (decision == RabbitMqDeliveryDecision.DeadLetter)
        {
            _logger.LogError(
                exception,
                "Integration event {EventName} was dead-lettered after {DeliveryAttempts} attempts",
                contract.Name,
                completedRetries + 1);
            await channel.BasicRejectAsync(delivery.DeliveryTag, requeue: false, cancellationToken);
            ServiceIntegrationTelemetry.DeadLetteredEvents.Add(
                1,
                new KeyValuePair<string, object?>("event.name", contract.Name));
            return;
        }

        try
        {
            var headers = delivery.BasicProperties.Headers is null
                ? new Dictionary<string, object?>()
                : new Dictionary<string, object?>(delivery.BasicProperties.Headers, StringComparer.Ordinal);
            headers[RabbitMqRetryHeader.Name] = completedRetries + 1;
            var retryProperties = new BasicProperties
            {
                AppId = delivery.BasicProperties.AppId,
                ContentEncoding = delivery.BasicProperties.ContentEncoding,
                ContentType = delivery.BasicProperties.ContentType,
                CorrelationId = delivery.BasicProperties.CorrelationId,
                DeliveryMode = DeliveryModes.Persistent,
                MessageId = delivery.BasicProperties.MessageId,
                Timestamp = delivery.BasicProperties.Timestamp,
                Type = delivery.BasicProperties.Type,
                Headers = headers,
            };
            await channel.BasicPublishAsync(
                topology.RetryExchange.Name,
                topology.MainQueue.Name,
                mandatory: true,
                retryProperties,
                body,
                cancellationToken);
            await channel.BasicAckAsync(delivery.DeliveryTag, multiple: false, cancellationToken);
            ServiceIntegrationTelemetry.RetriedEvents.Add(
                1,
                new KeyValuePair<string, object?>("event.name", contract.Name));
        }
        catch (Exception retryException) when (retryException is not OperationCanceledException)
        {
            _logger.LogWarning(
                retryException,
                "Could not enqueue retry for integration event {EventName}; original delivery will be requeued",
                contract.Name);
            await channel.BasicNackAsync(
                delivery.DeliveryTag,
                multiple: false,
                requeue: true,
                CancellationToken.None);
        }
    }
}

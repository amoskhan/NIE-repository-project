using System.Diagnostics;
using System.Text.Json;
using Application.Integration;
using Contracts.Integration;
using Infrastructure.Integration.Observability;
using Infrastructure.Integration.Options;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace Infrastructure.Integration.RabbitMq;

public sealed class RabbitMqEventTransport(
    IRabbitMqConnectionProvider connectionProvider,
    IOptions<ServiceIntegrationOptions> options) : IIntegrationEventTransport, IAsyncDisposable
{
    private readonly IRabbitMqConnectionProvider _connectionProvider = connectionProvider;
    private readonly ServiceIntegrationOptions _options = options.Value;
    private readonly SemaphoreSlim _publishGate = new(1, 1);
    private IChannel? _channel;

    public Task PublishAsync(
        IntegrationEventEnvelope envelope,
        CancellationToken cancellationToken) =>
        PublishCoreAsync(envelope, cancellationToken);

    private async Task PublishCoreAsync(
        IntegrationEventEnvelope envelope,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        await _publishGate.WaitAsync(cancellationToken);
        var started = Stopwatch.GetTimestamp();
        var parentContext = ActivityContext.TryParse(
            envelope.TraceParent,
            envelope.TraceState,
            out var parsedParent)
            ? parsedParent
            : default;
        using var activity = ServiceIntegrationTelemetry.ActivitySource.StartActivity(
            "rabbitmq publish",
            ActivityKind.Producer,
            parentContext);
        activity?.SetTag("messaging.system", "rabbitmq");
        activity?.SetTag("messaging.destination.name", _options.RabbitMq.Exchange);
        activity?.SetTag("messaging.message.id", envelope.MessageId.ToString("D"));
        activity?.SetTag("messaging.operation.name", "publish");
        activity?.SetTag("messaging.operation.type", "send");
        try
        {
            var channel = await GetChannelAsync(cancellationToken);
            var properties = new BasicProperties
            {
                AppId = envelope.Producer,
                ContentEncoding = "utf-8",
                ContentType = "application/json",
                CorrelationId = envelope.CorrelationId,
                DeliveryMode = DeliveryModes.Persistent,
                MessageId = envelope.MessageId.ToString("D"),
                Timestamp = new AmqpTimestamp(envelope.OccurredAtUtc.ToUnixTimeSeconds()),
                Type = envelope.EventName,
                Headers = new Dictionary<string, object?>
                {
                    ["x-nie-causation-id"] = envelope.CausationId ?? string.Empty,
                    ["x-nie-event-version"] = envelope.EventVersion,
                    ["traceparent"] = envelope.TraceParent ?? string.Empty,
                    ["tracestate"] = envelope.TraceState ?? string.Empty,
                },
            };
            var body = JsonSerializer.SerializeToUtf8Bytes(envelope, IntegrationJsonOptions.Default);
            if (body.Length > _options.RabbitMq.MaximumMessageBytes)
            {
                throw new InvalidOperationException("The integration event exceeds the configured RabbitMQ message-size limit.");
            }

            try
            {
                await channel.BasicPublishAsync(
                    _options.RabbitMq.Exchange,
                    IntegrationContractRoutingKey.Create(
                        envelope.EventName,
                        envelope.EventVersion),
                    mandatory: true,
                    properties,
                    body,
                    cancellationToken);
                ServiceIntegrationTelemetry.PublishedEvents.Add(
                    1,
                    new KeyValuePair<string, object?>("event.name", envelope.EventName));
            }
            catch (PublishException exception) when (exception.IsReturn)
            {
                throw new UnroutableIntegrationEventException(envelope.EventName, exception);
            }
        }
        catch (Exception exception) when (exception is AlreadyClosedException or OperationInterruptedException)
        {
            await ResetChannelAsync();
            throw;
        }
        finally
        {
            ServiceIntegrationTelemetry.PublishDurationMilliseconds.Record(
                Stopwatch.GetElapsedTime(started).TotalMilliseconds,
                new KeyValuePair<string, object?>("event.name", envelope.EventName));
            _publishGate.Release();
        }
    }

    private async Task<IChannel> GetChannelAsync(CancellationToken cancellationToken)
    {
        if (_channel is { IsOpen: true })
        {
            return _channel;
        }

        await ResetChannelAsync();
        var connection = await _connectionProvider.GetConnectionAsync(cancellationToken);
        _channel = await connection.CreateChannelAsync(
            new CreateChannelOptions(
                publisherConfirmationsEnabled: true,
                publisherConfirmationTrackingEnabled: true),
            cancellationToken);
        await _channel.ExchangeDeclareAsync(
            _options.RabbitMq.Exchange,
            ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken);
        return _channel;
    }

    private async Task ResetChannelAsync()
    {
        if (_channel is not null)
        {
            await _channel.DisposeAsync();
            _channel = null;
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _publishGate.WaitAsync();
        try
        {
            await ResetChannelAsync();
        }
        finally
        {
            _publishGate.Release();
            _publishGate.Dispose();
        }
    }
}

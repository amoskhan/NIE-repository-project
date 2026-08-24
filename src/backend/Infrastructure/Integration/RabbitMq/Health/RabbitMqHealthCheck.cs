using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Infrastructure.Integration.RabbitMq;

public sealed class RabbitMqHealthCheck(
    IRabbitMqConnectionProvider connectionProvider,
    RabbitMqSubscriptionState subscriptionState) : IHealthCheck
{
    private readonly IRabbitMqConnectionProvider _connectionProvider = connectionProvider;
    private readonly RabbitMqSubscriptionState _subscriptionState = subscriptionState;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var connection = await _connectionProvider.GetConnectionAsync(cancellationToken);
            if (!connection.IsOpen)
            {
                return HealthCheckResult.Unhealthy("RabbitMQ connection is closed.");
            }

            var subscriptions = _subscriptionState.Snapshot();
            return subscriptions.IsReady
                ? HealthCheckResult.Healthy(subscriptions.Status)
                : HealthCheckResult.Unhealthy(subscriptions.Status);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return HealthCheckResult.Unhealthy("RabbitMQ connection failed.", exception);
        }
    }
}

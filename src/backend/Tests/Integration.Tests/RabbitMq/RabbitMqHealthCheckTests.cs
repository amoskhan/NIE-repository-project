using Infrastructure.Integration.RabbitMq;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using RabbitMQ.Client;

namespace Integration.Tests;

public class RabbitMqHealthCheckTests
{
    [Fact]
    public async Task Broker_connection_failure_makes_readiness_unhealthy()
    {
        var state = new RabbitMqSubscriptionState();
        var check = new RabbitMqHealthCheck(new UnavailableConnectionProvider(), state);

        var result = await check.CheckHealthAsync(
            new HealthCheckContext(),
            TestContext.Current.CancellationToken);

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.IsType<InvalidOperationException>(result.Exception);
    }

    [Fact]
    public void Subscription_readiness_is_fail_closed_and_tracks_transitions()
    {
        var state = new RabbitMqSubscriptionState();

        Assert.False(state.Snapshot().IsReady);

        state.MarkReady(2);
        var ready = state.Snapshot();
        Assert.True(ready.IsReady);
        Assert.Contains("2", ready.Status, StringComparison.Ordinal);

        state.MarkUnavailable("Consumer was cancelled.");
        var unavailable = state.Snapshot();
        Assert.False(unavailable.IsReady);
        Assert.Equal("Consumer was cancelled.", unavailable.Status);
    }

    private sealed class UnavailableConnectionProvider : IRabbitMqConnectionProvider
    {
        public Task<IConnection> GetConnectionAsync(CancellationToken cancellationToken) =>
            Task.FromException<IConnection>(new InvalidOperationException("Broker unavailable for test."));
    }
}

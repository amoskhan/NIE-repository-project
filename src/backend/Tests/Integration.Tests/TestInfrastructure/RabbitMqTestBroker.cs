namespace Integration.Tests;

internal static class RabbitMqTestBroker
{
    /// <summary>
    /// Skips the calling test when no RabbitMQ broker is configured. A developer
    /// without local services gets a skip rather than a hard failure, while CI
    /// always sets the variable so nothing is silently skipped there.
    /// </summary>
    public static string RequireConnectionString()
    {
        var connectionString = Environment.GetEnvironmentVariable("NIE_TEST_RABBITMQ_CONNECTION");
        Assert.SkipWhen(
            string.IsNullOrWhiteSpace(connectionString),
            "Set NIE_TEST_RABBITMQ_CONNECTION to run RabbitMQ integration tests.");
        return connectionString!;
    }
}

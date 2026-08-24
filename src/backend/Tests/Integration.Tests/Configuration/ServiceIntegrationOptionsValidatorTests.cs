using Infrastructure.Integration.Options;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace Integration.Tests;

public class ServiceIntegrationOptionsValidatorTests
{
    [Fact]
    public void Production_requires_tls_and_service_authentication()
    {
        var validator = new ServiceIntegrationOptionsValidator(new TestHostEnvironment("Production"));
        var options = ValidOptions();
        options.RabbitMq.ConnectionString = "amqp://integration-user:password@broker:5672/application";
        options.Grpc.PeerAddress = "http://peer:8081";
        options.Grpc.RequireAuthentication = false;
        options.Grpc.Authority = string.Empty;
        options.Grpc.Audience = string.Empty;

        var result = validator.Validate(null, options);

        Assert.True(result.Failed);
        var failures = result.Failures ?? [];
        Assert.Contains(failures, failure => failure.Contains("amqps", StringComparison.Ordinal));
        Assert.Contains(failures, failure => failure.Contains("HTTPS", StringComparison.Ordinal));
        Assert.Contains(failures, failure => failure.Contains("authentication", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Development_accepts_local_cleartext_transports_with_bounded_settings()
    {
        var validator = new ServiceIntegrationOptionsValidator(new TestHostEnvironment("Development"));
        var options = ValidOptions();
        options.RabbitMq.ConnectionString = "amqp://integration-user:password@localhost:5672/application";
        options.Grpc.PeerAddress = "http://localhost:5003";
        options.Grpc.RequireAuthentication = false;
        options.Grpc.Authority = string.Empty;
        options.Grpc.Audience = string.Empty;

        var result = validator.Validate(null, options);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Retry_prefetch_deadline_and_message_size_must_be_bounded()
    {
        var validator = new ServiceIntegrationOptionsValidator(new TestHostEnvironment("Development"));
        var options = ValidOptions();
        options.RabbitMq.PrefetchCount = 0;
        options.RabbitMq.MaximumDeliveryAttempts = 101;
        options.Grpc.DeadlineMilliseconds = 0;
        options.Grpc.MaximumMessageBytes = 100_000_000;

        var result = validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.True((result.Failures ?? []).Count() >= 4);
    }

    [Fact]
    public void Inbox_retention_must_exceed_the_broker_replay_window()
    {
        var validator = new ServiceIntegrationOptionsValidator(new TestHostEnvironment("Development"));
        var options = ValidOptions();
        options.RabbitMq.MaximumReplayWindowDays = 30;
        options.Outbox.InboxRetentionDays = 30;

        var result = validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains(
            result.Failures ?? [],
            failure => failure.Contains("greater than", StringComparison.Ordinal));
    }

    private static ServiceIntegrationOptions ValidOptions() => new()
    {
        Enabled = true,
        ApplicationKey = "procurement",
        RabbitMq = new RabbitMqOptions
        {
            Enabled = true,
            ConnectionString = "amqps://integration-user:password@broker:5671/application",
        },
        Grpc = new GrpcIntegrationOptions
        {
            Enabled = true,
            PeerAddress = "https://peer.internal",
            RequireAuthentication = true,
            Authority = "https://identity.internal",
            Audience = "vendor-directory",
            RequiredInboundScope = "procurement-query.read",
            TokenEndpoint = "https://identity.internal/oauth2/token",
            ClientId = "procurement-service",
            ClientSecret = "not-a-secret-test-value",
            Scope = "vendor-directory.read",
        },
    };

    private sealed class TestHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;

        public string ApplicationName { get; set; } = "Integration.Tests";

        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}

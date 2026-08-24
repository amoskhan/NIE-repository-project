namespace Infrastructure.Integration.Options;

public sealed class ServiceIntegrationOptions
{
    public const string SectionName = "ServiceIntegration";

    public bool Enabled { get; set; }

    public string ApplicationKey { get; set; } = "application";

    public RabbitMqOptions RabbitMq { get; set; } = new();

    public GrpcIntegrationOptions Grpc { get; set; } = new();

    public IntegrationOutboxOptions Outbox { get; set; } = new();
}

public sealed class RabbitMqOptions
{
    public bool Enabled { get; set; }

    public string ConnectionString { get; set; } = string.Empty;

    public string Exchange { get; set; } = "nie.events";

    public string QueuePrefix { get; set; } = "application";

    public ushort PrefetchCount { get; set; } = 16;

    public int RetryDelaySeconds { get; set; } = 30;

    public int MaximumDeliveryAttempts { get; set; } = 5;

    public int MaximumReplayWindowDays { get; set; } = 7;

    public int MaximumMessageBytes { get; set; } = 1_048_576;
}

public sealed class GrpcIntegrationOptions
{
    public bool Enabled { get; set; }

    public string PeerAddress { get; set; } = string.Empty;

    public int DeadlineMilliseconds { get; set; } = 1500;

    public int MaximumRetryAttempts { get; set; } = 3;

    public int MaximumMessageBytes { get; set; } = 1_048_576;

    public bool RequireAuthentication { get; set; } = true;

    public string Authority { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;

    public string RequiredInboundScope { get; set; } = string.Empty;

    public string TokenEndpoint { get; set; } = string.Empty;

    public string ClientId { get; set; } = string.Empty;

    public string ClientSecret { get; set; } = string.Empty;

    public string Scope { get; set; } = string.Empty;
}

public sealed class IntegrationOutboxOptions
{
    public int BatchSize { get; set; } = 50;

    public int PollIntervalMilliseconds { get; set; } = 500;

    public int LeaseSeconds { get; set; } = 30;

    public int MaximumAttempts { get; set; } = 10;

    public int PublishedRetentionDays { get; set; } = 30;

    public int InboxRetentionDays { get; set; } = 30;

    public int RetentionSweepMinutes { get; set; } = 360;

    public int RetentionBatchSize { get; set; } = 1000;

    public int MetricsSampleSeconds { get; set; } = 15;
}

using System.Text.RegularExpressions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Infrastructure.Integration.Options;

public sealed class ServiceIntegrationOptionsValidator(
    IHostEnvironment environment) : IValidateOptions<ServiceIntegrationOptions>
{
    private static readonly Regex ApplicationKeyPattern = new(
        "^[a-z][a-z0-9-]{2,63}$",
        RegexOptions.CultureInvariant | RegexOptions.NonBacktracking);

    private readonly IHostEnvironment _environment = environment;

    public ValidateOptionsResult Validate(string? name, ServiceIntegrationOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (!options.Enabled)
        {
            return ValidateOptionsResult.Success;
        }

        var failures = new List<string>();
        if (!ApplicationKeyPattern.IsMatch(options.ApplicationKey))
        {
            failures.Add("ServiceIntegration:ApplicationKey must be a lowercase kebab-case key between 3 and 64 characters.");
        }

        if (!options.RabbitMq.Enabled && !options.Grpc.Enabled)
        {
            failures.Add("At least one service-integration transport must be enabled.");
        }

        ValidateRabbitMq(options.RabbitMq, failures);
        ValidateGrpc(options.Grpc, failures);
        ValidateOutbox(options.Outbox, options.RabbitMq, failures);

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }

    private void ValidateRabbitMq(RabbitMqOptions options, ICollection<string> failures)
    {
        if (!options.Enabled)
        {
            return;
        }

        if (!Uri.TryCreate(options.ConnectionString, UriKind.Absolute, out var connectionUri)
            || (connectionUri.Scheme != "amqp" && connectionUri.Scheme != "amqps"))
        {
            failures.Add("ServiceIntegration:RabbitMq:ConnectionString must be an amqp or amqps URI supplied by a secret provider.");
        }
        else if (!_environment.IsDevelopment() && connectionUri.Scheme != "amqps")
        {
            failures.Add("RabbitMQ connections outside Development must use amqps TLS.");
        }

        if (string.IsNullOrWhiteSpace(options.Exchange) || options.Exchange.Length > 128)
        {
            failures.Add("ServiceIntegration:RabbitMq:Exchange is required and cannot exceed 128 characters.");
        }

        if (!ApplicationKeyPattern.IsMatch(options.QueuePrefix))
        {
            failures.Add("ServiceIntegration:RabbitMq:QueuePrefix must be a lowercase kebab-case key between 3 and 64 characters.");
        }

        if (options.PrefetchCount is < 1 or > 500)
        {
            failures.Add("ServiceIntegration:RabbitMq:PrefetchCount must be between 1 and 500.");
        }

        if (options.RetryDelaySeconds is < 1 or > 3600)
        {
            failures.Add("ServiceIntegration:RabbitMq:RetryDelaySeconds must be between 1 and 3600.");
        }

        if (options.MaximumDeliveryAttempts is < 1 or > 20)
        {
            failures.Add("ServiceIntegration:RabbitMq:MaximumDeliveryAttempts must be between 1 and 20.");
        }

        if (options.MaximumReplayWindowDays is < 1 or > 365)
        {
            failures.Add("ServiceIntegration:RabbitMq:MaximumReplayWindowDays must be between 1 and 365.");
        }

        if (options.MaximumMessageBytes is < 16_384 or > 16_777_216)
        {
            failures.Add("ServiceIntegration:RabbitMq:MaximumMessageBytes must be between 16384 and 16777216.");
        }
    }

    private void ValidateGrpc(GrpcIntegrationOptions options, ICollection<string> failures)
    {
        if (!options.Enabled)
        {
            return;
        }

        if (!Uri.TryCreate(options.PeerAddress, UriKind.Absolute, out var peerUri)
            || (peerUri.Scheme != Uri.UriSchemeHttp && peerUri.Scheme != Uri.UriSchemeHttps))
        {
            failures.Add("ServiceIntegration:Grpc:PeerAddress must be an absolute HTTP or HTTPS URI.");
        }
        else if (!_environment.IsDevelopment() && peerUri.Scheme != Uri.UriSchemeHttps)
        {
            failures.Add("gRPC peer connections outside Development must use HTTPS.");
        }

        if (options.DeadlineMilliseconds is < 100 or > 30_000)
        {
            failures.Add("ServiceIntegration:Grpc:DeadlineMilliseconds must be between 100 and 30000.");
        }

        if (options.MaximumRetryAttempts is < 1 or > 5)
        {
            failures.Add("ServiceIntegration:Grpc:MaximumRetryAttempts must be between 1 and 5.");
        }

        if (options.MaximumMessageBytes is < 16_384 or > 16_777_216)
        {
            failures.Add("ServiceIntegration:Grpc:MaximumMessageBytes must be between 16384 and 16777216.");
        }

        if (!_environment.IsDevelopment() && !options.RequireAuthentication)
        {
            failures.Add("gRPC service authentication is mandatory outside Development.");
        }

        if (options.RequireAuthentication)
        {
            if (!Uri.TryCreate(options.Authority, UriKind.Absolute, out var authorityUri)
                || authorityUri.Scheme != Uri.UriSchemeHttps)
            {
                failures.Add("ServiceIntegration:Grpc:Authority must be an absolute HTTPS URI when authentication is enabled.");
            }

            if (string.IsNullOrWhiteSpace(options.Audience)
                || string.IsNullOrWhiteSpace(options.RequiredInboundScope))
            {
                failures.Add("ServiceIntegration:Grpc:Audience and RequiredInboundScope are required when authentication is enabled.");
            }

            if (!Uri.TryCreate(options.TokenEndpoint, UriKind.Absolute, out var tokenEndpoint)
                || (!_environment.IsDevelopment() && tokenEndpoint.Scheme != Uri.UriSchemeHttps))
            {
                failures.Add("ServiceIntegration:Grpc:TokenEndpoint must be an absolute HTTPS URI outside Development.");
            }

            if (string.IsNullOrWhiteSpace(options.ClientId)
                || string.IsNullOrWhiteSpace(options.ClientSecret)
                || string.IsNullOrWhiteSpace(options.Scope))
            {
                failures.Add("ServiceIntegration gRPC client credentials and scope are required when authentication is enabled.");
            }
        }
    }

    private static void ValidateOutbox(
        IntegrationOutboxOptions options,
        RabbitMqOptions rabbitMq,
        ICollection<string> failures)
    {
        if (options.BatchSize is < 1 or > 500)
        {
            failures.Add("ServiceIntegration:Outbox:BatchSize must be between 1 and 500.");
        }

        if (options.PollIntervalMilliseconds is < 100 or > 60_000)
        {
            failures.Add("ServiceIntegration:Outbox:PollIntervalMilliseconds must be between 100 and 60000.");
        }

        if (options.LeaseSeconds is < 5 or > 600)
        {
            failures.Add("ServiceIntegration:Outbox:LeaseSeconds must be between 5 and 600.");
        }

        if (options.MaximumAttempts is < 1 or > 100)
        {
            failures.Add("ServiceIntegration:Outbox:MaximumAttempts must be between 1 and 100.");
        }

        if (options.PublishedRetentionDays is < 1 or > 3650
            || options.InboxRetentionDays is < 1 or > 3650)
        {
            failures.Add("Integration outbox and inbox retention must be between 1 and 3650 days.");
        }

        if (rabbitMq.Enabled && options.InboxRetentionDays <= rabbitMq.MaximumReplayWindowDays)
        {
            failures.Add("ServiceIntegration:Outbox:InboxRetentionDays must be greater than ServiceIntegration:RabbitMq:MaximumReplayWindowDays.");
        }

        if (options.RetentionSweepMinutes is < 5 or > 10_080
            || options.RetentionBatchSize is < 1 or > 10_000)
        {
            failures.Add("Integration retention sweep and batch settings are outside supported bounds.");
        }

        if (options.MetricsSampleSeconds is < 5 or > 300)
        {
            failures.Add("ServiceIntegration:Outbox:MetricsSampleSeconds must be between 5 and 300.");
        }
    }
}

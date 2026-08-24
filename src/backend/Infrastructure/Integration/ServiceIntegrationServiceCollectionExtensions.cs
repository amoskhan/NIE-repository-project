using Application.Integration;
using Contracts.Grpc.Vendor.V1;
using Grpc.Core;
using Grpc.Net.Client.Configuration;
using Infrastructure.Integration.Grpc;
using Infrastructure.Integration.Options;
using Infrastructure.Integration.RabbitMq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Infrastructure.Integration;

public static class ServiceIntegrationServiceCollectionExtensions
{
    public static IServiceCollection AddServiceIntegration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<ServiceIntegrationOptions>()
            .Bind(configuration.GetSection(ServiceIntegrationOptions.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<ServiceIntegrationOptions>, ServiceIntegrationOptionsValidator>();

        var configured = configuration
            .GetSection(ServiceIntegrationOptions.SectionName)
            .Get<ServiceIntegrationOptions>() ?? new ServiceIntegrationOptions();

        if (configured.Enabled && configured.RabbitMq.Enabled)
        {
            services.AddSingleton<RabbitMqSubscriptionState>();
            services.AddSingleton<IRabbitMqConnectionProvider, RabbitMqConnectionProvider>();
            services.AddSingleton<IIntegrationEventTransport, RabbitMqEventTransport>();
            services.AddHostedService<IntegrationOutboxPublisherWorker>();
            services.AddHostedService<RabbitMqConsumerWorker>();
            services.AddHostedService<IntegrationMessageRetentionWorker>();
            services.AddHealthChecks().AddCheck<RabbitMqHealthCheck>(
                "rabbitmq",
                tags: ["ready", "service-integration"]);
        }

        if (configured.Enabled && configured.Grpc.Enabled)
        {
            if (configured.Grpc.RequireAuthentication)
            {
                services.AddHttpClient(OAuthClientCredentialsAccessTokenProvider.HttpClientName);
                services.AddSingleton<IServiceAccessTokenProvider, OAuthClientCredentialsAccessTokenProvider>();
                services.AddTransient<ServiceAccessTokenHandler>();
            }

            var grpcClientBuilder = services
                .AddGrpcClient<VendorDirectory.VendorDirectoryClient>((serviceProvider, clientOptions) =>
                {
                    var options = serviceProvider
                        .GetRequiredService<IOptions<ServiceIntegrationOptions>>()
                        .Value;
                    clientOptions.Address = new Uri(options.Grpc.PeerAddress);
                })
                .ConfigureChannel((serviceProvider, channelOptions) =>
                {
                    var options = serviceProvider
                        .GetRequiredService<IOptions<ServiceIntegrationOptions>>()
                        .Value;
                    channelOptions.MaxReceiveMessageSize = options.Grpc.MaximumMessageBytes;
                    channelOptions.MaxSendMessageSize = options.Grpc.MaximumMessageBytes;
                    var retryServiceConfig = CreateReadOnlyRetryServiceConfig(
                        options.Grpc.MaximumRetryAttempts);
                    if (retryServiceConfig is not null)
                    {
                        channelOptions.ServiceConfig = retryServiceConfig;
                    }
                });

            if (configured.Grpc.RequireAuthentication)
            {
                grpcClientBuilder.AddHttpMessageHandler<ServiceAccessTokenHandler>();
            }

            services.AddScoped<IVendorDirectoryClient, VendorDirectoryGrpcClient>();
        }

        return services;
    }

    internal static ServiceConfig? CreateReadOnlyRetryServiceConfig(int maximumRetryAttempts)
    {
        if (maximumRetryAttempts == 1)
        {
            return null;
        }

        return new ServiceConfig
        {
            MethodConfigs =
            {
                new MethodConfig
                {
                    // This RPC is a read-only lookup. Never apply this retry policy
                    // to new commands or to the service-wide default.
                    Names =
                    {
                        new MethodName
                        {
                            Service = VendorDirectory.Descriptor.FullName,
                            Method = "GetVendorSnapshot",
                        },
                    },
                    RetryPolicy = new RetryPolicy
                    {
                        MaxAttempts = maximumRetryAttempts,
                        InitialBackoff = TimeSpan.FromMilliseconds(100),
                        MaxBackoff = TimeSpan.FromSeconds(1),
                        BackoffMultiplier = 2,
                        RetryableStatusCodes = { StatusCode.Unavailable },
                    },
                },
            },
        };
    }
}

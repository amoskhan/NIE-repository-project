using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Sentry.OpenTelemetry;

namespace Api.Extensions;

/// <summary>
/// Configures Sentry and OpenTelemetry for traces, metrics, and correlated logs.
/// Set Sentry:Dsn at runtime to enable Sentry export; leave it empty to keep
/// OpenTelemetry running without embedding environment-specific keys.
/// </summary>
public static class ObservabilityExtensions
{
    public static WebApplicationBuilder AddObservability(
        this WebApplicationBuilder builder,
        string defaultServiceName = "application-api",
        IReadOnlyCollection<string>? activitySources = null,
        IReadOnlyCollection<string>? meters = null)
    {
        var configuration = builder.Configuration;
        var isLocalDevelopment = builder.Environment.IsDevelopment() || builder.Environment.IsEnvironment("Local");
        var sentryEnvironment = configuration["Sentry:Environment"] ?? (isLocalDevelopment ? "local" : builder.Environment.EnvironmentName);
        var sentryDsn = configuration["Sentry:Dsn"];
        var serviceName = configuration["OpenTelemetry:ServiceName"]
            ?? configuration["Sentry:ServiceName"]
            ?? defaultServiceName;

        if (!string.IsNullOrWhiteSpace(sentryDsn))
        {
            var tracesSampleRate = double.TryParse(configuration["Sentry:TracesSampleRate"], out var rate) ? rate : 0.2;
            var profilesSampleRate = double.TryParse(configuration["Sentry:ProfilesSampleRate"], out var profileRate) ? profileRate : tracesSampleRate;

            builder.WebHost.UseSentry(options =>
            {
                options.Dsn = sentryDsn;
                options.Debug = isLocalDevelopment;
                options.EnableLogs = true;
                options.EnableMetrics = true;
                options.Environment = sentryEnvironment;
                options.TracesSampleRate = tracesSampleRate;
                options.ProfilesSampleRate = profilesSampleRate;
                options.SendDefaultPii = false;
                options.MaxBreadcrumbs = 100;
                options.AttachStacktrace = true;
                options.DefaultTags["app"] = "main";
                options.DefaultTags["service"] = serviceName;
                options.UseOpenTelemetry();
            });

            builder.Logging.AddSentry(options =>
            {
                options.MinimumBreadcrumbLevel = LogLevel.Information;
                options.MinimumEventLevel = LogLevel.Error;
            });
        }

        ConfigureOpenTelemetry(builder, serviceName, sentryEnvironment, activitySources ?? [], meters ?? []);

        return builder;
    }

    private static void ConfigureOpenTelemetry(
        WebApplicationBuilder builder,
        string serviceName,
        string deploymentEnvironment,
        IReadOnlyCollection<string> activitySources,
        IReadOnlyCollection<string> meters)
    {
        var configuration = builder.Configuration;
        if (!configuration.GetValue("OpenTelemetry:Enabled", true))
        {
            return;
        }

        var serviceVersion = typeof(Program).Assembly.GetName().Version?.ToString();
        var configuredOtlpEndpoint = configuration["OpenTelemetry:ExporterEndpoint"];
        var otlpEndpoint = !string.IsNullOrWhiteSpace(configuredOtlpEndpoint)
            ? configuredOtlpEndpoint
            : configuration["OTEL_EXPORTER_OTLP_ENDPOINT"];

        void ConfigureResource(ResourceBuilder resource)
        {
            resource
                .AddService(
                    serviceName: serviceName,
                    serviceNamespace: "application",
                    serviceVersion: serviceVersion,
                    serviceInstanceId: Environment.MachineName)
                .AddAttributes(
                    [
                        new KeyValuePair<string, object>("deployment.environment", deploymentEnvironment)
                    ]);
        }

        var openTelemetry = builder.Services.AddOpenTelemetry()
            .ConfigureResource(ConfigureResource)
            .WithTracing(tracing =>
            {
                foreach (var source in activitySources.Distinct(StringComparer.Ordinal))
                {
                    tracing.AddSource(source);
                }

                tracing
                    .AddSource("Microsoft.Extensions.AI")
                    .AddSource("Npgsql")
                    .AddSource("Microsoft.EntityFrameworkCore")
                    .AddAspNetCoreInstrumentation(options =>
                    {
                        options.Filter = context =>
                            !context.Request.Path.StartsWithSegments("/health");
                    })
                    .AddHttpClientInstrumentation(options =>
                    {
                        options.RecordException = true;
                    })
                    .AddEntityFrameworkCoreInstrumentation()
                    .AddSentry();

                if (!string.IsNullOrWhiteSpace(otlpEndpoint))
                {
                    tracing.AddOtlpExporter(options =>
                    {
                        options.Endpoint = new Uri(otlpEndpoint);
                    });
                }
            });

        if (configuration.GetValue("OpenTelemetry:MetricsEnabled", true))
        {
            openTelemetry.WithMetrics(metrics =>
            {
                foreach (var meter in meters.Distinct(StringComparer.Ordinal))
                {
                    metrics.AddMeter(meter);
                }

                metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddMeter(
                        "Microsoft.Extensions.AI",
                        "Npgsql",
                        "System.Net.Http");

                if (!string.IsNullOrWhiteSpace(otlpEndpoint))
                {
                    metrics.AddOtlpExporter(options =>
                    {
                        options.Endpoint = new Uri(otlpEndpoint);
                    });
                }
            });
        }

        if (configuration.GetValue("OpenTelemetry:LogsEnabled", true))
        {
            var resourceBuilder = ResourceBuilder.CreateDefault();
            ConfigureResource(resourceBuilder);

            builder.Logging.AddOpenTelemetry(logging =>
            {
                logging.IncludeFormattedMessage = true;
                logging.IncludeScopes = true;
                logging.ParseStateValues = true;
                logging.SetResourceBuilder(resourceBuilder);

                if (!string.IsNullOrWhiteSpace(otlpEndpoint))
                {
                    logging.AddOtlpExporter(options =>
                    {
                        options.Endpoint = new Uri(otlpEndpoint);
                    });
                }
            });
        }
    }
}

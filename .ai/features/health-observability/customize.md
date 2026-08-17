# Health & Observability — Customize

## 1. Add a new dependency health check (e.g. external SAP API)

1. Install the relevant `AspNetCore.HealthChecks.*` package, or write a custom check implementing `IHealthCheck`:
   ```csharp
   public class SapApiHealthCheck : IHealthCheck
   {
       private readonly HttpClient _http;
       public SapApiHealthCheck(HttpClient http) { _http = http; }
       public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext ctx, CancellationToken ct)
       {
           try
           {
               var resp = await _http.GetAsync("https://sap.example/ping", ct);
               return resp.IsSuccessStatusCode ? HealthCheckResult.Healthy() : HealthCheckResult.Degraded($"SAP returned {resp.StatusCode}");
           }
           catch (Exception ex) { return HealthCheckResult.Unhealthy(ex.Message, ex); }
       }
   }
   ```
2. Register it in `Program.cs:74-76`:
   ```csharp
   builder.Services.AddHttpClient<SapApiHealthCheck>();
   builder.Services.AddHealthChecks()
       .AddNpgSql(...)
       .AddRedis(...)
       .AddCheck<SapApiHealthCheck>("sap");
   ```
3. The `/health` JSON response will now include a `sap` entry. Confirm via `curl http://localhost:5002/health | jq`.

## 2. Tune Sentry trace sampling

1. Edit `appsettings.{Env}.json`:
   ```json
   "Sentry": {
     "Dsn": "https://...@sentry.io/...",
     "Environment": "production",
     "TracesSampleRate": "0.05",
     "ServiceName": "myapp-api"
   }
   ```
2. The default in `ObservabilityExtensions.cs:21` is `0.2`. The boot code parses the string with `double.TryParse` so the value can live in env vars.
3. Restart. Confirm by hitting an endpoint and checking the Sentry → Performance dashboard for the new sample volume.

## 3. Disable Sentry in a specific environment

Set `Sentry:Dsn` to an empty string (or remove the key). The first early-return in `AddObservability` short-circuits the whole feature — no Sentry SDK init, no OTel registration. The app continues working normally without observability.

## 4. Switch OTel exporter from Sentry to OTLP / Jaeger

1. Replace `tracing.AddSentry()` in `ObservabilityExtensions.cs:44` with:
   ```csharp
   tracing.AddOtlpExporter(opt =>
   {
       opt.Endpoint = new Uri(builder.Configuration["Otel:Endpoint"] ?? "http://localhost:4317");
       opt.Protocol = OtlpExportProtocol.Grpc;
   });
   ```
2. Add the `OpenTelemetry.Exporter.OpenTelemetryProtocol` package.
3. You can keep Sentry for errors-only by removing `options.UseOpenTelemetry()` from the `UseSentry(...)` call. Sentry then captures errors via its own pipeline; OTel spans go to OTLP.

## 5. Add a custom span for an expensive operation

Auto-instrumentation captures HTTP requests, EF Core queries, and outbound HttpClient calls. For pure-CPU work (e.g. PDF generation) add a manual span:

```csharp
using var activity = MyActivitySource.StartActivity("GeneratePdf");
activity?.SetTag("entity.id", entityId);
// ... work ...
```

Where `MyActivitySource = new ActivitySource("MyApp")` is a static field. The OTel SDK picks up activities from any `ActivitySource` registered via `tracing.AddSource("MyApp")`.

## 6. Customize the `/health/ready` body

The current implementation returns:

```csharp
app.MapGet("/health/ready", () => Results.Ok(new
{
    status = "healthy",
    service = "apptemplate-api",
    timestamp = DateTimeHelper.Now
}));
```

To include build version / git SHA:

1. At build time, write the SHA into an env var (`COMMIT_SHA`).
2. Edit the lambda to read it:
   ```csharp
   var commit = Environment.GetEnvironmentVariable("COMMIT_SHA") ?? "dev";
   app.MapGet("/health/ready", () => Results.Ok(new {
       status = "healthy",
       service = "myapp-api",
       commit,
       timestamp = DateTimeHelper.Now
   }));
   ```

## 7. Make `/health` return a richer JSON body

By default `MapHealthChecks("/health")` returns a flat OK / Service Unavailable. To return per-check details:

```csharp
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var body = new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description
            })
        };
        await context.Response.WriteAsync(JsonSerializer.Serialize(body));
    }
});
```

Useful when a load balancer or uptime monitor should see which specific dependency failed, not just that the app is unhealthy.

## 8. Add the correlation id to outbound HttpClient calls

`CorrelationIdMiddleware` reads/sets `X-Correlation-Id` on the response. To propagate it on outbound calls:

1. Register a `DelegatingHandler`:
   ```csharp
   public class CorrelationHandler : DelegatingHandler
   {
       private readonly IHttpContextAccessor _accessor;
       public CorrelationHandler(IHttpContextAccessor accessor) { _accessor = accessor; }
       protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
       {
           var cid = _accessor.HttpContext?.TraceIdentifier;
           if (!string.IsNullOrEmpty(cid))
               request.Headers.TryAddWithoutValidation("X-Correlation-Id", cid);
           return base.SendAsync(request, ct);
       }
   }
   ```
2. Attach to typed clients:
   ```csharp
   builder.Services.AddTransient<CorrelationHandler>();
   builder.Services.AddHttpClient<IPushNotificationService, OneSignalPushNotificationService>()
       .AddHttpMessageHandler<CorrelationHandler>();
   ```

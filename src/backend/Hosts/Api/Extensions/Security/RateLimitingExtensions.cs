using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace Api.Extensions;

/// <summary>
/// Configures rate limiting policies using the built-in .NET rate limiter.
/// </summary>
public static class RateLimitingExtensions
{
    public const string FixedPolicy = "fixed";
    public const string SlidingPolicy = "sliding";

    public static IServiceCollection AddRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.AddFixedWindowLimiter(FixedPolicy, opt =>
            {
                opt.PermitLimit = configuration.GetValue("RateLimiting:Fixed:PermitLimit", 100);
                opt.Window = TimeSpan.FromSeconds(configuration.GetValue("RateLimiting:Fixed:WindowSeconds", 60));
                opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                opt.QueueLimit = configuration.GetValue("RateLimiting:Fixed:QueueLimit", 10);
            });

            options.AddSlidingWindowLimiter(SlidingPolicy, opt =>
            {
                opt.PermitLimit = configuration.GetValue("RateLimiting:Sliding:PermitLimit", 20);
                opt.Window = TimeSpan.FromSeconds(configuration.GetValue("RateLimiting:Sliding:WindowSeconds", 60));
                opt.SegmentsPerWindow = 4;
                opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                opt.QueueLimit = configuration.GetValue("RateLimiting:Sliding:QueueLimit", 5);
            });

            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            {
                var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                return RateLimitPartition.GetFixedWindowLimiter(clientIp, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = configuration.GetValue("RateLimiting:Global:PermitLimit", 300),
                    Window = TimeSpan.FromSeconds(configuration.GetValue("RateLimiting:Global:WindowSeconds", 60)),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 0,
                });
            });
        });

        return services;
    }
}

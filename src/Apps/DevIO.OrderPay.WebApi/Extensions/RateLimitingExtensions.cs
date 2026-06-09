using System.Globalization;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace DevIO.OrderPay.WebApi.Extensions;

public sealed class RateLimiterSettings
{
    public bool Enabled  { get; set; } = true;
    public int  GenLimit { get; set; } = 100;
    public int  WrtLimit { get; set; } = 20;
}

public static class RateLimitingExtensions
{
    public const string GeneralPolicy = "general";
    public const string WritesPolicy  = "writes";

    public static WebApplicationBuilder AddRateLimiting(this WebApplicationBuilder builder)
    {
        // Bind settings from configuration so tests can override via PostConfigure<RateLimiterSettings>.
        builder.Services.Configure<RateLimiterSettings>(o =>
        {
            o.Enabled  = builder.Configuration.GetValue<bool>("RateLimiting:Enabled", defaultValue: true);
            o.GenLimit = builder.Configuration.GetValue<int>("RateLimiting:General:PermitLimit", defaultValue: 100);
            o.WrtLimit = builder.Configuration.GetValue<int>("RateLimiting:Writes:PermitLimit",  defaultValue: 20);
        });

        builder.Services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.OnRejected = (context, _) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out TimeSpan retryAfter))
                    context.HttpContext.Response.Headers.RetryAfter =
                        ((int)retryAfter.TotalSeconds).ToString(CultureInfo.InvariantCulture);
                return ValueTask.CompletedTask;
            };

            options.AddPolicy(GeneralPolicy, context =>
            {
                var settings = context.RequestServices.GetRequiredService<IOptions<RateLimiterSettings>>().Value;
                if (!settings.Enabled)
                    return RateLimitPartition.GetNoLimiter(string.Empty);
                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: GetKey(context),
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit          = settings.GenLimit,
                        Window               = TimeSpan.FromMinutes(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit           = 0
                    });
            });

            options.AddPolicy(WritesPolicy, context =>
            {
                var settings = context.RequestServices.GetRequiredService<IOptions<RateLimiterSettings>>().Value;
                if (!settings.Enabled)
                    return RateLimitPartition.GetNoLimiter(string.Empty);
                return RateLimitPartition.GetSlidingWindowLimiter(
                    partitionKey: GetKey(context),
                    _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit          = settings.WrtLimit,
                        Window               = TimeSpan.FromMinutes(1),
                        SegmentsPerWindow    = 4,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit           = 0
                    });
            });
        });

        return builder;
    }

    // Prefer authenticated user identity over IP so users behind a shared NAT
    // each get their own quota rather than competing for one IP-based bucket.
    private static string GetKey(HttpContext context) =>
        context.User?.FindFirstValue("sub")
            ?? context.User?.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? context.Connection.RemoteIpAddress?.ToString()
            ?? "unknown";
}

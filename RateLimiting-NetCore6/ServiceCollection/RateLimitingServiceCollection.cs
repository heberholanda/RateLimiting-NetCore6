using System.Globalization;
using System.Net;
using System.Threading.RateLimiting;

namespace RateLimiting_NetCore6.ServiceCollection
{
    /// <summary>
    /// Configuration options for Rate Limiting policies.
    /// </summary>
    public class RateLimitingOptions
    {
        public PolicyOptions GlobalPolicy { get; set; } = new();
        public PolicyOptions IpPolicy { get; set; } = new();
        public PolicyOptions ApiPolicy { get; set; } = new();
        public PolicyOptions ClientPolicy { get; set; } = new();
        public List<string> EndpointWhitelist { get; set; } = new();
        public List<string> IpWhitelist { get; set; } = new();
        public List<string> ClientWhitelist { get; set; } = new();
    }

    public class PolicyOptions
    {
        public int PermitLimit { get; set; } = 10;
        public TimeSpan Window { get; set; } = TimeSpan.FromMinutes(1);
        public int QueueLimit { get; set; } = 0;
    }

    internal static class RateLimitingServiceCollection
    {
        /// <summary>
        /// Adds native ASP.NET Core Rate Limiting services to the DI container.
        /// Configures IP-based, Client-based, and endpoint-specific rate limiting using settings from appsettings.json.
        /// </summary>
        internal static IServiceCollection AddRateLimitingServiceCollection(this IServiceCollection services, IConfiguration configuration)
        {
            // Bind rate limiting configuration from appsettings.json
            var rateLimitConfig = configuration.GetSection("RateLimiting").Get<RateLimitingOptions>() ?? new RateLimitingOptions();

            services.AddRateLimiter(options =>
            {
                // Global rejection handler - returns 429 with custom message
                options.OnRejected = async (context, cancellationToken) =>
                {
                    context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                    
                    if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                    {
                        context.HttpContext.Response.Headers.RetryAfter = retryAfter.TotalSeconds.ToString(CultureInfo.InvariantCulture);
                    }

                    await context.HttpContext.Response.WriteAsJsonAsync(new
                    {
                        message = "Too many requests. Please try again later.",
                        statusCode = 429,
                        retryAfterSeconds = retryAfter.TotalSeconds
                    }, cancellationToken);
                };

                // Policy 1: Global IP-based rate limiting
                options.AddPolicy("IpPolicy", httpContext =>
                {
                    var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                    // Check if IP is in whitelist
                    if (rateLimitConfig.IpWhitelist.Contains(ipAddress))
                    {
                        return RateLimitPartition.GetNoLimiter<string>("whitelist");
                    }

                    return RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: ipAddress,
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = rateLimitConfig.IpPolicy.PermitLimit,
                            Window = rateLimitConfig.IpPolicy.Window,
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            QueueLimit = rateLimitConfig.IpPolicy.QueueLimit
                        });
                });

                // Policy 2: Client-based rate limiting (using X-ClientId header)
                options.AddPolicy("ClientPolicy", httpContext =>
                {
                    var clientId = httpContext.Request.Headers["X-ClientId"].FirstOrDefault() ?? "anonymous";

                    // Check if client is in whitelist
                    if (rateLimitConfig.ClientWhitelist.Contains(clientId))
                    {
                        return RateLimitPartition.GetNoLimiter<string>("whitelist");
                    }

                    return RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: clientId,
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = rateLimitConfig.ClientPolicy.PermitLimit,
                            Window = rateLimitConfig.ClientPolicy.Window,
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            QueueLimit = rateLimitConfig.ClientPolicy.QueueLimit
                        });
                });

                // Policy 3: API endpoints rate limiting
                options.AddPolicy("ApiPolicy", httpContext =>
                {
                    var endpoint = $"{httpContext.Request.Method}:{httpContext.Request.Path}";
                    var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                    // Check if endpoint is in whitelist
                    if (rateLimitConfig.EndpointWhitelist.Contains(endpoint))
                    {
                        return RateLimitPartition.GetNoLimiter<string>("whitelist");
                    }

                    return RateLimitPartition.GetSlidingWindowLimiter(
                        partitionKey: $"{ipAddress}:{endpoint}",
                        factory: _ => new SlidingWindowRateLimiterOptions
                        {
                            PermitLimit = rateLimitConfig.ApiPolicy.PermitLimit,
                            Window = rateLimitConfig.ApiPolicy.Window,
                            SegmentsPerWindow = 2,
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            QueueLimit = 0
                        });
                });

                // Policy 4: Token bucket for burst traffic
                options.AddPolicy("TokenBucketPolicy", httpContext =>
                {
                    var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                    return RateLimitPartition.GetTokenBucketLimiter(
                        partitionKey: ipAddress,
                        factory: _ => new TokenBucketRateLimiterOptions
                        {
                            TokenLimit = 100,
                            ReplenishmentPeriod = TimeSpan.FromMinutes(1),
                            TokensPerPeriod = 20,
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            QueueLimit = 10
                        });
                });

                // Policy 5: Concurrency limiter for resource-intensive endpoints
                options.AddPolicy("ConcurrencyPolicy", httpContext =>
                {
                    var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                    return RateLimitPartition.GetConcurrencyLimiter(
                        partitionKey: ipAddress,
                        factory: _ => new ConcurrencyLimiterOptions
                        {
                            PermitLimit = 5,
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            QueueLimit = 10
                        });
                });

                // Global limiter (fallback)
                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                {
                    var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                    return RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: ipAddress,
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = rateLimitConfig.GlobalPolicy.PermitLimit,
                            Window = rateLimitConfig.GlobalPolicy.Window,
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            QueueLimit = rateLimitConfig.GlobalPolicy.QueueLimit
                        });
                });
            });

            return services;
        }

        /// <summary>
        /// Adds native Rate Limiting middleware to the HTTP request pipeline.
        /// IMPORTANT: Must be called after UseRouting() and before UseAuthorization() in Program.cs.
        /// </summary>
        internal static IApplicationBuilder UseRateLimitingServiceCollection(this IApplicationBuilder app)
        {
            // Enable the native rate limiting middleware
            app.UseRateLimiter();

            return app;
        }
    }
}

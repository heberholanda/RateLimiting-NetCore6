using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

namespace RateLimiting_NetCore6.Controllers
{
    /// <summary>
    /// Controller for demonstrating IP-based rate limiting with native ASP.NET Core Rate Limiter.
    /// This controller showcases different rate limiting strategies.
    /// </summary>
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class IpRateLimitController : ControllerBase
    {
        private readonly ILogger<IpRateLimitController> _logger;

        public IpRateLimitController(ILogger<IpRateLimitController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Gets IP rate limiting information with IP-based policy applied.
        /// This endpoint is protected by the IpPolicy (3 requests per 30 seconds).
        /// </summary>
        /// <returns>IP address and policy information.</returns>
        [HttpGet]
        [EnableRateLimiting("IpPolicy")]
        public IActionResult GetRateLimitInfo()
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            
            _logger.LogInformation("IP Rate Limit endpoint accessed from IP: {IpAddress}", ipAddress);

            return Ok(new
            {
                ipAddress,
                message = "This endpoint is protected by IP-based rate limiting",
                policy = "IpPolicy: 3 requests per 30 seconds",
                timestamp = DateTime.UtcNow
            });
        }

        /// <summary>
        /// Test endpoint with Sliding Window rate limiting.
        /// Uses ApiPolicy which implements sliding window algorithm for smoother rate limiting.
        /// </summary>
        /// <returns>Rate limiting test result.</returns>
        [HttpGet]
        [EnableRateLimiting("ApiPolicy")]
        public IActionResult TestSlidingWindow()
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            return Ok(new
            {
                ipAddress,
                message = "Testing Sliding Window rate limiting",
                policy = "ApiPolicy: 6 requests per 60 seconds (sliding window)",
                timestamp = DateTime.UtcNow
            });
        }

        /// <summary>
        /// Test endpoint with Token Bucket rate limiting.
        /// Allows burst traffic up to token limit with gradual replenishment.
        /// </summary>
        /// <returns>Rate limiting test result.</returns>
        [HttpPost]
        [EnableRateLimiting("TokenBucketPolicy")]
        public IActionResult TestTokenBucket()
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            return Ok(new
            {
                ipAddress,
                message = "Testing Token Bucket rate limiting",
                policy = "TokenBucketPolicy: 100 tokens, 20 tokens per minute replenishment",
                timestamp = DateTime.UtcNow
            });
        }

        /// <summary>
        /// Test endpoint with Concurrency rate limiting.
        /// Limits the number of concurrent requests from the same IP.
        /// </summary>
        /// <returns>Rate limiting test result.</returns>
        [HttpGet]
        [EnableRateLimiting("ConcurrencyPolicy")]
        public async Task<IActionResult> TestConcurrency()
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            // Simulate a long-running operation
            await Task.Delay(2000);

            return Ok(new
            {
                ipAddress,
                message = "Testing Concurrency rate limiting",
                policy = "ConcurrencyPolicy: Maximum 5 concurrent requests per IP",
                timestamp = DateTime.UtcNow
            });
        }

        /// <summary>
        /// Endpoint without rate limiting applied.
        /// Use DisableRateLimiting attribute to bypass rate limiting.
        /// </summary>
        /// <returns>Unrestricted response.</returns>
        [HttpGet]
        [DisableRateLimiting]
        public IActionResult NoLimit()
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            return Ok(new
            {
                ipAddress,
                message = "This endpoint has no rate limiting",
                policy = "No rate limiting applied (whitelisted)",
                timestamp = DateTime.UtcNow
            });
        }

        /// <summary>
        /// Gets current rate limit statistics and metadata.
        /// </summary>
        /// <returns>Rate limit statistics.</returns>
        [HttpGet]
        [EnableRateLimiting("IpPolicy")]
        public IActionResult GetStatistics()
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            return Ok(new
            {
                ipAddress,
                message = "Rate limiting statistics",
                availablePolicies = new[]
                {
                    "IpPolicy - Fixed Window (3 req/30s)",
                    "ClientPolicy - Fixed Window (10 req/1m)",
                    "ApiPolicy - Sliding Window (6 req/1m)",
                    "TokenBucketPolicy - Token Bucket (100 tokens, 20/min)",
                    "ConcurrencyPolicy - Concurrency (5 concurrent)"
                },
                timestamp = DateTime.UtcNow
            });
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace RateLimiting_NetCore6.Controllers
{
    /// <summary>
    /// Controller for demonstrating Client-based rate limiting with native ASP.NET Core Rate Limiter.
    /// Client identification is done via the X-ClientId header.
    /// </summary>
    [ApiController]
    [Route("api/[controller]/[action]")]
    [EnableRateLimiting("ClientPolicy")]
    public class ClientRateLimitController : ControllerBase
    {
        private readonly ILogger<ClientRateLimitController> _logger;

        public ClientRateLimitController(ILogger<ClientRateLimitController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Gets client rate limiting information.
        /// Requires X-ClientId header for client identification.
        /// Protected by ClientPolicy (10 requests per minute).
        /// </summary>
        /// <returns>Client identification and policy information.</returns>
        [HttpGet]
        public IActionResult GetClientInfo()
        {
            var clientId = HttpContext.Request.Headers["X-ClientId"].FirstOrDefault() ?? "anonymous";
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            _logger.LogInformation("Client Rate Limit endpoint accessed by ClientId: {ClientId}", clientId);

            return Ok(new
            {
                clientId,
                ipAddress,
                message = "This endpoint is protected by Client-based rate limiting",
                policy = "ClientPolicy: 10 requests per minute",
                hint = "Include X-ClientId header to identify your client",
                whitelistedClients = new[] { "dev-id-1", "dev-id-2" },
                timestamp = DateTime.UtcNow
            });
        }

        /// <summary>
        /// Test endpoint for client policy validation.
        /// Simulates adding or updating client policies dynamically.
        /// </summary>
        /// <returns>Client policy information.</returns>
        [HttpPost]
        public IActionResult TestClientPolicy()
        {
            var clientId = HttpContext.Request.Headers["X-ClientId"].FirstOrDefault() ?? "anonymous";
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            var isWhitelisted = clientId is "dev-id-1" or "dev-id-2";

            return Ok(new
            {
                clientId,
                ipAddress,
                isWhitelisted,
                message = isWhitelisted 
                    ? "Client is whitelisted - no rate limiting applied" 
                    : "Client is subject to rate limiting",
                policy = "ClientPolicy: 10 requests per minute",
                timestamp = DateTime.UtcNow
            });
        }

        /// <summary>
        /// Simulates a high-frequency operation for testing rate limits.
        /// Use this endpoint to test if rate limiting is working correctly.
        /// </summary>
        /// <returns>Operation result.</returns>
        [HttpGet]
        public IActionResult HighFrequencyOperation()
        {
            var clientId = HttpContext.Request.Headers["X-ClientId"].FirstOrDefault() ?? "anonymous";

            return Ok(new
            {
                clientId,
                message = "High-frequency operation completed",
                policy = "ClientPolicy: 10 requests per minute",
                tip = "Make multiple rapid requests to test rate limiting",
                timestamp = DateTime.UtcNow
            });
        }

        /// <summary>
        /// Gets detailed information about client rate limiting configuration.
        /// </summary>
        /// <returns>Configuration details.</returns>
        [HttpGet]
        public IActionResult GetConfiguration()
        {
            var clientId = HttpContext.Request.Headers["X-ClientId"].FirstOrDefault() ?? "anonymous";

            return Ok(new
            {
                clientId,
                configuration = new
                {
                    clientIdentification = "X-ClientId header",
                    rateLimitPolicy = "Fixed Window",
                    permitLimit = 10,
                    windowSize = "1 minute",
                    queueLimit = 0,
                    whitelistedClients = new[] { "dev-id-1", "dev-id-2" }
                },
                usage = new
                {
                    headerName = "X-ClientId",
                    headerExample = "curl -H 'X-ClientId: my-client-app' https://localhost:5001/api/ClientRateLimit/GetClientInfo"
                },
                timestamp = DateTime.UtcNow
            });
        }

        /// <summary>
        /// Test endpoint with mixed IP and Client rate limiting.
        /// Demonstrates how both policies can work together.
        /// </summary>
        /// <returns>Combined policy information.</returns>
        [HttpPost]
        [EnableRateLimiting("IpPolicy")]
        public IActionResult TestCombinedPolicy()
        {
            var clientId = HttpContext.Request.Headers["X-ClientId"].FirstOrDefault() ?? "anonymous";
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            return Ok(new
            {
                clientId,
                ipAddress,
                message = "This endpoint applies both Client and IP rate limiting",
                policies = new[]
                {
                    "ClientPolicy: 10 requests per minute (controller-level)",
                    "IpPolicy: 3 requests per 30 seconds (method-level)"
                },
                note = "The most restrictive policy applies",
                timestamp = DateTime.UtcNow
            });
        }
    }
}

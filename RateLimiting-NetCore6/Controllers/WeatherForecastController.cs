using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace RateLimiting_NetCore6.Controllers
{
    /// <summary>
    /// Sample weather forecast controller to demonstrate rate limiting functionality.
    /// This endpoint is subject to the ApiPolicy rate limiting.
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    [EnableRateLimiting("ApiPolicy")]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly ILogger<WeatherForecastController> _logger;

        public WeatherForecastController(ILogger<WeatherForecastController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Gets a collection of random weather forecasts.
        /// This endpoint demonstrates rate limiting in action - try making multiple requests quickly.
        /// Protected by ApiPolicy (6 requests per minute using sliding window).
        /// </summary>
        /// <returns>A collection of weather forecast data for the next 5 days.</returns>
        [HttpGet(Name = "GetWeatherForecast")]
        public IEnumerable<WeatherForecast> Get()
        {
            _logger.LogInformation("Weather forecast requested");

            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateTime.Now.AddDays(index),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            })
            .ToArray();
        }

        /// <summary>
        /// Gets weather forecast for a specific city.
        /// This endpoint uses the same ApiPolicy rate limiting.
        /// </summary>
        /// <param name="city">City name to get forecast for.</param>
        /// <returns>Weather forecast data.</returns>
        [HttpGet("city/{city}")]
        public IActionResult GetByCity(string city)
        {
            _logger.LogInformation("Weather forecast requested for city: {City}", city);

            var forecast = new WeatherForecast
            {
                Date = DateTime.Now.AddDays(1),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            };

            return Ok(new
            {
                city,
                forecast,
                policy = "ApiPolicy: 6 requests per minute (sliding window)"
            });
        }

        /// <summary>
        /// Gets weather forecast without rate limiting.
        /// This endpoint is whitelisted and not subject to rate limits.
        /// </summary>
        /// <returns>Weather forecast data.</returns>
        [HttpGet("unlimited")]
        [DisableRateLimiting]
        public IActionResult GetUnlimited()
        {
            _logger.LogInformation("Unlimited weather forecast requested");

            return Ok(new
            {
                message = "This endpoint has no rate limiting",
                forecast = Enumerable.Range(1, 5).Select(index => new WeatherForecast
                {
                    Date = DateTime.Now.AddDays(index),
                    TemperatureC = Random.Shared.Next(-20, 55),
                    Summary = Summaries[Random.Shared.Next(Summaries.Length)]
                }).ToArray()
            });
        }
    }
}

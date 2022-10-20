using AspNetCoreRateLimit;
using System.Net;

namespace RateLimiting_NetCore6.ServiceCollection
{
    internal static class RateLimitingServiceCollection
    {
        internal static IServiceCollection AddRateLimitingServiceCollection(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddMemoryCache();

            // Ip / Client Rate Limiting from appsettings.json
            services.Configure<IpRateLimitOptions>(configuration.GetSection("IpRateLimiting"));
            services.Configure<ClientRateLimitOptions>(configuration.GetSection("ClientRateLimiting"));

            // Policies
            services.Configure<IpRateLimitPolicies>(options => configuration.GetSection("IpRateLimitPolicies").Bind(options));
            services.Configure<ClientRateLimitPolicies>(options => configuration.GetSection("ClientRateLimitPolicies").Bind(options));

            services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
            services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
            services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

            services.AddInMemoryRateLimiting();

            /*
            services.Configure<IpRateLimitOptions>(options =>
            {
                options.EnableEndpointRateLimiting = true;
                options.StackBlockedRequests = false;
                options.HttpStatusCode = (int)HttpStatusCode.TooManyRequests;
                options.RealIpHeader = "X-Real-IP";
                options.QuotaExceededMessage = "Maximum request limit exceeded!";
                options.QuotaExceededResponse = new QuotaExceededResponse()
                {
                    Content = "{{ \"message\": \"Whoa! Calm down, cowboy!\", \"details\": \"Quota exceeded. Maximum allowed: {0} per {1}. Please try again in {2} second(s).\" }}",
                    ContentType = "application/json",
                    StatusCode = (int)HttpStatusCode.TooManyRequests
            };
                options.EndpointWhitelist = new List<string>() { "get:/api/license", "*:/api/status" };
                //options.IpWhitelist = new List<string>() { "127.0.0.1", "::1/10", "192.168.0.0/24" };
                options.GeneralRules = new List<RateLimitRule>
                    {
                        new RateLimitRule
                        {
                            Endpoint = "*",
                            Period = "20s",
                            Limit = 2
                        }
                    };
            });
            */
            return services;
        }

        internal static IApplicationBuilder UseRateLimitingServiceCollection(this IApplicationBuilder app)
        {
            // Ip Rate Limiting
            app.UseIpRateLimiting();

            // Client Rate Limiting
            app.UseClientRateLimiting();

            return app;
        }
    }
}

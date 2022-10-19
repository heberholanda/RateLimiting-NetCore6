using AspNetCoreRateLimit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace RateLimiting_NetCore6.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class IpRateLimitController : ControllerBase
    {
		private readonly IpRateLimitOptions _options;
		private readonly IIpPolicyStore _ipPolicyStore;

		public IpRateLimitController(IOptions<IpRateLimitOptions> optionsAccessor, IIpPolicyStore ipPolicyStore)
		{
			_options = optionsAccessor.Value;
			_ipPolicyStore = ipPolicyStore;
		}

		[HttpGet]
		public async Task<IpRateLimitPolicies> GetAsync()
		{
			await _ipPolicyStore.SeedAsync();

			IpRateLimitPolicies? ipPolicies = await _ipPolicyStore.GetAsync(_options.IpPolicyPrefix);

			return ipPolicies;
		}

		[HttpPost]
		public async Task<IpRateLimitPolicies?> PostAsync()
		{
			await _ipPolicyStore.SeedAsync();

			IpRateLimitPolicies? ipPolicies = await _ipPolicyStore.GetAsync(_options.IpPolicyPrefix);

			if (ipPolicies != null)
            {
				var newIpRate = new IpRateLimitPolicy
				{
					Ip = "8.8.4.4",
					Rules = new List<RateLimitRule>(new RateLimitRule[]
					{
						new RateLimitRule
						{
							Endpoint = "*:/api/testupdate",
							Limit = 100,
							Period = "1d"
						}
					})
				};

				ipPolicies.IpRules.Add(newIpRate);

				await _ipPolicyStore.SetAsync(_options.IpPolicyPrefix, ipPolicies);

				return ipPolicies;
			}

			return null;
		}
	}
}
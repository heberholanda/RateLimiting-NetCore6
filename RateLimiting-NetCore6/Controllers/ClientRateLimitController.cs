using AspNetCoreRateLimit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Net;

namespace RateLimiting_NetCore6.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class ClientRateLimitController : ControllerBase
    {
		private readonly ClientRateLimitOptions _options;
        private readonly IClientPolicyStore _clientPolicyStore;

		public ClientRateLimitController(IOptions<ClientRateLimitOptions> optionsAccessor, IClientPolicyStore clientPolicyStore)
		{
			_options = optionsAccessor.Value;
			_clientPolicyStore = clientPolicyStore;
		}

		[HttpGet]
		public async Task<ClientRateLimitPolicy> GetAsync()
		{
			await _clientPolicyStore.SeedAsync();

			var id = $"{_options.ClientPolicyPrefix}_client-1";
			ClientRateLimitPolicy? clientPolicy = await _clientPolicyStore.GetAsync(id);

            return clientPolicy;
		}

		[HttpPost]
		public async Task<ClientRateLimitPolicy?> PostAsync()
		{
			await _clientPolicyStore.SeedAsync();

			var id = $"{_options.ClientPolicyPrefix}_client-1";
			ClientRateLimitPolicy? clientPolicies = await _clientPolicyStore.GetAsync(id);

			if (clientPolicies != null)
            {
				clientPolicies.Rules.Add(new RateLimitRule
				{
					Endpoint = "*/api/testpolicyupdate",
					Period = "1h",
					Limit = 100
				});
				await _clientPolicyStore.SetAsync(id, clientPolicies);

				return clientPolicies;
			}

			return null;
		}
	}
}
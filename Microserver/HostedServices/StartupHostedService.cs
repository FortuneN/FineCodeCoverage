using FineCodeCoverage.Core.Coverage;
using Microsoft.Extensions.Hosting;
using System.Threading;
using System.Threading.Tasks;

namespace FineCodeCoverage.HostedServices
{
	public class StartupHostedService : IHostedService
	{
		private readonly ICoverageService _coverageService;

		public StartupHostedService(ICoverageService coverageService)
		{
			_coverageService = coverageService;
		}

		public async Task StartAsync(CancellationToken cancellationToken)
		{
			await _coverageService.InitializeAsync();
		}

		public async Task StopAsync(CancellationToken cancellationToken)
		{
			await Task.CompletedTask;
		}
	}
}
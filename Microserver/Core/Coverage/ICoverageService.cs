using FineCodeCoverage.Core.Model;
using System.Threading.Tasks;

namespace FineCodeCoverage.Core.Coverage
{
	public interface ICoverageService
	{
		public Task InitializeAsync();

		public void ClearProcesses();

		public Task<CalculateCoverageResponse> CalculateCoverageAsync(CalculateCoverageRequest request);

		public Task CalculateCoverageAsync(CoverageProject project, CoverageProjectSettings defaultSettings);
	}
}

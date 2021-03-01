using System.Threading.Tasks;
using FineCodeCoverage.Engine.Model;

namespace FineCodeCoverage.Engine.Coverlet
{
	internal interface ICoverletDataCollectorUtil
	{
		bool CanUseDataCollector(ICoverageProject coverageProject);
		Task<bool> RunAsync(bool throwError = false);

		void Initialize(string appDataFolder);
	}
}

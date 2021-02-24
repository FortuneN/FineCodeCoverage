using System.Threading.Tasks;
using FineCodeCoverage.Engine.Model;

namespace FineCodeCoverage.Engine.Coverlet
{
    internal interface ICoverletGlobalUtil
    {
		void Initialize(string appDataFolder);
		Task<bool> RunAsync(ICoverageProject project, bool throwError = false);

	}
}

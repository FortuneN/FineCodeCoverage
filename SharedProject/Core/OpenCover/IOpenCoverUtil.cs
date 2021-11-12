using FineCodeCoverage.Engine.Model;
using System.Threading.Tasks;

namespace FineCodeCoverage.Engine.OpenCover
{
    internal interface IOpenCoverUtil
    {
		Task<bool> RunOpenCoverAsync(ICoverageProject project, bool throwError = false);
		void Initialize(string appDataFolder);
	}
}

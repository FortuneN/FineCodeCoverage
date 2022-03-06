using FineCodeCoverage.Engine.Model;
using System.Threading;
using System.Threading.Tasks;

namespace FineCodeCoverage.Engine.OpenCover
{
    internal interface IOpenCoverUtil
    {
		Task RunOpenCoverAsync(ICoverageProject project, CancellationToken cancellationToken);
		void Initialize(string appDataFolder, CancellationToken cancellationToken);
	}
}

using System.Threading;
using System.Threading.Tasks;
using FineCodeCoverage.Engine.Model;

namespace FineCodeCoverage.Engine
{
    internal interface ICoverageUtilManager
    {
        void Initialize(string appDataFolder, CancellationToken cancellationToken);
        Task RunCoverageAsync(ICoverageProject project, CancellationToken cancellationToken);
        string CoverageToolName(ICoverageProject project);
    }
}

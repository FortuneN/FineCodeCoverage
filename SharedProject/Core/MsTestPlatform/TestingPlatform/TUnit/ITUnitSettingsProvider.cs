using System.Threading.Tasks;
using System.Threading;

namespace FineCodeCoverage.Core.MsTestPlatform.TestingPlatform
{
    internal interface ITUnitSettingsProvider
    {
        Task<TUnitSettings> ProvideAsync(ITUnitCoverageProject tUnitCoverageProject, CancellationToken cancellationToken);
    }
}

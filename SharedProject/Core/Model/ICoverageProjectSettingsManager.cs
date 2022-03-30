using FineCodeCoverage.Options;
using System.Threading.Tasks;

namespace FineCodeCoverage.Engine.Model
{
    internal interface ICoverageProjectSettingsManager
    {
        Task<IAppOptions> GetSettingsAsync(ICoverageProject coverageProject);
    }
}

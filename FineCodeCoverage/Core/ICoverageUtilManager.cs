using System.Threading.Tasks;
using FineCodeCoverage.Engine.Model;

namespace FineCodeCoverage.Engine
{
    internal interface ICoverageUtilManager
    {
        void Initialize(string appDataFolder);
        Task<bool> RunCoverageAsync(ICoverageProject project, bool throwError = false);
    }
}

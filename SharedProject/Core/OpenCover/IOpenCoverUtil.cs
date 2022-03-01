using System.Threading.Tasks;
using FineCodeCoverage.Engine.Model;

namespace FineCodeCoverage.Engine.OpenCover
{
    internal interface IOpenCoverUtil
    {
        Task<bool> RunOpenCoverAsync(ICoverageProject project, bool throwError = false);
        void Initialize(string appDataFolder);
    }
}

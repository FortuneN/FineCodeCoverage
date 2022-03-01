using System.Threading.Tasks;
using FineCodeCoverage.Engine.Model;

namespace FineCodeCoverage.Engine.Coverlet
{
    internal interface ICoverletUtil
    {
        void Initialize(string appDataFolder);
        Task<bool> RunCoverletAsync(ICoverageProject project, bool throwError = false);
    }
}

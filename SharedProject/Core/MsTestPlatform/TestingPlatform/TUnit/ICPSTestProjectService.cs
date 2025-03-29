using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.Shell.Interop;
using System.Threading.Tasks;

namespace FineCodeCoverage.Core.MsTestPlatform.TestingPlatform
{
    internal interface ICPSTestProjectService
    {
        Task<ConfiguredProject> GetProjectAsync(IVsHierarchy hierarchy);
    }
}

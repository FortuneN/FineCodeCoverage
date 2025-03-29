using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem;

namespace FineCodeCoverage.Core.MsTestPlatform.TestingPlatform
{
    [Export(typeof(ICPSTestProjectService))]
    internal class CPSTestProjectService : ICPSTestProjectService
    {
        public async Task<ConfiguredProject> GetProjectAsync(IVsHierarchy hierarchy)
        {
            if (!hierarchy.IsCapabilityMatch("TestContainer"))
            {
                return null;
            }
            var unconfiguredProject = await hierarchy.AsUnconfiguredProjectAsync();
            if (unconfiguredProject == null) return null;
            return await unconfiguredProject.GetSuggestedConfiguredProjectAsync();
        }
    }
}

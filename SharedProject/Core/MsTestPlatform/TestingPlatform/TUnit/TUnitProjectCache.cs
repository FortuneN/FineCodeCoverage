using Microsoft.VisualStudio.Shell.Interop;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FineCodeCoverage.Core.MsTestPlatform.TestingPlatform
{
    [Export(typeof(ITUnitProjectCache))]
    internal class TUnitProjectCache : ITUnitProjectCache
    {
        private Dictionary<IVsHierarchy, ITUnitProject> projectLookup;
        public void Add(ITUnitProject tUnitProject)
        {
            projectLookup.Add(tUnitProject.Hierarchy, tUnitProject);
        }

        public void Clear()
        {
            foreach(var tUnitproject in projectLookup.Values)
            {
                tUnitproject.Dispose();
            }
            projectLookup = null;
        }

        public async Task<List<ITUnitProject>> GetTUnitProjectsAsync(CancellationToken cancellationToken)
        {
            var tUnitProjects = new List<ITUnitProject>();
            foreach (var project in projectLookup.Values)
            {
                await project.UpdateStateAsync(cancellationToken);
                if (project.IsTUnit)
                {
                    tUnitProjects.Add(project);
                }
            }
            return tUnitProjects;

        }

        public void Initialize(List<ITUnitProject> tUnitProjects)
        {
            projectLookup = tUnitProjects.ToDictionary(p => p.Hierarchy);
        }

        public void Remove(IVsHierarchy project)
        {
            projectLookup[project].Dispose();
            projectLookup.Remove(project);
        }
    }
}

using FineCodeCoverage.Engine.Model;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;

namespace FineCodeCoverage.Engine.MsTestPlatform.CodeCoverage
{
    [Export(typeof(IShimCopier))]
    internal class ShimCopier : IShimCopier
    {
        private void CopyShim(string shimPath, string outputFolder)
        {
            string destination = Path.Combine(outputFolder, Path.GetFileName(shimPath));
            if (!File.Exists(destination))
            {
                File.Copy(shimPath, destination);
            }
        }

        private void CopyShimForNetFrameworkProjects(string shimPath, IEnumerable<ICoverageProject> coverageProjects)
        {
            var netFrameworkCoverageProjects = coverageProjects.Where(cp => !cp.IsDotNetSdkStyle());
            foreach (var netFrameworkCoverageProject in netFrameworkCoverageProjects)
            {
                CopyShim(shimPath, netFrameworkCoverageProject.ProjectOutputFolder);
            }
        }

        public void Copy(string shimPath, IEnumerable<ICoverageProject> coverageProjects)
        {
            CopyShimForNetFrameworkProjects(shimPath, coverageProjects);
        }
    }

}

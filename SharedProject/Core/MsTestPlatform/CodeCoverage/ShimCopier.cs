using FineCodeCoverage.Core.Utilities;
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
        private readonly IFileUtil fileUtil;

        [ImportingConstructor]
        public ShimCopier(IFileUtil fileUtil)
        {
            this.fileUtil = fileUtil;
        }
        private void CopyShim(string shimPath, string outputFolder)
        {
            string destination = Path.Combine(outputFolder, Path.GetFileName(shimPath));
            if (!fileUtil.Exists(destination))
            {
                fileUtil.Copy(shimPath, destination);
            }
        }

        private void CopyShim(string shimPath, IEnumerable<ICoverageProject> coverageProjects)
        {
            foreach (var coverageProject in coverageProjects)
            {
                CopyShim(shimPath, coverageProject.ProjectOutputFolder);
            }
        }

        public void Copy(string shimPath, IEnumerable<ICoverageProject> coverageProjects)
        {
            var netFrameworkCoverageProjects = coverageProjects.Where(cp => cp.IsDotNetFramework);
            CopyShim(shimPath, netFrameworkCoverageProjects);
        }
    }

}

using FineCodeCoverage.Engine.Model;
using System.Collections.Generic;

namespace FineCodeCoverage.Engine.MsTestPlatform.CodeCoverage
{
    internal interface IShimCopier
    {
        void Copy(string shimPath, List<ICoverageProject> coverageProjects);
    }
}

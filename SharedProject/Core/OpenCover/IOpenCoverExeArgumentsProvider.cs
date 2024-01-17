using FineCodeCoverage.Engine.Model;
using System.Collections.Generic;

namespace FineCodeCoverage.Engine.OpenCover
{
    internal interface IOpenCoverExeArgumentsProvider
    {
        List<string> Provide(ICoverageProject coverageProject,string msTestPlatformExePath);
    }
}

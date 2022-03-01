using System.Collections.Generic;
using FineCodeCoverage.Engine.Model;

namespace FineCodeCoverage.Engine
{
    internal interface ICoverageToolOutputFolderProvider
    {
        string Provide(List<ICoverageProject> coverageProjects);
    }
}

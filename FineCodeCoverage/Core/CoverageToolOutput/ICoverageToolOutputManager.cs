using System.Collections.Generic;
using FineCodeCoverage.Engine.Model;

namespace FineCodeCoverage.Engine
{
    internal interface ICoverageToolOutputManager
    {
        void SetProjectCoverageOutputFolder(List<ICoverageProject> coverageProjects);
        string GetReportOutputFolder();
    }
}

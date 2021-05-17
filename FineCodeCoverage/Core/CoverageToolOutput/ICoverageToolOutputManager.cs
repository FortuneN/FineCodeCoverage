using System.Collections.Generic;
using FineCodeCoverage.Engine.Model;

namespace FineCodeCoverage.Engine
{
    internal interface ICoverageToolOutputManager
    {
        void SetProjectCoverageOutputFolder(List<ICoverageProject> coverageProjects);
        void OutputReports(string unifiedHtml, string processedReport, string unifiedXml);
    }
}

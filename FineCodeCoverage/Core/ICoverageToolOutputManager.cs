using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FineCodeCoverage.Engine.Model;

namespace FineCodeCoverage.Engine
{
    internal interface ICoverageToolOutputManager
    {
        void SetProjectCoverageOutputFolder(List<ICoverageProject> coverageProjects);
        void SetReportOutput(string unifiedHtml, string processedReport, string unifiedXml);
    }
}

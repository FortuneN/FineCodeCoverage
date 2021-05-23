using System.Collections.Generic;
using Palmmedia.ReportGenerator.Core.Parser.Analysis;
using Palmmedia.ReportGenerator.Core.Reporting.Builders;

namespace ReportGeneratorPlugins
{
    public class FccDarkReportBuilder : HtmlInlineAzurePipelinesDarkReportBuilder
    {
        public static string REPORT_TYPE = "FccDark";

        public override string ReportType => REPORT_TYPE;

        public override void CreateClassReport(Class @class, IEnumerable<FileAnalysis> fileAnalyses)
        {
            // ignore
        }
    }
}

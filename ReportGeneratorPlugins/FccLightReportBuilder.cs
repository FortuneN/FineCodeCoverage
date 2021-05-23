using Palmmedia.ReportGenerator.Core.Parser.Analysis;
using Palmmedia.ReportGenerator.Core.Reporting.Builders;
using System.Collections.Generic;

namespace ReportGeneratorPlugins
{
    public class FccLightReportBuilder : HtmlInlineAzurePipelinesReportBuilder
    {
        public static string REPORT_TYPE = "FccLight";

        public override string ReportType => REPORT_TYPE;

        public override void CreateClassReport(Class @class, IEnumerable<FileAnalysis> fileAnalyses)
        {
            // ignore
        }
    }
}

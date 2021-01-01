using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FineCodeCoverage.Core.ReportGenerator
{
	public interface IReportGeneratorService
	{
		public void Initialize();

		public Version GetReportGeneratorVersion();

		public void UpdateReportGenerator();

		public void InstallReportGenerator();

		public Task<(string UnifiedHtmlFile, string UnifiedXmlFile)> RunReportGeneratorAsync(IEnumerable<string> coverOutputFiles, bool darkMode);

		public Task<string> ProcessUnifiedHtmlFileAsync(string htmlFile, bool darkMode);
	}
}

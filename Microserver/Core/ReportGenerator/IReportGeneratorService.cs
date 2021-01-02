using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FineCodeCoverage.Core.ReportGenerator
{
	public interface IReportGeneratorService
	{
		public void Initialize();

		public Version GetVersion();

		public void UpdateVersion();

		public void Install();

		public Task<(string UnifiedHtmlFile, string UnifiedXmlFile)> RunAsync(IEnumerable<string> coverOutputFiles, bool darkMode);

		public Task<string> ProcessUnifiedHtmlFileAsync(string htmlFile, bool darkMode);
	}
}

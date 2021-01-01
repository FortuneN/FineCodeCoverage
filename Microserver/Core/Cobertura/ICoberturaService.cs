using System.Collections.Generic;
using System.Threading.Tasks;
using FineCodeCoverage.Core.Model;

namespace FineCodeCoverage.Core.Cobertura
{
	public interface ICoberturaService
	{
		public List<CoverageReport> LoadReportFiles(IEnumerable<string> inputFilePaths);

		public CoverageReport LoadReportFile(string inputFilePath);

		public Task CoverageXmlFileToJsonFileAsync(string xmlFile, string jsonFile, bool formattedJson = false);

		public string CoverageXmlTextToJsonText(string xmlText, bool formattedJson = false);

		public CoverageReport ProcessCoberturaXmlFile(string xmlFilePath, out List<CoverageLine> coverageLines);
	}
}
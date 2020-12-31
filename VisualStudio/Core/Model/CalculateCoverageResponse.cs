using System.Collections.Generic;

namespace FineCodeCoverage.Core.Model
{
	public class CalculateCoverageResponse
	{
		public string HtmlContent { get; set; } = string.Empty;

		public List<CoverageLine> CoverageLines { get; set; } = new List<CoverageLine>();

		public IEnumerable<CoverageLine> GetLines(string filePath, int startLineNumber, int endLineNumber)
		{
			filePath?.ToString();
			startLineNumber.ToString();
			endLineNumber.ToString();
			return new CoverageLine[0];
		}

		public string[] GetSourceFiles(string assemblyName, string qualifiedClassName)
		{
			assemblyName?.ToString();
			qualifiedClassName?.ToString();
			return new string[0];
		}
	}
}

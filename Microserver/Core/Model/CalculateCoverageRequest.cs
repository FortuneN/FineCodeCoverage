using System.Collections.Generic;

namespace FineCodeCoverage.Core.Model
{
	public class CalculateCoverageRequest
	{
		public bool DarkMode { get; set; }
		public string Source { get; set; }
		public string TestDllFile { get; set; }
		public AppOptions Settings { get; set; }
		public IEnumerable<CoverageProject> Projects { get; set; }
	}
}

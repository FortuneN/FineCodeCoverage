using FineCodeCoverage.Options;
using System.Collections.Generic;

namespace FineCodeCoverage.Core.Model
{
	public class CalculateCoverageRequest
	{
		public List<CoverageProject> Projects { get; set; }
		public bool DarkMode { get; set; }
		public AppOptions Settings { get; set; }
		public string Source { get; set; }
	}
}

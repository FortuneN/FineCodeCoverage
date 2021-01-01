using System;
using System.Collections.Generic;

namespace FineCodeCoverage.Core.Model
{
	public class CalculateCoverageRequest
	{
		public string RequestId { get; set; } = Guid.NewGuid().ToString();
		public bool DarkMode { get; set; }
		public string Source { get; set; }
		public string TestDllFile { get; set; }
		public CoverageProjectSettings Settings { get; set; }
		public IEnumerable<CoverageProject> Projects { get; set; }
	}
}

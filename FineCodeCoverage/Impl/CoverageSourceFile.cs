using System.Collections.Generic;

namespace FineCodeCoverage.Impl
{
	internal class CoverageSourceFile
	{
		public string FilePath { get; set; }

		public List<CoverageLine> Lines { get; } = new List<CoverageLine>();
	}
}
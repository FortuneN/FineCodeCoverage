using FineCodeCoverage.Engine.Model;
using Microsoft.VisualStudio.Text.Editor;

namespace FineCodeCoverage.Impl
{
	internal class CoverageLineGlyphTag : IGlyphTag
	{
		public CoverageLine CoverageLine { get; }

        public CoverageLineGlyphTag(CoverageLine coverageLine)
		{
			CoverageLine = coverageLine;
		}
	}
}

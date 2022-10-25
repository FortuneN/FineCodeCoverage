using FineCodeCoverage.Engine.Cobertura;
using FineCodeCoverage.Engine.Model;
using Microsoft.VisualStudio.Text.Editor;

namespace FineCodeCoverage.Impl
{
	internal class CoverageLineGlyphTag : IGlyphTag
	{
		public Line CoverageLine { get; }

        public CoverageLineGlyphTag(Line coverageLine)
		{
			CoverageLine = coverageLine;
		}
	}
}

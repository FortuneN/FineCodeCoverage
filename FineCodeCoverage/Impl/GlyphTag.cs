using FineCodeCoverage.Engine.Model;
using Microsoft.VisualStudio.Text.Editor;

namespace FineCodeCoverage.Impl
{
	internal class GlyphTag : IGlyphTag
	{
		public CoverageLine CoverageLine { get; }

		public GlyphTag(CoverageLine coverageLine)
		{
			CoverageLine = coverageLine;
		}
	}
}

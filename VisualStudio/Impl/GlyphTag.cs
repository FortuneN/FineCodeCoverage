using FineCodeCoverage.Core.Model;
using Microsoft.VisualStudio.Text.Editor;

namespace FineCodeCoverage.Impl
{
	public class GlyphTag : IGlyphTag
	{
		public CoverageLine CoverageLine { get; }

		public GlyphTag(CoverageLine coverageLine)
		{
			CoverageLine = coverageLine;
		}
	}
}

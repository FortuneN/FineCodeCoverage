using FineCodeCoverage.Engine.Cobertura;
using Microsoft.VisualStudio.Text.Editor;
using System.Windows.Media;

namespace FineCodeCoverage.Impl
{
	internal class CoverageLineGlyphTag : IGlyphTag
	{
		public Line CoverageLine { get; }
		public Color Colour { get; }

        public CoverageLineGlyphTag(Line coverageLine, Color colour)
		{
			Colour = colour;
			CoverageLine = coverageLine;
		}
	}
}

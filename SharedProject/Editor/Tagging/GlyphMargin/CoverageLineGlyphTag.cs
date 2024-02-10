using FineCodeCoverage.Engine.Model;
using Microsoft.VisualStudio.Text.Editor;
using System.Windows.Media;

namespace FineCodeCoverage.Editor.Tagging.GlyphMargin
{
	internal class CoverageLineGlyphTag : IGlyphTag
	{
		public ILine CoverageLine { get; }
		public Color Colour { get; }

        public CoverageLineGlyphTag(ILine coverageLine, Color colour)
		{
			Colour = colour;
			CoverageLine = coverageLine;
		}
	}
}

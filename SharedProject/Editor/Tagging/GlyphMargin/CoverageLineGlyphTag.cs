using Microsoft.VisualStudio.Text.Editor;
using System.Windows.Media;

namespace FineCodeCoverage.Editor.Tagging.GlyphMargin
{
	internal class CoverageLineGlyphTag : IGlyphTag
	{
		public Color Colour { get; }

        public CoverageLineGlyphTag(Color colour) => this.Colour = colour;
    }
}

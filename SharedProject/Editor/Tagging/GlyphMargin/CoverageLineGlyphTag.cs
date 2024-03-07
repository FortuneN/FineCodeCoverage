using System.Windows.Media;
using Microsoft.VisualStudio.Text.Editor;

namespace FineCodeCoverage.Editor.Tagging.GlyphMargin
{
    internal class CoverageLineGlyphTag : IGlyphTag
    {
        public Color Colour { get; }

        public CoverageLineGlyphTag(Color colour) => this.Colour = colour;
    }
}

using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;

namespace FineCodeCoverage.Editor.Tagging.GlyphMargin
{
    internal class CoverageLineGlyphFactory : IGlyphFactory
	{
        public UIElement GenerateGlyph(IWpfTextViewLine textViewLine, IGlyphTag glyphTag)
		{
			if (!(glyphTag is CoverageLineGlyphTag tag))
			{
				return null;
			}

            return new Rectangle
            {
                Fill = new SolidColorBrush(tag.Colour),
                Width = 3,
                Height = 16
            };
        }
	}
}

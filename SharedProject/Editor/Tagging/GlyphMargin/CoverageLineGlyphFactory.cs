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
            => glyphTag is CoverageLineGlyphTag tag
                ? this.GetColouredRectange(tag.Colour)
                : (UIElement)null;

        private Rectangle GetColouredRectange(Color colour) => new Rectangle
        {
            Fill = new SolidColorBrush(colour),
            Width = 3,
            Height = 16
        };
    }
}

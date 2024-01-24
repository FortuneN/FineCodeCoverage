using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using FineCodeCoverage.Core.Utilities;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;

namespace FineCodeCoverage.Impl
{
	internal class CoverageLineGlyphFactory : IGlyphFactory, IListener<CoverageColoursChangedMessage>
	{
        private ICoverageColours coverageColours;
        private CoverageType coverageType;
        private Rectangle glyph;

        public CoverageLineGlyphFactory(ICoverageColours coverageColours)
        {
            this.coverageColours = coverageColours;
        }

        public UIElement GenerateGlyph(IWpfTextViewLine textViewLine, IGlyphTag glyphTag)
		{
			if (!(glyphTag is CoverageLineGlyphTag tag))
			{
				return null;
			}

			coverageType = tag.CoverageLine.GetCoverageType();
			
			glyph = new Rectangle();
			glyph.Width = 3;
			glyph.Height = 16;

			SetGlyphColor();

			return glyph;
		}

        public void Handle(CoverageColoursChangedMessage message)
        {
			coverageColours = message.CoverageColours;
			SetGlyphColor();   
        }

        private void SetGlyphColor()
        {
			Color color = default;
			switch (coverageType)
			{
				case CoverageType.Partial:
					color = coverageColours.CoveragePartiallyTouchedArea;
					break;
				case CoverageType.NotCovered:
					color = coverageColours.CoverageNotTouchedArea;
					break;
				case CoverageType.Covered:
					color = coverageColours.CoverageTouchedArea;
					break;
			}
			var brush = new SolidColorBrush(color);
			glyph.Fill = brush;
        }
	}
}

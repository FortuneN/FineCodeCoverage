using System.Collections.Generic;
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
        private Rectangle glyph;
		private List<Rectangle> glyphs = new List<Rectangle>();

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

			var coverageType = tag.CoverageLine.GetCoverageType();
			
			var glyph = new Rectangle();
			glyph.Width = 3;
			glyph.Height = 16;
			glyph.Tag = coverageType;
			glyphs.Add(glyph);
			glyph.Unloaded += (s, e) =>
			{
                glyphs.Remove(glyph);
            };
			SetGlyphColor(glyph,coverageType);

			return glyph;
		}

        public void Handle(CoverageColoursChangedMessage message)
        {
			coverageColours = message.CoverageColours;
			glyphs.ForEach(glyph => SetGlyphColor(glyph, (CoverageType)glyph.Tag));
        }

        private void SetGlyphColor(Rectangle glyph, CoverageType coverageType)
        {
			var color = GetGlyphColor(coverageType);
			if (glyph.Fill == null )
			{
                glyph.Fill = new SolidColorBrush(color);
			}
			else
			{
				var currentBrush = (SolidColorBrush)glyph.Fill;
				if(currentBrush.Color != (color))
				{
                    currentBrush.Color = color;
				}
			}
        }

		private Color GetGlyphColor(CoverageType coverageType)
		{
            switch (coverageType)
			{
                case CoverageType.Partial:
                    return coverageColours.CoveragePartiallyTouchedArea;
                case CoverageType.NotCovered:
                    return coverageColours.CoverageNotTouchedArea;
                case CoverageType.Covered:
                    return coverageColours.CoverageTouchedArea;
            }
            return default;
        }
	}
}

using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;

namespace FineCodeCoverage.Impl
{
    internal class CoverageLineGlyphFactory : IGlyphFactory
    {
        private enum CoverageType { Covered, Partial, NotCovered }
        private ICoverageColours coverageColours;

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

            // vars

            var line = tag?.CoverageLine?.Line;
            var lineHitCount = line?.Hits ?? 0;
            var lineConditionCoverage = line?.ConditionCoverage?.Trim();

            var coverageType = CoverageType.NotCovered;

            if (lineHitCount > 0)
            {
                coverageType = CoverageType.Covered;

                if (!string.IsNullOrWhiteSpace(lineConditionCoverage) && !lineConditionCoverage.StartsWith("100"))
                {
                    coverageType = CoverageType.Partial;
                }
            }

            // result

            var result = new Rectangle();
            result.Width = 3;
            result.Height = 16;
            result.Fill = GetBrush(coverageType);
            // return

            return result;
        }

        private Brush GetBrush(CoverageType coverageType)
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
            return new SolidColorBrush(color);
        }
    }
}

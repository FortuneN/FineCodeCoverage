using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;

namespace FineCodeCoverage.Impl
{
	public class GlyphFactory : IGlyphFactory
	{
		private static readonly SolidColorBrush CustomGoldBrush = new SolidColorBrush(Color.FromRgb(255, 165, 0));

		public UIElement GenerateGlyph(IWpfTextViewLine textViewLine, IGlyphTag glyphTag)
		{
			if (!(glyphTag is GlyphTag tag))
			{
				return null;
			}

			// vars

			var line = tag?.CoverageLine?.Line;
			var lineHitCount = line?.Hits ?? 0;
			var lineConditionCoverage = line?.ConditionCoverage?.Trim();

			// brush (color)

			var brush = Brushes.Red;

			if (lineHitCount > 0)
			{
				brush = Brushes.Green;

				if (!string.IsNullOrWhiteSpace(lineConditionCoverage) && !lineConditionCoverage.StartsWith("100"))
				{
					brush = CustomGoldBrush;
				}
			}

			// result

			var result = new Rectangle();
			result.Width = 3;
			result.Height = 16;
			result.Fill = brush;

			// return

			return result;
		}
	}
}

using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Text.Formatting;
using System.Linq;

namespace FineCodeCoverage.Impl
{
	[ContentType("code")]
	[TagType(typeof(GlyphTag))]
	[Order(Before = "VsTextMarker")]
	[Export(typeof(IGlyphFactoryProvider))]
	[Name(Vsix.GlyphFactoryProviderName)]
	internal class GlyphFactoryProvider: IGlyphFactoryProvider
	{
		private static readonly SolidColorBrush CustomGoldBrush = new SolidColorBrush(Color.FromRgb(255, 165, 0));

		public IGlyphFactory GetGlyphFactory(IWpfTextView textView, IWpfTextViewMargin textViewMargin)
		{
			return new GlyphFactory(textView, textViewMargin, GetGlyph);
		}

		private UIElement GetGlyph(IWpfTextView textView, IWpfTextViewMargin textViewMargin, IWpfTextViewLine textViewLine, IGlyphTag glyphTag)
		{
			if (!(glyphTag is GlyphTag tag))
			{
				return null;
			}

			// vars
			
			var line = tag?.CoverageLine?.Line;
			var lineHitCount = line?.Hits ?? 0;
			var lineConditionCoverage = line?.ConditionCoverage;

			// brush (color)

			var brush = Brushes.Red;

			if (lineHitCount > 0)
			{
				brush = Brushes.Green;

				if (!string.IsNullOrWhiteSpace(lineConditionCoverage) && !lineConditionCoverage.Contains("100"))
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
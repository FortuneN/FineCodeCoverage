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

			// brush (color)

			var tooltip = default(string);
			var brush = tag.CoverageLine.Line.Hits > 0 ? Brushes.Green : Brushes.Red;
			
			if (brush == Brushes.Green && !string.IsNullOrWhiteSpace(tag.CoverageLine.Line.ConditionCoverage))
			{
				brush = CustomGoldBrush;
				tooltip = tag.CoverageLine.Line.ConditionCoverage;
			}

			// result

			var result = new Rectangle();
			result.Width = 3;
			result.Height = 16;
			result.Fill = brush;
			result.ToolTip = tooltip; // not showing, why?
						
			// return

			return result;
		}

		private static string Pluralize(string text, int count)
		{
			var result = $"{count} {text}";

			if (count != 1)
			{
				if (text.EndsWith("ch"))
				{
					result += "es";
				}
				else
				{
					result += "s";
				}
			}

			return result;
		}
	}
}
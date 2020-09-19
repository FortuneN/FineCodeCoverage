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
		private static SolidColorBrush CustomGoldBrush = new SolidColorBrush(Color.FromRgb(255, 165, 0));

		public IGlyphFactory GetGlyphFactory(IWpfTextView textView, IWpfTextViewMargin textViewMargin)
		{
			return new GlyphFactory(textView, textViewMargin, GetGlyph);
		}

		private UIElement GetGlyph(IWpfTextView textView, IWpfTextViewMargin textViewMargin, IWpfTextViewLine textViewLine, IGlyphTag glyphTag)
		{
			var tag = (GlyphTag)glyphTag;

			// brush (color)

			var brush = tag.CoverageLine.HitCount > 0 ? Brushes.Green : Brushes.Red;

			if (brush == Brushes.Green)
			{
				if (tag.CoverageLine.LineBranches.Count > 1)
				{
					if (tag.CoverageLine.LineBranches.Any(x => x.Hits == 0))
					{
						brush = CustomGoldBrush; // partial coverage
					}
				}
			}

			// return

			return new Rectangle
			{
				Width = 2,
				Height = 16,
				Fill = brush
			};
		}
	}
}
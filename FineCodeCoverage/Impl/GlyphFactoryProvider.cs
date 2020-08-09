using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Text.Formatting;

namespace FineCodeCoverage.Impl
{
	[ContentType("code")]
	[TagType(typeof(GlyphTag))]
	[Order(Before = "VsTextMarker")]
	[Export(typeof(IGlyphFactoryProvider))]
	[Name(Vsix.GlyphFactoryProviderName)]
	internal class GlyphFactoryProvider: IGlyphFactoryProvider
	{
		public IGlyphFactory GetGlyphFactory(IWpfTextView textView, IWpfTextViewMargin textViewMargin)
		{
			return new GlyphFactory(textView, textViewMargin, GetGlyph);
		}

		private UIElement GetGlyph(IWpfTextView textView, IWpfTextViewMargin textViewMargin, IWpfTextViewLine textViewLine, IGlyphTag glyphTag)
		{
			var tag = (GlyphTag)glyphTag;

			return new Rectangle
			{
				Width = 2,
				Height = 16,
				Fill = tag.IsCovered ? Brushes.Green : Brushes.Red
			};
		}
	}
}
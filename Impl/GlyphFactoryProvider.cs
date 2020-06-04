using System.Windows;
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
	[Name(ProjectMetaData.GlyphFactoryProviderName)]
	internal class GlyphFactoryProvider: IGlyphFactoryProvider
	{
		public IGlyphFactory GetGlyphFactory(IWpfTextView textView, IWpfTextViewMargin textViewMargin)
		{
			return new GlyphFactory(textView, textViewMargin, GetGlyph);
		}

		private UIElement GetGlyph(IWpfTextView textView, IWpfTextViewMargin textViewMargin, IWpfTextViewLine _textViewLine, IGlyphTag glyphTag)
		{
			return (UIElement)glyphTag;
		}
	}
}
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;

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
			return new GlyphFactory();
		}
	}
}
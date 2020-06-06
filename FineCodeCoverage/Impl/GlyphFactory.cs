using System;
using System.Windows;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;

namespace FineCodeCoverage.Impl
{
	internal class GlyphFactory : IGlyphFactory
	{
		private readonly IWpfTextView _wpfTextView;
		private readonly IWpfTextViewMargin _wpfTextViewMargin;
		private readonly Func<IWpfTextView, IWpfTextViewMargin, IWpfTextViewLine, IGlyphTag, UIElement> _generateGlyphFunc;
		
		public GlyphFactory(IWpfTextView wpfTextView, IWpfTextViewMargin wpfTextViewMargin, Func<IWpfTextView, IWpfTextViewMargin, IWpfTextViewLine, IGlyphTag, UIElement> generateGlyphFunc)
		{
			_wpfTextView = wpfTextView;
			_wpfTextViewMargin = wpfTextViewMargin;
			_generateGlyphFunc = generateGlyphFunc;
		}

		public UIElement GenerateGlyph(IWpfTextViewLine wpfTextViewLine, IGlyphTag glyphTag)
		{
			return _generateGlyphFunc(_wpfTextView, _wpfTextViewMargin, wpfTextViewLine, glyphTag);
		}
	}
}

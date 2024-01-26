using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using FineCodeCoverage.Core.Utilities;
using OrderAttribute = Microsoft.VisualStudio.Utilities.OrderAttribute;
using System;

namespace FineCodeCoverage.Impl
{
	[ContentType("code")]
	[TagType(typeof(CoverageLineGlyphTag))]
	[Order(Before = "VsTextMarker")]
	[Name(Vsix.GlyphFactoryProviderName)]
	[Export(typeof(IGlyphFactoryProvider))]
	internal class CoverageLineGlyphFactoryProvider: IGlyphFactoryProvider
	{
        private readonly IMVVMGlyphFactory mvvmGlyphFactory;

        [ImportingConstructor]
		public CoverageLineGlyphFactoryProvider(
            IMVVMGlyphFactory mvvmGlyphFactory
		)
		{
            this.mvvmGlyphFactory = mvvmGlyphFactory;
        }

        public IGlyphFactory GetGlyphFactory(IWpfTextView textView, IWpfTextViewMargin textViewMargin)
		{
			return  new CoverageLineGlyphFactory(mvvmGlyphFactory);
        }

    }
}
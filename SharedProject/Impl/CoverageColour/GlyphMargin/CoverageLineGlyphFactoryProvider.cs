using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using FineCodeCoverage.Core.Utilities;
using OrderAttribute = Microsoft.VisualStudio.Utilities.OrderAttribute;

namespace FineCodeCoverage.Impl
{
	[ContentType("code")]
	[TagType(typeof(CoverageLineGlyphTag))]
	[Order(Before = "VsTextMarker")]
	[Name(Vsix.GlyphFactoryProviderName)]
	[Export(typeof(IGlyphFactoryProvider))]
	internal class CoverageLineGlyphFactoryProvider: IGlyphFactoryProvider
	{
        private readonly ICoverageColoursProvider coverageColoursProvider;
        private readonly IEventAggregator eventAggregator;

        [ImportingConstructor]
		public CoverageLineGlyphFactoryProvider(
			ICoverageColoursProvider coverageColoursProvider,
			IEventAggregator eventAggregator
		)
		{
            this.coverageColoursProvider = coverageColoursProvider;
            this.eventAggregator = eventAggregator;
        }
		public IGlyphFactory GetGlyphFactory(IWpfTextView textView, IWpfTextViewMargin textViewMargin)
		{
			var glyphFactory =  new CoverageLineGlyphFactory(coverageColoursProvider.GetCoverageColours());
			eventAggregator.AddListener(glyphFactory,false);
			return glyphFactory;
		}
	}
}
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text.Tagging;
using FineCodeCoverage.Core.Utilities;
using Microsoft.VisualStudio.Text.Editor;

namespace FineCodeCoverage.Impl
{
    [ContentType("code")]
    [TagType(typeof(CoverageLineGlyphTag))]
    [Name(Vsix.TaggerProviderName)]
	[Export(typeof(IViewTaggerProvider))]
	internal class CoverageLineGlyphTaggerProvider : IViewTaggerProvider, ILineSpanTagger<CoverageLineGlyphTag>
    {
        private readonly ICoverageTaggerProvider<CoverageLineGlyphTag> coverageTaggerProvider;
        private readonly IEventAggregator eventAggregator;
        private readonly ICoverageColoursProvider coverageColoursProvider;

        [ImportingConstructor]
        public CoverageLineGlyphTaggerProvider(
            IEventAggregator eventAggregator,
            ICoverageColoursProvider coverageColoursProvider,
            ICoverageTaggerProviderFactory coverageTaggerProviderFactory
        )
        {
            coverageTaggerProvider = coverageTaggerProviderFactory.Create<CoverageLineGlyphTag,GlyphFilter>(this);
            this.eventAggregator = eventAggregator;
            this.coverageColoursProvider = coverageColoursProvider;
        }

        public ITagger<T> CreateTagger<T>(ITextView textView,ITextBuffer buffer) where T : ITag
        {
            var coverageTagger =  coverageTaggerProvider.CreateTagger(textView,buffer);
            if (coverageTagger == null) return null;
            return new CoverageLineGlyphTagger(eventAggregator, coverageTagger) as ITagger<T>;
        }

        public TagSpan<CoverageLineGlyphTag> GetTagSpan(ILineSpan lineSpan)
        {
            var coverageLine = lineSpan.Line;
            var coverageColours = coverageColoursProvider.GetCoverageColours();
            var colour = coverageColours.GetColour(coverageLine.CoverageType).Background;
            return new TagSpan<CoverageLineGlyphTag>(lineSpan.Span, new CoverageLineGlyphTag(coverageLine, colour));
        }
    }

    
}
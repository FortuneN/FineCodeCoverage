using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text.Tagging;
using FineCodeCoverage.Core.Utilities;
using Microsoft.VisualStudio.Text.Editor;
using FineCodeCoverage.Editor.Tagging.Base;
using FineCodeCoverage.Editor.Management;
using FineCodeCoverage.Editor.DynamicCoverage;
using System.Windows.Media;

namespace FineCodeCoverage.Editor.Tagging.GlyphMargin
{
    [ContentType(SupportedContentTypeLanguages.CSharp)]
    [ContentType(SupportedContentTypeLanguages.VisualBasic)]
    [ContentType(SupportedContentTypeLanguages.CPP)]
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
            this.coverageTaggerProvider = coverageTaggerProviderFactory.Create<CoverageLineGlyphTag,GlyphFilter>(this);
            this.eventAggregator = eventAggregator;
            this.coverageColoursProvider = coverageColoursProvider;
        }

        public ITagger<T> CreateTagger<T>(ITextView textView,ITextBuffer buffer) where T : ITag
        {
            ICoverageTagger<CoverageLineGlyphTag> coverageTagger = this.coverageTaggerProvider.CreateTagger(textView,buffer);
            return coverageTagger == null ? null : new CoverageLineGlyphTagger(this.eventAggregator, coverageTagger) as ITagger<T>;
        }

        public TagSpan<CoverageLineGlyphTag> GetTagSpan(ILineSpan lineSpan)
        {
            IDynamicLine coverageLine = lineSpan.Line;
            ICoverageColours coverageColours = this.coverageColoursProvider.GetCoverageColours();
            Color colour = coverageColours.GetColour(coverageLine.CoverageType).Background;
            return new TagSpan<CoverageLineGlyphTag>(lineSpan.Span, new CoverageLineGlyphTag(colour));
        }
    }
}
using System.ComponentModel.Composition;
using System.Windows.Media;
using FineCodeCoverage.Core.Utilities;
using FineCodeCoverage.Editor.DynamicCoverage;
using FineCodeCoverage.Editor.DynamicCoverage.ContentTypes;
using FineCodeCoverage.Editor.DynamicCoverage.ContentTypes.Blazor;
using FineCodeCoverage.Editor.DynamicCoverage.ContentTypes.Roslyn;
using FineCodeCoverage.Editor.Management;
using FineCodeCoverage.Editor.Tagging.Base;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace FineCodeCoverage.Editor.Tagging.GlyphMargin
{
    [ContentType(CSharpCoverageContentType.ContentType)]
    [ContentType(VBCoverageContentType.ContentType)]
    [ContentType(CPPCoverageContentType.ContentType)]
    [ContentType(BlazorCoverageContentType.ContentType)]
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
            this.coverageTaggerProvider = coverageTaggerProviderFactory.Create<CoverageLineGlyphTag, GlyphFilter>(this);
            this.eventAggregator = eventAggregator;
            this.coverageColoursProvider = coverageColoursProvider;
        }

        public ITagger<T> CreateTagger<T>(ITextView textView, ITextBuffer buffer) where T : ITag
        {
            ICoverageTagger<CoverageLineGlyphTag> coverageTagger = this.coverageTaggerProvider.CreateTagger(textView, buffer);
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
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text.Tagging;
using FineCodeCoverage.Core.Utilities;
using FineCodeCoverage.Engine.Model;
using FineCodeCoverage.Options;

namespace FineCodeCoverage.Impl
{
    [ContentType("code")]
    [TagType(typeof(CoverageLineGlyphTag))]
    [Name(Vsix.TaggerProviderName)]
	[Export(typeof(ITaggerProvider))]
	internal class CoverageLineGlyphTaggerProvider : CoverageLineTaggerProviderBase<CoverageLineGlyphTagger, CoverageLineGlyphTag, GlyphTagFilter>
    {
        private readonly ICoverageColoursProvider coverageColoursProvider;

        [ImportingConstructor]
        public CoverageLineGlyphTaggerProvider(
            IEventAggregator eventAggregator,
            ICoverageColoursProvider coverageColoursProvider,
            IAppOptionsProvider appOptionsProvider,
            ILineSpanLogic lineSpanLogic
        ) : base(eventAggregator,appOptionsProvider, lineSpanLogic)
        {
            this.coverageColoursProvider = coverageColoursProvider;
        }

        protected override CoverageLineGlyphTagger CreateCoverageTagger(
            ITextBuffer textBuffer, IFileLineCoverage lastCoverageLines, IEventAggregator eventAggregator, GlyphTagFilter coverageTypeFilter, ILineSpanLogic lineSpanLogic)
        {
            return new CoverageLineGlyphTagger(textBuffer, lastCoverageLines,eventAggregator,coverageColoursProvider.GetCoverageColours(), coverageTypeFilter, lineSpanLogic);
        }

    }
}